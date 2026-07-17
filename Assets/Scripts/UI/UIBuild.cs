using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 레이어 2 — 여러 화면에서 반복되는 조립만 담은 헬퍼. 토큰(UITheme)만 사용.
// 상점 전용 1회성 배치는 여기 넣지 않고 생성기에 인라인한다(CLAUDE.md: 재사용될 때만 추상화).
// 폰트는 Resources 밖이라 로드할 수 없으므로 호출자가 주입한다(에디터 생성기가 AssetDatabase 로 로드).
public static class UIBuild
{
    // 라운드+두꺼운 외곽선 패널 표면.
    public static RectTransform Panel(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = UISpriteFactory.RoundedOutlined(
            UITheme.RadLg, UITheme.OutlineWidth, UITheme.PanelBase, UITheme.Outline);
        img.type = Image.Type.Sliced;
        rt.sizeDelta = size;
        return rt;
    }

    // 골드 CTA 버튼(라운드+외곽선). 라벨은 호출자가 Label 로 붙인다.
    public static Button PrimaryButton(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.sprite = UISpriteFactory.RoundedOutlined(
            UITheme.RadMd, UITheme.OutlineWidth, UITheme.Accent, UITheme.Outline);
        img.type = Image.Type.Sliced;

        ApplyCtaColors(go.GetComponent<Button>());
        return go.GetComponent<Button>();
    }

    // 골드 CTA 버튼 색 상태 적용(기존 버튼 리스킨에도 재사용).
    public static void ApplyCtaColors(Button btn)
    {
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = UITheme.AccentPressed;
        colors.selectedColor = Color.white;
        colors.disabledColor = UITheme.TextDisabled;
        colors.fadeDuration = UITheme.FadeDuration;
        btn.colors = colors;
    }

    // TMP 라벨. 부모를 스트레치로 채운다.
    public static TMP_Text Label(RectTransform parent, string text, float size, Color color,
        TMP_FontAsset font, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        Stretch((RectTransform)go.transform);
        return t;
    }

    // 부모를 꽉 채우는 앵커 스트레치(+선택 패딩).
    public static void Stretch(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }
}
