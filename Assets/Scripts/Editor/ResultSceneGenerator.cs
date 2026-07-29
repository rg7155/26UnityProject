#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 결과 화면을 모바일 세로 카드형으로 in-place 리스킨. 실행: Tools/UI/Build Result Screen
//
// 전략(타이틀/업그레이드와 동일): 파괴/재생성 금지. ResultScene 의 기존 오브젝트
//   (_retryButton/_titleButton/_scoreText/_timeText/_bestScoreText/_bestTimeText/_goldText)를
//   재부모화·리스킨하고, 신규 장식(타이틀 라벨·카드·디바이더)만 생성.
// 절차 스프라이트는 UIProceduralSprite 로 위임(프리팹/씬에 sprite 직접 굽지 않음 → 흰 박스 방지).
// ResultScene.cs 본문은 무수정 — 필드는 SerializedObject 로만 참조, .text 는 건드리지 않는다.
// in-place · idempotent(이름 재사용).
public static class ResultSceneGenerator
{
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";

    // ── 세로 튜닝값(스크린샷 피드백으로 조정) — Reference 1080×1920 기준 ──
    // Title/Card 는 top-pivot 앵커로 위→아래 순서 배치(카드 높이가 내용에 따라 변해도 안 겹침).
    const float TitleTopY = 180f;
    const float TitleHeight = 100f;
    const float CardTopGap = UITheme.S6;
    const float CardWidth = 760f;
    const float DividerH = 3f;
    const float StatRowH = 64f;
    const float BottomBarH = 320f;
    const float RetryButtonH = 128f;
    const float TitleButtonH = 96f;

    [MenuItem("Tools/UI/Build Result Screen")]
    public static void BuildResultScreen()
    {
        var result = Object.FindFirstObjectByType<ResultScene>(FindObjectsInactive.Include);
        if (result == null)
        {
            Debug.LogError("[ResultSceneGenerator] 씬에서 ResultScene 을 찾지 못했습니다. Result 씬을 연 상태로 실행하세요.");
            return;
        }
        var canvas = UIGenScene.ResolveMainCanvas("ResultSceneGenerator"); // 전용 '@' 캔버스 오탐 방지
        if (canvas == null) return;
        var canvasRT = (RectTransform)canvas.transform;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[ResultSceneGenerator] 폰트를 찾지 못했습니다: {FontPath} (기존 폰트 유지).");

        var so = new SerializedObject(result);
        var retryButton = so.FindProperty("_retryButton").objectReferenceValue as Button;
        var titleButton = so.FindProperty("_titleButton").objectReferenceValue as Button;
        var scoreText = so.FindProperty("_scoreText").objectReferenceValue as TMP_Text;
        var timeText = so.FindProperty("_timeText").objectReferenceValue as TMP_Text;
        var bestScoreText = so.FindProperty("_bestScoreText").objectReferenceValue as TMP_Text;
        var bestTimeText = so.FindProperty("_bestTimeText").objectReferenceValue as TMP_Text;
        var goldText = so.FindProperty("_goldText").objectReferenceValue as TMP_Text;

        // ── ResultRoot 컨테이너(투명, 풀스크린) ──
        // 중복 방지: Canvas 하위에 "ResultRoot"가 여러 개면(이전 실행 잔여) 하나로 합친다.
        // 버튼/텍스트는 ResultScene 의 SerializeField 가 오브젝트 자체를 물고 있으므로,
        // 중복 파괴 전에 그 아래 있으면 먼저 canonical 로 재부모화해 참조를 보존한다.
        var preserved = new Object[] { retryButton, titleButton, scoreText, timeText, bestScoreText, bestTimeText, goldText };
        var resultRoot = DeduplicateByName(canvasRT, "ResultRoot", preserved);
        if (resultRoot.GetComponent<Image>() == null) resultRoot.gameObject.AddComponent<Image>();
        ClearImage(resultRoot);
        Stretch(resultRoot);
        UILayerAssign.AssignLayer(resultRoot.gameObject, UILayer.Hud);

        // ── SafeArea 컨테이너(노치 회피) ──
        var safeArea = FindOrCreateChild(resultRoot, "SafeArea");
        ClearImage(safeArea);
        safeArea.anchorMin = Vector2.zero;
        safeArea.anchorMax = Vector2.one;
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
        if (safeArea.GetComponent<SafeAreaFitter>() == null) safeArea.gameObject.AddComponent<SafeAreaFitter>();

        // ── Title "RESULT" — SafeArea 상단, 카드보다 위(top-pivot 앵커로 겹침 방지) ──
        var title = FindOrCreateLabel(safeArea, "Title", font);
        StyleLabel(title, "RESULT", UITheme.Display, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
        var titleRT = title.rectTransform;
        titleRT.anchorMin = titleRT.anchorMax = titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(900f, TitleHeight);
        titleRT.anchoredPosition = new Vector2(0f, -TitleTopY);

        // ── ResultCard(내용에 hug 하는 라운드 카드) — Title 바로 아래, top-pivot 앵커 ──
        // 옛 버전이 만든 빈 "Stack" 컨테이너가 있으면 자식을 카드로 끌어올리고 제거(idempotent 정리).
        var staleStack = safeArea.Find("ResultCard/Stack") as RectTransform;
        if (staleStack != null)
        {
            var stragglers = new List<Transform>();
            foreach (Transform child in staleStack) stragglers.Add(child);
            foreach (var child in stragglers) child.SetParent(staleStack.parent, false);
            Object.DestroyImmediate(staleStack.gameObject);
        }

        var card = FindOrCreateChild(safeArea, "ResultCard");
        ApplyProcedural(card.gameObject, true, UITheme.RadLg, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        card.GetComponent<Image>().color = Color.white;
        card.GetComponent<Image>().raycastTarget = false;
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 1f);
        card.sizeDelta = new Vector2(CardWidth, 0f); // 높이는 ContentSizeFitter 가 내용에 맞춰 계산
        card.anchoredPosition = new Vector2(0f, -(TitleTopY + TitleHeight + CardTopGap));

        // 카드 자체가 세로 스택 + 내용 높이에 hug — 별도 Stack 컨테이너 없이 직접 자식으로 배치.
        var vlg = card.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset((int)UITheme.S5, (int)UITheme.S5, (int)UITheme.S5, (int)UITheme.S5);
        vlg.spacing = UITheme.S3;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = card.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = card.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── 이번 판(강조) ──
        StatRow(scoreText, card, font, UITheme.Header, FontStyles.Bold, UITheme.TextPrimary);
        StatRow(timeText, card, font, UITheme.Header, FontStyles.Bold, UITheme.TextPrimary);
        StatRow(goldText, card, font, UITheme.Header, FontStyles.Bold, UITheme.Gold);

        // ── Divider(얇은 선 — LayoutElement 로 높이 고정, flexibleHeight=0 으로 확장 방지) ──
        var divider = FindOrCreateChild(card, "Divider");
        ClearImage(divider);
        divider.GetComponent<Image>().color = UITheme.Divider;
        divider.GetComponent<Image>().raycastTarget = false;
        var dividerLe = divider.GetComponent<LayoutElement>();
        if (dividerLe == null) dividerLe = divider.gameObject.AddComponent<LayoutElement>();
        dividerLe.minHeight = DividerH;
        dividerLe.preferredHeight = DividerH;
        dividerLe.flexibleHeight = 0f;
        dividerLe.flexibleWidth = 0f;

        // ── 최고 기록(보조) ──
        StatRow(bestScoreText, card, font, UITheme.Caption, FontStyles.Normal, UITheme.TextSecondary);
        StatRow(bestTimeText, card, font, UITheme.Caption, FontStyles.Normal, UITheme.TextSecondary);

        // ── BottomBar ──
        var bottom = FindOrCreateChild(safeArea, "BottomBar");
        ClearImage(bottom);
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.sizeDelta = new Vector2(0f, BottomBarH);
        bottom.anchoredPosition = Vector2.zero;

        if (retryButton != null)
        {
            var retryRT = (RectTransform)retryButton.transform;
            retryRT.SetParent(bottom, false);
            ApplyProcedural(retryButton.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.Accent, UITheme.Outline);
            var rimg = retryButton.GetComponent<Image>();
            if (rimg != null) rimg.color = Color.white;
            UIBuild.ApplyCtaColors(retryButton);
            retryRT.anchorMin = new Vector2(0f, 0f);
            retryRT.anchorMax = new Vector2(1f, 0f);
            retryRT.pivot = new Vector2(0.5f, 0f);
            retryRT.sizeDelta = new Vector2(-UITheme.S5 * 2f, RetryButtonH);
            retryRT.anchoredPosition = new Vector2(0f, UITheme.S6 + TitleButtonH + UITheme.S3);
            var retryLabel = retryButton.GetComponentInChildren<TMP_Text>(true);
            if (retryLabel != null)
            {
                StyleLabel(retryLabel, "RETRY", UITheme.Button, FontStyles.Bold, UITheme.Outline, TextAlignmentOptions.Center);
                Stretch(retryLabel.rectTransform);
            }
        }

        if (titleButton != null)
        {
            var titleBtnRT = (RectTransform)titleButton.transform;
            titleBtnRT.SetParent(bottom, false);
            ApplyProcedural(titleButton.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
            var timg = titleButton.GetComponent<Image>();
            if (timg != null) timg.color = Color.white;
            var colors = titleButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = UITheme.Divider;
            colors.selectedColor = Color.white;
            colors.disabledColor = UITheme.TextDisabled;
            colors.fadeDuration = UITheme.FadeDuration;
            titleButton.colors = colors;
            titleBtnRT.anchorMin = new Vector2(0f, 0f);
            titleBtnRT.anchorMax = new Vector2(1f, 0f);
            titleBtnRT.pivot = new Vector2(0.5f, 0f);
            titleBtnRT.sizeDelta = new Vector2(-UITheme.S5 * 2f, TitleButtonH);
            titleBtnRT.anchoredPosition = new Vector2(0f, UITheme.S6);
            var titleLabel = titleButton.GetComponentInChildren<TMP_Text>(true);
            if (titleLabel != null)
            {
                StyleLabel(titleLabel, "TITLE", UITheme.Button, FontStyles.Bold, UITheme.TextPrimary, TextAlignmentOptions.Center);
                Stretch(titleLabel.rectTransform);
            }
        }

        // ── CanvasScaler 세로 세팅 (이미 설정된 씬은 건드리지 않음 — 씬 고유 기준 보존) ──
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        EditorUtility.SetDirty(result);
        EditorSceneManager.MarkSceneDirty(result.gameObject.scene);
        Selection.activeGameObject = resultRoot.gameObject;
        Debug.Log("[ResultSceneGenerator] 결과 화면 세로 카드 리스킨 완료. 필요 시 에디터에서 미세조정.");
    }

    // 통계 라벨 하나를 스택 자식으로 재부모화 + 리스킨(.text 는 건드리지 않음).
    static void StatRow(TMP_Text label, RectTransform stackParent, TMP_FontAsset font, float size, FontStyles style, Color color)
    {
        if (label == null) return;
        var rt = label.rectTransform;
        rt.SetParent(stackParent, false);
        if (font != null) label.font = font;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        var le = label.GetComponent<LayoutElement>();
        if (le == null) le = label.gameObject.AddComponent<LayoutElement>();
        le.minHeight = StatRowH;
        le.preferredHeight = StatRowH;
        le.flexibleHeight = 0f;
    }

    // ── 헬퍼 ──
    // 스프라이트를 직접 굽지 않고 UIProceduralSprite 컴포넌트로 위임 — 저장 후에도 살아남는다.
    static void ApplyProcedural(GameObject go, bool outlined, int radius, int outlineWidth, Color fill, Color line)
    {
        var ps = go.GetComponent<UIProceduralSprite>();
        if (ps == null) ps = go.AddComponent<UIProceduralSprite>();
        ps.Configure(outlined, radius, outlineWidth, fill, line);
    }

    // 이름이 같은 직속 자식이 여러 개(이전 실행 잔여)면 첫 번째를 canonical 로 남기고 나머지는 파괴.
    // 파괴 전 preserve 목록(ResultScene 이 SerializeField 로 물고 있는 오브젝트)이 그 아래 있으면
    // canonical 로 먼저 재부모화해 참조가 끊기지 않게 한다.
    static RectTransform DeduplicateByName(RectTransform rootParent, string name, Object[] preserve)
    {
        // 1) 보존 대상(버튼/텍스트)을 먼저 루트로 대피 — 어떤 ResultRoot 안(중첩 포함)에 있든
        //    이후 파괴에서 살린다. 이 순서가 핵심: 파괴 전에 무조건 빼낸다.
        foreach (var obj in preserve)
        {
            var t = (obj as Component)?.transform;
            if (t != null) t.SetParent(rootParent, false);
        }

        // 2) 같은 이름(중첩 잔여 포함) 전부 파괴. 바깥부터 파괴하면 안쪽은 함께 사라진다.
        var matches = new List<RectTransform>();
        foreach (var rt in rootParent.GetComponentsInChildren<RectTransform>(true))
            if (rt != rootParent && rt.name == name) matches.Add(rt);
        foreach (var m in matches)
            if (m != null) Object.DestroyImmediate(m.gameObject);

        // 3) 깨끗한 새 컨테이너 하나 생성.
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var created = (RectTransform)go.transform;
        created.SetParent(rootParent, false);
        return created;
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

    static void Stretch(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }
}
#endif
