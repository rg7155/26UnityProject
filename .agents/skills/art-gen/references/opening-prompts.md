# 오프닝 배경 5장 프롬프트 원문

ChatGPT 이미지 생성에 **그대로 복사해 사용**한다.

> **전투 스프라이트와는 규격이 다르다.** 배경은 알파가 필요 없어 **그린스크린을 쓰지 않고**,
> `tools/art_import.py` 경로도 타지 않는다. 생성물을 그대로 반입한다.
> 전투 스프라이트 규격은 [art-spec.md](art-spec.md) · [prompts.md](prompts.md)를 볼 것.

| 항목 | 값 |
|---|---|
| 생성 크기 | **1024 × 1792 (9:16 세로)** |
| 반입 크기 | 1080 × 1920 |
| 저장 위치 | `Assets/Resources/UI/Opening/` |
| 파일명 | `Opening01_DeadArchive.png` 등 — **`OpeningAssetGenerator`가 이 이름으로 찾는다** |
| 알파 | 불필요 (불투명 배경) |

---

## 순서 — Opening01을 먼저 확정한다

**`Opening01`을 먼저 뽑아 확정하고, 채택본을 나머지 4장 생성에 레퍼런스로 첨부한다.**
`art-spec.md` 3-2절의 "플레이어 먼저"와 같은 방식이다.

P1을 기준점으로 삼는 이유: **가장 단순해서 실패 원인 파악이 쉽고**, 여기서 정해지는
암부 톤·시안 광원 세기·디테일 밀도가 나머지 4장에 그대로 승계된다.
P3(링)이나 P4(요새)를 먼저 뽑으면 형태를 만드느라 톤이 흐려진다.

2번째부터 붙일 한 줄:
```
Match the palette, lighting logic, detail density and flat shading of the
attached reference image exactly.
```

---

## 공통 규격 문단

생성 모델은 장이 넘어갈수록 **디테일을 늘리고, 색을 추가하고, 여백을 채우고, 원근을 과장한다.**
이 넷이 흔들림의 90%다. 아래를 고정으로 5장 전부에 붙인다.

```
Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
Exactly two colors plus the near-black background: one dominant, one accent.
The ONLY light source is a neon emissive element inside the scene — no sun,
no sky light, no rim light, no lens flare.
The bottom 40% of the frame is empty flat darkness — no bright elements, no
high-contrast edges, no detail. All subject matter sits in the upper 60%.
IMPORTANT — although the scene is dark, the main structures in the upper 60%
must stay clearly readable as silhouettes. Their edges and planes should sit
around 20-30% brightness against the near-black background, NOT fade into it.
This will be viewed on a phone screen in daylight. Do not crush everything to
pure black.
No text, no logo, no watermark, no border or frame, no vignette, no depth of
field. Any human figure is a plain silhouette, no face, no fingers, under 15%
of frame height.
```

**팔레트** — 이 hex 밖의 색을 쓰지 않는다.
```
배경   #10131C      시안 #35D9F5      마젠타 #FF5488
레드   #FF1E1E      골드 #FFC23C      보스 오렌지 #F08000
```

> **보스 오렌지 `#F08000`은 UITheme 토큰이 아니라 실제 인게임 화면에서 측정한 값이다.**
> `인게임/보스.png`의 밝은 픽셀 분포는 마젠타 `#F03060` 42% / 시안 `#30C0F0` 16.5% /
> 오렌지 `#F06000~F09000` 7.4% 였다. MONOLITH 본체가 이 오렌지다.
> 오프닝의 보스를 마젠타로 그리면 **플레이어가 실제로 만나는 보스와 다른 물건이 된다.**

**하단 40%를 비우는 이유**: 그 자리에 대사가 올라간다. 배경이 밝거나 복잡하면 글자가 안 읽힌다.
검수에서 **하단 40%에 밝은 점이나 강한 엣지가 있으면 반려**한다. 이게 1차 기준이다.
(2차는 게임의 스크림 레이어, 3차는 TMP Underlay)

---

## Opening01_DeadArchive — 붕괴

★ **이것부터. 나머지 4장의 레퍼런스가 된다.**

```
A dead server hall after total power loss, seen in one-point perspective.
Rows of collapsed server racks recede from both sides toward a vanishing
point at 33% from the top of the frame. At the far end of the corridor a
single small cyan (#35D9F5) indicator light still glows — the only light in
the image. Everything else is near-black (#10131C). Still, empty, already
over. The corridor floor in the lower half dissolves into flat darkness with
no readable detail.

Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
Exactly two colors plus the near-black background: one dominant, one accent.
The ONLY light source is a neon emissive element inside the scene — no sun,
no sky light, no rim light, no lens flare.
The bottom 40% of the frame is empty flat darkness — no bright elements, no
high-contrast edges, no detail. All subject matter sits in the upper 60%.
IMPORTANT — although the scene is dark, the main structures in the upper 60%
must stay clearly readable as silhouettes. Their edges and planes should sit
around 20-30% brightness against the near-black background, NOT fade into it.
This will be viewed on a phone screen in daylight. Do not crush everything to
pure black.
No text, no logo, no watermark, no border or frame, no vignette, no depth of
field. Any human figure is a plain silhouette, no face, no fingers, under 15%
of frame height.
```

## Opening02_LastBackup — 기동

```
A cracked-open hatching capsule standing in a dark chamber, front view at eye
level. The capsule sits in the upper middle of the frame and is the only
bright mass — cyan (#35D9F5) light spills from the opening. On the floor
below, that light casts a single long human-shaped shadow, low contrast,
inside the lower 40% of the frame. Clinical and cold — a reactivation, not a
rescue. No figure is actually visible, only the shadow.

Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
Exactly two colors plus the near-black background: one dominant, one accent.
The ONLY light source is a neon emissive element inside the scene — no sun,
no sky light, no rim light, no lens flare.
The bottom 40% of the frame is empty flat darkness — no bright elements, no
high-contrast edges, no detail. All subject matter sits in the upper 60%.
IMPORTANT — although dark, the structures in the upper 60% must stay clearly
readable as silhouettes at 20-30% brightness. Do not crush them to pure black.
No text, no logo, no watermark, no frame, no vignette, no depth of field.
```

## Opening03_FiveSeconds — 규칙 ★ 게임의 핵심

> 이 한 장이 게임의 핵심 기믹(5초 버퍼)과 주제를 동시에 설명한다.
> **다섯 칸만 켜져 있다는 게 한눈에 읽혀야 한다.** 시간이 없으면 이 장부터 만든다.

```
A large circular time dial floating in the air above a dark circuit-board
city, low angle looking up. The ring spans about 70% of the frame width and
its center sits at 35% from the top. The dial is divided into many equal
tick segments around its circumference, but ONLY FIVE adjacent segments are
lit in bright cyan (#35D9F5) — every other segment is dark and unlit. The
count of five must be immediately readable. Beautiful but stingy. The city
below is a flat single-tone silhouette with no window lights.
NO gold, NO magenta, NO red anywhere in this image.

Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
The ONLY light source is the glowing dial itself — no sun, no sky light, no
rim light, no lens flare.
The bottom 40% of the frame is empty flat darkness — no bright elements, no
high-contrast edges, no detail. All subject matter sits in the upper 60%.
IMPORTANT — although dark, the structures in the upper 60% must stay clearly
readable as silhouettes at 20-30% brightness. Do not crush them to pure black.
No text, no logo, no watermark, no frame, no vignette, no depth of field.
```

## Opening04_Monolith — 적대

> ⚠️ **인게임 보스를 그대로 반영한 스펙이다.** 실제 MONOLITH 는 각진 요새가 아니라
> **거대한 원형 기어/링 구조에 주황 코어가 타는 형태**다(`인게임/보스.png`).
> 오프닝의 보스가 실제와 다르면 첫 대면의 인상이 깨진다.

```
A colossal circular machine fortress standing on the horizon of a dark
circuit plain, low angle. The horizon line sits at 40% from the top.
The structure is a giant ring — a massive gear-like wheel of dark gray
armored plates, segmented around its rim, more than 60% of the frame wide,
blocking the upper half of the image. At its center burns a single large
orange (#F08000) core, hotter and brighter toward the middle. Thin orange
light seeps between the armor segments. The metal plates themselves are
gray, not colored.
On the near ridge below stand a few tiny hostile silhouettes glowing faint
magenta (#FF5488), and one lone pilot silhouette seen from behind, under 12%
of the frame height, placed in the LEFT THIRD of the frame. Overwhelming —
the size difference is the message.

Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
The ONLY light source is the fortress's own orange core — no sun, no sky
light, no rim light, no lens flare.
The bottom 40% of the frame is empty flat darkness — the near plain is flat
shadow with no readable detail. No bright elements there.
IMPORTANT — although dark, the ring structure must stay clearly readable as a
silhouette at 20-30% brightness. Do not crush it to pure black.
No text, no logo, no watermark, no frame, no vignette, no depth of field.
The pilot and the small hostiles are plain silhouettes — no faces, no fingers.
```

## Opening05_Horizon — 결

> 로고 `REWIND SURVIVORS`가 y 25~40% 구간에 얹힌다. **그 자리를 비워야 한다.**

```
Dawn over a dark circuit-board plain, front view at eye level. The horizon
line sits at 55% from the top, and a thin band of gold (#FFC23C) light bleeds
along it. The entire upper half of the image is nearly empty sky in
near-black (#10131C) — keep the region between 25% and 40% from the top
completely clear and free of any object. The plain below is a flat
single-tone silhouette with zero detail. Warm but not sentimental — a
restart, not a happy ending. Cyan is minimal or absent.

Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
No sun disc, no clouds, no stars, no rim light, no lens flare.
The bottom 40% of the frame is empty flat darkness — no bright elements, no
high-contrast edges, no detail.
No text, no logo, no watermark, no frame, no vignette, no depth of field.
```

---

## 검수 기준

한 장이라도 아래에 걸리면 반려하고 재생성한다.

1. **하단 40%에 밝은 점이나 강한 엣지가 있는가** → 대사가 안 읽힌다. 가장 흔한 실패
2. 팔레트 밖의 색이 들어왔는가 (특히 3번째 색)
3. 글자·프레임·비네트가 들어왔는가 — 모델이 습관적으로 넣는다
4. 지평선/소실점 높이가 명세와 크게 어긋나는가
5. 5장을 나란히 놓았을 때 **한 세트로 보이는가** — 톤·디테일 밀도가 튀는 장이 있으면 그 장만 재생성
6. (Opening03 한정) **켜진 눈금이 정확히 다섯 개로 읽히는가**
7. **구조물이 근접 검정에 묻히지 않는가** — 폰 화면 밝기에서 실루엣이 읽혀야 한다.
   1차본이 이 항목에서 걸렸다(모델이 규격의 "near-black"을 끝까지 밀어붙였다)
8. (Opening04 한정) **보스가 원형 기어 구조에 주황 코어인가** — 인게임 보스와 같은 물건이어야 한다

반려본은 `ArtSource/Generated/Archive/`로 보내고 `REJECTED.md`에 사유를 남긴다.

## 반입

```
Assets/Resources/UI/Opening/Opening01_DeadArchive.png
Assets/Resources/UI/Opening/Opening02_LastBackup.png
Assets/Resources/UI/Opening/Opening03_FiveSeconds.png
Assets/Resources/UI/Opening/Opening04_Monolith.png
Assets/Resources/UI/Opening/Opening05_Horizon.png
```

임포트 설정은 **Sprite (2D and UI)**, Alpha Is Transparency 불필요, `maxTextureSize 2048`.
넣은 뒤 `Tools/Story/Create Opening Assets`를 다시 실행하면 자동으로 연결된다.
