#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 레이어 3 — 상점 UI 를 코드로 in-place 리스킨. 실행: Tools/UI/Build Shop Popup
//
// 전략: 파괴 후 재생성 금지. 열린 Title 씬에서 기존 ShopPanel(UI_ShopPanel)을 찾아
//   1) ShopPanel(전체화면·토글 대상) 자체를 백드롭 딤으로,
//   2) 중앙 라운드 팝업(ShopPopup)을 만들고 기존 자식(타이틀/골드/닫기/스크롤)을 그 안으로 재부모화,
//   3) Content 의 VerticalLayoutGroup 을 3열 GridLayoutGroup 으로 교체,
//   4) 셀 프리팹을 라운드 카드로 리스킨.
// 재부모화는 오브젝트 참조를 유지하므로 [SerializeField] 배선이 끊기지 않는다.
// 재실행해도 이름으로 기존 요소를 재사용해 중복 생성하지 않는다(idempotent).
public static class ShopUIGenerator
{
    const string FontPath = "Assets/Font/NotoSansKR-VariableFont_wght SDF.asset";
    const string CellPrefabPath = "Assets/Prefabs/ShopItemCell.prefab";

    // ── 레이아웃 튜닝값(스크린샷 피드백으로 조정) — CanvasScaler Reference 1080x1920 기준 ──
    static readonly Vector2 PopupSize = new Vector2(960f, 1320f);
    const float HeaderH = 110f;
    static readonly Vector2 GoldChipSize = new Vector2(240f, 64f);
    const float CloseSize = 80f;
    static readonly Vector2 CellSize = new Vector2(280f, 270f);

    [MenuItem("Tools/UI/Build Shop Popup")]
    public static void BuildShopPopup()
    {
        var panel = Object.FindFirstObjectByType<UI_ShopPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogError("[ShopUIGenerator] 씬에서 UI_ShopPanel 을 찾지 못했습니다. Title 씬을 연 상태로 실행하세요.");
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[ShopUIGenerator] 폰트를 찾지 못했습니다: {FontPath} (기존 폰트 유지).");

        var panelRT = (RectTransform)panel.transform;

        UILayerAssign.AssignLayer(panel.gameObject, UILayer.Modal); // 상점 모달은 항상 최상단 레이어

        // 기존 배선 참조 읽기(무수정 로직의 [SerializeField]) — 이 오브젝트들을 리스킨/재부모화한다.
        var so = new SerializedObject(panel);
        var goldText = so.FindProperty("_goldText").objectReferenceValue as TMP_Text;
        var closeButton = so.FindProperty("_closeButton").objectReferenceValue as Button;
        var cellContainer = so.FindProperty("_cellContainer").objectReferenceValue as Transform;

        // ── 1) 백드롭 딤: 전용 풀스크린 Dim 자식(항상 최하단). ShopPanel 자체 Image 는 투명 처리해 간섭 제거 ──
        var panelImg = panel.GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.color = Color.clear;
            panelImg.raycastTarget = false;
        }
        var dim = FindOrCreateChild(panelRT, "ShopDim");
        var dimImg = dim.GetComponent<Image>();
        dimImg.sprite = null;
        dimImg.type = Image.Type.Simple;
        dimImg.color = UITheme.Backdrop; // 반투명 어두운 오버레이
        dimImg.raycastTarget = true;     // 뒤 클릭 차단
        UIBuild.Stretch(dim);

        // ── 2) 팝업 본체(중앙 라운드 패널) ──
        var popup = FindOrCreateChild(panelRT, "ShopPopup");
        var popupImg = popup.GetComponent<Image>();
        ApplyProcedural(popup.gameObject, true, UITheme.RadLg, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        popupImg.color = Color.white;
        popupImg.raycastTarget = true;
        Center(popup, PopupSize);

        // ── 3) 헤더바 ──
        var header = FindOrCreateChild(popup, "HeaderBar");
        var headerImg = header.GetComponent<Image>();
        ApplyProcedural(header.gameObject, false, UITheme.RadMd, 0, UITheme.PanelHeader, UITheme.PanelHeader);
        headerImg.color = Color.white;
        headerImg.raycastTarget = false;
        TopStrip(header, HeaderH);

        // 타이틀(기존 ShopText 재사용 후 재부모화)
        var title = FindTmpByName(panel, "ShopText");
        if (title == null) title = UIBuild.Label(header, "상점", UITheme.Header, UITheme.TextPrimary, font, TextAlignmentOptions.Left);
        title.rectTransform.SetParent(header, false);
        title.text = "상점";
        title.font = font != null ? font : title.font;
        title.fontSize = UITheme.Header;
        title.fontStyle = FontStyles.Bold;
        title.color = UITheme.TextPrimary;
        title.alignment = TextAlignmentOptions.Left;
        title.raycastTarget = false;
        // 우측 예약 폭: X 버튼 코너 존 → 칩 여백 → 칩 폭 → 타이틀 여백
        float closeZone = CloseSize + UITheme.S3;              // X 버튼이 차지하는 우측 폭
        float chipRightInset = closeZone + UITheme.S4;          // 칩 우측 여백(X 와 최소 S4)
        float titleRightInset = chipRightInset + GoldChipSize.x + UITheme.S4;
        UIBuild.Stretch(title.rectTransform);
        title.rectTransform.offsetMin = new Vector2(UITheme.S5, 0f);
        title.rectTransform.offsetMax = new Vector2(-titleRightInset, 0f);

        // ── 4) 골드칩(헤더 우측) + goldText 재부모화 ──
        var chip = FindOrCreateChild(header, "GoldChip");
        var chipImg = chip.GetComponent<Image>();
        ApplyProcedural(chip.gameObject, false, UITheme.RadSm, 0, UITheme.CardSurface, UITheme.CardSurface);
        chipImg.color = Color.white;
        chipImg.raycastTarget = false;
        chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(1f, 0.5f);
        chip.sizeDelta = GoldChipSize;
        chip.anchoredPosition = new Vector2(-chipRightInset, 0f); // X 버튼 왼쪽으로 S4 여백 확보
        if (goldText != null)
        {
            goldText.rectTransform.SetParent(chip, false);
            goldText.font = font != null ? font : goldText.font;
            goldText.fontSize = UITheme.Caption;
            goldText.fontStyle = FontStyles.Bold;
            goldText.color = UITheme.Gold;
            goldText.alignment = TextAlignmentOptions.Center;
            goldText.raycastTarget = false;
            UIBuild.Stretch(goldText.rectTransform, UITheme.S2);
        }

        // ── 5) 닫기버튼(팝업 우상단, 리스킨) ──
        if (closeButton != null)
        {
            var closeRT = (RectTransform)closeButton.transform;
            closeRT.SetParent(popup, false);
            closeRT.anchorMin = closeRT.anchorMax = closeRT.pivot = new Vector2(1f, 1f);
            closeRT.sizeDelta = new Vector2(CloseSize, CloseSize);
            closeRT.anchoredPosition = new Vector2(-UITheme.S3, -UITheme.S3);
            var closeImg = closeButton.GetComponent<Image>();
            if (closeImg != null)
            {
                ApplyProcedural(closeButton.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.Danger, UITheme.Outline);
                closeImg.color = Color.white;
            }
            UIBuild.ApplyCtaColors(closeButton);
            var closeLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (closeLabel != null)
            {
                closeLabel.text = "X";
                closeLabel.font = font != null ? font : closeLabel.font;
                closeLabel.fontSize = UITheme.Button;
                closeLabel.fontStyle = FontStyles.Bold;
                closeLabel.color = UITheme.TextPrimary;
                closeLabel.alignment = TextAlignmentOptions.Center;
                closeLabel.raycastTarget = false;
                UIBuild.Stretch(closeLabel.rectTransform);
            }
        }

        // ── 6) 그리드(3열) + 스크롤 재부모화 ──
        if (cellContainer != null)
        {
            var vlg = cellContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) Object.DestroyImmediate(vlg);
            var grid = cellContainer.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = cellContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = CellSize;
            grid.spacing = new Vector2(UITheme.S2, UITheme.S2);
            grid.padding = new RectOffset((int)UITheme.S4, (int)UITheme.S4, (int)UITheme.S4, (int)UITheme.S4);
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = cellContainer.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            var contentImg = cellContainer.GetComponent<Image>();
            if (contentImg != null) contentImg.color = Color.clear; // 그리드 배경 회색 제거

            var scroll = cellContainer.GetComponentInParent<ScrollRect>(true);
            if (scroll != null)
            {
                scroll.horizontal = false; // 세로 전용
                scroll.vertical = true;
                if (scroll.horizontalScrollbar != null)
                {
                    scroll.horizontalScrollbar.gameObject.SetActive(false);
                    scroll.horizontalScrollbar = null;
                }
                // 스크롤 영역 회색 제거 — ScrollRect 자체 배경 Image 를 투명 처리(팝업 다크 톤이 비치게).
                // Viewport Image 는 Mask 그래픽(showMaskGraphic=0)이라 이미 안 보이고, 알파를 건드리면
                // 클리핑이 깨질 수 있으므로 손대지 않는다.
                var scrollImg = scroll.GetComponent<Image>();
                if (scrollImg != null) scrollImg.color = Color.clear; // raycastTarget 유지 → 빈 영역 드래그 스크롤 가능
                var scrollRT = (RectTransform)scroll.transform;
                scrollRT.SetParent(popup, false);
                scrollRT.anchorMin = Vector2.zero;
                scrollRT.anchorMax = Vector2.one;
                scrollRT.pivot = new Vector2(0.5f, 0.5f);
                scrollRT.offsetMin = new Vector2(UITheme.S5, UITheme.S5);
                scrollRT.offsetMax = new Vector2(-UITheme.S5, -(HeaderH + UITheme.S4));
            }
        }

        // ── 7) 참조 배선(이미 채워져 있으면 보존, null 일 때만) ──
        WireIfNull(so, "_goldText", goldText);
        WireIfNull(so, "_closeButton", closeButton);
        WireIfNull(so, "_cellContainer", cellContainer);
        so.ApplyModifiedProperties();

        ReskinCellPrefab(font);

        dim.SetAsFirstSibling(); // 딤은 항상 최하단(팝업보다 뒤)

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Selection.activeGameObject = popup.gameObject;
        Debug.Log("[ShopUIGenerator] 상점 팝업 리스킨 완료. 필요 시 에디터에서 미세조정.");
    }

    // ── 셀 프리팹 리스킨(라운드 카드 + 장식 아이콘 슬롯 + 장식 골드 스트립) ──
    static void ReskinCellPrefab(TMP_FontAsset font)
    {
        var root = PrefabUtility.LoadPrefabContents(CellPrefabPath);
        if (root == null)
        {
            Debug.LogWarning($"[ShopUIGenerator] 셀 프리팹을 찾지 못했습니다: {CellPrefabPath}");
            return;
        }
        try
        {
            var cell = root.GetComponent<UI_ShopItemCell>();
            if (cell == null) { Debug.LogWarning("[ShopUIGenerator] 셀 프리팹에 UI_ShopItemCell 이 없습니다."); return; }

            var cso = new SerializedObject(cell);
            var button = cso.FindProperty("_button").objectReferenceValue as Button;
            var label = cso.FindProperty("_label").objectReferenceValue as TMP_Text;
            var rootRT = (RectTransform)root.transform;
            rootRT.sizeDelta = CellSize;

            // 카드 배경 = _button Image
            if (button != null)
            {
                var cardImg = button.GetComponent<Image>();
                if (cardImg != null)
                {
                    ApplyProcedural(button.gameObject, true, UITheme.RadMd, UITheme.OutlineWidth, UITheme.CardSurface, UITheme.Outline);
                    cardImg.color = Color.white;
                }
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = UITheme.Divider;
                colors.selectedColor = Color.white;
                colors.disabledColor = UITheme.TextDisabled;
                colors.fadeDuration = UITheme.FadeDuration;
                button.colors = colors;
            }

            const float iconH = 56f;
            const float stripH = 44f;   // 가격 텍스트를 담을 만큼 — 빈 장식 띠였던 것을 실제 가격표로

            // 장식 아이콘 슬롯(상단 중앙) — 데이터에 아이콘 없음, 순수 장식
            var icon = FindOrCreateChild(rootRT, "IconSlot");
            var iconImg = icon.GetComponent<Image>();
            ApplyProcedural(icon.gameObject, false, UITheme.RadSm, 0, UITheme.PanelBase, UITheme.PanelBase);
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 1f);
            icon.sizeDelta = new Vector2(iconH, iconH);
            icon.anchoredPosition = new Vector2(0f, -UITheme.S2);

            // 골드 스트립(하단) — 가격표. 카드 전체가 구매 버튼이므로 여기에 별도 Button 은 두지 않는다
            var strip = FindOrCreateChild(rootRT, "PriceStrip");
            var stripImg = strip.GetComponent<Image>();
            ApplyProcedural(strip.gameObject, false, UITheme.RadSm, 0, UITheme.Accent, UITheme.Accent);
            stripImg.color = Color.white;
            stripImg.raycastTarget = false;
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 0f);
            strip.sizeDelta = new Vector2(-UITheme.S3 * 2f, stripH);
            strip.anchoredPosition = new Vector2(0f, UITheme.S2);

            // 가격 텍스트 — 골드 배경 위라 어두운 글자. UI_ShopItemCell._priceLabel 로 배선
            // Image 를 붙이는 FindOrCreateChild 를 쓰지 않는다 — 한 오브젝트에 Graphic 은 하나만
            var priceTr = strip.Find("PriceLabel") as RectTransform;
            if (priceTr == null)
            {
                var pgo = new GameObject("PriceLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                priceTr = (RectTransform)pgo.transform;
                priceTr.SetParent(strip, false);
            }
            var priceRT = priceTr;
            var priceLabel = priceRT.GetComponent<TMP_Text>();
            priceLabel.font = font != null ? font : priceLabel.font;
            priceLabel.fontSize = UITheme.Button;
            priceLabel.fontStyle = FontStyles.Bold;
            priceLabel.color = UITheme.Outline;
            priceLabel.alignment = TextAlignmentOptions.Center;
            priceLabel.raycastTarget = false;
            priceRT.anchorMin = Vector2.zero;
            priceRT.anchorMax = Vector2.one;
            priceRT.offsetMin = Vector2.zero;
            priceRT.offsetMax = Vector2.zero;

            // 라벨(아이콘 슬롯과 스트립 사이로 인셋)
            if (label != null)
            {
                label.font = font != null ? font : label.font;
                label.fontSize = UITheme.Body;
                label.color = UITheme.TextPrimary;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.raycastTarget = false;
                var lrt = label.rectTransform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(UITheme.S2, stripH + UITheme.S2 * 2f);
                lrt.offsetMax = new Vector2(-UITheme.S2, -(iconH + UITheme.S2 * 2f));
                label.transform.SetAsLastSibling(); // 장식 위에 텍스트
            }

            cso.FindProperty("_priceLabel").objectReferenceValue = priceLabel;
            cso.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ── 헬퍼 ──
    // 스프라이트를 직접 굽지 않고 UIProceduralSprite 컴포넌트로 위임 — 저장 후 Instantiate 에서도 살아남는다.
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

    static TMP_Text FindTmpByName(Component root, string namePart)
    {
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            if (t.gameObject.name.Contains(namePart)) return t;
        return null;
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

    static void WireIfNull(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null && p.objectReferenceValue == null && value != null)
            p.objectReferenceValue = value;
    }
}
#endif
