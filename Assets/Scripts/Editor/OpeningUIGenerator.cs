using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 씬에 오프닝 오버레이 계층을 생성한다. 실행: Tools/UI/Build Opening Overlay
//
// 에이전트는 Unity 에디터를 드래그로 만질 수 없다(ui-kit SKILL.md 와 같은 전제).
// 그래서 UI 는 [MenuItem] 생성기로 만들고, 사용자는 그 결과 프리팹/계층을 손볼 수 있다.
// 이 방식은 워크트리 작업에도 유리하다 — 씬 파일(.unity)은 머지가 불가능한데,
// 생성기는 스크립트라 워크트리에서 안전하게 쓰고 씬 수정은 머지 후 한 번만 실행하면 된다.
//
// 좌표는 전부 앵커(0~1)로 잡는다. 타이틀 캔버스 기준 해상도가 1080x1920 이고 실제 화면은
// 540x960 이라 픽셀을 그대로 쓰면 배율에 묶인다.
// 폰트 크기만 예외로 캔버스 공간 값이라, 기획서의 540 기준 pt 를 2배로 환산한다.
public static class OpeningUIGenerator
{
    const string Tag = "OpeningUIGenerator";
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";
    const string DataPath = "Assets/Resources/Story/Opening.asset";
    const string RootName = "OpeningOverlay";

    // 540 기준 기획값 -> 캔버스(1080) 환산 = 2배
    const float BodySize    = UITheme.Body * 2f;      // 24pt -> 48
    const float SpeakerSize = UITheme.Caption * 2f;   // 20pt -> 40
    const float SkipSize    = UITheme.Caption * 2f;

    // 화면 하단 기준 정규화 y. 기획 레이아웃(540x960, 위에서 잰 값)을 아래 기준으로 뒤집은 값이다.
    const float ScrimTop    = 0.50f;   // 위에서 480
    const float SpeakerMinY = 0.300f;  // 위에서 672
    const float SpeakerMaxY = 0.333f;  // 위에서 640
    const float BodyMinY    = 0.156f;  // 위에서 810
    const float BodyMaxY    = 0.281f;  // 위에서 690
    const float DotsY       = 0.083f;  // 위에서 880

    // 표시등 발광 지름(캔버스 1080 기준). 화면 폭의 약 7% — 배경의 점광을 덮되
    // 조명처럼 번지지는 않는 크기다.
    const float BlinkSize   = 76f;

    // 상태줄 — 로비의 RecordCard(중심 y -60, 높이 220) 아래 빈 구간에 놓는다.
    // 캔버스 1080x1920 기준 BottomBar(높이 300)와 겹치지 않는 유일한 넓은 띠다.
    const float StatusY      = -230f;
    const float StatusWidth  = 900f;
    const float StatusHeight = 60f;

    [MenuItem("Tools/UI/Build Opening Overlay")]
    public static void Build()
    {
        var canvas = UIGenScene.ResolveMainCanvas(Tag);
        if (canvas == null) return;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[{Tag}] 폰트를 찾지 못했습니다: {FontPath} (기본 폰트 유지)");

        var data = AssetDatabase.LoadAssetAtPath<OpeningData>(DataPath);
        if (data == null)
            Debug.LogWarning($"[{Tag}] {DataPath} 가 없습니다. Tools/Story/Create Opening Assets 를 먼저 실행하세요");

        // 루트 — 화면 전체를 덮고 탭을 받는다.
        RectTransform root = FindOrCreate((RectTransform)canvas.transform, RootName);
        Stretch(root);
        root.SetAsLastSibling();   // 타이틀 UI 위에 덮는다. 기존 계층은 건드리지 않는다

        var group = Ensure<CanvasGroup>(root);
        group.alpha = 1f;
        group.blocksRaycasts = true;

        // 루트 자체가 히트 영역이다. 투명하지만 raycastTarget 이어야 탭이 잡힌다.
        var hit = Ensure<Image>(root);
        hit.color = UITheme.AppBg;      // 배경 이미지가 없을 때도 타이틀이 비쳐 보이지 않게
        hit.raycastTarget = true;

        // 배경 일러스트
        RectTransform bgRt = FindOrCreate(root, "Background");
        Stretch(bgRt);
        var bg = Ensure<Image>(bgRt);
        bg.color = Color.white;
        bg.preserveAspect = false;      // 세로 풀블리드. 9:16 로 생성하므로 왜곡되지 않는다
        bg.raycastTarget = false;
        var kenBurns = Ensure<KenBurns>(bgRt);   // 정지 배경에 느린 줌/팬을 준다

        // 표시등 발광 — 배경의 자식이라 줌/팬을 같이 받는다. 그래야 그림 위 제자리에 붙어 있다.
        RectTransform blinkRt = FindOrCreate(bgRt, "BlinkDot");
        blinkRt.anchorMin = blinkRt.anchorMax = new Vector2(0.5f, 0.66f);
        blinkRt.pivot = new Vector2(0.5f, 0.5f);
        blinkRt.anchoredPosition = Vector2.zero;
        blinkRt.sizeDelta = new Vector2(BlinkSize, BlinkSize);
        Ensure<Image>(blinkRt);
        var blink = Ensure<OpeningBlinkDot>(blinkRt);
        blinkRt.gameObject.SetActive(false);     // 페이지 데이터가 blink 를 켤 때만 나타난다

        // 스크림 — 하단 절반을 어둡게. 배경이 밝아도 텍스트가 읽히게 하는 2차 방어선
        RectTransform scrimRt = FindOrCreate(root, "Scrim");
        Anchor(scrimRt, 0f, 0f, 1f, ScrimTop);
        Ensure<Image>(scrimRt);
        Ensure<OpeningScrim>(scrimRt).Configure(0f, 0.88f);

        // 화자명
        TMP_Text speaker = FindOrCreateLabel(root, "Speaker", font);
        Anchor((RectTransform)speaker.transform, 0.074f, SpeakerMinY, 0.926f, SpeakerMaxY);
        Style(speaker, "ARC", SpeakerSize, FontStyles.Bold, UITheme.Cyan, TextAlignmentOptions.BottomLeft);

        // 본문 — 타자기가 여기에 찍는다
        TMP_Text body = FindOrCreateLabel(root, "Body", font);
        Anchor((RectTransform)body.transform, 0.074f, BodyMinY, 0.926f, BodyMaxY);
        Style(body, string.Empty, BodySize, FontStyles.Normal, UITheme.TextPrimary, TextAlignmentOptions.TopLeft);
        body.lineSpacing = 35f;   // 행간 1.35 — 24pt 한글은 기본 행간이 좁다
        // 아웃라인은 한글 획을 먹으므로 Underlay(그림자)로 대비를 만든다.
        // 머티리얼 키워드는 생성기에서 켜기 어려워 사용자가 인스펙터에서 확인하도록 남긴다.
        var typer = Ensure<TypewriterText>((RectTransform)body.transform);

        // 페이지 인디케이터 — 남은 길이가 보이면 스킵률이 내려간다
        RectTransform dotsRt = FindOrCreate(root, "PageDots");
        Anchor(dotsRt, 0.35f, DotsY - 0.012f, 0.65f, DotsY + 0.012f);
        var layout = Ensure<HorizontalLayoutGroup>(dotsRt);
        layout.spacing = UITheme.S3;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        int pageCount = data != null && data.pages != null ? data.pages.Length : 5;
        var dots = new Image[pageCount];
        for (int i = 0; i < pageCount; i++)
        {
            RectTransform dot = FindOrCreate(dotsRt, $"Dot{i}");
            dot.sizeDelta = new Vector2(16f, 16f);
            var le = Ensure<LayoutElement>(dot);
            le.preferredWidth = 16f;
            le.preferredHeight = 16f;
            var img = Ensure<Image>(dot);
            img.raycastTarget = false;
            Ensure<UIProceduralSprite>(dot).ConfigureCircle(8, Color.white);
            dots[i] = img;
        }

        // SKIP — 노치를 피해야 하므로 Safe Area 안에 둔다
        RectTransform safe = FindOrCreate(root, "SafeArea");
        Stretch(safe);
        Ensure<SafeAreaFitter>(safe);

        RectTransform skipRt = FindOrCreate(safe, "SkipButton");
        Anchor(skipRt, 0.74f, 0.925f, 0.96f, 0.975f);
        var skipImg = Ensure<Image>(skipRt);
        skipImg.color = new Color(1f, 1f, 1f, 0f);   // 히트 영역만. 시각적으로는 글자만 보인다
        skipImg.raycastTarget = true;
        var skipBtn = Ensure<Button>(skipRt);
        skipBtn.targetGraphic = skipImg;

        TMP_Text skipLabel = FindOrCreateLabel(skipRt, "Label", font);
        Stretch((RectTransform)skipLabel.transform);
        Style(skipLabel, "SKIP", SkipSize, FontStyles.Bold, UITheme.TextSecondary, TextAlignmentOptions.Center);

        // 상태줄 — 오프닝이 끝난 뒤 타이틀에 남는 한 줄.
        // 오버레이 안이 아니라 로비 계층에 둔다(오버레이는 재생이 끝나면 통째로 꺼진다).
        var lobby = canvas.transform.Find("TitleLobby") as RectTransform;
        RectTransform statusParent = lobby != null ? lobby : (RectTransform)canvas.transform;
        TMP_Text status = FindOrCreateLabel(statusParent, "OpeningStatusLine", font);
        var statusRt = (RectTransform)status.transform;
        statusRt.anchorMin = statusRt.anchorMax = new Vector2(0.5f, 0.5f);
        statusRt.pivot = new Vector2(0.5f, 0.5f);
        statusRt.anchoredPosition = new Vector2(0f, StatusY);
        statusRt.sizeDelta = new Vector2(StatusWidth, StatusHeight);
        Style(status, "기동 대기. 버퍼 5초.", SpeakerSize, FontStyles.Normal,
              UITheme.TextSecondary, TextAlignmentOptions.Center);

        // 시퀀스 배선
        var seq = Ensure<OpeningSequence>(root);
        var so = new SerializedObject(seq);
        so.FindProperty("_data").objectReferenceValue = data;
        so.FindProperty("_background").objectReferenceValue = bg;
        so.FindProperty("_speakerLabel").objectReferenceValue = speaker;
        so.FindProperty("_body").objectReferenceValue = typer;
        so.FindProperty("_skipButton").objectReferenceValue = skipBtn;
        so.FindProperty("_kenBurns").objectReferenceValue = kenBurns;
        so.FindProperty("_blinkDot").objectReferenceValue = blink;
        var dotsProp = so.FindProperty("_pageDots");
        dotsProp.arraySize = dots.Length;
        for (int i = 0; i < dots.Length; i++)
            dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        // 타이틀 배선 — 에이전트가 인스펙터를 드래그할 수 없으므로 참조도 코드로 건다.
        var title = UnityEngine.Object.FindFirstObjectByType<TitleScene>(FindObjectsInactive.Include);
        if (title != null)
        {
            var tso = new SerializedObject(title);
            tso.FindProperty("_opening").objectReferenceValue = seq;
            tso.FindProperty("_statusText").objectReferenceValue = status;
            tso.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"[{Tag}] 씬에서 TitleScene 을 찾지 못했습니다. " +
                             "_opening / _statusText 를 인스펙터에서 직접 연결하세요");
        }

        // 평소에는 꺼둔다. TitleScene 이 최초 1회 판정 후 켠다.
        root.gameObject.SetActive(false);

        UIGenScene.PurgeStrays(Tag, root);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log($"[{Tag}] 오프닝 오버레이 생성 완료 ({pageCount}페이지). " +
                  $"상태줄은 '{statusParent.name}/OpeningStatusLine' 에 만들었습니다. " +
                  "씬을 저장하세요. Body 의 TMP Underlay 는 인스펙터에서 확인 필요");
    }

    // --- 헬퍼 (기존 생성기들과 같은 형태) ---

    static RectTransform FindOrCreate(RectTransform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null) return existing;

        var go = new GameObject(name, typeof(RectTransform));
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

    static T Ensure<T>(Component target) where T : Component
    {
        var c = target.GetComponent<T>();
        return c != null ? c : target.gameObject.AddComponent<T>();
    }

    static void Style(TMP_Text t, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
    }

    static void Stretch(RectTransform rt) => Anchor(rt, 0f, 0f, 1f, 1f);

    static void Anchor(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
