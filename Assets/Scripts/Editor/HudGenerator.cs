#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 인게임 HUD(모바일 세로)를 코드로 in-place 생성/리스킨. 실행: Tools/UI/Build HUD
//
// 상단 상태 스트립(SafeArea): HP/XP 슬라이더 재스타일 + Score/Timer/Gold 신규 TMP + HudStats 배선.
// 우하단 Rewind 버튼: 원형 버튼 + 쿨다운 링(Filled Radial360) + Auto pip + RewindButtonUI 배선.
// 절차 스프라이트는 UIProceduralSprite 로 위임(직렬화 null/흰 박스 방지). 기존 로직 파일 무수정 — 배선/스타일만.
// in-place · idempotent(이름 재사용). JoystickUIGenerator/TitleLobbyGenerator 패턴 계승.
public static class HudGenerator
{
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";

    // ── 세로 튜닝값(스크린샷 피드백으로 조정) — Reference 1080×1920 기준 ──
    const float StatusStripH = 180f;
    const float HpBarH = 30f;
    const float ExpBarH = 22f;
    static readonly Vector2 GoldChipSize = new Vector2(150f, 44f);
    const float StatRowH = 56f;
    const float LvBadgeW = 90f;
    const float RewindBtnSize = 150f;
    const int RewindTexRadius = 128; // 절차 텍스처 해상도(표시 크기는 sizeDelta)
    const int RingThicknessTex = 18;
    const float PipSize = 28f;

    [MenuItem("Tools/UI/Build HUD")]
    public static void BuildHud()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[HudGenerator] 씬에서 Canvas 를 찾지 못했습니다. GameScene 을 연 상태로 실행하세요.");
            return;
        }
        var canvasRT = (RectTransform)canvas.transform;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[HudGenerator] 폰트를 찾지 못했습니다: {FontPath} (기존 폰트 유지).");

        // ── HudRoot(SafeArea 인셋, 풀스크린) ──
        var hudRoot = FindOrCreateChild(canvasRT, "HudRoot");
        ClearImage(hudRoot);
        hudRoot.anchorMin = Vector2.zero;
        hudRoot.anchorMax = Vector2.one;
        hudRoot.offsetMin = Vector2.zero;
        hudRoot.offsetMax = Vector2.zero;
        if (hudRoot.GetComponent<SafeAreaFitter>() == null) hudRoot.gameObject.AddComponent<SafeAreaFitter>();

        // ── 상단 StatusStrip(top-stretch) ──
        var strip = FindOrCreateChild(hudRoot, "StatusStrip");
        ClearImage(strip);
        strip.anchorMin = new Vector2(0f, 1f);
        strip.anchorMax = new Vector2(1f, 1f);
        strip.pivot = new Vector2(0.5f, 1f);
        strip.sizeDelta = new Vector2(0f, StatusStripH);
        strip.anchoredPosition = Vector2.zero;

        // HP / XP 슬라이더를 스트립 상단으로 재배치 + 리스킨
        float hpY = -UITheme.S4;
        float xpY = hpY - HpBarH - UITheme.S2;
        float rowY = xpY - ExpBarH - UITheme.S3;

        var hpBar = Object.FindFirstObjectByType<HpBar>(FindObjectsInactive.Include);
        if (hpBar != null) SkinBar(hpBar, "_slider", strip, hpY, HpBarH, UITheme.Danger, 0f);
        else Debug.LogWarning("[HudGenerator] HpBar 를 찾지 못했습니다(스킵).");

        var expBar = Object.FindFirstObjectByType<ExpBar>(FindObjectsInactive.Include);
        if (expBar != null)
        {
            SkinBar(expBar, "_slider", strip, xpY, ExpBarH, UITheme.Cyan, LvBadgeW + UITheme.S2); // XP 바를 Lv 배지 폭만큼 우측으로
            PlaceLevelBadge(expBar, "_levelText", strip, xpY, ExpBarH, font);
        }
        else Debug.LogWarning("[HudGenerator] ExpBar 를 찾지 못했습니다(스킵).");

        // ── 상태 한 줄: Score(좌) · Timer(중앙) · Gold칩(우) ──
        var statRow = FindOrCreateChild(strip, "StatRow");
        ClearImage(statRow);
        statRow.anchorMin = new Vector2(0f, 1f);
        statRow.anchorMax = new Vector2(1f, 1f);
        statRow.pivot = new Vector2(0.5f, 1f);
        statRow.sizeDelta = new Vector2(-UITheme.S5 * 2f, StatRowH);
        statRow.anchoredPosition = new Vector2(0f, rowY);

        // 세 요소를 겹치지 않는 x 영역으로 분할(비율 앵커 — 좁은 캔버스에서도 안 겹침)
        var scoreText = FindOrCreateLabel(statRow, "ScoreText", font);
        StyleLabel(scoreText, "0", UITheme.Body, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Left);
        FracRow(scoreText.rectTransform, 0f, 0.35f);

        var timerText = FindOrCreateLabel(statRow, "TimerText", font);
        StyleLabel(timerText, "00:00", UITheme.Header, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
        FracRow(timerText.rectTransform, 0.35f, 0.65f);

        var goldChip = FindOrCreateChild(statRow, "GoldChip");
        ApplyProcedural(goldChip.gameObject, false, UITheme.RadSm, 0, UITheme.CardSurface, UITheme.CardSurface);
        goldChip.GetComponent<Image>().color = Color.white;
        goldChip.GetComponent<Image>().raycastTarget = false;
        goldChip.anchorMin = goldChip.anchorMax = goldChip.pivot = new Vector2(1f, 0.5f);
        goldChip.sizeDelta = GoldChipSize;
        goldChip.anchoredPosition = Vector2.zero;
        var goldText = FindOrCreateLabel(goldChip, "GoldText", font);
        StyleLabel(goldText, "0", UITheme.Caption, FontStyles.Bold, UITheme.Gold, TextAlignmentOptions.Center);
        Stretch(goldText.rectTransform, UITheme.S2);

        // HudStats 배선(스트립에 부착)
        var hudStats = strip.GetComponent<HudStats>();
        if (hudStats == null) hudStats = strip.gameObject.AddComponent<HudStats>();
        var hs = new SerializedObject(hudStats);
        WireIfNull(hs, "_timerText", timerText);
        WireIfNull(hs, "_scoreText", scoreText);
        WireIfNull(hs, "_goldText", goldText);
        hs.ApplyModifiedProperties();

        // ── 우하단 Rewind 버튼 ──
        var rewindRT = FindOrCreateChild(hudRoot, "RewindButton");
        EnsureProcedural(rewindRT).ConfigureCircle(RewindTexRadius, UITheme.CardSurface); // 어두운 원형 웰
        rewindRT.GetComponent<Image>().color = Color.white;
        var rewindBtn = rewindRT.GetComponent<Button>();
        if (rewindBtn == null) rewindBtn = rewindRT.gameObject.AddComponent<Button>();
        UIBuild.ApplyCtaColors(rewindBtn);
        rewindRT.anchorMin = rewindRT.anchorMax = rewindRT.pivot = new Vector2(1f, 0f);
        rewindRT.sizeDelta = new Vector2(RewindBtnSize, RewindBtnSize);
        rewindRT.anchoredPosition = new Vector2(-UITheme.S6, UITheme.S6);

        // 쿨다운 링(Filled Radial360) — procedural 적용 후 type=Filled 세팅(순서 중요)
        var ring = FindOrCreateChild(rewindRT, "CooldownRing");
        var ringImg = ring.GetComponent<Image>();
        ringImg.raycastTarget = false;
        ringImg.color = Color.white;
        Center(ring);
        ring.sizeDelta = new Vector2(RewindBtnSize, RewindBtnSize);
        EnsureProcedural(ring).ConfigureRing(RewindTexRadius, RingThicknessTex, UITheme.Cyan, true); // preserveType
        ringImg.type = Image.Type.Filled;
        ringImg.fillMethod = Image.FillMethod.Radial360;
        ringImg.fillOrigin = (int)Image.Origin360.Top;
        ringImg.fillClockwise = true;
        ringImg.fillAmount = 1f;

        // 라벨
        var rewindLabel = FindOrCreateLabel(rewindRT, "Label", font);
        StyleLabel(rewindLabel, "REWIND", UITheme.Caption, FontStyles.Bold, UITheme.Cyan, TextAlignmentOptions.Center);
        Stretch(rewindLabel.rectTransform);

        // Auto 차지 pip(상단 중앙, 1개)
        var pip = FindOrCreateChild(rewindRT, "Pip0");
        var pipImg = pip.GetComponent<Image>();
        pipImg.raycastTarget = false;
        pipImg.color = Color.white;
        pip.anchorMin = pip.anchorMax = pip.pivot = new Vector2(0.5f, 1f);
        pip.sizeDelta = new Vector2(PipSize, PipSize);
        pip.anchoredPosition = new Vector2(0f, UITheme.S2);
        EnsureProcedural(pip).ConfigureCircle(RewindTexRadius, UITheme.Positive);

        // RewindButtonUI 배선
        var rewindUI = rewindRT.GetComponent<RewindButtonUI>();
        if (rewindUI == null) rewindUI = rewindRT.gameObject.AddComponent<RewindButtonUI>();
        var ru = new SerializedObject(rewindUI);
        WireIfNull(ru, "_button", rewindBtn);
        WireIfNull(ru, "_cooldownRing", ringImg);
        var pipsProp = ru.FindProperty("_autoPips");
        if (pipsProp != null && pipsProp.arraySize == 0)
        {
            pipsProp.arraySize = 1;
            pipsProp.GetArrayElementAtIndex(0).objectReferenceValue = pipImg;
        }
        ru.ApplyModifiedProperties();

        EditorUtility.SetDirty(hudStats);
        EditorUtility.SetDirty(rewindUI);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = hudRoot.gameObject;
        Debug.Log("[HudGenerator] HUD 생성 + 배선 완료. 필요 시 에디터에서 미세조정.");
    }

    // ── 슬라이더 리스킨 + 스트립 재배치(leftInset: 바 왼쪽 추가 여백 — Lv 배지 자리) ──
    static void SkinBar(Component bar, string sliderProp, RectTransform strip, float y, float height,
        Color fillColor, float leftInset)
    {
        var so = new SerializedObject(bar);
        var slider = so.FindProperty(sliderProp).objectReferenceValue as Slider;
        if (slider == null) { Debug.LogWarning($"[HudGenerator] {bar.GetType().Name}.{sliderProp} 가 비어있습니다(스킵)."); return; }

        // 재배치(같은 오브젝트 유지 → 참조 보존). 좌/우 비대칭 인셋을 sizeDelta+anchoredPos 로 표현.
        var barRT = (RectTransform)slider.transform;
        barRT.SetParent(strip, false);
        barRT.anchorMin = new Vector2(0f, 1f);
        barRT.anchorMax = new Vector2(1f, 1f);
        barRT.pivot = new Vector2(0.5f, 1f);
        float leftPad = UITheme.S5 + leftInset;
        float rightPad = UITheme.S5;
        barRT.sizeDelta = new Vector2(-(leftPad + rightPad), height);
        barRT.anchoredPosition = new Vector2((leftPad - rightPad) * 0.5f, y);

        // 트랙(Background) + Fill 재도색, 핸들 숨김
        var bg = slider.transform.Find("Background");
        if (bg != null) ApplyProcedural(bg.gameObject, false, UITheme.RadSm, 0, UITheme.CardSurface, UITheme.CardSurface);
        if (slider.fillRect != null)
        {
            ApplyProcedural(slider.fillRect.gameObject, false, UITheme.RadSm, 0, fillColor, fillColor);
            var fimg = slider.fillRect.GetComponent<Image>();
            if (fimg != null) fimg.color = Color.white;
        }
        if (slider.handleRect != null) slider.handleRect.gameObject.SetActive(false);
    }

    // Lv.N 텍스트를 XP 바 왼쪽 배지로 재부모화·재배치(.text 는 ExpBar 가 세팅 — 건드리지 않음)
    static void PlaceLevelBadge(Component expBar, string textProp, RectTransform strip, float y, float height, TMP_FontAsset font)
    {
        var so = new SerializedObject(expBar);
        var lvText = so.FindProperty(textProp).objectReferenceValue as TMP_Text;
        if (lvText == null) return;
        var rt = lvText.rectTransform;
        rt.SetParent(strip, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(LvBadgeW, height);
        rt.anchoredPosition = new Vector2(UITheme.S5, y);
        if (font != null) lvText.font = font;
        lvText.fontSize = UITheme.Caption;
        lvText.fontStyle = FontStyles.Bold;
        lvText.color = UITheme.Cyan;
        lvText.alignment = TextAlignmentOptions.Left;
        lvText.raycastTarget = false;
    }

    // ── 헬퍼 ──
    static void ApplyProcedural(GameObject go, bool outlined, int radius, int outlineWidth, Color fill, Color line)
    {
        var ps = go.GetComponent<UIProceduralSprite>();
        if (ps == null) ps = go.AddComponent<UIProceduralSprite>();
        ps.Configure(outlined, radius, outlineWidth, fill, line);
    }

    static UIProceduralSprite EnsureProcedural(RectTransform rt)
    {
        var ps = rt.GetComponent<UIProceduralSprite>();
        if (ps == null) ps = rt.gameObject.AddComponent<UIProceduralSprite>();
        return ps;
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

    // 부모 행을 [xMin,xMax] 가로 비율로 나눠 채운다(좁은 캔버스에서도 요소 간 겹침 방지).
    static void FracRow(RectTransform rt, float xMin, float xMax)
    {
        rt.anchorMin = new Vector2(xMin, 0f);
        rt.anchorMax = new Vector2(xMax, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Center(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
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
