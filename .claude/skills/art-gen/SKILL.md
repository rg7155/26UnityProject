---
name: art-gen
description: Rewind Survivors의 전투 스프라이트를 AI 이미지 생성으로 만들어 게임에 반입하는 방법. 치비 캐릭터 네온 규격 v2, 대상별 프롬프트 원문, 그린스크린 크로마키 반입 자동화(tools/art_import.py), 임포트 설정 자동 적용, 4프레임 스트립 애니메이션 규격 포함. 플레이어·적·보스·무기·탄환의 비트맵 아트를 만들거나 리스킨할 때 game-art-director가 반드시 참조.
---

# Art Gen — AI 생성 비트맵 아트 제작 스킬

Rewind Survivors의 **전투 스프라이트**를 AI로 생성해 게임에 넣는 방법.
UI 아트는 이 스킬이 아니라 [ui-kit](../ui-kit/SKILL.md)이 담당한다 — 그쪽은 외부 리소스 0,
코드로 절차 생성이 원칙이다. 두 스킬은 **담당 영역이 겹치지 않는다.**

## 왜 파이프라인인가

v1에는 규격이 없었다. 「4방향 대칭 네온 규격」이라는 말만 `개발_진행상황.md` L109에 있었고
그게 무엇인지 정의한 문서가 없었다. 결과는 이렇다.

- 생성물마다 여백이 달라 **PPU를 인스펙터에서 손으로 맞췄다** — 100 / 577 / 700 / 800 / 1150
- 인스턴싱 적 3종은 Sprite를 못 쓰므로 **머티리얼 `_MainTex` tiling/offset으로 여백을 수동 크롭**했다
  — `0.692/0.154`, `0.35/0.325`, `0.877/0.0615`
- 배경 제거 절차가 문서에도 코드에도 없어 **재현이 불가능**했다
- 반려본 8장이 `Archive/`에 사유 기록 없이 남았다

**v2는 여백을 반입 단계에서 기계적으로 통일해 이 부채를 없앤다.** 여백이 규격화되면
PPU는 단일값, 머티리얼 UV는 `scale 1 / offset 0`으로 수렴한다.
수동 조정이 다시 등장하면 그건 규격이나 스크립트를 고쳐야 한다는 신호다.

## 3-단계 구조

```
[3] 게임 반입     프리팹/머티리얼 배선, 육안 검증
        ↑
[2] 반입 자동화   tools/art_import.py + GeneratedSpritePostprocessor.cs
        ↑ 소비
[1] 규격 + 프롬프트  references/art-spec.md + references/prompts.md
```

### 레이어 1 — 규격과 프롬프트

- **[references/art-spec.md](references/art-spec.md)** — 해상도, 그린스크린 배경, 정면 뷰·치비 규칙,
  여백, 팔레트(`UITheme.cs` 실측값), 조명, 채택 기준, 반려 대안 사다리
- **[references/prompts.md](references/prompts.md)** — 대상별 프롬프트 **원문 전량**.
  공통 규격 문단을 고정으로 두고 대상 문장 1~2줄만 갈아끼운다. **일관성은 이 고정 문단에서 나온다**

**대상별로 프롬프트를 새로 쓰지 않는다.** 새 대상이 생기면 기존 형식에 한 항목을 더한다.

### 레이어 2 — 반입 자동화

**`tools/art_import.py`**

```
ArtSource/Generated/Source/<Name>_Source.png   (그린스크린, 불투명)
  ① 크로마키 → 알파 + despill (녹색 지배도 d = G - max(R,B) 기준, 경계는 선형 램프)
  ② 가로 스트립 N등분
  ③ 전 프레임 공통 바운딩박스로 크롭   ★ 프레임마다 따로 크롭하면 재생 시 피사체가 떨린다
  ④ 정사각 캔버스 중앙 배치 + 균일 패딩 (FILL_RATIO 0.85)
  ⑤ 512(전투) / 1024(보스) 리샘플 후 가로 스트립으로 재결합
  ⑥ 원형이어야 하는 오브젝트는 대칭 점수 검사 후 경고
Assets/Art/Sprites/Generated/<Name>.png
+ tools/art_import_log.md 에 변환 파라미터 append (재현 가능성)
```

**`Assets/Scripts/Editor/GeneratedSpritePostprocessor.cs`**
`Assets/Art/Sprites/Generated/` 아래 PNG의 임포트 설정을 자동 적용한다 —
Sprite / Pivot Center / Bilinear / Mip off / Clamp / Alpha From Input /
**기본 PPU 512(플레이어는 기존 월드 크기 보존용 236)** /
**실제 스트립만 Multiple + 가로 균등 슬라이싱** /
maxTextureSize는 정지 1024·일반 스트립 2048·보스 스트립 4096.

### 레이어 3 — 게임 반입

| 렌더 경로 | 대상 | 애니메이션 방법 |
|---|---|---|
| SpriteRenderer | Player, Boss_Monolith, Orbiter, TreasureChest, Projectile 계열, Telegraph | `SpriteFrameAnimator`가 `Sprite[]` 교체. **Animator·AnimationClip 불필요** |
| GPU Instancing | 적 3종 (`EnemyInstanceRenderer.cs`) | 커스텀 셰이더의 per-instance `_UVRect` |

**한 형식, 두 경로.** 같은 가로 스트립 PNG를 두 경로가 모두 소비한다.
SpriteRenderer는 잘린 `Sprite[]`를, 인스턴싱은 텍스처와 UV rect를 쓴다.
아트를 한 번만 만들면 되고, **셰이더가 실패해도 SpriteRenderer 경로는 그대로 산다.**

## 절대 규칙

1. **파일명을 바꾸지 않는다.** 리스킨 대상은 기존 이름을 유지한다 — 파일명이 바뀌면
   `.meta`의 guid가 새로 발급되어 프리팹·머티리얼 참조가 전부 끊긴다
2. **PPU와 머티리얼 UV를 인스펙터에서 손으로 맞추지 않는다.** 크기가 안 맞으면 규격이나 스크립트를 고친다
3. **프레임 수는 4로 고정.** 늘릴수록 생성 모델의 프레임 간 일관성이 급격히 나빠진다
4. **스트립은 한 번의 생성으로 뽑는다.** 4장을 따로 생성하면 캐릭터가 흔들린다
5. **반려본은 버리지 않는다.** `ArtSource/Generated/Archive/` + `REJECTED.md`에 사유를 남긴다
6. **발광을 이미지에 굽지 않는다.** 게임의 Bloom(`@PostFX Profile`, intensity 1 / threshold 0.6)과
   이중으로 먹어 형태가 뭉개진다

## 성능 수치 보호

이 프로젝트는 포트폴리오이고, 서류 5곳에 성능 수치가 인용되어 있다 —
`PlayerLoop 90ms 중 Separation 79ms = 88%`, `79ms → 0.6ms`, `1,000마리 약 200FPS`,
`2,000마리 약 40FPS`.

**아트 작업은 Separation Job / SpatialHashGrid / Object Pool / RewindManager 를 건드리지 않는다.**
인스턴싱 셰이더를 도입할 때는 반드시 1,000마리 Profiler를 재측정하고,
수치가 어긋나면 **셰이더를 되돌린다. 수치가 우선이다.**

## 작업 흐름

```
① 규격·프롬프트 확인          references/ 두 파일
② ChatGPT 이미지 생성          대상당 3안
③ 채택 1안 → ArtSource/Generated/Source/<Name>_Source.png
   반려 2안 → Archive/ + REJECTED.md 사유 기록
④ python tools/art_import.py [이름...]
⑤ Unity 에디터에서 육안 검증   art-spec.md 8절 채택 기준
⑥ 프리팹/머티리얼 배선         m_Color 를 흰색으로 되돌림 (연사탄 제외)
⑦ 개발_진행상황.md 갱신        ⬜/🔄 → ✅
```

**⑤에서 반려되면 ②로 돌아간다.** art-spec.md 9절의 대안 사다리를 순서대로 탄다.
