---
title: 틱 방식 판정이 빠른 이동체를 건너뛰는 틈
tags: [collision, orbit, weapon, tick, spatial-hash, cooldown]
symptom: 궤도(Orbit) 무기가 빠르게 회전하면 적을 판정 없이 통과
severity: high
commit: 07799f3
files: [Assets/Scripts/Weapon/OrbitWeapon.cs, Assets/Scripts/Weapon/OrbitWeaponData.cs]
---

## 증상
Orbit 위성 무기가 회전 속도가 빠를 때 적을 스치고도 데미지가 들어가지 않음.
회전이 느릴 땐 정상.

## 근본 원인
`hitInterval` 틱 방식으로 일정 주기마다만 판정했다. 위성이 빠르게 도는 경우
판정 틱과 틱 사이에 적을 지나쳐 버려, 실제 겹침이 있었는데도 판정 프레임에
걸리지 않는 **판정 틈**이 생긴다. 판정 주기가 이동 속도에 종속돼 있었던 게 문제.

## 해결
- 틱 대신 **매 프레임 `QueryNeighbors`** 로 겹침을 검사.
- 같은 적을 연속 프레임에 반복 타격하지 않도록 **EntityId별 `hitCooldown`**
  (`_pulseTime` 기준)을 둠 → 회전 속도와 무관하게 일관된 타격.
- 여러 위성이 동시에 같은 적을 때리는 경우도 쿨다운으로 자연 dedup.

## 재인식 패턴 ⚠️
**"주기적 판정(틱/interval)" + "빠른 이동체"** 조합이 보이면 판정 틈 의심.
→ 판정을 매 프레임으로 바꾸고, 중복 타격은 **대상별 쿨다운**으로 억제하라.
판정 빈도가 이동 속도에 종속되면 안 된다.

## 관련
- [[separation-overpowers-target-drift]] — 같은 커밋대 적 이동 계열 이슈
