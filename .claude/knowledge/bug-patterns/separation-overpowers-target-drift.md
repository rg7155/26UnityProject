---
title: steering 가중치가 목표 추적을 압도해 적이 반대로 드리프트
tags: [steering, separation, movement, weight, clamp, enemy-mover]
symptom: 적이 밀집하면 플레이어를 향하지 않고 오히려 반대 방향으로 흘러감
severity: medium
commit: 1ff2f27
files: [Assets/Scripts/Enemy/EnemyMover.cs]
---

## 증상
적이 많이 뭉치는 상황에서, 개별 적이 플레이어 추적을 멈추고 군집 바깥
(플레이어 반대 방향)으로 드리프트함.

## 근본 원인
이동은 두 힘의 합성이다: **target 추적(가중치 1.0)** + **separation(밀어내기)**.
밀집 시 주변 이웃이 많아지면 separation 힘의 합이 누적돼 target 추적(1.0)을
**압도**한다. 그러면 합성 벡터가 플레이어가 아니라 군집을 벗어나는 방향을 가리켜,
적이 목표를 등지고 흐른다. steering 힘에 상한이 없던 게 원인.

## 해결
- `EnemyMover`에 `_maxSeparation`(0.8) 클램프 추가.
- separation 힘의 크기를 target 가중치(1.0) 아래로 제한 → 추적이 항상 우선.

## 재인식 패턴 ⚠️
**"여러 steering 힘 가중합" + "한 힘이 개수에 비례해 누적"** → 압도 현상 의심.
→ 보조 힘(separation/avoidance)은 **주 힘(target)보다 작게 클램프**하라.
증상은 "밀집할수록 목표를 등진다".

## 관련
- [[steering-weight-clamping]] — 이 버그에서 일반화한 설계 교훈
- [[jobsystem-uninitialized-slot-ghost]] — 같은 커밋의 Separation 버그
