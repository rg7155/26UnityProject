---
title: 좌우 반전을 부모 localScale.x 로 하면 자식이 함께 미러링돼 궤도 위성이 순간이동
tags: [unity, transform, localscale, flip, sprite-renderer, hierarchy, orbit, weapon]
symptom: "플레이어가 좌우로 방향을 바꾸는 순간 Orbit 위성이 반대편으로 순간이동하고 회전 방향이 뒤집힘"
commit: (pending)
files: [Assets/Scripts/Player/PlayerController.cs, Assets/Scripts/Weapon/OrbitWeapon.cs]
---

## 증상

측면 뷰 아트를 도입하면서 좌우 반전을 넣었더니, 플레이어가 방향을 바꾸는 순간
주위를 도는 Orbit 위성이 반대편으로 튀고 회전 방향도 거꾸로 보인다.
피격 판정은 정상이라(월드 좌표를 쓰므로) 로직 버그로 오인하기 쉽다.

## 근본 원인

반전을 `transform.localScale.x = -1` 로 구현했다. **스케일은 자식 전체에 상속된다.**

`OrbitWeapon.cs:44` 가 위성을 플레이어 자식으로 붙이고(`Instantiate(prefab, transform)`),
`Update` 에서 `localPosition = (cos θ, sin θ) * radius` 로 배치한다.
부모 스케일 x 가 −1 이 되면 월드 x 오프셋이 뒤집혀, 각도 θ 의 위성이 180°−θ 위치에 그려진다.
즉 **반전하는 순간 전 위성이 거울 위치로 점프**하고 회전 방향도 반대로 보인다.

궤도가 원이라 형태는 같아 보이지만 위상이 뒤집히는 것이 핵심이다.

## 해결

플레이어는 `SpriteRenderer.flipX` 로 반전한다. 렌더링에만 적용돼 transform 계층을 건드리지 않는다.

```csharp
if (input.x != 0f && _sprite != null)
    _sprite.flipX = input.x < 0f;   // 아트는 오른쪽을 보는 것이 기본
```

**적은 `localScale` 을 계속 쓴다.** GPU 인스턴싱 경로라 `SpriteRenderer` 자체가 없고
(`EnemyInstanceRenderer` 가 `localToWorldMatrix` 를 넘긴다), 적 프리팹에는 자식이 없어
같은 문제가 생기지 않는다. 단 음수 스케일은 와인딩을 뒤집으므로 머티리얼 `_Cull` 을 `0`(Off) 으로 둬야 한다.

## 재인식 패턴 ⚠️

- **자식을 가진 오브젝트에 `localScale` 부호 반전을 넣기 전에 자식 목록을 먼저 본다.**
  무기·이펙트·UI 앵커가 자식으로 붙어 있으면 전부 미러링된다
- 반전 순간 자식이 "순간이동"하는데 판정은 멀쩡하다 → 부모 스케일 상속을 의심한다
- `SpriteRenderer` 가 있으면 `flipX` 가 정답이다. `localScale` 은 SpriteRenderer 가 없는
  인스턴싱 경로에서만 어쩔 수 없이 쓴다
- 같은 이유로 `localScale` 은 크기 조절에도 쓰지 않는다 — Collider 가 함께 커진다
  (`문서/개발/에디터_설정.md` 크기 조절 원칙). 피격 punch 를 렌더 행렬에만 곱하는 것도 같은 맥락
