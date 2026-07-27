#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 레이어 3 — 타이틀 로비에 퀘스트 패널을 코드로 생성/리스킨. 실행: Tools/UI/Build Quest Panel
//
// 전략: 상점 팝업(ShopUIGenerator)과 시각적으로 동일한 톤(PanelBase/PanelHeader/Outline)을 쓰되,
//   상점과 달리 씬에 기존 QuestPanel/QuestButton 오브젝트가 없으므로 이 생성기가 처음부터 만든다.
//   재실행해도 이름으로 기존 요소를 재사용해 중복 생성하지 않는다(idempotent).
// 절차 스프라이트는 UIProceduralSprite 로 위임 — 프리팹/씬에 sprite 를 직접 굽지 않는다.
// TitleScene.cs / UI_QuestPanel.cs / UI_QuestCell.cs 본문은 무수정 — [SerializeField] 는 코드로 배선.
public static class QuestUIGenerator
{
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";
    const string CellPrefabPath = "Assets/Prefabs/QuestItemCell.prefab";

    // ── 레이아웃 튜닝값(스크린샷 피드백으로 조정) — CanvasScaler Reference 1080x1920 기준 ──
    static readonly Vector2 PopupSize = new Vector2(960f, 1320f);
    const float HeaderH = 110f;
    const float CloseSize = 80f;
    static readonly Vector2 CellSize = new Vector2(880f, 140f); // 한 줄 카드 — 실제 폭은 LayoutElement(flexibleWidth=1) 이 뷰포트 가용폭에 맞춤
    const float QuestButtonH = 96f;   // TitleLobbyGenerator 의 ShopButtonH 와 동일 규격
    const float NameLabelW = 220f;
    const float RewardLabelW = 96f;
    const float ClaimButtonW = 150f;

    [MenuItem("Tools/UI/Build Quest Panel")]
    public static void BuildQuestPanel()
    {
        var title = Object.FindFirstObjectByType<TitleScene>(FindObjectsInactive.Include);
        if (title == null)
        {
            Debug.LogError("[QuestUIGenerator] 씬에서 TitleScene 을 찾지 못했습니다. Title 씬을 연 상태로 실행하세요.");
            return;
        }
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[QuestUIGenerator] 씬에서 Canvas 를 찾지 못했습니다.");
            return;
        }
        canvas = canvas.rootCanvas; // nested Canvas(UILayerCanvas) 오탐 방지
        var canvasRT = (RectTransform)canvas.transform;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[QuestUIGenerator] 폰트를 찾지 못했습니다: {FontPath} (기존 폰트 유지).");

        var titleSo = new SerializedObject(title);
        var shopButton = titleSo.FindProperty("_shopButton").objectReferenceValue as Button;
        var existingQuestButton = titleSo.FindProperty("_questButton").objectReferenceValue as Button;
        var existingQuestPanel = titleSo.FindProperty("_questPanel").objectReferenceValue as GameObject;

        // ── 1) 퀘스트 패널 루트(백드롭+팝업 컨테이너, 토글 대상) ──
        RectTransform panelRT = existingQuestPanel != null
            ? (RectTransform)existingQuestPanel.transform
            : FindOrCreateChild(canvasRT, "QuestPanel");
        var panelGO = panelRT.gameObject;
        UILayerAssign.AssignLayer(panelGO, UILayer.Modal); // 상점과 동일 레이어(모달 최상단)

        var panelImg = panelGO.GetComponent<Image>();
        if (panelImg == null) panelImg = panelGO.AddComponent<Image>();
        panelImg.color = Color.clear;
        panelImg.raycastTarget = false;
        Stretch(panelRT);

        var quest = panelGO.GetComponent<UI_QuestPanel>();
        if (quest == null) quest = panelGO.AddComponent<UI_QuestPanel>();

        // ── 2) 백드롭 딤 ──
        var dim = FindOrCreateChild(panelRT, "QuestDim");
        var dimImg = dim.GetComponent<Image>();
        dimImg.sprite = null;
        dimImg.type = Image.Type.Simple;
        dimImg.color = UITheme.Backdrop;
        dimImg.raycastTarget = true;
        Stretch(dim);

        // ── 3) 팝업 본체 ──
        var popup = FindOrCreateChild(panelRT, "QuestPopup");
        ApplyProcedural(popup.gameObject, true, UITheme.RadLg, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        popup.GetComponent<Image>().color = Color.white;
        popup.GetComponent<Image>().raycastTarget = true;
        Center(popup, PopupSize);

        // ── 4) 헤더바 ──
        var header = FindOrCreateChild(popup, "HeaderBar");
        ApplyProcedural(header.gameObject, false, UITheme.RadMd, 0, UITheme.PanelHeader, UITheme.PanelHeader);
        header.GetComponent<Image>().color = Color.white;
        header.GetComponent<Image>().raycastTarget = false;
        TopStrip(header, HeaderH);

        var headerTitle = FindOrCreateLabel(header, "QuestTitleLabel", font);
        StyleLabel(headerTitle, "QUEST", UITheme.Header, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Left);
        Stretch(headerTitle.rectTransform);
        float closeZone = CloseSize + UITheme.S3;
        headerTitle.rectTransform.offsetMin = new Vector2(UITheme.S5, 0f);
        headerTitle.rectTransform.offsetMax = new Vector2(-closeZone, 0f);

        // ── 5) 닫기버튼(팝업 우상단) ──
        var closeBtnRT = FindOrCreateChild(popup, "CloseButton");
        var closeButton = closeBtnRT.GetComponent<Button>();
        if (closeButton == null) closeButton = closeBtnRT.gameObject.AddComponent<Button>();
        ApplyProcedural(closeBtnRT.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.Danger, UITheme.Outline);
        closeBtnRT.GetComponent<Image>().color = Color.white;
        UIBuild.ApplyCtaColors(closeButton);
        closeBtnRT.anchorMin = closeBtnRT.anchorMax = closeBtnRT.pivot = new Vector2(1f, 1f);
        closeBtnRT.sizeDelta = new Vector2(CloseSize, CloseSize);
        closeBtnRT.anchoredPosition = new Vector2(-UITheme.S3, -UITheme.S3);
        var closeLabel = FindOrCreateLabel(closeBtnRT, "Label", font);
        StyleLabel(closeLabel, "X", UITheme.Button, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
        Stretch(closeLabel.rectTransform);

        // ── 6) 스크롤 + 세로 리스트(퀘스트는 카드 정보가 많아 3열 그리드 대신 1열 리스트) ──
        var scrollRT = FindOrCreateChild(popup, "Scroll");
        var scrollImg = scrollRT.GetComponent<Image>();
        scrollImg.sprite = null;
        scrollImg.color = Color.clear;
        scrollImg.raycastTarget = true; // 빈 영역 드래그 스크롤
        var scrollRect = scrollRT.GetComponent<ScrollRect>();
        if (scrollRect == null) scrollRect = scrollRT.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.pivot = new Vector2(0.5f, 0.5f);
        scrollRT.offsetMin = new Vector2(UITheme.S5, UITheme.S5);
        scrollRT.offsetMax = new Vector2(-UITheme.S5, -(HeaderH + UITheme.S4));

        var viewport = FindOrCreateChild(scrollRT, "Viewport");
        var viewportImg = viewport.GetComponent<Image>();
        viewportImg.sprite = null;
        viewportImg.color = Color.white;
        var mask = viewport.GetComponent<Mask>();
        if (mask == null) mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        Stretch(viewport);
        scrollRect.viewport = viewport;

        var content = FindOrCreateChild(viewport, "Content");
        var contentImg = content.GetComponent<Image>();
        contentImg.color = Color.clear;
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = UITheme.S3;
        vlg.padding = new RectOffset((int)UITheme.S2, (int)UITheme.S2, (int)UITheme.S2, (int)UITheme.S2);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, content.sizeDelta.y); // 폭 delta 0 → Content 폭 = 뷰포트 폭(초과분 없음 → 셀 좌우 잘림 방지)
        scrollRect.content = content;

        // ── 7) 셀 프리팹 생성/리스킨 ──
        var cellPrefab = BuildOrReskinCellPrefab(font);

        // ── 8) 참조 배선 ──
        var qso = new SerializedObject(quest);
        WireIfNull(qso, "_closeButton", closeButton);
        WireIfNull(qso, "_cellContainer", content);
        WireIfNull(qso, "_cellPrefab", cellPrefab);
        qso.ApplyModifiedProperties();

        dim.SetAsFirstSibling();
        panelGO.SetActive(false); // 상점처럼 기본 비활성, 버튼으로 토글

        // ── 9) 타이틀 로비 하단바에 QUEST 버튼 배치(SHOP 버튼 바로 위) ──
        Button questButton = existingQuestButton;
        if (questButton == null && shopButton != null)
        {
            var bottomBar = shopButton.transform.parent as RectTransform;
            var qBtnRT = FindOrCreateChild(bottomBar, "QuestButton");
            questButton = qBtnRT.GetComponent<Button>();
            if (questButton == null) questButton = qBtnRT.gameObject.AddComponent<Button>();

            var shopRT = (RectTransform)shopButton.transform;
            ApplyProcedural(qBtnRT.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
            qBtnRT.GetComponent<Image>().color = Color.white;
            var colors = questButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = UITheme.Divider;
            colors.selectedColor = Color.white;
            colors.disabledColor = UITheme.TextDisabled;
            colors.fadeDuration = UITheme.FadeDuration;
            questButton.colors = colors;

            qBtnRT.anchorMin = shopRT.anchorMin;
            qBtnRT.anchorMax = shopRT.anchorMax;
            qBtnRT.pivot = shopRT.pivot;
            qBtnRT.sizeDelta = shopRT.sizeDelta;
            qBtnRT.anchoredPosition = shopRT.anchoredPosition + new Vector2(0f, QuestButtonH + UITheme.S3);

            var qLabel = FindOrCreateLabel(qBtnRT, "Label", font);
            StyleLabel(qLabel, "QUEST", UITheme.Button, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
            Stretch(qLabel.rectTransform);
        }
        else if (questButton == null)
        {
            Debug.LogWarning("[QuestUIGenerator] ShopButton 을 찾지 못해 QuestButton 을 자동 배치하지 못했습니다. TitleLobby 를 먼저 빌드하세요.");
        }

        WireIfNull(titleSo, "_questButton", questButton);
        WireIfNull(titleSo, "_questPanel", panelGO);
        titleSo.ApplyModifiedProperties();

        EditorUtility.SetDirty(title);
        EditorUtility.SetDirty(quest);
        EditorSceneManager.MarkSceneDirty(title.gameObject.scene);
        Selection.activeGameObject = popup.gameObject;
        Debug.Log("[QuestUIGenerator] 퀘스트 패널 생성/리스킨 완료. 필요 시 에디터에서 미세조정.");
    }

    // ── 셀 프리팹 생성(없으면 신규) 또는 리스킨(있으면 in-place) ──
    static UI_QuestCell BuildOrReskinCellPrefab(TMP_FontAsset font)
    {
        GameObject root;
        bool isNew = !System.IO.File.Exists(CellPrefabPath);
        if (isNew)
            root = new GameObject("QuestItemCell", typeof(RectTransform), typeof(Image));
        else
            root = PrefabUtility.LoadPrefabContents(CellPrefabPath);

        try
        {
            var rootRT = (RectTransform)root.transform;
            // 부모(Content) 의 VerticalLayoutGroup 은 childControlHeight=false 라 이 sizeDelta.y 가 실제
            // 행 높이로 그대로 쓰인다(childControlWidth=true 라 폭은 부모가 덮어씀 — 세로 리스트 표준 패턴).
            rootRT.sizeDelta = CellSize;

            ApplyProcedural(root, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.CardSurface, UITheme.Outline);
            root.GetComponent<Image>().color = Color.white;

            // 매 실행 자식을 전부 지우고 새로 빌드 — 여러 레이아웃 버전을 거치며 누적된
            // 옛 자식/순서/배선 꼬임을 원천 차단. 루트 컴포넌트(Image/HLG/UIProceduralSprite)는 유지.
            for (int i = rootRT.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(rootRT.GetChild(i).gameObject);

            // 한 줄 카드 레이아웃: HorizontalLayoutGroup 이 좌우 폭을 자동 분배 → 수동 앵커 계산으로
            // 인한 오버플로/클리핑을 원천 차단(카드 폭이 부모(Content) 폭에 맞춰 늘어나도 자동 대응).
            var hlg = root.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset((int)UITheme.S4, (int)UITheme.S4, (int)UITheme.S3, (int)UITheme.S3);
            hlg.spacing = UITheme.S3;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 카드 자체를 가변폭(flexibleWidth=1)으로 만들어 Content 뷰포트 가용폭에 정확히 맞춘다.
            // (이전엔 카드 폭을 HLG 가 자식 고정폭 합산으로 산정 → 뷰포트보다 넓어져 좌우가 잘렸다.
            //  flexibleWidth 를 명시하면 부모 VerticalLayoutGroup 이 카드를 정확히 컨테이너 폭까지만 늘린다.)
            SetLayoutElement(root, -1f, -1f, 1f);

            // ── 이름(좌, 고정폭) ──
            var nameLabel = FindOrCreateLabel(rootRT, "NameLabel", font);
            StyleLabel(nameLabel, "Quest", UITheme.Body, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap; // 고정폭 안에서 줄바꿈 대신 말줄임(긴 이름 대비)
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            SetLayoutElement(nameLabel.gameObject, NameLabelW, -1f, 0f);

            // ── 진행바(가운데, 가변폭 — 남는 공간을 전부 차지) ──
            var progressArea = FindOrCreateChild(rootRT, "ProgressArea");
            ClearImage(progressArea);
            SetLayoutElement(progressArea.gameObject, 0f, -1f, 1f);

            const float barH = 40f;
            const float fillInset = 4f; // 트랙보다 살짝 안쪽 — 각진 Fill 모서리를 라운드 트랙 프레임이 감싸 라운드바처럼 보이게
            var track = FindOrCreateChild(progressArea, "ProgressTrack");
            ApplyProcedural(track.gameObject, false, UITheme.RadSm, 0, UITheme.Divider, UITheme.Divider);
            track.GetComponent<Image>().color = Color.white;
            track.GetComponent<Image>().raycastTarget = false;
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.pivot = new Vector2(0.5f, 0.5f);
            track.sizeDelta = new Vector2(0f, barH);
            track.anchoredPosition = Vector2.zero;

            // Fill: Image.Type.Filled 는 sprite 가 있어야 fillAmount 로 잘린다(sprite=null 이면 항상 꽉 참).
            // 각진 사각 절차 스프라이트(radius 0)를 Filled 로 써서 깔끔한 사각 채움을 얻고, 트랙보다 살짝
            // 안쪽으로 인셋해 라운드 트랙 프레임 안에 자연스럽게 앉힌다. 절차 스프라이트는 직렬화 안 되므로
            // UIProceduralSprite 로 런타임 재생성 + _preserveType 로 Filled 타입 유지.
            var fill = FindOrCreateChild(track, "ProgressFill");
            var fillPs = fill.GetComponent<UIProceduralSprite>();
            if (fillPs == null) fillPs = fill.gameObject.AddComponent<UIProceduralSprite>();
            fillPs.Configure(false, 0, 0, UITheme.Positive, UITheme.Positive);
            var fillPsSo = new SerializedObject(fillPs);
            fillPsSo.FindProperty("_preserveType").boolValue = true;
            fillPsSo.ApplyModifiedProperties();
            var fillImg = fill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f; // 실제 값은 UI_QuestCell.Refresh() 가 런타임에 설정
            fillImg.color = Color.white; // 색은 절차 스프라이트에 반영됨
            fillImg.raycastTarget = false;
            Stretch(fill, fillInset);

            var progressLabel = FindOrCreateLabel(progressArea, "ProgressLabel", font);
            StyleLabel(progressLabel, "0 / 0", UITheme.Caption, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
            progressLabel.rectTransform.anchorMin = track.anchorMin;
            progressLabel.rectTransform.anchorMax = track.anchorMax;
            progressLabel.rectTransform.pivot = track.pivot;
            progressLabel.rectTransform.sizeDelta = track.sizeDelta;
            progressLabel.rectTransform.anchoredPosition = track.anchoredPosition;

            // ── 보상(우, 고정폭) ──
            var rewardLabel = FindOrCreateLabel(rootRT, "RewardLabel", font);
            StyleLabel(rewardLabel, "0G", UITheme.Caption, FontStyles.Bold, UITheme.Gold, TextAlignmentOptions.MidlineRight);
            SetLayoutElement(rewardLabel.gameObject, RewardLabelW, -1f, 0f);

            // ── 수령 버튼(우측 끝, 고정폭) ──
            var claimBtnRT = FindOrCreateChild(rootRT, "ClaimButton");
            var claimButton = claimBtnRT.GetComponent<Button>();
            if (claimButton == null) claimButton = claimBtnRT.gameObject.AddComponent<Button>();
            ApplyProcedural(claimBtnRT.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.Accent, UITheme.Outline);
            claimBtnRT.GetComponent<Image>().color = Color.white;
            UIBuild.ApplyCtaColors(claimButton);
            SetLayoutElement(claimBtnRT.gameObject, ClaimButtonW, -1f, 0f);
            var claimLabel = FindOrCreateLabel(claimBtnRT, "Label", font);
            StyleLabel(claimLabel, "CLAIM", UITheme.Button, FontStyles.Bold, UITheme.Outline, TextAlignmentOptions.Center);
            Stretch(claimLabel.rectTransform);

            var cell = root.GetComponent<UI_QuestCell>();
            if (cell == null) cell = root.AddComponent<UI_QuestCell>();

            // 자식을 매 실행 새로 만드므로 강제 재배선(WireIfNull 은 옛 참조를 남겨 이름 미표시/순서 꼬임 유발)
            var cso = new SerializedObject(cell);
            cso.FindProperty("_nameLabel").objectReferenceValue = nameLabel;
            cso.FindProperty("_progressFill").objectReferenceValue = fillImg;
            cso.FindProperty("_progressLabel").objectReferenceValue = progressLabel;
            cso.FindProperty("_rewardLabel").objectReferenceValue = rewardLabel;
            cso.FindProperty("_claimButton").objectReferenceValue = claimButton;
            cso.FindProperty("_claimLabel").objectReferenceValue = claimLabel;
            cso.ApplyModifiedProperties();

            if (isNew)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
            }
        }
        finally
        {
            if (isNew) Object.DestroyImmediate(root);
            else PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<UI_QuestCell>(CellPrefabPath);
    }

    // ── 헬퍼 ──
    static void ApplyProcedural(GameObject go, bool outlined, int radius, int outlineWidth, Color fill, Color line)
    {
        var ps = go.GetComponent<UIProceduralSprite>();
        if (ps == null) ps = go.AddComponent<UIProceduralSprite>();
        ps.Configure(outlined, radius, outlineWidth, fill, line);
    }

    // 순수 컨테이너 용 — 자체 배경 없이 Image 만 투명 처리(HorizontalLayoutGroup 자식 폭 계산용 RectTransform 확보).
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

    // HorizontalLayoutGroup 자식 폭 지정 — preferredWidth<0 이면 고정폭 미지정(가변폭 전용).
    // layoutPriority 를 명시적으로 올려 TMP_Text/HorizontalLayoutGroup 등 같은 오브젝트의 다른
    // ILayoutElement 구현(자동 계산된 선호 폭)보다 항상 이 값이 우선하도록 고정 — 우선순위가
    // 같아 자동계산값이 이겨버리면 고정폭 지정이 무시되어 카드가 뷰포트보다 넓어지는 문제가 재발한다.
    static void SetLayoutElement(GameObject go, float preferredWidth, float preferredHeight, float flexibleWidth)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.layoutPriority = 1;
        le.preferredWidth = preferredWidth >= 0f ? preferredWidth : -1f;
        le.preferredHeight = preferredHeight >= 0f ? preferredHeight : -1f;
        le.flexibleWidth = flexibleWidth;
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

    static void Center(RectTransform rt, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
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
