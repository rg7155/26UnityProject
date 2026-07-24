#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// 메뉴 화면 최하단에 단색 다크 풀스크린 배경을 깐다. 실행: Tools/UI/Build Menu Background
// Canvas 최하단(sibling 0)에 MenuBackground(Image, 단색) + 카메라 클리어색을 AppBg 로 통일.
// 단색이라 절차 스프라이트 불필요. idempotent(이름으로 재사용). in-place.
public static class MenuBackgroundGenerator
{
    [MenuItem("Tools/UI/Build Menu Background")]
    public static void BuildMenuBackground()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[MenuBackgroundGenerator] 씬에서 Canvas 를 찾지 못했습니다. 대상 씬을 연 상태로 실행하세요.");
            return;
        }

        canvas = canvas.rootCanvas;   // UILayerCanvas 중첩 Canvas 오탐 방지 — 항상 루트 기준
        var canvasRT = (RectTransform)canvas.transform;
        var bgRT = canvasRT.Find("MenuBackground") as RectTransform;
        if (bgRT == null)
        {
            var go = new GameObject("MenuBackground", typeof(RectTransform), typeof(Image));
            bgRT = (RectTransform)go.transform;
            bgRT.SetParent(canvasRT, false);
        }
        var bgImg = bgRT.GetComponent<Image>();
        if (bgImg == null) bgImg = bgRT.gameObject.AddComponent<Image>();
        bgImg.sprite = null;
        bgImg.type = Image.Type.Simple;
        bgImg.color = UITheme.AppBg;
        bgImg.raycastTarget = false;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgRT.SetAsFirstSibling(); // 항상 최하단(다른 UI 뒤). ShopPanel/ShopDim 은 위 계층이라 자연히 그 앞에 그려짐.

        UILayerAssign.AssignLayer(bgRT.gameObject, UILayer.Background);

        // 카메라 빈틈 방어 — 클리어색도 AppBg 로
        var cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UITheme.AppBg;
            EditorUtility.SetDirty(cam);
        }

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = bgRT.gameObject;
        Debug.Log("[MenuBackgroundGenerator] 메뉴 배경 생성 완료(Canvas 최하단 + 카메라 클리어색).");
    }
}
#endif
