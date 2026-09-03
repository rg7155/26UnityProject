#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 인게임 보스 UI(상단 보스 HP 바 + WARNING 배너)를 코드로 in-place 생성/리스킨. 실행: Tools/UI/Build Boss HUD
//
// 배치: 상단 밴드 좌표는 전부 UIHudLayout 이 소유한다. 여기서 기준선을 재계산하지 않는다
//   (실측은 StatusStrip 변화만 따라갈 뿐 같은 슬롯을 노리는 다른 위젯을 몰라서 겹쳤다).
// 구조: BossHpBar/BossWarningUI 컴포넌트는 항상 켜져 있는 컨테이너(BossHud)에 붙이고, _root 에는 자식
//   (BossHpRoot/BossWarningRoot)을 연결한다. 컴포넌트 자신을 끄면 Update 가 멈춰 다시 켜지지 않기 때문.
// WARNING 점멸은 로직이 아니라 비주얼 담당 — _root 하위 CanvasGroup + UIBlink 가 처리하고,
//   로직의 SetActive 토글이 OnEnable 로 위상을 리셋해준다.
// 절차 스프라이트는 UIProceduralSprite 로 위임(직렬화 null/흰 박스 방지). 로직 파일 무수정 — 배선/스타일만.
// in-place · idempotent(이름 재사용). HudGenerator 패턴 계승.
public static class BossHudGenerator
{
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";

    const float FillInset = UITheme.S1; // 트랙 프레임 안쪽으로 Fill 을 살짝 밀어넣어 라운드바처럼 보이게

    [MenuItem("Tools/UI/Build Boss HUD")]
    public static void BuildBossHud()
    {
        var canvas = UIGenScene.ResolveMainCanvas("BossHudGenerator"); // @PauseCanvas 오탐 방지 — 순서 비의존 확정
        if (canvas == null) return;
        var canvasRT = (RectTransform)canvas.transform;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[BossHudGenerator] 폰트를 찾지 못했습니다: {FontPath} (기존 폰트 유지).");

        // ── HudRoot(SafeArea 인셋, 풀스크린) — HudGenerator 와 같은 이름/설정이라 실행 순서 무관 ──
        var hudRoot = FindOrCreateChild(canvasRT, "HudRoot");
        ClearImage(hudRoot);
        Stretch(hudRoot);
        if (hudRoot.GetComponent<SafeAreaFitter>() == null) hudRoot.gameObject.AddComponent<SafeAreaFitter>();
        UILayerAssign.AssignLayer(hudRoot.gameObject, UILayer.Hud);

        // ── BossHud 컨테이너(투명·풀스크린) — 항상 활성. 두 로직 컴포넌트가 여기 붙는다 ──
        var bossHud = FindOrCreateChild(hudRoot, "BossHud");
        ClearImage(bossHud);
        Stretch(bossHud);

        // ── (1) 보스 HP 바 ──
        var hpRoot = FindOrCreateChild(bossHud, "BossHpRoot");
        ApplyProcedural(hpRoot.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.BossReactorSurface, UITheme.BossReactorOutline);
        var hpRootImg = hpRoot.GetComponent<Image>();
        hpRootImg.color = Color.white;
        hpRootImg.raycastTarget = false; // 조이스틱 드래그를 막지 않도록 HUD 전 요소 raycast 해제
        TopStrip(hpRoot, UIHudLayout.BossBandH, UITheme.S5, UIHudLayout.BossBandY);

        var nameRow = FindOrCreateChild(hpRoot, "NameRow");
        ClearImage(nameRow);
        TopStrip(nameRow, UIHudLayout.BossNameRowH, UITheme.S3, -UITheme.S2);

        var nameText = FindOrCreateLabel(nameRow, "BossNameText", font);
        StyleLabel(nameText, "BOSS", UITheme.Caption, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
        FracRow(nameText.rectTransform, 0f, 0.6f);

        var hpText = FindOrCreateLabel(nameRow, "BossHpText", font);
        StyleLabel(hpText, "0 / 0", UITheme.Caption, FontStyles.Bold, UITheme.BossReactorText, TextAlignmentOptions.MidlineRight);
        FracRow(hpText.rectTransform, 0.6f, 1f);

        var sliderRT = FindOrCreateChild(hpRoot, "BossHpSlider");
        ClearImage(sliderRT);
        TopStrip(sliderRT, UIHudLayout.BossBarH, UITheme.S3, -(UITheme.S2 + UIHudLayout.BossNameRowH + UITheme.S1));

        // 트랙은 프레임(CardSurface)보다 더 어두운 리세스 — 백드롭<패널<리세스 깊이 표현
        var track = FindOrCreateChild(sliderRT, "Background");
        ApplyProcedural(track.gameObject, false, UITheme.RadSm, 0, UITheme.BossReactorTrack, UITheme.BossReactorTrack);
        track.GetComponent<Image>().color = Color.white;
        track.GetComponent<Image>().raycastTarget = false;
        Stretch(track);

        var fillArea = FindOrCreateChild(sliderRT, "Fill Area");
        ClearImage(fillArea);
        Stretch(fillArea, FillInset);

        var fill = FindOrCreateChild(fillArea, "Fill");
        ApplyProcedural(fill.gameObject, false, UITheme.RadSm, 0, UITheme.BossReactorFill, UITheme.BossReactorFill);
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = Color.white;
        fillImg.raycastTarget = false;
        Stretch(fill); // 앵커는 Slider 가 value 에 맞춰 덮어쓴다

        var slider = sliderRT.GetComponent<Slider>();
        if (slider == null) slider = sliderRT.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.interactable = false; // 표시 전용 — 터치로 HP 를 끌 수 없게
        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.targetGraphic = null;
        slider.handleRect = null;
        slider.fillRect = fill;
        slider.value = 1f;

        // ── (2) WARNING 배너 ──
        var warnRoot = FindOrCreateChild(bossHud, "BossWarningRoot");
        ApplyProcedural(warnRoot.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.CardSurface, UITheme.Danger);
        var warnImg = warnRoot.GetComponent<Image>();
        warnImg.color = Color.white;
        warnImg.raycastTarget = false;
        TopStrip(warnRoot, UIHudLayout.WarnBandH, UITheme.S5, UIHudLayout.WarnBandY);
        if (warnRoot.GetComponent<UIBlink>() == null) warnRoot.gameObject.AddComponent<UIBlink>(); // CanvasGroup 은 RequireComponent 가 붙여준다

        var warnLabel = FindOrCreateLabel(warnRoot, "WarningLabel", font);
        StyleLabel(warnLabel, "WARNING", UITheme.Header, FontStyles.Bold, UITheme.Danger, TextAlignmentOptions.Center);
        TopStrip(warnLabel.rectTransform, UIHudLayout.WarnLabelH, UITheme.S4, -UITheme.S4);

        var warnSub = FindOrCreateLabel(warnRoot, "WarningSubLabel", font);
        StyleLabel(warnSub, "BOSS INCOMING", UITheme.Caption, FontStyles.Bold, UITheme.TextSecondary, TextAlignmentOptions.Center);
        TopStrip(warnSub.rectTransform, UIHudLayout.WarnSubH, UITheme.S4, -(UITheme.S4 + UIHudLayout.WarnLabelH + UITheme.S1));

        // ── (3) 로직 컴포넌트 배선 — 컨테이너에 붙이고 _root 는 자식을 가리킨다 ──
        var bossHpBar = bossHud.GetComponent<BossHpBar>();
        if (bossHpBar == null) bossHpBar = bossHud.gameObject.AddComponent<BossHpBar>();
        var hb = new SerializedObject(bossHpBar);
        WireReference(hb, "_root", hpRoot.gameObject);
        WireReference(hb, "_slider", slider);
        WireReference(hb, "_nameText", nameText);
        WireReference(hb, "_hpText", hpText);
        hb.ApplyModifiedProperties();

        var warningUI = bossHud.GetComponent<BossWarningUI>();
        if (warningUI == null) warningUI = bossHud.gameObject.AddComponent<BossWarningUI>();
        var wu = new SerializedObject(warningUI);
        WireReference(wu, "_root", warnRoot.gameObject);
        wu.ApplyModifiedProperties();

        // 잘못된 캔버스에 생긴 BossHud 제거 — 자기가 만드는 것만 치운다.
        // 그 위의 HudRoot 정리는 HudGenerator 담당(HpBar/ExpBar 대피를 그쪽이 하므로 여기서 지우면 안 됨).
        UIGenScene.PurgeStrays("BossHudGenerator", bossHud);

        // 보스전 전용 — 로직이 매 프레임 토글하므로 기본은 꺼둔 상태로 저장
        hpRoot.gameObject.SetActive(false);
        warnRoot.gameObject.SetActive(false);

        EditorUtility.SetDirty(bossHpBar);
        EditorUtility.SetDirty(warningUI);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = bossHud.gameObject;
        Debug.Log($"[BossHudGenerator] 보스 HUD 생성 + 배선 완료 (UIHudLayout 밴드 y={UIHudLayout.BossBandY}/{UIHudLayout.WarnBandY}). 필요 시 에디터에서 미세조정.");
    }

    // ── 헬퍼(HudGenerator 와 동일 관례) ──
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

    // 상단 스트레치 배치 — 좌우 sidePad 인셋, 위에서 y 만큼 내려온다.
    static void TopStrip(RectTransform rt, float height, float sidePad, float y)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-sidePad * 2f, height);
        rt.anchoredPosition = new Vector2(0f, y);
    }

    // 부모 행을 가로 비율로 나눠 채운다(좁은 캔버스에서도 이름/수치가 안 겹침).
    static void FracRow(RectTransform rt, float xMin, float xMax)
    {
        rt.anchorMin = new Vector2(xMin, 0f);
        rt.anchorMax = new Vector2(xMax, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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

    static void WireReference(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null && value != null)
            p.objectReferenceValue = value;
    }
}
#endif
