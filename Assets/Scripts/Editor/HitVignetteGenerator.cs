#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// 피격 시 화면 테두리 붉은 섬광 오버레이를 in-place 생성/리스킨. 실행: Tools/UI/Build Hit Vignette
//
// 배치: HudRoot(SafeArea 인셋) 가 아니라 Canvas 직속이다 — 노치·라운드 코너까지 덮어야 "화면 테두리"로 읽힌다.
// 순서: HudRoot 와 같은 Hud 레이어라 sortingOrder 가 같고, 형제 마지막에 두어 HUD 위 / Modal·Pause 아래에 그린다.
// raycastTarget 은 반드시 꺼둔다 — 풀스크린이라 켜져 있으면 가상 조이스틱 드래그를 통째로 먹는다.
// in-place · idempotent(이름 재사용). BossHudGenerator 패턴 계승.
public static class HitVignetteGenerator
{
    const int Band = 35; // 테두리 두께(px). 9-slice border 라 화면 비율이 바뀌어도 사방이 같은 두께

    [MenuItem("Tools/UI/Build Hit Vignette")]
    public static void BuildHitVignette()
    {
        var canvas = UIGenScene.ResolveMainCanvas("HitVignetteGenerator");
        if (canvas == null) return;
        var canvasRT = (RectTransform)canvas.transform;

        var root = canvasRT.Find("HitVignette") as RectTransform;
        if (root == null)
        {
            var go = new GameObject("HitVignette", typeof(RectTransform), typeof(Image));
            root = (RectTransform)go.transform;
            root.SetParent(canvasRT, false);
        }
        if (root.GetComponent<Image>() == null) root.gameObject.AddComponent<Image>();

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        var ps = root.GetComponent<UIProceduralSprite>();
        if (ps == null) ps = root.gameObject.AddComponent<UIProceduralSprite>();
        ps.ConfigureEdgeGlow(Band, UITheme.HitFlash);

        var img = root.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f); // 알파는 HitVignette 가 소유 — 저장 상태는 투명
        img.raycastTarget = false;

        if (root.GetComponent<HitVignette>() == null) root.gameObject.AddComponent<HitVignette>();
        UILayerAssign.AssignLayer(root.gameObject, UILayer.Hud);

        UIGenScene.PurgeStrays("HitVignetteGenerator", root);

        EditorUtility.SetDirty(root.gameObject);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log($"[HitVignetteGenerator] 피격 비네트 생성 완료 (Canvas 직속, 테두리 {Band}px).");
    }
}
#endif
