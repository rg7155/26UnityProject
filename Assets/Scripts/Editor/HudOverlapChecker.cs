#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// HudRoot 안의 HUD 블록들이 서로 겹치는지 월드 Rect 로 검사. 실행: Tools/UI/Check HUD Overlap
//
// 오탐 회피: Selectable(버튼·슬라이더)이나 보이는 Graphic 을 만나면 거기서 탐색을 멈추고 그 Rect 만
// 후보로 삼는다. 슬라이더 트랙 위의 Fill, 버튼 위의 라벨처럼 "겹치는 게 정상"인 위젯 내부 레이어링과
// 투명 풀스크린 컨테이너(HudRoot/StatusStrip/BossHud)가 자동으로 후보에서 빠진다.
// 후보끼리는 조상-자손 관계가 아니므로 부모-자식 오탐도 구조적으로 생기지 않는다.
public static class HudOverlapChecker
{
    [MenuItem("Tools/UI/Check HUD Overlap")]
    public static void CheckHudOverlap()
    {
        var canvas = UIGenScene.ResolveMainCanvas("HudOverlapChecker");
        if (canvas == null) return;

        // 이름만으로 찾으면 잘못된 캔버스의 유령 HudRoot 를 검사해 정작 화면의 겹침을 놓친다.
        var hudRoot = canvas.transform.Find("HudRoot") as RectTransform;
        if (hudRoot == null)
        {
            Debug.LogError($"[HudOverlapChecker] '{canvas.name}' 밑에서 HudRoot 를 찾지 못했습니다. GameScene 을 연 상태로 Tools/UI/Build HUD 를 먼저 실행하세요.");
            return;
        }

        WarnStrayHud(canvas);

        var blocks = new List<RectTransform>();
        Collect(hudRoot, blocks);

        int hits = 0;
        for (int i = 0; i < blocks.Count; i++)
            for (int j = i + 1; j < blocks.Count; j++)
                if (WorldRect(blocks[i]).Overlaps(WorldRect(blocks[j])))
                {
                    Debug.LogWarning($"[HudOverlapChecker] 겹침: {Path(blocks[i])} ↔ {Path(blocks[j])}", blocks[i]);
                    hits++;
                }

        if (hits == 0) Debug.Log($"[HudOverlapChecker] 겹침 없음 — HUD 블록 {blocks.Count}개 검사 통과.");
    }

    // 메인 캔버스 밖에 HUD 컴포넌트가 있으면 중복 생성 신호 — 겹침 검사보다 먼저 알린다.
    static readonly System.Type[] HudTypes =
    {
        typeof(HudStats), typeof(RewindButtonUI), typeof(BossHpBar), typeof(BossWarningUI),
        typeof(HpBar), typeof(ExpBar), typeof(VirtualJoystick),
    };

    static void WarnStrayHud(Canvas main)
    {
        foreach (var type in HudTypes)
            foreach (var o in Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var comp = o as Component;
                if (comp == null) continue;
                var owner = comp.GetComponentInParent<Canvas>(true);
                if (owner != null && owner.rootCanvas == main) continue;
                Debug.LogWarning($"[HudOverlapChecker] 메인 캔버스('{main.name}') 밖의 {type.Name}: {UIGenScene.Path(comp.transform)} — 중복 HUD 의심. Tools/UI 생성기를 다시 실행하세요.", comp);
            }
    }

    static void Collect(Transform parent, List<RectTransform> into)
    {
        foreach (Transform child in parent)
        {
            var rt = child as RectTransform;
            if (rt == null) continue;
            if (rt.GetComponent<Selectable>() != null || IsVisible(rt)) into.Add(rt);
            else Collect(rt, into);
        }
    }

    static bool IsVisible(RectTransform rt)
    {
        var g = rt.GetComponent<Graphic>();
        return g != null && g.enabled && g.color.a > 0f;
    }

    static Rect WorldRect(RectTransform rt)
    {
        var c = new Vector3[4];
        rt.GetWorldCorners(c);
        return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
    }

    static string Path(RectTransform rt) => rt.parent != null ? $"{rt.parent.name}/{rt.name}" : rt.name;
}
#endif
