using UnityEngine;

// 레이어 2 — 디자인 토큰 단일 출처(design-tokens.md 를 코드로 옮긴 것).
// UI 구현은 매직값 대신 이 상수만 참조한다.
public static class UITheme
{
    // ── 색 ──
    public static readonly Color AppBg         = Hex("10131C");
    public static readonly Color Backdrop      = Hex("0A0C14", 0.65f);
    public static readonly Color PanelBase     = Hex("232A3D");
    public static readonly Color PanelHeader   = Hex("2E3752");
    public static readonly Color CardSurface   = Hex("1A2032");
    public static readonly Color Outline       = Hex("0B0E18");
    public static readonly Color Divider       = Hex("3A4460");
    public static readonly Color TextPrimary   = Hex("FFFFFF");
    public static readonly Color TextSecondary = Hex("A7B0C6");
    public static readonly Color TextDisabled  = Hex("5C6480");
    public static readonly Color Accent        = Hex("FFC23C");
    public static readonly Color AccentPressed = Hex("E5A521");
    public static readonly Color Gold          = Hex("FFC23C");
    public static readonly Color Cyan          = Hex("35D9F5");
    public static readonly Color Positive      = Hex("79E86A");
    public static readonly Color Danger        = Hex("FF5488");

    // ── 여백(px) — 이 값만 사용 ──
    public const float S1 = 4, S2 = 8, S3 = 12, S4 = 16, S5 = 24, S6 = 32, S7 = 48;

    // ── 라운드 반경(px) ──
    public const int RadSm = 10, RadMd = 18, RadLg = 28;

    // ── 외곽선·폰트 크기 ──
    public const int OutlineWidth = 5;
    public const float Display = 48, Header = 34, Button = 28, Body = 24, Caption = 20;

    // ── 버튼 색 전환 시간(초) ──
    public const float FadeDuration = 0.08f;

    static Color Hex(string hex, float a = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        c.a = a;
        return c;
    }
}
