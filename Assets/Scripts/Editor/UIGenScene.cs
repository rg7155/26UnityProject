#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

// 씬 UI 생성기 공통 — 메인 캔버스 확정 + 잘못된 위치에 생긴 중복 생성물 정리.
//
// FindFirstObjectByType<Canvas> 는 반환 순서를 보장하지 않는다. 이 프로젝트는 PauseScreenGenerator 가
// 전용 루트 캔버스(@PauseCanvas)를 따로 만들기 때문에, 그 API 로 캔버스를 잡으면 실행 순서에 따라
// HUD 한 벌이 통째로 엉뚱한 캔버스 밑에 생긴다(두 캔버스의 Reference 해상도가 달라 두 배율로 겹쳐 보임).
// 그래서 후보를 전부 모아 규칙으로 확정하고, 확정하지 못하면 조용히 아무거나 고르지 않고 중단한다.
public static class UIGenScene
{
    public const string MainCanvasName = "Canvas"; // GameScene/Title/Result 공통 — 메인 루트 캔버스 이름

    // 메인 루트 캔버스를 확정한다. 후보를 하나로 좁히지 못하면 null(호출부는 즉시 중단).
    // 규칙(순서 비의존 — 씬의 캔버스 "집합"만 보고 판정한다):
    //   1) 모든 Canvas 를 rootCanvas 로 정규화해 중복 제거(UILayerCanvas 중첩 Canvas 흡수)
    //   2) '@' 로 시작하는 루트는 후보 제외 — 생성기 전용 오버레이 캔버스(@PauseCanvas)
    //   3) 남은 후보가 1개면 그것, 여러 개면 이름이 MainCanvasName 인 유일한 하나
    //   4) 그래도 못 정하면 LogError
    public static Canvas ResolveMainCanvas(string tag)
    {
        var roots = new List<Canvas>();
        foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var root = c.rootCanvas;
            if (root == null || roots.Contains(root)) continue;
            if (root.name.StartsWith("@")) continue;
            roots.Add(root);
        }

        if (roots.Count == 0)
        {
            Debug.LogError($"[{tag}] 메인 Canvas 를 찾지 못했습니다. 대상 씬을 연 상태로 실행하세요(전용 '@' 캔버스는 후보에서 제외됨).");
            return null;
        }
        if (roots.Count == 1) return roots[0];

        Canvas named = null;
        int namedCount = 0;
        foreach (var c in roots)
            if (c.name == MainCanvasName) { named = c; namedCount++; }
        if (namedCount == 1) return named;

        var names = new List<string>();
        foreach (var c in roots) names.Add(c.name);
        Debug.LogError($"[{tag}] 메인 Canvas 를 확정할 수 없습니다(후보 {roots.Count}개: {string.Join(", ", names)}). " +
                       $"메인 캔버스 이름을 '{MainCanvasName}' 로 맞추거나, 전용 오버레이 캔버스에 '@' 접두사를 붙이세요.");
        return null;
    }

    // canonical 과 같은 이름인데 다른 위치에 있는 생성물을 제거한다(잘못된 캔버스에 생긴 유령 UI).
    // 반드시 canonical 을 다 만들고 "보존 대상 재부모화(대피)"까지 끝난 뒤에 호출할 것.
    // 파괴 전 안전장치: 그 서브트리에만 존재하는 로직 컴포넌트가 있으면 지우지 않고 경고만 한다
    //   (KB nested-canvas-generator-duplication — 파괴는 서브트리 전체를 지워 참조를 날린다).
    public static void PurgeStrays(string tag, Transform canonical)
    {
        if (canonical == null) return;

        var strays = new List<Transform>();
        foreach (var rt in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rt == null || rt == canonical || rt.name != canonical.name) continue;
            if (canonical.IsChildOf(rt)) continue; // canonical 의 조상이면 지우는 순간 canonical 도 사라진다
            strays.Add(rt);
        }

        foreach (var stray in strays)
        {
            if (stray == null) continue; // 앞서 지운 stray 의 자식이었다면 이미 파괴됨
            if (HoldsSoleLogic(tag, stray)) continue;
            Debug.LogWarning($"[{tag}] 잘못된 위치의 중복 '{stray.name}' 제거: {Path(stray)}", canonical);
            UnityEngine.Object.DestroyImmediate(stray.gameObject);
        }
    }

    // 이 서브트리 밖에 같은 타입 인스턴스가 하나도 없는 컴포넌트가 있으면 true(= 씬의 유일본이라 파괴 금지).
    static bool HoldsSoleLogic(string tag, Transform stray)
    {
        foreach (var mb in stray.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            Type type = mb.GetType();
            bool hasOutside = false;
            foreach (var other in UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var comp = other as Component;
                if (comp == null || comp == mb || comp.transform.IsChildOf(stray)) continue;
                hasOutside = true;
                break;
            }
            if (!hasOutside)
            {
                Debug.LogWarning($"[{tag}] '{Path(stray)}' 는 씬에 하나뿐인 {type.Name} 를 갖고 있어 제거하지 않았습니다. " +
                                 "올바른 캔버스 쪽 UI 를 먼저 생성한 뒤 다시 실행하세요.", stray);
                return true;
            }
        }
        return false;
    }

    public static string Path(Transform t)
    {
        var parts = new List<string>();
        for (var cur = t; cur != null; cur = cur.parent) parts.Add(cur.name);
        parts.Reverse();
        return string.Join("/", parts);
    }
}
#endif
