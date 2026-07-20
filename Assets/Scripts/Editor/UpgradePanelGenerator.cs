#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 레벨업 업그레이드 패널을 세로 스택 모달로 in-place 리스킨. 실행: Tools/UI/Build Upgrade Panel
//
// 전략(상점과 동일): 파괴/재생성 금지. GameScene 의 UI_UpgradePanel(전체화면·토글 대상)을 찾아
//   1) 패널 자체 Image 를 백드롭 딤으로,
//   2) 중앙 라운드 팝업(UpgradePopup, 화면 폭 86%)을 만들고 헤더 + 세로 카드 컨테이너 배치,
//   3) 기존 _buttons[3] 를 카드 컨테이너로 재부모화(같은 오브젝트 유지 → 배열 참조 보존)하고 리스킨.
// 절차 스프라이트는 전부 UIProceduralSprite 컴포넌트로 위임(프리팹/씬에 sprite 직접 굽지 않음 → 흰 박스 방지).
// 배열 참조(_buttons/_nameTexts/_descTexts)는 이미 배선돼 있으므로 보존하고 재부모화·리스타일만 한다.
public static class UpgradePanelGenerator
{
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";

    // ── 레이아웃 튜닝값(스크린샷 피드백으로 조정) ──
    const float PopupHeight = 680f;
    const float PopupWidthPct = 0.86f;   // 화면 폭 대비(세로 화면 좌우 안 잘리게)
    const float HeaderH = 72f;
    const float CardH = 140f;

    [MenuItem("Tools/UI/Build Upgrade Panel")]
    public static void BuildUpgradePanel()
    {
        var panel = Object.FindFirstObjectByType<UI_UpgradePanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogError("[UpgradePanelGenerator] 씬에서 UI_UpgradePanel 을 찾지 못했습니다. GameScene 을 연 상태로 실행하세요.");
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[UpgradePanelGenerator] 폰트를 찾지 못했습니다: {FontPath} (기존 폰트 유지).");

        var panelRT = (RectTransform)panel.transform;

        // ── 1) 백드롭 딤: 패널 자체 Image(전체화면·토글 대상)를 딤으로 ──
        var dim = panel.GetComponent<Image>();
        if (dim == null) dim = panel.gameObject.AddComponent<Image>();
        dim.sprite = null;
        dim.type = Image.Type.Simple;
        dim.color = UITheme.Backdrop;
        dim.raycastTarget = true; // 모달 — 뒤 클릭 차단

        // ── 2) 팝업 본체(중앙 라운드 패널, 폭 86%) ──
        var popup = FindOrCreateChild(panelRT, "UpgradePopup");
        ApplyProcedural(popup.gameObject, true, UITheme.RadLg, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        popup.GetComponent<Image>().color = Color.white;
        popup.GetComponent<Image>().raycastTarget = true;
        float sideMargin = (1f - PopupWidthPct) * 0.5f;
        popup.anchorMin = new Vector2(sideMargin, 0.5f);
        popup.anchorMax = new Vector2(1f - sideMargin, 0.5f);
        popup.pivot = new Vector2(0.5f, 0.5f);
        popup.anchoredPosition = Vector2.zero;
        popup.sizeDelta = new Vector2(0f, PopupHeight);

        // ── 3) 헤더바 + "LEVEL UP" ──
        var header = FindOrCreateChild(popup, "HeaderBar");
        ApplyProcedural(header.gameObject, false, UITheme.RadMd, 0, UITheme.PanelHeader, UITheme.PanelHeader);
        header.GetComponent<Image>().color = Color.white;
        header.GetComponent<Image>().raycastTarget = false;
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, HeaderH);
        header.anchoredPosition = Vector2.zero;

        var title = FindOrCreateLabel(header, "Title", font);
        title.text = "LEVEL UP";
        title.fontSize = UITheme.Header;
        title.fontStyle = FontStyles.Bold;
        title.color = UITheme.Gold;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        Stretch(title.rectTransform);

        // ── 4) 세로 카드 컨테이너 ──
        var cards = FindOrCreateChild(popup, "Cards");
        var cardsImg = cards.GetComponent<Image>();
        if (cardsImg != null) Object.DestroyImmediate(cardsImg); // 컨테이너는 그래픽 불필요
        cards.anchorMin = Vector2.zero;
        cards.anchorMax = Vector2.one;
        cards.pivot = new Vector2(0.5f, 0.5f);
        cards.offsetMin = new Vector2(UITheme.S5, UITheme.S5);
        cards.offsetMax = new Vector2(-UITheme.S5, -(HeaderH + UITheme.S4));
        var vlg = cards.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = cards.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = UITheme.S4;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ── 5) 기존 카드(_buttons[i]) 재부모화 + 리스킨 ──
        var so = new SerializedObject(panel);
        var buttonsProp = so.FindProperty("_buttons");
        var namesProp = so.FindProperty("_nameTexts");
        var descsProp = so.FindProperty("_descTexts");
        int count = buttonsProp != null ? buttonsProp.arraySize : 0;
        for (int i = 0; i < count; i++)
        {
            var button = buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue as Button;
            if (button == null) continue;
            StyleCard(button, cards, font,
                GetElement<TMP_Text>(namesProp, i),
                GetElement<TMP_Text>(descsProp, i));
        }
        so.ApplyModifiedProperties(); // 배열은 변경하지 않음 — 보존 확인용

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Selection.activeGameObject = popup.gameObject;
        Debug.Log("[UpgradePanelGenerator] 업그레이드 패널 세로 스택 리스킨 완료. 필요 시 에디터에서 미세조정.");
    }

    static void StyleCard(Button button, RectTransform cardsParent, TMP_FontAsset font, TMP_Text nameText, TMP_Text descText)
    {
        var cardRT = (RectTransform)button.transform;
        cardRT.SetParent(cardsParent, false);

        ApplyProcedural(button.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.CardSurface, UITheme.Outline);
        var img = button.GetComponent<Image>();
        if (img != null) img.color = Color.white;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = UITheme.Divider;
        colors.selectedColor = Color.white;
        colors.disabledColor = UITheme.TextDisabled;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var le = button.GetComponent<LayoutElement>();
        if (le == null) le = button.gameObject.AddComponent<LayoutElement>();
        le.minHeight = CardH;
        le.preferredHeight = CardH;
        le.flexibleWidth = 1f;

        const float nameH = 40f;
        if (nameText != null)
        {
            nameText.font = font != null ? font : nameText.font;
            nameText.fontSize = UITheme.Button;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = UITheme.TextPrimary;
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.raycastTarget = false;
            var nrt = nameText.rectTransform;
            nrt.anchorMin = new Vector2(0f, 1f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.pivot = new Vector2(0.5f, 1f);
            nrt.sizeDelta = new Vector2(-UITheme.S4 * 2f, nameH);
            nrt.anchoredPosition = new Vector2(0f, -UITheme.S3);
        }
        if (descText != null)
        {
            descText.font = font != null ? font : descText.font;
            descText.fontSize = UITheme.Caption;
            descText.color = UITheme.TextSecondary;
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.raycastTarget = false;
            var drt = descText.rectTransform;
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.offsetMin = new Vector2(UITheme.S4, UITheme.S3);
            drt.offsetMax = new Vector2(-UITheme.S4, -(nameH + UITheme.S3 + UITheme.S2));
        }
    }

    // ── 헬퍼 ──
    // 스프라이트를 직접 굽지 않고 UIProceduralSprite 컴포넌트로 위임 — 저장 후에도 살아남는다.
    static void ApplyProcedural(GameObject go, bool outlined, int radius, int outlineWidth, Color fill, Color line)
    {
        var ps = go.GetComponent<UIProceduralSprite>();
        if (ps == null) ps = go.AddComponent<UIProceduralSprite>();
        ps.Configure(outlined, radius, outlineWidth, fill, line);
    }

    static RectTransform FindOrCreateChild(RectTransform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null)
        {
            if (existing.GetComponent<Image>() == null) existing.gameObject.AddComponent<Image>();
            return existing;
        }
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static TMP_Text FindOrCreateLabel(RectTransform parent, string name, TMP_FontAsset font)
    {
        var found = parent.Find(name);
        TMP_Text t = found != null ? found.GetComponent<TMP_Text>() : null;
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            t = go.GetComponent<TextMeshProUGUI>();
        }
        if (font != null) t.font = font;
        return t;
    }

    static T GetElement<T>(SerializedProperty arrayProp, int i) where T : Object
    {
        if (arrayProp == null || i >= arrayProp.arraySize) return null;
        return arrayProp.GetArrayElementAtIndex(i).objectReferenceValue as T;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
