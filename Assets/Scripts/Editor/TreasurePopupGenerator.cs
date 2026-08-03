#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 레이어 3 — 보물상자 슬롯 룰렛 팝업(TIME VAULT)을 GameScene 에 코드로 생성/리스킨.
// 실행: Tools/UI/Build Treasure Popup
//
// 전략: 씬에 기존 오브젝트가 없으므로 이 생성기가 처음부터 만든다. 이름으로 기존 요소를 재사용해
//   재실행해도 계층이 늘지 않는다(idempotent). UI_TreasurePopup.cs 본문은 무수정 —
//   [SerializeField] 는 SerializedObject 로 배선한다.
// 절차 스프라이트는 전부 UIProceduralSprite 로 위임 — 씬에 sprite 를 직접 구우면 재시작 후 흰 박스가 된다.
//
// ⚠️ 계획서(§3-1)는 안쪽 9칸을 SetActive(false) 하라고 했지만 그대로 하면 5×5 격자가 무너진다.
//    GridLayoutGroup 은 비활성 자식을 배치 대상에서 아예 빼기 때문에 뒤 칸들이 앞으로 당겨진다.
//    → 오브젝트는 살려 두고 Image 만 끈다. CenterArt 가 그 위를 덮으므로 보이는 결과는 동일하다.
public static class TreasurePopupGenerator
{
    const string Tag = "TreasurePopupGenerator";
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";

    // ── 레이아웃 튜닝값 — GameScene 메인 Canvas 좌표계 기준 ──
    // 이 씬의 CanvasScaler 는 Reference 800×600 / match 0.5 라, 1080×1920 화면에서 캔버스 좌표계가
    // 약 520×924 다(Title/Result 씬의 1080×1920 과 다르다). 아래 값은 그 좌표계 기준이며
    // 기존 GameScene 생성기(HudGenerator / UpgradePanelGenerator)와 같은 스케일이다.
    const int GridSide = 5;
    const int SlotCount = 16;            // 테두리 칸 수. 런타임 상수를 참조하지 않는다(에디터→런타임 의존 금지)
    const float PopupWidthPct = 0.86f;   // UpgradePanelGenerator 와 동일 — 세로 화면에서 좌우가 안 잘린다
    const float HeaderH = 72f;
    const float ResultH = 64f;
    const float CellSize = 64f;
    const float CellSpacing = UITheme.S2;
    const float MarkerPad = UITheme.S1;  // 마커가 이웃 칸을 침범하지 않는 최대 여유(= spacing/2)
    const float EmblemSize = 120f;

    static readonly float RingSize = GridSide * CellSize + (GridSide - 1) * CellSpacing;
    static readonly float CenterSize = 3f * CellSize + 2f * CellSpacing;
    static readonly float PopupHeight =
        HeaderH + UITheme.S4 + RingSize + UITheme.S4 + ResultH + UITheme.S5;

    // 테두리 시계방향 순서(좌상단 → 우측 → 아래 → 좌측 → 위로)를 5×5 자식 인덱스로 옮긴 것.
    // 로직의 _cells/_cellLabels 배열 순서와 1:1 이어야 하므로 절대 바꾸지 않는다(계획서 §3-3).
    // 이 배열에 없는 9개(6,7,8,11,12,13,16,17,18)가 곧 CenterArt 가 덮는 안쪽 칸이다.
    static readonly int[] ClockwiseChild = { 0, 1, 2, 3, 4, 9, 14, 19, 24, 23, 22, 21, 20, 15, 10, 5 };

    // 값별 색 위계(0=20/40, 1=80, 2=150, 3=300). TreasureReward.Layout 과 순서는 같지만 참조하지는 않는다
    // — 에디터 코드가 런타임 로직에 의존하면 순환이 생긴다. 숫자 자체는 런타임이 채우므로
    //   이 배열이 어긋나도 표시 값이 틀리지는 않는다(색 위계만 어긋난다).
    static readonly int[] CellTier = { 0, 0, 1, 0, 2, 0, 0, 1, 0, 0, 3, 0, 1, 0, 0, 0 };

    // 마커는 칸 위에 겹치는 빈 프레임이라 안쪽이 비어야 한다(칸 숫자를 가리면 안 됨).
    static readonly Color MarkerHollow = new Color(UITheme.Gold.r, UITheme.Gold.g, UITheme.Gold.b, 0f);

    [MenuItem("Tools/UI/Build Treasure Popup")]
    public static void BuildTreasurePopup()
    {
        var canvas = UIGenScene.ResolveMainCanvas(Tag); // 전용 '@' 캔버스 오탐 방지
        if (canvas == null) return;
        var canvasRT = (RectTransform)canvas.transform;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[{Tag}] 폰트를 찾지 못했습니다: {FontPath} (기존 폰트 유지).");

        // ── 1) 루트 — 백드롭 딤 + 전체화면 스킵 버튼 ──
        var root = FindOrCreateChild(canvasRT, "TreasurePopup");
        root.gameObject.SetActive(true); // 생성 중에는 켜 둔다(레이아웃을 굽기 위해). 마지막에 다시 끈다
        Stretch(root);
        UILayerAssign.AssignLayer(root.gameObject, UILayer.Modal);

        var backdrop = root.GetComponent<Image>();
        backdrop.sprite = null;
        backdrop.type = Image.Type.Simple;
        backdrop.color = UITheme.Backdrop;
        backdrop.raycastTarget = true; // 뒤 클릭 차단 + 스킵 탭 수신

        var skip = root.GetComponent<Button>();
        if (skip == null) skip = root.gameObject.AddComponent<Button>();
        skip.targetGraphic = backdrop;
        skip.transition = Selectable.Transition.None; // 탭할 때 화면 전체 딤이 번쩍이면 안 된다

        var logic = root.GetComponent<UI_TreasurePopup>();
        if (logic == null) logic = root.gameObject.AddComponent<UI_TreasurePopup>();

        // ── 2) 팝업 본체(중앙 라운드 패널) ──
        var popup = FindOrCreateChild(root, "Popup");
        popup.gameObject.SetActive(true);
        ApplyRounded(popup.gameObject, true, UITheme.RadLg, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        var popupImg = popup.GetComponent<Image>();
        popupImg.color = Color.white;
        popupImg.raycastTarget = false; // 탭 스킵이 백드롭 버튼까지 통과해야 한다 — 팝업이 흡수하면 안 됨
        float side = (1f - PopupWidthPct) * 0.5f;
        popup.anchorMin = new Vector2(side, 0.5f);
        popup.anchorMax = new Vector2(1f - side, 0.5f);
        popup.pivot = new Vector2(0.5f, 0.5f);
        popup.anchoredPosition = Vector2.zero;
        popup.sizeDelta = new Vector2(0f, PopupHeight);

        if (popup.GetComponent<CanvasGroup>() == null) popup.gameObject.AddComponent<CanvasGroup>();
        if (popup.GetComponent<UIModalTween>() == null) popup.gameObject.AddComponent<UIModalTween>();

        // ── 3) 헤더바 + 타이틀 ──
        var header = FindOrCreateChild(popup, "HeaderBar");
        ApplyRounded(header.gameObject, false, UITheme.RadMd, 0, UITheme.PanelHeader, UITheme.PanelHeader);
        var headerImg = header.GetComponent<Image>();
        headerImg.color = Color.white;
        headerImg.raycastTarget = false;
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, HeaderH);
        header.anchoredPosition = Vector2.zero;

        var title = FindOrCreateLabel(header, "Title", font);
        title.text = "TIME VAULT";
        title.fontSize = UITheme.Header;
        title.fontStyle = FontStyles.Bold;
        title.color = UITheme.Gold;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        Stretch(title.rectTransform);

        // ── 4) Ring — 5×5 격자 컨테이너(그래픽 없음) ──
        var ring = FindOrCreateBare(popup, "Ring");
        ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 1f);
        ring.pivot = new Vector2(0.5f, 1f);
        ring.sizeDelta = new Vector2(RingSize, RingSize);
        ring.anchoredPosition = new Vector2(0f, -(HeaderH + UITheme.S4));

        var grid = ring.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = ring.gameObject.AddComponent<GridLayoutGroup>();
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = GridSide;
        grid.cellSize = new Vector2(CellSize, CellSize);
        grid.spacing = new Vector2(CellSpacing, CellSpacing);
        grid.padding = new RectOffset(0, 0, 0, 0);

        // ── 5) 25칸 생성 — 테두리 16칸만 스타일·라벨, 안쪽 9칸은 Image 만 끈다 ──
        var cellImages = new Image[SlotCount];
        var cellLabels = new TMP_Text[SlotCount];

        for (int child = 0; child < GridSide * GridSide; child++)
        {
            var cell = FindOrCreateChild(ring, $"Cell{child:00}");
            cell.SetSiblingIndex(child); // GridLayoutGroup 배치는 sibling 순서다 — 재실행해도 순서 고정
            cell.gameObject.SetActive(true);
            var img = cell.GetComponent<Image>();

            int slot = System.Array.IndexOf(ClockwiseChild, child);
            if (slot < 0)
            {
                // 안쪽 9칸 — 격자 자리는 유지하되 보이지 않게 한다(위를 CenterArt 가 덮는다)
                var ps = cell.GetComponent<UIProceduralSprite>();
                if (ps != null) Object.DestroyImmediate(ps);
                var staleLabel = cell.Find("Label");
                if (staleLabel != null) Object.DestroyImmediate(staleLabel.gameObject);
                img.sprite = null;
                img.color = Color.clear;
                img.raycastTarget = false;
                img.enabled = false;
                continue;
            }

            int tier = CellTier[slot];
            ApplyRounded(cell.gameObject, true, UITheme.RadSm, UITheme.OutlineWidth, TierFill(tier), TierLine(tier));
            img.enabled = true;
            img.color = Color.white;
            img.raycastTarget = false;

            var label = FindOrCreateLabel(cell, "Label", font);
            label.text = string.Empty; // 골드 숫자는 런타임이 TreasureReward.Layout 에서 채운다 — 표와 화면이 어긋날 수 없다
            label.fontSize = UITheme.Caption;
            label.fontStyle = FontStyles.Bold;
            label.color = TierText(tier);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            Stretch(label.rectTransform);

            cellImages[slot] = img;
            cellLabels[slot] = label;
        }

        // ── 6) Marker — Cell* 과 같은 부모(Ring)의 마지막 자식. 로직이 world position 으로 옮긴다 ──
        var marker = FindOrCreateChild(ring, "Marker");
        marker.SetAsLastSibling();
        marker.gameObject.SetActive(true);
        ApplyRounded(marker.gameObject, true, UITheme.RadSm, UITheme.OutlineWidth, MarkerHollow, UITheme.Gold);
        var markerImg = marker.GetComponent<Image>();
        markerImg.color = Color.white;
        markerImg.raycastTarget = false;
        marker.anchorMin = marker.anchorMax = marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = new Vector2(CellSize + MarkerPad * 2f, CellSize + MarkerPad * 2f);
        marker.anchoredPosition = Vector2.zero;

        var markerLE = marker.GetComponent<LayoutElement>();
        if (markerLE == null) markerLE = marker.gameObject.AddComponent<LayoutElement>();
        markerLE.ignoreLayout = true; // GridLayoutGroup 이 마커를 26번째 칸으로 배치하면 안 된다

        // ── 7) CenterArt — Ring 다음 형제라 안쪽 3×3 영역을 덮는다 ──
        var centerArt = FindOrCreateChild(popup, "CenterArt");
        centerArt.SetSiblingIndex(ring.GetSiblingIndex() + 1);
        ApplyRounded(centerArt.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.CardSurface, UITheme.Outline);
        var centerImg = centerArt.GetComponent<Image>();
        centerImg.color = Color.white;
        centerImg.raycastTarget = false;
        centerArt.anchorMin = centerArt.anchorMax = new Vector2(0.5f, 1f);
        centerArt.pivot = new Vector2(0.5f, 1f);
        centerArt.sizeDelta = new Vector2(CenterSize, CenterSize);
        centerArt.anchoredPosition = new Vector2(0f, -(HeaderH + UITheme.S4 + (RingSize - CenterSize) * 0.5f));

        var emblem = FindOrCreateChild(centerArt, "Emblem");
        var emblemSprite = emblem.GetComponent<UIProceduralSprite>();
        if (emblemSprite == null) emblemSprite = emblem.gameObject.AddComponent<UIProceduralSprite>();
        emblemSprite.ConfigureRing(Mathf.RoundToInt(EmblemSize * 0.5f), UITheme.OutlineWidth, UITheme.Gold);
        var emblemImg = emblem.GetComponent<Image>();
        emblemImg.color = Color.white;
        emblemImg.raycastTarget = false;
        emblem.anchorMin = emblem.anchorMax = emblem.pivot = new Vector2(0.5f, 0.5f);
        emblem.sizeDelta = new Vector2(EmblemSize, EmblemSize);
        emblem.anchoredPosition = Vector2.zero;

        var emblemLabel = FindOrCreateLabel(centerArt, "Label", font);
        emblemLabel.text = "VAULT";
        emblemLabel.fontSize = UITheme.Caption;
        emblemLabel.fontStyle = FontStyles.Bold;
        emblemLabel.color = UITheme.Gold;
        emblemLabel.alignment = TextAlignmentOptions.Center;
        emblemLabel.raycastTarget = false;
        Stretch(emblemLabel.rectTransform);

        // ── 8) ResultText — "+120 G" (초기 빈 문자열) ──
        var result = FindOrCreateLabel(popup, "ResultText", font);
        result.text = string.Empty;
        result.fontSize = UITheme.Display;
        result.fontStyle = FontStyles.Bold;
        result.color = UITheme.Gold;
        result.alignment = TextAlignmentOptions.Center;
        result.raycastTarget = false;
        var resultRT = result.rectTransform;
        resultRT.anchorMin = new Vector2(0f, 0f);
        resultRT.anchorMax = new Vector2(1f, 0f);
        resultRT.pivot = new Vector2(0.5f, 0f);
        resultRT.sizeDelta = new Vector2(-UITheme.S5 * 2f, ResultH);
        resultRT.anchoredPosition = new Vector2(0f, UITheme.S5);

        // 격자 좌표를 지금 구워 둔다. 이걸 안 하면 씬에 (0,0) 로 저장돼, 런타임 첫 프레임에
        // 마커가 모든 칸에서 같은 자리로 튄다(레이아웃 리빌드는 프레임 끝에야 돈다).
        LayoutRebuilder.ForceRebuildLayoutImmediate(popup);

        // ── 9) [SerializeField] 배선 (드래그 대체) ──
        var so = new SerializedObject(logic);
        SetRef(so, "_popupRoot", popup);
        SetArray(so.FindProperty("_cells"), cellImages);
        SetArray(so.FindProperty("_cellLabels"), cellLabels);
        SetRef(so, "_marker", marker);
        SetRef(so, "_resultText", result);
        SetRef(so, "_skipButton", skip);
        so.ApplyModifiedProperties();

        UIGenScene.PurgeStrays(Tag, root);

        root.gameObject.SetActive(false); // 팝업은 항상 비활성으로 저장한다 — 로직도 Start() 에서 한 번 더 강제한다

        EditorUtility.SetDirty(logic);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log($"[{Tag}] 보물상자 슬롯 팝업 생성 완료. ★ Ctrl+S 로 씬을 저장하세요 (저장하지 않으면 레이아웃이 되돌아갑니다).");
    }

    // ── 색 위계 ──
    static Color TierFill(int tier)
    {
        switch (tier)
        {
            case 1: return UITheme.PanelHeader;                                  // 80 — 살짝 밝게
            case 2: return Color.Lerp(UITheme.CardSurface, UITheme.Cyan, 0.35f); // 150
            case 3: return Color.Lerp(UITheme.CardSurface, UITheme.Gold, 0.55f); // 300 — 잭팟
            default: return UITheme.CardSurface;                                 // 20 / 40
        }
    }

    static Color TierLine(int tier)
    {
        switch (tier)
        {
            case 2: return UITheme.Cyan;
            case 3: return UITheme.Gold;
            default: return UITheme.Outline;
        }
    }

    static Color TierText(int tier)
    {
        switch (tier)
        {
            case 1: return UITheme.TextPrimary;
            case 2: return UITheme.TextPrimary;
            case 3: return UITheme.Outline; // 골드 면 위에는 어두운 글자가 읽힌다
            default: return UITheme.TextSecondary;
        }
    }

    // ── 헬퍼 ──
    // 스프라이트를 직접 굽지 않고 UIProceduralSprite 로 위임 — 씬에 구우면 재시작 후 흰 박스가 된다.
    static void ApplyRounded(GameObject go, bool outlined, int radius, int outlineWidth, Color fill, Color line)
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

    // 그래픽이 필요 없는 레이아웃 컨테이너(드로우콜 낭비 방지).
    static RectTransform FindOrCreateBare(RectTransform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            existing = (RectTransform)go.transform;
            existing.SetParent(parent, false);
            return existing;
        }
        var img = existing.GetComponent<Image>();
        if (img != null) Object.DestroyImmediate(img);
        return existing;
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

    static void SetRef(SerializedObject so, string field, Object value)
    {
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"[{Tag}] UI_TreasurePopup 에 '{field}' 필드가 없습니다 — 배선을 건너뜁니다.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    static void SetArray(SerializedProperty prop, Object[] values)
    {
        if (prop == null)
        {
            Debug.LogWarning($"[{Tag}] 배열 필드를 찾지 못해 배선을 건너뜁니다.");
            return;
        }
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
