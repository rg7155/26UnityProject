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

        UILayerAssign.AssignLayer(panel.gameObject, UILayer.Modal); // 업그레이드 모달은 항상 최상단 레이어

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
        var highlightsProp = so.FindProperty("_weaponHighlights");
        int count = buttonsProp != null ? buttonsProp.arraySize : 0;
        if (highlightsProp != null) highlightsProp.arraySize = count; // 카드와 1:1 — 인덱스가 어긋나면 엉뚱한 카드가 강조된다
        for (int i = 0; i < count; i++)
        {
            var button = buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue as Button;
            if (button == null) continue;
            StyleCard(button, cards, font,
                GetElement<TMP_Text>(namesProp, i),
                GetElement<TMP_Text>(descsProp, i));

            var highlight = BuildWeaponHighlight((RectTransform)button.transform, font);
            if (highlightsProp != null)
                highlightsProp.GetArrayElementAtIndex(i).objectReferenceValue = highlight;
        }
        so.ApplyModifiedProperties(); // _buttons/_nameTexts/_descTexts 는 보존, _weaponHighlights 만 생성기가 소유

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

    // 무기 언락 카드 강조: 골드 테두리 + 우하단 "NEW WEAPON" 배지.
    // 카드 rect 를 그대로 덮으므로 카드 크기·기존 텍스트 배치에 영향이 없다(3택 레이아웃 보존).
    static GameObject BuildWeaponHighlight(RectTransform cardRT, TMP_FontAsset font)
    {
        var root = FindOrCreateChild(cardRT, "WeaponHighlight");
        root.gameObject.SetActive(true); // 레이아웃 계산을 위해 잠시 켠다(끝에서 다시 끈다)
        var rootImg = root.GetComponent<Image>();
        if (rootImg != null) Object.DestroyImmediate(rootImg); // 컨테이너는 그래픽 불필요
        Stretch(root);
        root.SetAsFirstSibling(); // 카드 배경 위·이름/설명 텍스트 아래에 그린다

        // 카드 자체 외곽선과 동일한 반경·두께로 겹쳐 어두운 테두리를 골드로 치환한다.
        var frame = FindOrCreateChild(root, "Frame");
        ApplyProcedural(frame.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, Transparent(UITheme.Gold), UITheme.Gold);
        var frameImg = frame.GetComponent<Image>();
        frameImg.color = Color.white;
        frameImg.raycastTarget = false;
        Stretch(frame);

        var badge = FindOrCreateChild(root, "Badge");
        ApplyProcedural(badge.gameObject, false, UITheme.RadSm, 0, UITheme.Gold, UITheme.Gold);
        var badgeImg = badge.GetComponent<Image>();
        badgeImg.color = Color.white;
        badgeImg.raycastTarget = false;
        badge.anchorMin = new Vector2(1f, 0f);
        badge.anchorMax = new Vector2(1f, 0f);
        badge.pivot = new Vector2(1f, 0f);
        badge.anchoredPosition = new Vector2(-UITheme.S4, UITheme.S3);

        var hlg = badge.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = badge.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset((int)UITheme.S3, (int)UITheme.S3, (int)UITheme.S1, (int)UITheme.S1);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 라벨 길이에 배지 폭을 맞춘다 — 고정 폭을 쓰면 문구가 바뀔 때 잘린다.
        var fitter = badge.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = badge.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var label = FindOrCreateLabel(badge, "Label", font);
        label.text = "NEW WEAPON";
        label.fontSize = UITheme.Caption;
        label.fontStyle = FontStyles.Bold;
        label.color = UITheme.Outline; // 골드 배경 위 가독성
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        LayoutRebuilder.ForceRebuildLayoutImmediate(badge);
        root.gameObject.SetActive(false); // 스탯 카드에선 꺼진 채로 시작 — 켜는 시점은 UI_UpgradePanel.Show 가 소유
        return root.gameObject;
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

    // 토큰 색의 알파만 0 으로 — 외곽선만 남기는 프레임의 fill 로 쓴다(투명 fill 을 검정으로 두면 안쪽에 어두운 띠가 생긴다).
    static Color Transparent(Color c)
    {
        c.a = 0f;
        return c;
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
