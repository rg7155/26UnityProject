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

## 순서 — Opening02가 톤 기준이다

> **처음에는 Opening01을 기준으로 삼으려 했으나 바꿨다.**
> P1 1차본이 너무 어두워 구조물이 배경에 묻혔고(밝기 하한을 안 적은 규격의 잘못),
> 반면 **P2 1차본이 밝기·디테일·캐릭터를 전부 통과**했다.
> 결과가 좋은 쪽을 기준으로 두는 것이 맞다. **P2가 톤 기준이다.**

**모든 장은 `Opening02` 채택본을 레퍼런스로 첨부한다. P1도 포함이다**(P1은 재생성 대상).

| 첨부할 레퍼런스 | 무엇을 승계하는가 | 대상 |
|---|---|---|
| **`Opening02` 채택본** | 톤 — 팔레트, 암부 밝기, 광원 논리, 디테일 밀도 | **P1·P3·P4·P5 전부** |
| `Assets/Art/Sprites/Generated/PlayerEnergyCoreSymmetric.png` | 캐릭터 — 헬멧·바이저·트림·코어 | P3·P4·P5 (P1은 캐릭터 없음) |

톤 레퍼런스에 붙일 한 줄 — **프롬프트 끝에 그대로 추가한다**:
```
Match the palette, lighting logic, ambient darkness level, detail density and
flat cel shading of the attached tone reference image exactly.
```

캐릭터 레퍼런스 지시는 각 프롬프트 본문에 이미 들어 있다(「캐릭터 규격」 절 참고).

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
field.
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

---

## 캐릭터 규격 — 파일럿

**5장 중 4장(P2~P5)에 파일럿이 등장한다.** P1만 예외인데, 아직 깨어나기 전이라 서사상 맞다.

> 초안에서는 "생성 일관성 리스크"를 이유로 캐릭터를 실루엣으로만 두었다. **그 판단이 과했다.**
> 프레임 간 일관성이 어려운 것은 *애니메이션 스트립*이고, 정지 일러스트 5장은
> 레퍼런스 이미지를 붙이면 충분히 통제된다. 캐릭터를 빼면 감정을 얹을 대상이 사라져
> 화면이 허전해진다 — 리스크는 없앴지만 온기도 같이 없앤 셈이었다.

### 레퍼런스 첨부 (필수)

P2~P5 생성 시 **인게임 플레이어 스프라이트를 반드시 첨부**한다.
```
Assets/Art/Sprites/Generated/PlayerEnergyCoreSymmetric.png
```

### 비율은 인게임과 다르게 간다

인게임은 2.5등신 치비다. **오프닝 일러스트는 약 6등신 정상 비율로 그린다.**
시네마틱 구도에서 치비를 크게 그리면 톤이 무너진다. 오프닝 일러스트에서 인게임 SD 캐릭터를
정상 비율로 그리는 것은 모바일 게임의 일반적인 관례다(탕탕특공대·뱀서 모두 그렇다).

**같은 사람으로 읽히게 하는 건 비율이 아니라 디자인 요소다.** 아래 넷만 지키면 된다.

| 고정 요소 | 내용 |
|---|---|
| 헬멧 | 둥근 풀페이스. **가로로 긴 시안 바이저 슬릿 하나.** 얼굴·눈·입 없음 |
| 아머 | 미디엄 그레이 판금 + **시안(#35D9F5) 발광 트림** — 가슴·어깨·부츠 |
| 가슴 | 원형 에너지 코어가 시안으로 빛난다 |
| 무기 | 컴팩트 블래스터 (P4·P5에서만. P2·P3에는 없다) |

**허용되는 차이**: 판금 디테일, 주름, 자세, 명암. 완전히 같을 필요는 없다.
**허용되지 않는 차이**: 바이저를 눈 두 개로 바꾸는 것, 얼굴을 드러내는 것, 트림 색을 바꾸는 것.

### 크기와 배치

캐릭터가 텍스트 영역(하단 40%)을 침범하면 안 된다. **상반신이 상단 60% 안에 들어와야 한다.**
전신을 넣을 때는 다리가 하단 40%에 들어가도 되지만, 그 부분은 어둡게 묻혀야 한다.

**하단 40%를 비우는 이유**: 그 자리에 대사가 올라간다. 배경이 밝거나 복잡하면 글자가 안 읽힌다.
검수에서 **하단 40%에 밝은 점이나 강한 엣지가 있으면 반려**한다. 이게 1차 기준이다.
(2차는 게임의 스크림 레이어, 3차는 TMP Underlay)

---

## Opening01_DeadArchive — 붕괴

> **1차본이 반려됐다** — 너무 어두워 서버랙이 배경에 묻혔다.
> 재생성 시 **`Opening02` 채택본을 톤 레퍼런스로 첨부**하고 밝기를 맞춘다.
> 이 장에는 캐릭터가 없다. 아직 깨어나기 전이라 서사와 맞다.

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
field.
```

## Opening02_LastBackup — 기동

> **파일럿이 처음 등장하는 장이다.** 초안은 그림자만 있었는데, 깨어나는 순간에
> 정작 사람이 없어 서사가 비었다.

```
A pilot waking inside a cracked-open hatching capsule, front view at eye
level. The capsule stands in the upper middle of the frame, its shell split
open, cyan (#35D9F5) light spilling out from inside. The pilot is slumped
forward in the opening, just regaining consciousness — one hand braced on the
capsule rim, head lowered. He is the brightest subject in the frame, lit only
by the capsule's own glow. Clinical and cold — a reactivation, not a rescue.

The attached reference image is the in-game player sprite. Keep the SAME
character identity — round full-face helmet with ONE wide horizontal cyan
(#35D9F5) visor slit and no face, medium-gray armor plating with glowing cyan
trim on chest, shoulders and boots, and a round glowing cyan energy core on
the chest. Draw him at realistic proportions (about 6 heads tall), NOT the
chibi proportions of the reference — only the design elements carry over.

Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
The bottom 40% of the frame is empty flat darkness — no bright elements, no
high-contrast edges, no readable detail. Text will be placed there.
IMPORTANT — although dark, the subject must stay clearly readable as a
silhouette at 20-30% brightness. Do not crush it to pure black.
No text, no logo, no watermark, no frame, no vignette, no depth of field.
```

## Opening03_FiveSeconds — 규칙 ★ 게임의 핵심

> 이 한 장이 게임의 핵심 기믹(5초 버퍼)과 주제를 동시에 설명한다.
> **다섯 칸만 켜져 있다는 게 한눈에 읽혀야 한다.** 시간이 없으면 이 장부터 만든다.

```
A large circular time dial floating in the air above a dark circuit-board
city, low angle looking up. The ring spans about 70% of the frame width and
its center sits at 35% from the top. The dial is divided into many equal tick
segments around its circumference, but ONLY FIVE adjacent segments are lit in
bright cyan (#35D9F5) — every other segment is dark and unlit. The count of
five must be immediately readable.
In the lower middle of the upper 60%, a lone pilot stands with his back to
the viewer, head tilted up toward the dial, about 25% of the frame height —
small against the ring but clearly a person, not a speck. He carries no
weapon in this scene. Beautiful but stingy — the ring gives him only five.
NO gold, NO magenta, NO red anywhere in this image.

The attached reference image is the in-game player sprite. Keep the SAME
character identity — round full-face helmet with ONE wide horizontal cyan
(#35D9F5) visor slit and no face, medium-gray armor plating with glowing cyan
trim on chest, shoulders and boots, and a round glowing cyan energy core on
the chest. Draw him at realistic proportions (about 6 heads tall), NOT the
chibi proportions of the reference — only the design elements carry over.

Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
The bottom 40% of the frame is empty flat darkness — no bright elements, no
high-contrast edges, no readable detail. Text will be placed there.
IMPORTANT — although dark, the subject must stay clearly readable as a
silhouette at 20-30% brightness. Do not crush it to pure black.
No text, no logo, no watermark, no frame, no vignette, no depth of field.
```

## Opening04_Monolith — 적대

> ⚠️ **인게임 보스를 그대로 반영한 스펙이다.** 실제 MONOLITH 는 각진 요새가 아니라
> **거대한 원형 기어/링 구조에 주황 코어가 타는 형태**다(`인게임/보스.png`).
> 오프닝의 보스가 실제와 다르면 첫 대면의 인상이 깨진다.

```
A colossal circular machine fortress on the horizon of a dark circuit plain,
low angle. The horizon line sits at 40% from the top. The structure is a
giant ring — a massive gear-like wheel of dark gray armored plates, segmented
around its rim, more than 60% of the frame wide, blocking the upper half of
the image. At its center burns a single large orange (#F08000) core, hotter
toward the middle. Thin orange light seeps between the armor segments. The
metal plates themselves are gray, not colored.
On the near ridge stands the lone pilot seen from behind, about 22% of the
frame height, placed in the LEFT THIRD of the frame, his compact blaster held
low at his side. A few small hostile shapes glowing faint magenta (#FF5488)
scatter across the ridge between him and the fortress. Overwhelming — the
size difference is the message.

The attached reference image is the in-game player sprite. Keep the SAME
character identity — round full-face helmet with ONE wide horizontal cyan
(#35D9F5) visor slit and no face, medium-gray armor plating with glowing cyan
trim on chest, shoulders and boots, and a round glowing cyan energy core on
the chest. Draw him at realistic proportions (about 6 heads tall), NOT the
chibi proportions of the reference — only the design elements carry over.

The ONLY light source is the fortress's own orange core and the pilot's cyan
trim — no sun, no sky light, no rim light, no lens flare.
Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
The bottom 40% of the frame is empty flat darkness — no bright elements, no
high-contrast edges, no readable detail. Text will be placed there.
IMPORTANT — although dark, the subject must stay clearly readable as a
silhouette at 20-30% brightness. Do not crush it to pure black.
No text, no logo, no watermark, no frame, no vignette, no depth of field.
```

## Opening05_Horizon — 결

> 로고 `REWIND SURVIVORS`가 y 25~40% 구간에 얹힌다. **그 자리를 비워야 한다.**
> **파일럿이 화면에 서 있는 유일한 장이자, 오프닝과 인게임을 잇는 다리다.**

```
Dawn over a dark circuit-board plain, front view at eye level. The horizon
line sits at 55% from the top, and a thin band of gold (#FFC23C) light bleeds
along it. The region between 25% and 40% from the top must stay completely
empty — a logo will be placed there.
The pilot stands just below the horizon line, seen from behind, facing the
dawn, about 30% of the frame height and slightly RIGHT of center so he does
not sit under the logo. His silhouette is dark against the gold band, with
his cyan (#35D9F5) trim and chest core glowing — the only cool light in the
image. His blaster hangs at his side. Warm but not sentimental — a restart,
not a happy ending.

The attached reference image is the in-game player sprite. Keep the SAME
character identity — round full-face helmet with ONE wide horizontal cyan
(#35D9F5) visor slit and no face, medium-gray armor plating with glowing cyan
trim on chest, shoulders and boots, and a round glowing cyan energy core on
the chest. Draw him at realistic proportions (about 6 heads tall), NOT the
chibi proportions of the reference — only the design elements carry over.

Vertical 9:16 composition. Flat cel-shaded vector style — no photorealism,
no texture, no film grain, no noise.
The bottom 40% of the frame is empty flat darkness — no bright elements, no
high-contrast edges, no readable detail. Text will be placed there.
IMPORTANT — although dark, the subject must stay clearly readable as a
silhouette at 20-30% brightness. Do not crush it to pure black.
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
9. (P2~P5) **파일럿이 같은 사람으로 읽히는가** — 바이저 슬릿 하나 / 시안 트림 / 가슴 코어.
   비율은 달라도 되지만 이 셋이 바뀌면 반려
10. (P2~P5) **파일럿이 하단 40%를 침범하지 않는가** — 상반신이 상단 60% 안에 있어야 한다

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
