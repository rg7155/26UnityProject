# 전투 스프라이트 생성 프롬프트 원문

ChatGPT 이미지 생성에 **그대로 복사해 사용**한다. 규격의 근거는 [art-spec.md](art-spec.md).

`_workspace_archive/plan_scrolling_bg.md` L119~150(배경 타일)과 같은 형식이다.
**배경 타일 2종은 이미 채택·검증되었으므로 다시 만들지 않는다.** 세계관은 그대로 두고
캐릭터만 치비로 바꾼다.

---

## 순서가 중요하다 — 플레이어부터

**플레이어 한 장이 스타일 기준점이다.** 이 장을 확정한 뒤, 그 이미지를 **이후 모든 생성에
레퍼런스로 첨부**한다. 탄환·텔레그래프처럼 스타일을 따라가는 대상을 먼저 만들면
기준 없이 각자 다른 그림이 나온다.

| 순 | 대상 | 형식 | 비고 |
|---|---|---|---|
| **1** | **Player 정지** | 1장 | ★ **스타일 기준점.** 이게 확정돼야 나머지가 시작된다 |
| 2 | Player idle 스트립 | 4프레임 | 1번을 레퍼런스로 |
| 3 | Player move 스트립 | 4프레임 | 상체·총 고정, 하체 런 사이클 |
| 4 | Boss MONOLITH | 4프레임 | |
| 5 | 적 3종 | 각 4프레임 | 실루엣으로 구분 |
| 6 | Orbiter | 4프레임 | |
| 7 | 탄환 2종 + 보스 원형탄 | 각 1장 | |
| 8 | 텔레그래프 2종 | 각 1장 | |
| 9 | 보물상자 | 1장 | 시간 남으면 |

---

## 공통 규격 문단

대상별로 프롬프트를 새로 쓰지 않는다. 아래 문단을 고정으로 두고 **대상 문장 1~2줄만 갈아끼운다.**

```
Three-quarter SIDE view, body turned to face RIGHT, weapon aimed RIGHT,
centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge.
Chibi proportions: large head, short stubby legs, wide compact build, about
2.5 heads tall. Face reduced to a single visor slit — no nose, no mouth, no
facial expression. Bold dark outlines and flat cel shading. Armor base plates
in MEDIUM GRAY, not near-black, so the silhouette reads clearly against a
very dark background.
No soft glow bloom baked into the image, no drop shadow, no vignette, no
gradient background, no perspective, no ground plane. Flat even lighting.
The subject fills about 85% of the frame with even margin on all sides.
No text, no logo, no watermark, no frame or border.
```

**2번부터는 여기에 한 줄을 더 붙인다** (레퍼런스 이미지를 함께 첨부):

```
Match the art style, line weight, proportions, palette and shading of the
attached reference image exactly.
```

### 왜 배경이 검정이 아니라 녹색인가

이 아트에는 어두운 부분(외곽선, 그림자 면, 어두운 장비)이 있다. 검정 배경으로 뽑으면
그 부분까지 투명해져 **구멍이 뚫린다.** 기존 9장도 실제로 그린스크린으로 만들어졌다
(소스 코너 픽셀이 `#20E820` 부근). `art_import.py`가 크로마키로 알파를 뽑고
초록 번짐(spill)까지 제거한다.

### 생성 후 반드시 확인

- 배경이 균일한 밝은 녹색인가. 그라데이션이 지거나 어두우면 크로마키가 지저분해진다
- **피사체에 녹색 계열이 없는가.** 배경과 함께 지워진다.
  연사탄(`RapidShot #C7FF00`)은 흰색으로 생성한 뒤 틴트로 색을 입힌다 — F-2 참고
- 오른쪽을 보고 있는가. 왼쪽 버전은 만들지 않는다 — 코드가 뒤집는다
- 아머가 너무 어둡지 않은가. 게임 배경(#10131C)에서 묻히면 반려다

---

# A. 플레이어 ★ 스타일 기준점

## A-1. `PlayerEnergyCoreSymmetric` — 정지 (먼저 이것부터)

> 파일명에 `Symmetric`이 남아 있는 건 **guid를 보존하기 위해서다.**
> 이름을 바꾸면 프리팹 참조가 끊긴다. 내용은 더 이상 대칭이 아니다.

```
A chibi cyberpunk hero for a top-down action game, turned to face RIGHT.
A stocky pilot in medium-gray armor with glowing cyan (#35D9F5) trim along
the helmet, chest, shoulders and boots, a visor helmet with a single bright
cyan slit, and an energy core glowing on the chest. He holds a compact
futuristic blaster in both hands, aimed horizontally to the RIGHT, in a
braced shooting stance.

Three-quarter SIDE view, body turned to face RIGHT, weapon aimed RIGHT,
centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge.
Chibi proportions: large head, short stubby legs, wide compact build, about
2.5 heads tall. Face reduced to a single visor slit — no nose, no mouth, no
facial expression. Bold dark outlines and flat cel shading. Armor base plates
in MEDIUM GRAY, not near-black, so the silhouette reads clearly against a
very dark background.
No soft glow bloom baked into the image, no drop shadow, no vignette, no
gradient background, no perspective, no ground plane. Flat even lighting.
The subject fills about 85% of the frame with even margin on all sides.
No text, no logo, no watermark, no frame or border.
```

**3안을 뽑고 1안을 채택한다. 이 채택본이 이후 전부의 레퍼런스가 된다.**
채택 기준은 [art-spec.md](art-spec.md) 8절 — 512로 줄여 게임 화면에 올렸을 때 실루엣이 식별되는가.

## A-2. `PlayerEnergyCoreSymmetric` — 4프레임 스트립

A-1 채택본을 **레퍼런스로 첨부**한다.

변화 요소는 **제자리 바운스 하나만**. 걷기 사이클은 4프레임 안에 일관되게 안 나온다.

```
The attached reference character, rendered as a 4-frame idle bounce animation.

Render this as a SINGLE horizontal strip image containing exactly 4 frames
side by side, evenly spaced, each frame the same square size.
It must be the SAME character in all 4 frames — identical design, identical
colors, identical proportions, identical facing. Do not re-pose the arms,
do not change the head, do not redesign anything. Only the change described
below differs across the frames. No dividing lines or borders between frames.

The ONLY difference between frames: the whole character bobs gently up and
down in place — lowest, slightly up, highest, slightly up — with a small
squash at the lowest frame and a small stretch at the highest. Horizontal
position is identical in all four frames.

Match the art style, line weight, proportions, palette and shading of the
attached reference image exactly.

Three-quarter SIDE view, body turned to face RIGHT, weapon aimed RIGHT,
centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge.
Bold dark outlines and flat cel shading. Flat even lighting, no soft glow
bloom baked into the image, no drop shadow, no vignette, no gradient
background, no perspective, no ground plane.
No text, no logo, no watermark, no frame or border.
```

> 세로 바운스는 `art_import.py`의 **전 프레임 공통 바운딩박스** 덕분에 정상 동작한다.
> 프레임마다 따로 크롭하면 바운스가 상쇄되어 사라진다.

## A-3. `PlayerEnergyCoreMove` — 이동 4프레임 스트립

A-1 채택본을 **레퍼런스로 첨부**한다. 상체와 총은 안정적으로 유지하고
하체에서만 명확한 런 사이클을 만든다.

```
The attached reference character, rendered as a dynamic 4-frame combat jogging animation for a top-down survivor action game.

Render this as ONE SINGLE horizontal sprite strip containing exactly 4 square frames side by side in one row. All four cells must have exactly the same size. No gaps, dividing lines, borders, labels, or frame numbers.

It must be exactly the SAME character in all four frames: identical helmet, visor, armor design, cyan trim, colors, proportions, line weight, shading, weapon design, and facing direction. Do not redesign, rotate, mirror, or change the character between frames.

The character continuously faces RIGHT and keeps the futuristic blaster firmly aimed horizontally to the RIGHT in every frame. The helmet, torso, both arms, hands, and blaster remain visually consistent. Do not animate recoil and do not change the gun shape or muzzle length.

Create a clear looping combat jog cycle:
Frame 1: right leg steps forward and left leg moves back, torso slightly lowered.
Frame 2: legs pass near the center, body rises slightly.
Frame 3: left leg steps forward and right leg moves back, torso slightly lowered.
Frame 4: legs pass near the center, body rises slightly.

Make the leg movement clearly readable and energetic, with a subtle vertical body bounce and slight shoulder movement. Keep the horizontal body position identical across all four frames. Do not make the character travel across the canvas.

Three-quarter SIDE view, body turned to face RIGHT, weapon aimed RIGHT. Chibi proportions: large head, short thick legs, wide compact body, approximately 2.5 heads tall. Medium-gray armor with bright cyan (#35D9F5) trim, a single cyan visor slit, bold dark outlines, and flat cel shading.

Match the attached reference image exactly in art style, character identity, proportions, palette, armor details, weapon, and shading.

Each frame must contain the complete character. Do not crop the helmet, gun muzzle, arms, legs, or boots. Keep at least 10% empty green margin around the full character inside EVERY frame. The gun muzzle and both boots must remain completely inside their own square cell.

Solid bright green (#00FF00) chroma-key background filling the entire image edge to edge. Flat even lighting. No baked glow bloom, motion blur, speed lines, dust, particles, drop shadow, ground plane, perspective background, vignette, text, logo, watermark, frame, or border.
```

> 이동 포즈는 다리 보폭 때문에 가로 실루엣 편차가 3%를 넘을 수 있다.
> 이 경우 헬멧·몸통·총의 일관성과 세로 크기 편차를 우선 판정한다.

---

# B. 보스 — `BossMonolithSymmetric` (4프레임)

보스는 1024 유지. 플레이어 채택본을 레퍼런스로 첨부한다.
치비지만 **압도적으로 커야** 하므로 등신 대신 **덩치**로 표현한다.

```
A massive chibi mech boss called MONOLITH for a top-down action game, turned
to face RIGHT. A hulking armored war machine in hot pink (#FF5488) over
medium-gray metal, with heavy shoulder blocks far wider than its body, thick
stubby legs, a small head with a single wide glowing visor, a large exposed
reactor core in the chest, and a huge arm cannon aimed to the RIGHT.
Intimidating and heavy, clearly many times bigger than a normal character.

Render this as a SINGLE horizontal strip image containing exactly 4 frames
side by side, evenly spaced, each frame the same square size.
It must be the SAME machine in all 4 frames — identical design, identical
colors, identical proportions, identical facing. Do not re-pose it, do not
redesign anything. Only the change described below differs across the frames.
No dividing lines or borders between frames.

The ONLY difference between frames: the chest reactor core glows at
increasing then decreasing intensity — dim, medium, brightest, medium.
The body, arms and silhouette are identical in all four frames.

Match the art style, line weight and shading of the attached reference image.

Three-quarter SIDE view, body turned to face RIGHT, weapon aimed RIGHT,
centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge.
Bold dark outlines and flat cel shading. Flat even lighting, no soft glow
bloom baked into the image, no drop shadow, no vignette, no gradient
background, no perspective, no ground plane.
No text, no logo, no watermark, no frame or border.
```

> **페이즈 2는 별도 생성하지 않는다.** `BossController.cs:138`이 이미 페이즈별
> `_sprite.color`를 바꾸고 있으므로 틴트로 처리한다. 생성 1회를 아낀다.

---

# C. 적 3종 — 실루엣으로 구분

색만 다르면 플레이 중 구분이 안 된다. **머리 모양과 덩치로 구분**한다.
전부 플레이어 채택본을 레퍼런스로 첨부한다.

## C-1. `BasicEnemyDroneSymmetric` — 표준 병사 (4프레임)

```
A chibi enemy foot soldier for a top-down action game, turned to face RIGHT.
A hostile android in hot pink (#FF5488) over medium-gray metal, with a
rounded helmet, a single glowing pink visor slit, a plain armored torso and
short thick limbs, holding a small rifle aimed to the RIGHT.
Standard build — the baseline enemy.

Render this as a SINGLE horizontal strip image containing exactly 4 frames
side by side, evenly spaced, each frame the same square size.
It must be the SAME character in all 4 frames — identical design, identical
colors, identical proportions, identical facing. Do not re-pose it, do not
redesign anything. Only the change described below differs across the frames.
No dividing lines or borders between frames.

The ONLY difference between frames: the whole character bobs gently up and
down in place — lowest, slightly up, highest, slightly up. Horizontal
position is identical in all four frames.

Match the art style, line weight and shading of the attached reference image.

Three-quarter SIDE view, body turned to face RIGHT, weapon aimed RIGHT,
centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge.
Chibi proportions, about 3.5 heads tall. Bold dark outlines and flat cel
shading. Flat even lighting, no soft glow bloom baked into the image, no drop
shadow, no vignette, no gradient background, no perspective, no ground plane.
No text, no logo, no watermark, no frame or border.
```

## C-2. `FastEnemyDroneSymmetric` — 스카우트 (4프레임)

**C-1보다 확실히 가늘고 작게.** 대상 문장만 갈아끼우고 나머지는 C-1과 동일하다.

```
A chibi enemy scout for a top-down action game, turned to face RIGHT.
A slim fast-moving android in hot pink (#FF5488) over medium-gray metal,
noticeably thinner and smaller than a standard soldier, with a narrow pointed
visor head, swept-back shoulder fins and long thin legs, holding a short
compact pistol aimed to the RIGHT. Built for speed.

Render this as a SINGLE horizontal strip image containing exactly 4 frames
side by side, evenly spaced, each frame the same square size.
It must be the SAME character in all 4 frames — identical design, identical
colors, identical proportions, identical facing. Do not re-pose it, do not
redesign anything. Only the change described below differs across the frames.
No dividing lines or borders between frames.

The ONLY difference between frames: the whole character bobs gently up and
down in place — lowest, slightly up, highest, slightly up. Horizontal
position is identical in all four frames.

Match the art style, line weight and shading of the attached reference image.

Three-quarter SIDE view, body turned to face RIGHT, weapon aimed RIGHT,
centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge.
Chibi proportions, about 3.5 heads tall. Bold dark outlines and flat cel
shading. Flat even lighting, no soft glow bloom baked into the image, no drop
shadow, no vignette, no gradient background, no perspective, no ground plane.
No text, no logo, no watermark, no frame or border.
```

## C-3. `TankEnemyDroneSymmetric` — 중장갑 (4프레임)

**C-1보다 확실히 넓고 두껍게.**

```
A chibi heavy enemy brute for a top-down action game, turned to face RIGHT.
A bulky armored android in hot pink (#FF5488) over medium-gray metal, much
wider and thicker than a standard soldier, with a boxy reinforced helmet, a
narrow eye slot, huge slab-like shoulder plates, a barrel chest with visible
plate seams and short stumpy legs, carrying a heavy shield on its RIGHT arm.
Slow and heavily armored.

Render this as a SINGLE horizontal strip image containing exactly 4 frames
side by side, evenly spaced, each frame the same square size.
It must be the SAME character in all 4 frames — identical design, identical
colors, identical proportions, identical facing. Do not re-pose it, do not
redesign anything. Only the change described below differs across the frames.
No dividing lines or borders between frames.

The ONLY difference between frames: the whole character bobs gently up and
down in place — lowest, slightly up, highest, slightly up. Horizontal
position is identical in all four frames.

Match the art style, line weight and shading of the attached reference image.

Three-quarter SIDE view, body turned to face RIGHT, weapon aimed RIGHT,
centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge.
Chibi proportions, about 3.5 heads tall. Bold dark outlines and flat cel
shading. Flat even lighting, no soft glow bloom baked into the image, no drop
shadow, no vignette, no gradient background, no perspective, no ground plane.
No text, no logo, no watermark, no frame or border.
```

---

# D. `OrbiterEnergyBlade` — 궤도 위성 (4프레임)

캐릭터가 아니라 오브젝트다. 치비 문단을 빼고 간다.
`OrbitWeapon`은 위치만 갱신하고 회전을 안 시키므로 **회전 대칭 형태**로 만든다.

```
A small orbiting energy shield drone for a top-down action game.
A compact hovering disc in cyan (#35D9F5) and dark metal with a bright ring
edge, a small central lens, and four short blade fins arranged evenly around
it so it looks the same from any angle.

Render this as a SINGLE horizontal strip image containing exactly 4 frames
side by side, evenly spaced, each frame the same square size.
It must be the SAME object in all 4 frames, at the SAME position and SAME
size. Only the change described below differs. No dividing lines between frames.

The ONLY difference between frames: the outer ring edge brightens and dims —
dim, bright, brightest, bright.

Match the art style, line weight and shading of the attached reference image.

Centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge. Bold dark outlines and flat cel shading.
Flat even lighting, no soft glow bloom baked into the image, no drop shadow,
no vignette, no gradient background, no perspective, no ground plane.
The subject fills about 85% of the frame with even margin on all sides.
No text, no logo, no watermark, no frame or border.
```

---

# E. 탄환 · 보스 원형탄

⚠️ **발사체는 현재 회전하지 않는다** (`ProjectileWeapon.cs:50`이 `Quaternion.identity`).
그래서 **방향성 없는 구체 형태**로 만든다.

방향성 있는 탄환을 쓰려면 진행 방향 회전을 코드로 한 줄 추가해야 한다.
그건 별도 작업이고, 여기서는 회전 없는 전제로 간다.

## E-1. `Projectile` — 기본 탄환

```
A small glowing energy orb projectile for a top-down action game.
A round bright gold (#FFC23C) core with a white-hot center and a thin bright
outer ring. Looks identical from every direction.

Centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge. Bold dark outline and flat cel shading.
Flat even lighting, no soft glow bloom baked into the image, no drop shadow,
no vignette, no gradient background, no perspective, no ground plane.
The subject fills about 85% of the frame with even margin on all sides.
No text, no logo, no watermark, no frame or border.
```

## E-2. `ProjectileRapid` — 연사 탄환

> ⚠️ **이 대상만 흰색으로 뽑는다.** 원래 색인 라임(`#C7FF00`)은 녹색 지배도가 높아
> 크로마키에 배경과 함께 지워진다. **흰색으로 생성한 뒤 `Projectile_Rapid.prefab`의
> `m_Color`를 기존 값(`0.78, 1, 0`) 그대로 두어 틴트로 라임을 입힌다.**
> 다른 대상은 목표 색으로 직접 생성하고 틴트를 흰색으로 되돌린다 — 여기만 반대다.

E-1과 **형태를 다르게** 해야 플레이 중 구분된다 — 기본탄은 둥근 구체, 연사탄은 작은 각진 결정.

```
A small glowing energy shard projectile for a top-down action game.
A compact faceted crystal in pure white with a pale cyan (#35D9F5) edge,
smaller and sharper than a round bullet. No green or lime tones anywhere.

Centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge. Bold dark outline and flat cel shading.
Flat even lighting, no soft glow bloom baked into the image, no drop shadow,
no vignette, no gradient background, no perspective, no ground plane.
The subject fills about 85% of the frame with even margin on all sides.
No text, no logo, no watermark, no frame or border.
```

## E-3. `BossRadialProjectile` — 보스 원형탄

현재 Unity 내장 **흰 사각형**이다. 보스 탄막이 네모인 게 지금 가장 눈에 띄는 결함이다.

```
A hostile energy orb for a top-down bullet-hell boss attack.
A round spiked sphere in hot pink (#FF5488) with a dark core and short blades
arranged evenly around it, looking identical from every direction.

Centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge. Bold dark outline and flat cel shading.
Flat even lighting, no soft glow bloom baked into the image, no drop shadow,
no vignette, no gradient background, no perspective, no ground plane.
The subject fills about 85% of the frame with even margin on all sides.
No text, no logo, no watermark, no frame or border.
```

---

# F. 보스 공격 예고

`BossTelegraph.cs:43`이 `Quaternion.Euler`로 회전시키는 경우가 있으므로 **정원(正圓) 기준**으로 만든다.

## F-1. `TelegraphRing` — 예고 링

```
A thin warning ring for a top-down game attack indicator.
A single perfect circle outline in warning red (#FF1E1E), evenly thick all
the way around, hollow in the middle, with a subtle dashed inner edge.
Perfectly centered and perfectly circular.

Centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge. Flat even lighting, no soft glow bloom baked
into the image, no drop shadow, no vignette, no gradient background, no
perspective, no ground plane.
The subject fills about 85% of the frame with even margin on all sides.
No text, no logo, no watermark, no frame or border.
```

## F-2. `TelegraphFill` — 예고 영역 채움

```
A filled circular danger zone for a top-down game attack indicator.
A solid disc in warning red (#FF1E1E) at low visual weight, with a slightly
brighter clean edge and faint concentric rings inside. Perfectly circular
and centered.

Centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge. Flat even lighting, no soft glow bloom baked
into the image, no drop shadow, no vignette, no gradient background, no
perspective, no ground plane.
The subject fills about 85% of the frame with even margin on all sides.
No text, no logo, no watermark, no frame or border.
```

---

# G. `TreasureChestSciFi` — 보물상자 (정지 1장)

**우선순위 최하.** 등장 빈도가 낮고 `TreasureChest.cs:37`이 이미 깜빡임 연출을 갖고 있다.
시간이 모자라면 기존 에셋을 유지한다.

```
A chibi sci-fi treasure container for a top-down action game, facing the
viewer. A chunky armored vault box with a gold (#FFC23C) glowing seam across
the lid, dark metal body, rounded corners, and small cyan (#35D9F5) indicator
lights. Cute and blocky.

Centered on a solid bright green (#00FF00) chroma-key background that fills
the entire canvas edge to edge. Bold dark outlines and
flat cel shading. Flat even lighting, no soft glow bloom baked into the
image, no drop shadow, no vignette, no gradient background, no perspective,
no ground plane.
The subject fills about 85% of the frame with even margin on all sides.
No text, no logo, no watermark, no frame or border.
```

---

# 반려 처리

**대상당 3안을 뽑고 1안을 채택한다.** 반려본은 `ArtSource/Generated/Archive/`로 보내고
사유를 `REJECTED.md`에 남긴다 — 현재 폐기본 8장이 사유 기록 없이 방치돼 있다.

반려 대안 사다리는 [art-spec.md](art-spec.md) 9절.
스트립이 계속 흔들리면 **정지 1장 + 코드 기반 바운스**로 후퇴한다 — 무손실이다.
