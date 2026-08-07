#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 인게임 Pause 오버레이를 코드로 in-place 생성. 실행: Tools/UI/Build Pause Screen
//
// 구조 핵심: PauseController(입력 폴링 포함)는 PauseRoot(항상 활성)에 부착하고,
// 실제 표시/숨김 대상(_panelRoot)은 그 자식 PausePanel 로 분리한다.
// PauseRoot 자체를 SetActive(false) 하면 Update()의 ESC 폴링도 함께 멈추기 때문.
// 절차 스프라이트는 UIProceduralSprite 로 위임(직렬화 null/흰 박스 방지).
// 기존 로직(PauseController/PauseStatsView) 무수정 — 배선/스타일만. in-place · idempotent.
public static class PauseScreenGenerator
{
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";

    // ── 세로 튜닝값(스크린샷 피드백으로 조정) — Reference 1080×1920 기준 ──
    const float TitleTopY = 220f;
    const float TitleHeight = 100f;
    const float CardTopGap = UITheme.S6;
    const float CardWidth = 760f;
    const float StatRowH = 68f;
    const float VolumeRowH = 88f; // 모바일 터치 타깃 하한 — 트랙보다 훨씬 높게 잡아 행 어디를 눌러도 잡힌다
    const float BottomBarH = 320f;
    const float ResumeButtonH = 128f;
    const float TitleButtonH = 96f;
    static readonly Vector2 PauseEntryBtnSize = new Vector2(72f, 72f);
    const float PauseEntryGap = UITheme.S6; // REWIND 와의 세로 간격 — 자주 누르는 REWIND 의 오터치 방지

    static readonly (string Label, string Field)[] StatRows =
    {
        ("DAMAGE", "_damageText"),
        ("FIRE RATE", "_fireRateText"),
        ("MOVE SPEED", "_moveSpeedText"),
        ("MAX HP", "_maxHpText"),
        ("RANGE", "_rangeText"),
        ("WEAPONS", "_weaponCountText"),
    };

    [MenuItem("Tools/UI/Build Pause Screen")]
    public static void BuildPauseScreen()
    {
        var canvas = UIGenScene.ResolveMainCanvas("PauseScreenGenerator"); // 자기가 만든 @PauseCanvas 를 되잡지 않도록
        if (canvas == null) return;
        var canvasRT = (RectTransform)canvas.transform;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[PauseScreenGenerator] 폰트를 찾지 못했습니다: {FontPath} (기존 폰트 유지).");

        // ── 전용 루트 캔버스(@PauseCanvas) — 메인 캔버스(GameScene 800×600)와 독립된 세로 1080×1920 ──
        // HUD는 메인 캔버스 기준으로 튜닝돼 있어 건드리지 않고, Pause 오버레이만 올바른 세로 비율로 렌더한다.
        var pauseCanvasRT = GetOrCreatePauseCanvas();

        // ── PauseRoot(풀스크린, 항상 활성 — PauseController 부착 대상) ──
        var pauseRoot = FindOrCreateChild(pauseCanvasRT, "PauseRoot");
        ClearImage(pauseRoot);
        Stretch(pauseRoot);

        // ── PausePanel(표시/숨김 대상 — _panelRoot) ──
        var panel = FindOrCreateChild(pauseRoot, "PausePanel");
        ClearImage(panel);
        Stretch(panel);

        // Backdrop — 뒤 입력 차단 + 딤
        var backdrop = FindOrCreateChild(panel, "Backdrop");
        var backdropImg = backdrop.GetComponent<Image>();
        backdropImg.sprite = null;
        backdropImg.color = UITheme.Backdrop;
        backdropImg.raycastTarget = true;
        Stretch(backdrop);

        // SafeArea 컨테이너
        var safeArea = FindOrCreateChild(panel, "SafeArea");
        ClearImage(safeArea);
        Stretch(safeArea);
        if (safeArea.GetComponent<SafeAreaFitter>() == null) safeArea.gameObject.AddComponent<SafeAreaFitter>();

        // ── "PAUSED" 타이틀 ──
        var title = FindOrCreateLabel(safeArea, "Title", font);
        StyleLabel(title, "PAUSED", UITheme.Display, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
        var titleRT = title.rectTransform;
        titleRT.anchorMin = titleRT.anchorMax = titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(900f, TitleHeight);
        titleRT.anchoredPosition = new Vector2(0f, -TitleTopY);

        // ── 스텟 카드(라운드 + 세로 스택 + 내용 hug) ──
        var card = FindOrCreateChild(safeArea, "StatsCard");
        ApplyProcedural(card.gameObject, true, UITheme.RadLg, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        card.GetComponent<Image>().color = Color.white;
        card.GetComponent<Image>().raycastTarget = false;
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 1f);
        card.sizeDelta = new Vector2(CardWidth, 0f); // 높이는 ContentSizeFitter 가 내용에 맞춰 계산
        card.anchoredPosition = new Vector2(0f, -(TitleTopY + TitleHeight + CardTopGap));

        var vlg = card.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset((int)UITheme.S5, (int)UITheme.S5, (int)UITheme.S5, (int)UITheme.S5);
        vlg.spacing = UITheme.S2;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = card.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = card.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 6행: 왼쪽 라벨명 + 오른쪽 값(placeholder "--")
        var valueLabels = new TMP_Text[StatRows.Length];
        for (int i = 0; i < StatRows.Length; i++)
        {
            var row = BuildStatRow(card, StatRows[i].Field, StatRows[i].Label, font);
            valueLabels[i] = row;
        }

        // PauseStatsView 배선(스텟 카드에 부착)
        var statsView = card.GetComponent<PauseStatsView>();
        if (statsView == null) statsView = card.gameObject.AddComponent<PauseStatsView>();
        var svo = new SerializedObject(statsView);
        for (int i = 0; i < StatRows.Length; i++)
            WireIfNull(svo, StatRows[i].Field, valueLabels[i]);
        svo.ApplyModifiedProperties();

        // ── 사운드 카드(BGM/SFX 볼륨) ──
        // StatsCard 는 ContentSizeFitter 로 높이가 런타임 결정이라 그 아래로 쌓으면 행 수가 바뀔 때 겹친다.
        // 그래서 위에서 내려오지 않고 SafeArea 하단(BottomBar 위)을 기준으로 올려 붙인다.
        var soundCard = FindOrCreateChild(safeArea, "SoundCard");
        ApplyProcedural(soundCard.gameObject, true, UITheme.RadLg, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        var soundCardImg = soundCard.GetComponent<Image>();
        soundCardImg.color = Color.white;
        soundCardImg.raycastTarget = false;
        soundCard.anchorMin = soundCard.anchorMax = soundCard.pivot = new Vector2(0.5f, 0f);
        soundCard.sizeDelta = new Vector2(CardWidth, 0f); // 높이는 ContentSizeFitter 가 내용에 맞춰 계산
        soundCard.anchoredPosition = new Vector2(0f, BottomBarH + UITheme.S6);

        var soundVlg = soundCard.GetComponent<VerticalLayoutGroup>();
        if (soundVlg == null) soundVlg = soundCard.gameObject.AddComponent<VerticalLayoutGroup>();
        soundVlg.padding = new RectOffset((int)UITheme.S5, (int)UITheme.S5, (int)UITheme.S5, (int)UITheme.S5);
        soundVlg.spacing = UITheme.S3;
        soundVlg.childAlignment = TextAnchor.UpperCenter;
        soundVlg.childControlWidth = true;
        soundVlg.childControlHeight = true;
        soundVlg.childForceExpandWidth = true;
        soundVlg.childForceExpandHeight = false;

        var soundCsf = soundCard.GetComponent<ContentSizeFitter>();
        if (soundCsf == null) soundCsf = soundCard.gameObject.AddComponent<ContentSizeFitter>();
        soundCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        soundCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var soundHeader = FindOrCreateLabel(soundCard, "Header", font);
        StyleLabel(soundHeader, "SOUND", UITheme.Header, FontStyles.Bold, UITheme.TextSecondary, TextAlignmentOptions.Left);

        var bgmSlider = BuildVolumeRow(soundCard, "BgmRow", "BGM", UITheme.Accent, font);
        var effectSlider = BuildVolumeRow(soundCard, "EffectRow", "SFX", UITheme.Cyan, font);

        var volumeView = soundCard.GetComponent<PauseVolumeView>();
        if (volumeView == null) volumeView = soundCard.gameObject.AddComponent<PauseVolumeView>();
        var vvo = new SerializedObject(volumeView);
        WireIfNull(vvo, "_bgmSlider", bgmSlider);
        WireIfNull(vvo, "_effectSlider", effectSlider);
        vvo.ApplyModifiedProperties();

        // ── 하단 버튼 2개 ──
        var bottom = FindOrCreateChild(safeArea, "BottomBar");
        ClearImage(bottom);
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.sizeDelta = new Vector2(0f, BottomBarH);
        bottom.anchoredPosition = Vector2.zero;

        var resumeBtn = FindOrCreateButton(bottom, "ResumeButton");
        ApplyProcedural(resumeBtn.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.Accent, UITheme.Outline);
        resumeBtn.GetComponent<Image>().color = Color.white;
        UIBuild.ApplyCtaColors(resumeBtn);
        var resumeRT = (RectTransform)resumeBtn.transform;
        resumeRT.anchorMin = new Vector2(0f, 0f);
        resumeRT.anchorMax = new Vector2(1f, 0f);
        resumeRT.pivot = new Vector2(0.5f, 0f);
        resumeRT.sizeDelta = new Vector2(-UITheme.S5 * 2f, ResumeButtonH);
        resumeRT.anchoredPosition = new Vector2(0f, UITheme.S6 + TitleButtonH + UITheme.S3);
        var resumeLabel = FindOrCreateLabel(resumeRT, "Label", font);
        StyleLabel(resumeLabel, "RESUME", UITheme.Button, FontStyles.Bold, UITheme.Outline, TextAlignmentOptions.Center);
        Stretch(resumeLabel.rectTransform);

        var titleBtn = FindOrCreateButton(bottom, "TitleButton");
        ApplyProcedural(titleBtn.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        var titleBtnImg = titleBtn.GetComponent<Image>();
        titleBtnImg.color = Color.white;
        var tColors = titleBtn.colors;
        tColors.normalColor = Color.white;
        tColors.highlightedColor = Color.white;
        tColors.pressedColor = UITheme.Divider;
        tColors.selectedColor = Color.white;
        tColors.disabledColor = UITheme.TextDisabled;
        tColors.fadeDuration = UITheme.FadeDuration;
        titleBtn.colors = tColors;
        var titleBtnRT = (RectTransform)titleBtn.transform;
        titleBtnRT.anchorMin = new Vector2(0f, 0f);
        titleBtnRT.anchorMax = new Vector2(1f, 0f);
        titleBtnRT.pivot = new Vector2(0.5f, 0f);
        titleBtnRT.sizeDelta = new Vector2(-UITheme.S5 * 2f, TitleButtonH);
        titleBtnRT.anchoredPosition = new Vector2(0f, UITheme.S6);
        var titleBtnLabel = FindOrCreateLabel(titleBtnRT, "Label", font);
        StyleLabel(titleBtnLabel, "TITLE", UITheme.Button, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
        Stretch(titleBtnLabel.rectTransform);

        // ── 우하단 Pause 진입 버튼 — HUD 레이어(PauseRoot 밖, 항상 눌리는 위치) ──
        // 상단은 정보 표시(StatusStrip/보스 밴드) 전용이라 조작 버튼은 우하단 클러스터로 내린다.
        var hudRoot = canvasRT.Find("HudRoot") as RectTransform;
        var pauseEntryParent = hudRoot != null ? hudRoot : canvasRT;
        var pauseEntryBtn = FindOrCreateButton(pauseEntryParent, "PauseEntryButton");
        ApplyProcedural(pauseEntryBtn.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.CardSurface, UITheme.Outline);
        pauseEntryBtn.GetComponent<Image>().color = Color.white;
        UIBuild.ApplyCtaColors(pauseEntryBtn);
        if (hudRoot == null)
            UILayerAssign.AssignLayer(pauseEntryBtn.gameObject, UILayer.Hud);
        var entryRT = (RectTransform)pauseEntryBtn.transform;
        entryRT.anchorMin = entryRT.anchorMax = entryRT.pivot = new Vector2(1f, 0f);
        entryRT.sizeDelta = PauseEntryBtnSize;
        // REWIND 기하는 HudGenerator 가 소유 — 여기선 읽기만 하고 그 위로 쌓는다(가로 중심 정렬).
        float entryX = HudGenerator.RewindBtnMargin + (HudGenerator.RewindBtnSize - PauseEntryBtnSize.x) * 0.5f;
        float entryY = HudGenerator.RewindBtnMargin + HudGenerator.RewindBtnSize + PauseEntryGap;
        entryRT.anchoredPosition = new Vector2(-entryX, entryY);
        var entryLabel = FindOrCreateLabel(entryRT, "Label", font);
        StyleLabel(entryLabel, "II", UITheme.Button, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
        Stretch(entryLabel.rectTransform);

        // 잘못된 HudRoot 에 생긴 진입 버튼 제거 — 배선보다 먼저. 기존 _pauseButton 이 그 유령을 가리키고
        // 있었다면 여기서 null 이 되고, 아래 WireIfNull 이 올바른 버튼으로 다시 배선한다.
        UIGenScene.PurgeStrays("PauseScreenGenerator", entryRT);

        // ── PauseController 배선(항상 활성 PauseRoot 에 부착, _panelRoot 는 PausePanel) ──
        var controller = pauseRoot.GetComponent<PauseController>();
        if (controller == null) controller = pauseRoot.gameObject.AddComponent<PauseController>();
        var co = new SerializedObject(controller);
        WireIfNull(co, "_panelRoot", panel.gameObject);
        WireIfNull(co, "_resumeButton", resumeBtn);
        WireIfNull(co, "_titleButton", titleBtn);
        WireIfNull(co, "_pauseButton", pauseEntryBtn);
        co.ApplyModifiedProperties();

        // 구버전 잔재 제거: 이전엔 PauseRoot 가 메인 캔버스 밑에 있었다. 배선 뒤에 지워야
        // 정식 PauseRoot 가 PauseController 를 이미 갖고 있어 "유일본 보호"에 걸리지 않는다.
        UIGenScene.PurgeStrays("PauseScreenGenerator", pauseRoot);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(statsView);
        EditorUtility.SetDirty(volumeView);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = pauseRoot.gameObject;
        Debug.Log("[PauseScreenGenerator] Pause 화면 생성 + 배선 완료. 필요 시 에디터에서 미세조정.");
    }

    // 메인 캔버스와 독립된 전용 오버레이 루트 캔버스(세로 1080×1920). idempotent.
    static RectTransform GetOrCreatePauseCanvas()
    {
        var existing = GameObject.Find("@PauseCanvas");
        var go = existing != null ? existing : new GameObject("@PauseCanvas");

        var canvas = go.GetComponent<Canvas>();
        if (canvas == null) canvas = go.AddComponent<Canvas>();   // Canvas 추가 시 RectTransform 자동 부착
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = (int)UILayer.Pause;                 // HUD/모달 위

        var scaler = go.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();

        return (RectTransform)go.transform;
    }

    // 스텟 카드 한 행: 왼쪽 라벨명(고정) + 오른쪽 값(placeholder "--"). 값 TMP_Text 반환.
    static TMP_Text BuildStatRow(RectTransform card, string rowName, string labelText, TMP_FontAsset font)
    {
        var row = FindOrCreateChild(card, rowName);
        ClearImage(row);
        var le = row.GetComponent<LayoutElement>();
        if (le == null) le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = StatRowH;
        le.preferredHeight = StatRowH;
        le.flexibleHeight = 0f;

        var nameLabel = FindOrCreateLabel(row, "Name", font);
        StyleLabel(nameLabel, labelText, UITheme.Body, FontStyles.Normal, UITheme.TextSecondary, TextAlignmentOptions.Left);
        var nameRT = nameLabel.rectTransform;
        nameRT.anchorMin = new Vector2(0f, 0f);
        nameRT.anchorMax = new Vector2(0.55f, 1f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;

        var valueLabel = FindOrCreateLabel(row, "Value", font);
        StyleLabel(valueLabel, "--", UITheme.Body, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Right);
        var valueRT = valueLabel.rectTransform;
        valueRT.anchorMin = new Vector2(0.55f, 0f);
        valueRT.anchorMax = new Vector2(1f, 1f);
        valueRT.offsetMin = Vector2.zero;
        valueRT.offsetMax = Vector2.zero;

        return valueLabel;
    }

    // 볼륨 행 한 줄: 왼쪽 이름 + 오른쪽 조작 가능한 슬라이더(트랙 + Fill + 원형 핸들). Slider 반환.
    static Slider BuildVolumeRow(RectTransform card, string rowName, string labelText, Color fillColor, TMP_FontAsset font)
    {
        var row = FindOrCreateChild(card, rowName);
        ClearImage(row);
        var le = row.GetComponent<LayoutElement>();
        if (le == null) le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = VolumeRowH;
        le.preferredHeight = VolumeRowH;
        le.flexibleHeight = 0f;

        var nameLabel = FindOrCreateLabel(row, "Name", font);
        StyleLabel(nameLabel, labelText, UITheme.Body, FontStyles.Bold, UITheme.TextSecondary, TextAlignmentOptions.MidlineLeft);
        FracRow(nameLabel.rectTransform, 0f, 0.35f);

        var sliderRT = FindOrCreateChild(row, "Slider");
        FracRow(sliderRT, 0.35f, 1f);
        // 얇은 트랙이 아니라 행 전체(88px)가 눌리도록 투명 raycast 타깃으로 남긴다 — ClearImage 를 쓰면 raycast 가 꺼진다
        var sliderImg = sliderRT.GetComponent<Image>();
        sliderImg.sprite = null;
        sliderImg.color = Color.clear;
        sliderImg.raycastTarget = true;

        // 트랙은 카드(PanelBase)보다 어두운 리세스 — 백드롭<패널<리세스 깊이 표현
        var track = FindOrCreateChild(sliderRT, "Background");
        ApplyProcedural(track.gameObject, false, UITheme.RadSm, 0, UITheme.Outline, UITheme.Outline);
        var trackImg = track.GetComponent<Image>();
        trackImg.color = Color.white;
        trackImg.raycastTarget = false;
        CenterStrip(track, UITheme.S5, 0f);

        var fillArea = FindOrCreateChild(sliderRT, "Fill Area");
        ClearImage(fillArea);
        CenterStrip(fillArea, UITheme.S5 - UITheme.S1 * 2f, UITheme.S1);

        var fill = FindOrCreateChild(fillArea, "Fill");
        ApplyProcedural(fill.gameObject, false, UITheme.RadSm, 0, fillColor, fillColor);
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = Color.white;
        fillImg.raycastTarget = false;
        Stretch(fill); // 앵커는 Slider 가 value 에 맞춰 덮어쓴다

        // 좌우 인셋 = 핸들 반지름. 없으면 0/1 끝값에서 핸들이 트랙 밖으로 반쯤 튀어나간다.
        var slideArea = FindOrCreateChild(sliderRT, "Handle Slide Area");
        ClearImage(slideArea);
        CenterStrip(slideArea, UITheme.S7, UITheme.S7 * 0.5f);

        var handle = FindOrCreateChild(slideArea, "Handle");
        var handlePs = handle.GetComponent<UIProceduralSprite>();
        if (handlePs == null) handlePs = handle.gameObject.AddComponent<UIProceduralSprite>();
        handlePs.ConfigureCircle((int)(UITheme.S7 * 0.5f), UITheme.TextPrimary);
        var handleImg = handle.GetComponent<Image>();
        handleImg.color = Color.white;
        handleImg.raycastTarget = true;
        handle.anchorMin = new Vector2(0f, 0f); // Slider 가 x 앵커만 value 로 덮어쓰고 y 는 0~1 로 강제한다
        handle.anchorMax = new Vector2(0f, 1f); // → 슬라이드 영역 높이가 곧 핸들 높이(= S7 정사각)
        handle.pivot = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(UITheme.S7, 0f);
        handle.anchoredPosition = Vector2.zero;

        var slider = sliderRT.GetComponent<Slider>();
        if (slider == null) slider = sliderRT.gameObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = true;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.transition = Selectable.Transition.ColorTint;
        slider.targetGraphic = handleImg;
        var colors = slider.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = UITheme.Accent;
        colors.selectedColor = Color.white;
        colors.disabledColor = UITheme.TextDisabled;
        colors.fadeDuration = UITheme.FadeDuration;
        slider.colors = colors;
        slider.fillRect = fill;
        slider.handleRect = handle;
        // value 는 세팅하지 않는다 — PauseVolumeView.Start() 가 세이브 값으로 채운다

        return slider;
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

    static Button FindOrCreateButton(RectTransform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null)
        {
            if (existing.GetComponent<Image>() == null) existing.gameObject.AddComponent<Image>();
            var btn = existing.GetComponent<Button>();
            if (btn == null) btn = existing.gameObject.AddComponent<Button>();
            return btn;
        }
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return go.GetComponent<Button>();
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

    // 부모 행을 가로 비율로 나눠 채운다.
    static void FracRow(RectTransform rt, float xMin, float xMax)
    {
        rt.anchorMin = new Vector2(xMin, 0f);
        rt.anchorMax = new Vector2(xMax, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // 부모 세로 중앙에 놓는 가로 스트립 — 좌우 sidePad 인셋, 높이 고정.
    static void CenterStrip(RectTransform rt, float height, float sidePad)
    {
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(-sidePad * 2f, height);
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
