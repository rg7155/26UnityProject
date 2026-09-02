---
title: MeshRenderer 의 sortingOrder 는 Inspector 에 없다 — 기본값 0 이 음수 Order 스프라이트를 가린다
tags: [2d, urp, rendering, sorting-order, meshrenderer, spriterenderer, background, telegraph]
symptom: 배경 교체 후 보스 예고원(장판)이 화면에 안 보임. 화면 흔들림·피해 판정은 정상 동작해 로직 버그로 오인하기 쉬움
severity: high
files: [Assets/Scripts/Utils/ScrollingBackground.cs, Assets/Scenes/GameScene.unity]
---

## 증상
무한 스크롤 배경을 GL 그리드에서 **타일 텍스처 쿼드 2겹(MeshRenderer)** 으로 교체한 뒤,
보스 슬램 패턴의 바닥 예고원이 **완전히 보이지 않게** 되었다.
화면 흔들림과 피해 판정은 정상이라 "예고원 표시 로직이 깨졌다"고 의심하게 되지만,
`BossTelegraph` 코드에는 아무 문제가 없다.

## 근본 원인
2D 렌더 순서는 **`sortingLayer` → `sortingOrder` → 거리(z)** 순으로 결정된다.
**`sortingOrder` 가 z 보다 우선한다.**

배경 쿼드를 z 로 멀리 밀어 "깊이상 뒤"로 배치해도, `sortingOrder` 가 크면 앞에 그려진다.

| 오브젝트 | sortingOrder |
|---|---|
| `BG_Base` / `BG_Detail` (MeshRenderer) | **0** (기본값) |
| `Telegraph_Fill` / `Telegraph_Ring` (SpriteRenderer) | **-20 / -19** |

플레이어(10)·발사체(5)·보스탄(8)은 전부 0보다 커서 멀쩡했고,
**음수 Order 를 쓰는 예고원만** 배경에 먹혔다. 그래서 "일부만 안 보이는" 형태로 나타난다.

결정적으로 **`MeshRenderer` 는 Sorting Layer / Order in Layer 필드를 Inspector 에 노출하지 않는다.**
`SpriteRenderer` 에만 있다. 그래서 값을 확인하려 Inspector 를 열어도 **필드 자체가 없어서**
"설정이 잘못됐을 리 없다"고 넘어가게 된다. API 에는 존재하므로 **코드로만 지정 가능**하다.

## 해결
렌더러 참조를 이미 들고 있는 컴포넌트의 `Awake` 에서 지정:

```csharp
// sortingOrder 는 깊이보다 우선한다 — 기본값 0 이면 음수 Order 인 보스 예고원(-20/-19)을
// 통째로 가린다. MeshRenderer 는 Inspector 에 이 필드가 없어 코드로만 지정할 수 있다.
_baseQuad.sortingOrder   = -100;
_detailQuad.sortingOrder = -99;
```

씬 오브젝트의 값을 손으로 고치는 방식은 **불가능**하다(필드가 UI 에 없다).
코드 소유로 두면 씬 편집·프리팹 재생성에도 살아남는다.

## 재인식 패턴 ⚠️
**"배경/이펙트를 SpriteRenderer 가 아닌 렌더러(MeshRenderer·LineRenderer·ParticleSystem)로
교체했다"** + **"그 뒤 일부 오브젝트만 안 보인다"** 면 sortingOrder 를 먼저 의심하라.

→ 안 보이는 것들의 공통점이 **음수 Order** 인지 확인하라. 그렇다면 원인은 로직이 아니라 렌더 순서다.
→ z 를 아무리 밀어도 해결되지 않는다. **sortingOrder 가 z 보다 우선**하기 때문.
→ Inspector 에 필드가 없다고 해서 값이 없는 게 아니다. 기본값 **0** 이 조용히 적용돼 있다.

렌더 순서를 쓰는 오브젝트를 추가하면 `문서/개발/에디터_설정.md` 의 **Order in Layer 표에 반드시 등재**할 것.
이 버그는 배경이 그 표에 없어서 발생했다.

## 관련
- `문서/개발/에디터_설정.md` > `Order in Layer — 2D 스프라이트 렌더 순서`
- 렌더 순서 문제는 로직 버그처럼 보인다 — 판정·연출이 정상 동작하면 렌더링 쪽을 먼저 보라.
