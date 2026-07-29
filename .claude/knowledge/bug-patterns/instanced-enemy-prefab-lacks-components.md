---
title: GPU 인스턴싱 적 프리팹엔 SpriteRenderer·Rigidbody2D가 없어, 복제해 만든 개별 렌더 오브젝트가 보이지 않거나 중력에 떨어짐
tags: [gpu-instancing, prefab, spriterenderer, rigidbody2d, gravity, boss, invisible, drawmeshinstanced]
symptom: 잡몹 프리팹을 복제해 만든 보스가 화면에 전혀 안 보인다. 하이어라키에서도 못 찾겠는데 피해와 화면 흔들림은 주기적으로 발생. Rigidbody2D를 추가하니 이번엔 y좌표가 -1000까지 계속 떨어진다
severity: high
commit: (pending)
files: [Assets/Scripts/Enemy/EnemyInstanceRenderer.cs, Assets/Prefabs/Enemy/Enemy.prefab, Assets/Prefabs/Enemy/Boss_Monolith.prefab]
---

## 증상
보스(=개별 렌더링이 필요한 소수 개체)를 만들려고 기존 잡몹 프리팹을 복제했다.
프리팹 미리보기에는 도형이 보이는데 **플레이 중에는 아무것도 안 보인다.**
그런데 5초마다 화면이 흔들리고 플레이어가 피해를 입는다 — 로직은 돌고 있다.

이어서 Rigidbody2D를 추가하자 이번엔 보스 y좌표가 매 프레임 감소해 **-1000까지 낙하**했다.

## 근본 원인

### 1) 안 보이는 이유 — 잡몹 프리팹엔 SpriteRenderer가 없다
3개월차 최적화에서 `EnemyInstanceRenderer`(`Graphics.DrawMeshInstanced`)를 도입하며
**잡몹 프리팹의 SpriteRenderer를 제거**했다. 잡몹의 겉모습은 프리팹이 아니라
씬의 `@Renderers/Renderer_*` 오브젝트가 매 `LateUpdate`에 그린다.

`EnemyInstanceRenderer.Register`는 `_registry`에 없는 프리팹에 대해 **조용히 no-op**이다.
보스는 의도적으로 인스턴싱에서 제외했으므로(1개체라 이득 0 + 개체별 색 제어 불가)
**그리는 주체가 아무도 없는 상태**가 된다. 에러도 경고도 없다.

### 2) 위치를 못 찾은 이유 — 풀 오브젝트는 @Pool 자식
`ObjectManager.Get`이 `Object.Instantiate(prefab, _root)`로 생성하므로
스폰된 적/보스는 현재 씬이 아니라 **`DontDestroyOnLoad > @Pool`** 아래에 있다.
GameScene 하이어라키를 아무리 뒤져도 안 나온다.

### 3) 피해만 들어온 이유 — 원거리 패턴은 거리 제한이 없다
장판(GroundSlam)은 **플레이어 위치**를 노린다. 보스가 화면 밖 어디에 있든 발동한다.
즉 "보이지 않는데 맞는" 상태는 모순이 아니라 자연스러운 결과다.

### 4) 낙하한 이유 — Rigidbody2D 기본값이 Dynamic + Gravity 1
잡몹 프리팹엔 **Rigidbody2D 자체가 없다**(Collider2D만 있고, 트리거 콜백은
플레이어 쪽 Rigidbody2D가 발생시킨다). 그래서 "Rigidbody2D 설정을 바꿔라"는
지시를 따르려면 **새로 추가**할 수밖에 없고, Unity 기본값이 Dynamic + Gravity Scale 1이다.
이동을 `transform.position`으로 하는 프로젝트에서 Dynamic 바디는 물리 낙하를 그대로 먹는다.

## 해결
개별 렌더링이 필요한 적(보스 등)은 잡몹 프리팹을 복제한 뒤 **빠진 컴포넌트를 직접 채운다.**

| 컴포넌트 | 설정 | 이유 |
|---|---|---|
| `SpriteRenderer` | 새로 추가. Sprite/Color 지정 | 인스턴싱 렌더러가 안 그려준다. 페이즈 색 전환·피격 플래시도 이게 있어야 가능 |
| `Rigidbody2D` | **Body Type = Kinematic, Gravity Scale = 0**, Sleeping Mode = Never Sleep | Dynamic이면 낙하 + 잡몹/발사체에 밀려 패턴 시작 위치가 흔들린다. Never Sleep은 `OnTriggerStay2D` 유지용 |
| `Collider2D` | 복제로 승계됨(Is Trigger) | — |

새로 만드는 **발사체 프리팹도 같은 함정**이다. 잡몹 프리팹을 복제 원본으로 삼지 말고
빈 GameObject에서 SpriteRenderer + Collider2D(Trigger) + Kinematic Rigidbody2D를 직접 붙인다.

## 재인식 패턴 ⚠️
- **"프리팹 미리보기엔 보이는데 플레이 중 안 보인다" + 로직은 정상 동작** →
  그 오브젝트를 그리는 주체가 프리팹 안이 아니라 **외부 렌더러**인지 의심.
  GPU 인스턴싱/커스텀 드로우를 쓰는 프로젝트에서 "인스턴싱 제외" 결정을 내렸다면
  **렌더 컴포넌트를 되돌려 놓아야 한다.** 등록 실패가 조용한 no-op이면 단서가 전혀 없다.
- **스폰된 오브젝트를 씬 하이어라키에서 못 찾겠다** → `DontDestroyOnLoad > @Pool` 확인.
  플레이 중 하이어라키 검색창에 이름을 치는 게 가장 빠르다.
- **보이지 않는데 피해는 들어온다** → 원거리/플레이어 추적형 패턴은 거리 제한이 없다.
  개체가 죽은 게 아니라 **화면 밖에 살아있다**고 봐야 한다. 좌표부터 찍어라.
- **오브젝트가 -y로 무한 이동** → Rigidbody2D가 Dynamic + Gravity 1인지 확인.
  `transform.position`으로 이동하는 프로젝트에 Dynamic 바디를 새로 붙이면 항상 이 증상이 난다.
- 일반화: **최적화를 위해 프리팹에서 표준 컴포넌트를 제거한 프로젝트**에서는,
  그 프리팹이 "정상적인 프리팹"이라는 가정이 깨진다. 복제해 새 개체를 만들기 전에
  **무엇이 빠져 있는지 먼저 확인**하라.

## 관련
- 인스턴싱 제외 판단 근거와 대안 비교는 보스 계획서(`_workspace/plan.md` §0) 참조
- 풀 오브젝트의 씬 경계 문제는 [pooled-object-survives-scene-change](pooled-object-survives-scene-change.md)
