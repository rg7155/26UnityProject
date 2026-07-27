# 디자인 토큰 — Rewind Survivors UI

모든 색·여백·라운드·폰트 크기의 **단일 출처**. `UITheme.cs`가 이 표를 코드로 옮긴 것이며,
UI 구현은 반드시 `UITheme`를 참조한다. 매직값 하드코딩 금지.

## 톤 결정
인게임은 다크 네온(검정 배경 + 시안/그린/마젠타). 메뉴·팝업은 그 위에 뜨는
**밝은 다크블루 패널 + 두꺼운 외곽선 + 골드 CTA**로 통일한다. 탕탕특공대의 카툰
팝업 느낌을 살리되, 밝은 파스텔이 아니라 게임의 네온 다크와 어울리는 톤을 쓴다.

## 팔레트

| 토큰 | HEX | 용도 |
|------|-----|------|
| `AppBg` | `#10131C` | 앱/메뉴 최하단 풀스크린 배경(Canvas·카메라 클리어) |
| `Backdrop` | `#0A0C14` @ 65% α | 팝업 뒤 화면 딤 |
| `PanelBase` | `#232A3D` | 팝업 본체 표면 |
| `PanelHeader` | `#2E3752` | 헤더 스트립 |
| `CardSurface` | `#1A2032` | 셀/카드(리세스된 어두운 면) |
| `Outline` | `#0B0E18` | 시그니처 두꺼운 외곽선 |
| `Divider` | `#3A4460` | 구분선 |
| `TextPrimary` | `#FFFFFF` | 주 텍스트 |
| `TextSecondary` | `#A7B0C6` | 보조 텍스트 |
| `TextDisabled` | `#5C6480` | 비활성 텍스트 |
| `Accent` (CTA) | `#FFC23C` | 구매/확인 버튼(골드) |
| `AccentPressed` | `#E5A521` | CTA 눌림 |
| `Gold` | `#FFC23C` | 재화 수치·아이콘 |
| `Cyan` | `#35D9F5` | 주 하이라이트(플레이어색) |
| `Positive` | `#79E86A` | HP/경험치/증가 수치 |
| `Danger` | `#FF5488` | 위험/감소/마젠타 |
| `Success` | `#5BD860` | 완료 체크 |

TMP는 리니어 이슈로 색이 흐려질 수 있으니, TMP 텍스트 색은 `UITheme`에서 같은 HEX를
`Color32`로 지정한다.

## 여백 스케일 (spacing)
**이 값만 사용한다.** 임의 여백(7·13·19px) 금지.

| 토큰 | px | 용도 |
|------|----|----|
| `S1` | 4 | 아이콘-텍스트 미세 간격 |
| `S2` | 8 | 기본 요소 간격(그리드 스냅 단위) |
| `S3` | 12 | 카드 내부 패딩 |
| `S4` | 16 | 요소 그룹 간격 |
| `S5` | 24 | 섹션 간격·팝업 패딩 |
| `S6` | 32 | 큰 섹션 분리 |
| `S7` | 48 | 화면 주요 블록 분리 |

## 라운드 반경 (corner radius)
| 토큰 | px | 용도 |
|------|----|----|
| `RadSm` | 10 | 작은 칩·태그 |
| `RadMd` | 18 | 버튼·카드 |
| `RadLg` | 28 | 팝업 본체 |

## 외곽선·그림자
| 토큰 | 값 | 용도 |
|------|----|----|
| `OutlineWidth` | 5px | 라운드 사각형 외곽선 |
| `ShadowColor` | `#000000` @ 50% α | 드롭 섀도(TMP/Image `Shadow` 컴포넌트) |
| `ShadowOffset` | (0, -4) | 아래로 4px |

## 타이포그래피 (NotoSansKR SDF)
폰트: `Assets/Font/NotoSansKR-VariableFont_wght SDF.asset`
※ px 크기는 Reference Resolution(게임뷰 타깃) 기준. CanvasScaler가 실기기에 맞춰 스케일.

| 토큰 | 크기 | 두께 | 용도 |
|------|------|------|------|
| `Display` | 48 | Bold | 타이틀 로고·큰 제목 |
| `Header` | 34 | Bold | 팝업 헤더 |
| `Button` | 28 | Bold | 버튼 라벨 |
| `Body` | 24 | Regular | 본문·아이템명 |
| `Caption` | 20 | SemiBold | 가격·수치·[n/m] |

## UITheme.cs 스켈레톤
```csharp
using UnityEngine;

// 디자인 토큰 단일 출처. UI 구현은 매직값 대신 이 상수만 참조.
public static class UITheme
{
    // ── 색 ──
    public static readonly Color Backdrop     = Hex("0A0C14", 0.65f);
    public static readonly Color PanelBase    = Hex("232A3D");
    public static readonly Color PanelHeader  = Hex("2E3752");
    public static readonly Color CardSurface  = Hex("1A2032");
    public static readonly Color Outline      = Hex("0B0E18");
    public static readonly Color Divider      = Hex("3A4460");
    public static readonly Color TextPrimary  = Hex("FFFFFF");
    public static readonly Color TextSecondary= Hex("A7B0C6");
    public static readonly Color TextDisabled = Hex("5C6480");
    public static readonly Color Accent       = Hex("FFC23C");
    public static readonly Color AccentPressed= Hex("E5A521");
    public static readonly Color Gold         = Hex("FFC23C");
    public static readonly Color Cyan         = Hex("35D9F5");
    public static readonly Color Positive     = Hex("79E86A");
    public static readonly Color Danger       = Hex("FF5488");

    // ── 여백 ──
    public const float S1 = 4, S2 = 8, S3 = 12, S4 = 16, S5 = 24, S6 = 32, S7 = 48;

    // ── 라운드 ──
    public const int RadSm = 10, RadMd = 18, RadLg = 28;

    // ── 외곽선·폰트 크기 ──
    public const int OutlineWidth = 5;
    public const float Display = 48, Header = 34, Button = 28, Body = 24, Caption = 20;

    static Color Hex(string hex, float a = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        c.a = a;
        return c;
    }
}
```
