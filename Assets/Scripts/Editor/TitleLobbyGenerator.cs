#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 타이틀 화면을 모바일 세로 '로비형'으로 in-place 리스킨. 실행: Tools/UI/Build Title Lobby
//
// 전략(상점/메뉴 배경과 동일): 파괴/재생성 금지. TitleScene 의 기존 오브젝트
//   (_startButton/_bestText/_shopButton)을 재부모화·리스킨하고, 신규 장식(로고·기록카드·재화바)만 생성.
// 절차 스프라이트는 UIProceduralSprite 로 위임(프리팹/씬에 sprite 직접 굽지 않음 → 흰 박스 방지).
// TitleScene.cs/TitleGoldDisplay.cs 본문은 무수정 — 필드명만 SerializedObject 로 참조.
// in-place · idempotent(이름 재사용).
public static class TitleLobbyGenerator
{
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";
    const string Brand = "REWIND SURVIVORS";

    // ── 세로 튜닝값(스크린샷 피드백으로 조정) — Reference 1080×1920 기준 ──
    const float TopBarH = 120f;
    const float BottomBarH = 300f;
    static readonly Vector2 GoldChipSize = new Vector2(220f, 56f);
    static readonly Vector2 RecordCardSize = new Vector2(640f, 220f);
    const float PlayButtonH = 128f;
    const float ShopButtonH = 96f;
    const float ShopWidthPct = 0.65f;   // SHOP 버튼 폭(화면 대비, 중앙 정렬)
    const float LogoY = 220f;           // 중앙 클러스터를 화면 세로 중앙에 가깝게
    const float RecordCardY = -60f;

    [MenuItem("Tools/UI/Build Title Lobby")]
    public static void BuildTitleLobby()
    {
        var title = Object.FindFirstObjectByType<TitleScene>(FindObjectsInactive.Include);
        if (title == null)
        {
            Debug.LogError("[TitleLobbyGenerator] 씬에서 TitleScene 을 찾지 못했습니다. Title 씬을 연 상태로 실행하세요.");
            return;
        }
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[TitleLobbyGenerator] 씬에서 Canvas 를 찾지 못했습니다.");
            return;
        }
        var canvasRT = (RectTransform)canvas.transform;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[TitleLobbyGenerator] 폰트를 찾지 못했습니다: {FontPath} (기존 폰트 유지).");

        var so = new SerializedObject(title);
        var startButton = so.FindProperty("_startButton").objectReferenceValue as Button;
        var bestText = so.FindProperty("_bestText").objectReferenceValue as TMP_Text;
        var shopButton = so.FindProperty("_shopButton").objectReferenceValue as Button;

        // ── TitleLobby 컨테이너(투명, 풀스크린) ──
        var lobby = FindOrCreateChild(canvasRT, "TitleLobby");
        ClearImage(lobby);
        Stretch(lobby);
        UILayerAssign.AssignLayer(lobby.gameObject, UILayer.Hud);

        // ── SafeArea 컨테이너(노치 회피) — TopBar 를 그 안에 둔다 ──
        var safeArea = FindOrCreateChild(lobby, "SafeArea");
        ClearImage(safeArea);
        safeArea.anchorMin = Vector2.zero;
        safeArea.anchorMax = Vector2.one;
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
        if (safeArea.GetComponent<SafeAreaFitter>() == null) safeArea.gameObject.AddComponent<SafeAreaFitter>();

        // ── TopBar(재화바) — SafeArea 하위로(이전 레이아웃에서 lobby 직속이면 재부모화) ──
        var topBar = (safeArea.Find("TopBar") ?? lobby.Find("TopBar")) as RectTransform;
        if (topBar == null) topBar = FindOrCreateChild(safeArea, "TopBar");
        else topBar.SetParent(safeArea, false);
        ClearImage(topBar);
        TopStrip(topBar, TopBarH);

        var brand = FindOrCreateLabel(topBar, "BrandLabel", font);
        StyleLabel(brand, Brand, UITheme.Caption, FontStyles.Bold, UITheme.TextSecondary, TextAlignmentOptions.Left);
        Stretch(brand.rectTransform);
        brand.rectTransform.offsetMin = new Vector2(UITheme.S5, 0f);
        brand.rectTransform.offsetMax = new Vector2(-(GoldChipSize.x + UITheme.S5), 0f);

        var chip = FindOrCreateChild(topBar, "GoldChip");
        ApplyProcedural(chip.gameObject, false, UITheme.RadSm, 0, UITheme.CardSurface, UITheme.CardSurface);
        chip.GetComponent<Image>().color = Color.white;
        chip.GetComponent<Image>().raycastTarget = false;
        chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(1f, 0.5f);
        chip.sizeDelta = GoldChipSize;
        chip.anchoredPosition = new Vector2(-UITheme.S5, 0f);

        var goldText = FindOrCreateLabel(chip, "GoldText", font);
        StyleLabel(goldText, "0", UITheme.Caption, FontStyles.Bold, UITheme.Gold, TextAlignmentOptions.Center); // 런타임에 TitleGoldDisplay 가 덮어씀
        Stretch(goldText.rectTransform, UITheme.S2);

        var goldDisplay = chip.GetComponent<TitleGoldDisplay>();
        if (goldDisplay == null) goldDisplay = chip.gameObject.AddComponent<TitleGoldDisplay>();
        var gso = new SerializedObject(goldDisplay);
        WireIfNull(gso, "_goldText", goldText);
        gso.ApplyModifiedProperties();

        // ── CenterGroup ──
        var center = FindOrCreateChild(lobby, "CenterGroup");
        ClearImage(center);
        center.anchorMin = center.anchorMax = center.pivot = new Vector2(0.5f, 0.5f);
        center.sizeDelta = Vector2.zero;
        center.anchoredPosition = Vector2.zero;

        var logo = FindOrCreateLabel(center, "BigLogo", font);
        StyleLabel(logo, Brand, UITheme.Display, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
        logo.textWrappingMode = TextWrappingModes.Normal;
        var logoRT = logo.rectTransform;
        logoRT.anchorMin = logoRT.anchorMax = logoRT.pivot = new Vector2(0.5f, 0.5f);
        logoRT.sizeDelta = new Vector2(900f, 240f);
        logoRT.anchoredPosition = new Vector2(0f, LogoY);

        var card = FindOrCreateChild(center, "RecordCard");
        ApplyProcedural(card.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.CardSurface, UITheme.Outline);
        card.GetComponent<Image>().color = Color.white;
        card.GetComponent<Image>().raycastTarget = false;
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = RecordCardSize;
        card.anchoredPosition = new Vector2(0f, RecordCardY);

        const float bestCaptionH = 32f;
        var bestCaption = FindOrCreateLabel(card, "BestCaption", font);
        StyleLabel(bestCaption, "BEST", UITheme.Caption, FontStyles.Bold, UITheme.TextSecondary, TextAlignmentOptions.Center);
        var bcrt = bestCaption.rectTransform;
        bcrt.anchorMin = new Vector2(0f, 1f);
        bcrt.anchorMax = new Vector2(1f, 1f);
        bcrt.pivot = new Vector2(0.5f, 1f);
        bcrt.sizeDelta = new Vector2(0f, bestCaptionH);
        bcrt.anchoredPosition = new Vector2(0f, -UITheme.S3);

        if (bestText != null)
        {
            bestText.rectTransform.SetParent(card, false);
            StyleLabel(bestText, bestText.text, UITheme.Body, FontStyles.Normal, UITheme.TextPrimary, TextAlignmentOptions.Center);
            var brt = bestText.rectTransform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.offsetMin = new Vector2(UITheme.S4, UITheme.S4);
            brt.offsetMax = new Vector2(-UITheme.S4, -(bestCaptionH + UITheme.S3));
        }

        // ── BottomBar ──
        var bottom = FindOrCreateChild(lobby, "BottomBar");
        ClearImage(bottom);
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.sizeDelta = new Vector2(0f, BottomBarH);
        bottom.anchoredPosition = Vector2.zero;

        if (startButton != null)
        {
            var playRT = (RectTransform)startButton.transform;
            playRT.SetParent(bottom, false);
            ApplyProcedural(startButton.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.Accent, UITheme.Outline);
            var pimg = startButton.GetComponent<Image>();
            if (pimg != null) pimg.color = Color.white;
            UIBuild.ApplyCtaColors(startButton);
            playRT.anchorMin = new Vector2(0f, 0f);
            playRT.anchorMax = new Vector2(1f, 0f);
            playRT.pivot = new Vector2(0.5f, 0f);
            playRT.sizeDelta = new Vector2(-UITheme.S5 * 2f, PlayButtonH);
            playRT.anchoredPosition = new Vector2(0f, UITheme.S6); // 홈 인디케이터 여유(하단 인셋)
            var playLabel = startButton.GetComponentInChildren<TMP_Text>(true);
            if (playLabel != null)
            {
                StyleLabel(playLabel, "PLAY", UITheme.Button, FontStyles.Bold, UITheme.Outline, TextAlignmentOptions.Center);
                Stretch(playLabel.rectTransform);
            }
        }

        if (shopButton != null)
        {
            var shopRT = (RectTransform)shopButton.transform;
            shopRT.SetParent(bottom, false);
            ApplyProcedural(shopButton.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
            var simg = shopButton.GetComponent<Image>();
            if (simg != null) simg.color = Color.white;
            var colors = shopButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = UITheme.Divider;
            colors.selectedColor = Color.white;
            colors.disabledColor = UITheme.TextDisabled;
            colors.fadeDuration = 0.08f;
            shopButton.colors = colors;
            // PLAY 바로 위, 화면 폭 65% 중앙 정렬(보조 위계)
            float sideFrac = (1f - ShopWidthPct) * 0.5f;
            shopRT.anchorMin = new Vector2(sideFrac, 0f);
            shopRT.anchorMax = new Vector2(1f - sideFrac, 0f);
            shopRT.pivot = new Vector2(0.5f, 0f);
            shopRT.sizeDelta = new Vector2(0f, ShopButtonH);
            shopRT.anchoredPosition = new Vector2(0f, UITheme.S6 + PlayButtonH + UITheme.S3);
            var shopLabel = shopButton.GetComponentInChildren<TMP_Text>(true);
            if (shopLabel != null)
            {
                StyleLabel(shopLabel, "SHOP", UITheme.Button, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
                Stretch(shopLabel.rectTransform);
            }
        }

        // ── sibling 정리: MenuBackground 위에 로비. 상점(모달)의 상하 순서는 UILayer(Modal)가 결정. ──
        var menuBg = canvasRT.Find("MenuBackground");
        if (menuBg != null) lobby.SetSiblingIndex(menuBg.GetSiblingIndex() + 1);

        // ── CanvasScaler 세로 세팅 ──
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        EditorUtility.SetDirty(title);
        EditorUtility.SetDirty(goldDisplay);
        EditorSceneManager.MarkSceneDirty(title.gameObject.scene);
        Selection.activeGameObject = lobby.gameObject;
        Debug.Log("[TitleLobbyGenerator] 타이틀 로비 세로 리스킨 완료. 필요 시 에디터에서 미세조정.");
    }

    // ── 헬퍼 ──
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

    static void StyleLabel(TMP_Text t, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
    }

    static void ClearImage(RectTransform rt)
    {
        var img = rt.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = null;
            img.color = Color.clear;
            img.raycastTarget = false;
        }
    }

    static void TopStrip(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
    }

    static void Stretch(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }

    static void WireIfNull(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null && p.objectReferenceValue == null && value != null)
            p.objectReferenceValue = value;
    }
}
#endif
