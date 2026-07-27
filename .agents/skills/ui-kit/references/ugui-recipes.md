# UGUI 레시피 — 코드기반 구현 패턴

절차적 스프라이트 → 컴포넌트 헬퍼 → 에디터 생성기 순서. 아래 코드는 **패턴 참고용
스켈레톤**이다. 실제 구현 시 프로젝트 네이밍/스타일에 맞춰 다듬는다.

## 목차
1. 레이어 1 — 절차적 9-slice 스프라이트 (`UISpriteFactory`)
2. 레이어 2 — 컴포넌트 조립 헬퍼 (`UIBuild`)
3. 레이어 3 — 에디터 생성기 (`[MenuItem]`)
4. 앵커·레이아웃 치트시트
5. 흔한 함정

---

## 1. 레이어 1 — 절차적 9-slice 스프라이트

라운드 사각형 + 두꺼운 외곽선을 픽셀로 그린다. `border`로 9-slice 지정 → 크기가
변해도 모서리·테두리 두께가 유지된다. static 캐시로 중복 생성 방지.

```csharp
using System.Collections.Generic;
using UnityEngine;

// 외부 PNG 없이 라운드/테두리 스프라이트를 코드로 생성. 9-slice로 무한 확대 가능.
public static class UISpriteFactory
{
    static readonly Dictionary<string, Sprite> _cache = new();

    // 라운드 사각형 + 외곽선. radius/outline은 px, tex 한 변은 radius*2+여유.
    public static Sprite RoundedOutlined(int radius, int outline, Color fill, Color line)
    {
        string key = $"ro_{radius}_{outline}_{ColorUtility.ToHtmlStringRGBA(fill)}_{ColorUtility.ToHtmlStringRGBA(line)}";
        if (_cache.TryGetValue(key, out var cached)) return cached;

        int pad = 2;                       // 안티에일리어싱 여유
        int size = radius * 2 + pad * 2;    // 모서리 두 개 + 여유
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = CornerDistance(x, y, size, radius); // 라운드 경계까지 부호거리
            float aInside = Mathf.Clamp01(0.5f - d);       // 채움 영역 알파(AA)
            float aLine   = Mathf.Clamp01(0.5f - Mathf.Abs(d + outline * 0.5f) + outline * 0.5f);
            // 외곽선은 경계에서 outline px 안쪽까지
            Color c = Color.Lerp(fill, line, EdgeMix(d, outline));
            c.a *= aInside;
            px[y * size + x] = c;
        }
        tex.SetPixels(px);
        tex.Apply();

        // 9-slice: 모서리(radius+outline)를 고정, 가운데를 늘림
        int b = radius + outline;
        var rect = new Rect(0, 0, size, size);
        var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        _cache[key] = sprite;
        return sprite;
    }

    // 채움만(외곽선 없는 라운드) — 카드 배경 등
    public static Sprite Rounded(int radius, Color fill)
        => RoundedOutlined(radius, 0, fill, fill);

    // 라운드 사각형 경계까지의 부호거리(안=음수, 밖=양수)
    static float CornerDistance(int x, int y, int size, int radius)
    {
        // 중심 기준 코너 원 방식: 각 모서리에서만 라운딩
        float half = size * 0.5f;
        float dx = Mathf.Abs(x + 0.5f - half) - (half - radius);
        float dy = Mathf.Abs(y + 0.5f - half) - (half - radius);
        dx = Mathf.Max(dx, 0);
        dy = Mathf.Max(dy, 0);
        return Mathf.Sqrt(dx * dx + dy * dy) - radius;
    }

    static float EdgeMix(float d, int outline)
        => outline <= 0 ? 0f : Mathf.Clamp01((d + outline) / outline);
}
```

> 위 거리 함수는 정확 SDF는 아니고 실용 근사다. 결과가 만족스럽지 않으면 사용자
> 스크린샷을 받아 `CornerDistance`/`EdgeMix`를 조정한다. 핵심은 **외부 파일 없이
> 라운드+테두리를 얻는 것.**

**대안 (더 단순):** 정밀 라운드가 필요 없으면 Unity 빌트인 `UISprite`(둥근 9-slice)를
`Image.sprite`에 넣고 `type = Sliced`, `color`만 토큰으로 바꿔도 카툰 느낌이 난다.
빌트인은 `Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd")`로 로드.

### ⚠️ 절대 규칙 — 절차적 스프라이트는 "런타임에 적용"한다 (프리팹/씬에 굽지 말 것)
`new Texture2D`로 만든 스프라이트는 **디스크 에셋이 아니라 메모리 임시 객체**다.
에디터 스크립트에서 이걸 `Image.sprite`에 직접 대입하고 프리팹/씬을 저장하면
**직렬화되지 못해 sprite 참조가 null로 저장**된다 → 런타임 Instantiate 시 **흰 박스**.
(같은 세션 씬 오브젝트만 살아있는 메모리 참조로 잠깐 멀쩡해 보여 헷갈린다.)

→ 해결: 스프라이트를 굽지 말고, **런타임에 재생성해 꽂는 컴포넌트**를 쓴다.
`UIProceduralSprite`(`[ExecuteAlways] [RequireComponent(typeof(Image))]`)가 파라미터
(radius/outline/fill/line)만 직렬화하고 `OnEnable`에서 `UISpriteFactory`로 스프라이트를
매번 만들어 `Image.sprite`에 넣는다. Instantiate·씬 재오픈·도메인 리로드 모두에서 산다.

```csharp
[ExecuteAlways, RequireComponent(typeof(Image))]
public class UIProceduralSprite : MonoBehaviour
{
    [SerializeField] bool _outlined;
    [SerializeField] int _radius, _outlineWidth;
    [SerializeField] Color _fill, _line;
    void OnEnable() => Apply();
    public void Apply()
    {
        var img = GetComponent<Image>();
        img.sprite = _outlined
            ? UISpriteFactory.RoundedOutlined(_radius, _outlineWidth, _fill, _line)
            : UISpriteFactory.Rounded(_radius, _fill);
        img.type = Image.Type.Sliced;
    }
}
```
에디터 생성기는 `Image.sprite = ...` 대신 이 컴포넌트를 `GetOrAdd`하고 토큰 파라미터만
세팅한다. (단색 배경은 스프라이트가 없으니 이 컴포넌트 불필요 — `color`만 지정.)
상세 사례: KB `bug-patterns/procedural-sprite-not-serialized.md`.

---

## 2. 레이어 2 — 컴포넌트 조립 헬퍼

여러 화면에서 반복되는 조립만 헬퍼로. 한 화면 전용 배치는 생성기에 인라인한다.

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 반복되는 UI 조립 헬퍼. 토큰(UITheme)만 사용.
public static class UIBuild
{
    public static RectTransform Panel(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = UISpriteFactory.RoundedOutlined(
            UITheme.RadLg, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        img.type = Image.Type.Sliced;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        return rt;
    }

    public static Button PrimaryButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = UISpriteFactory.RoundedOutlined(
            UITheme.RadMd, UITheme.OutlineWidth, UITheme.Accent, UITheme.Outline);
        img.type = Image.Type.Sliced;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = UITheme.AccentPressed;
        colors.disabledColor = UITheme.TextDisabled;
        btn.colors = colors;

        Label((RectTransform)go.transform, label, UITheme.Button, UITheme.Outline, TextAlignmentOptions.Center)
            .rectTransform.anchorMin = Vector2.zero; // 버튼 채우기
        return btn;
    }

    public static TMP_Text Label(RectTransform parent, string text, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var go = new GameObject("Label", typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.font = Resources.Load<TMP_FontAsset>("Fonts/NotoSansKR SDF") // 경로는 실제 위치에 맞춤
                 ?? t.font;
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
        Stretch((RectTransform)go.transform);
        return t;
    }

    // 부모를 꽉 채우도록 앵커 스트레치
    public static void Stretch(RectTransform rt, float pad = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }
}
```

---

## 3. 레이어 3 — 에디터 생성기 `[MenuItem]`

`Assets/Scripts/Editor/` 에 둔다(에디터 전용 어셈블리). 실행하면 프리팹/씬 계층을
만들고 기존 스크립트의 `[SerializeField]`를 **코드로 자동 배선**한다. 이게 "드래그
없이 완성된 프리팹"의 핵심이다.

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

// 상점 팝업을 코드로 생성/리스킨. 실행: Tools/UI/Build Shop Popup
public static class ShopUIGenerator
{
    [MenuItem("Tools/UI/Build Shop Popup")]
    public static void BuildShopPopup()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogError("씬에 Canvas가 없다."); return; }

        // 백드롭(딤) — 클릭 차단 + 어둡게
        var backdrop = new GameObject("ShopBackdrop", typeof(Image));
        backdrop.transform.SetParent(canvas.transform, false);
        var bImg = backdrop.GetComponent<Image>();
        bImg.color = UITheme.Backdrop;
        UIBuild.Stretch((RectTransform)backdrop.transform);

        // 팝업 본체 (중앙 앵커)
        var popup = UIBuild.Panel(backdrop.transform, "ShopPopup", new Vector2(720, 1040));
        Center(popup);

        // 헤더바 / 골드칩 / 그리드 컨테이너 / 닫기버튼 ...
        var grid = new GameObject("CellContainer", typeof(GridLayoutGroup)).GetComponent<GridLayoutGroup>();
        grid.transform.SetParent(popup, false);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;                    // 3×N 그리드 (레퍼런스)
        grid.cellSize = new Vector2(200, 260);
        grid.spacing = new Vector2(UITheme.S4, UITheme.S4);

        var close = UIBuild.PrimaryButton(popup, "CloseButton", "X");

        // ── 기존 스크립트 참조 자동 배선 (드래그 대체) ──
        var shop = popup.gameObject.AddComponent<UI_ShopPanel>();
        var so = new SerializedObject(shop);
        so.FindProperty("_cellContainer").objectReferenceValue = grid.transform;
        so.FindProperty("_closeButton").objectReferenceValue = close;
        // _goldText, _cellPrefab 도 동일 방식으로 세팅
        so.ApplyModifiedProperties();

        Selection.activeGameObject = popup.gameObject;
        Debug.Log("상점 팝업 생성 완료. 필요 시 에디터에서 미세조정.");
    }

    static void Center(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }
}
#endif
```

**리스킨 모드:** 이미 프리팹이 있으면 파괴 후 재생성하지 말고, 기존 오브젝트를 찾아
`Image.sprite`/`color`/폰트만 교체한다. 사용자가 수동 편집한 배치를 보존하기 위해서다.
`GetComponentsInChildren`으로 순회하며 in-place 갱신.

---

## 4. 앵커·레이아웃 치트시트

| 위젯 | 앵커 | 비고 |
|------|------|------|
| 백드롭 딤 | Stretch(0,0)-(1,1) | 화면 전체, `Image`로 클릭 차단 |
| 중앙 팝업 | (0.5,0.5) 고정 | `anchoredPosition = 0` |
| 헤더 스트립 | 상단 Stretch, 하단 고정 높이 | `anchorMin(0,1) anchorMax(1,1)` |
| 골드 칩 | 우상단 (1,1) | 오프셋으로 패딩 |
| 그리드 | 부모 채우기 + 패딩 | `GridLayoutGroup` 3열 |
| 하단 버튼 | 하단 중앙 (0.5,0) | |

- 세로 스크롤 그리드: `ScrollRect` + `Viewport(Mask)` + `Content(GridLayoutGroup +
  ContentSizeFitter[Vertical=PreferredSize])`.
- `CanvasScaler`: Scale With Screen Size, Match = 0.5.

## 5. 흔한 함정

- **TMP 폰트 null** — `Resources.Load<TMP_FontAsset>` 경로가 실제와 다르면 폰트가 안 뜬다.
  폰트를 `Resources/Fonts/`로 옮기거나 생성기에서 `AssetDatabase.LoadAssetAtPath`로 로드.
- **Sliced인데 안 늘어남** — `Image.type = Sliced` + 스프라이트 `border` 필수. 둘 중
  하나만 빠져도 모서리가 뭉갠다.
- **에디터 스크립트를 런타임 폴더에 둠** — `UnityEditor` 네임스페이스는 빌드에서 컴파일
  실패. 반드시 `Editor/` 폴더 또는 `#if UNITY_EDITOR`.
- **파괴 후 재생성으로 사용자 편집 날림** — 리스킨은 in-place 갱신 우선.
- **색이 흐려짐** — Color Space(Linear)에서 TMP/Image 색은 `Color32`로 지정하면 안정적.
- **Raycast 낭비** — 텍스트/장식 Image는 `raycastTarget = false`로 꺼서 클릭 성능 확보.
```
