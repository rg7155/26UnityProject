---
title: 여러 steering 힘을 합성할 땐 보조 힘을 주 힘 아래로 클램프한다
tags: [steering, movement, design-principle, separation, clamp]
origin_commit: 1ff2f27
---

## 교훈
군중 이동은 여러 steering 힘의 가중합으로 만든다
(target 추적 + separation + avoidance …). 이때 **개수·밀도에 비례해 커지는
보조 힘**(separation 등)은 상황에 따라 주 힘(target)을 압도할 수 있다.
그러면 "목표를 향한다"는 의도가 밀집 상황에서 깨진다.

## 원칙
- **주 힘(의도)은 항상 우선**이어야 한다 → 보조 힘에 상한(clamp)을 둔다.
- 보조 힘의 최대 크기를 주 힘 가중치 아래로 고정 (예: target 1.0, separation ≤ 0.8).
- "밀집할수록 목표를 등지는" 증상이 보이면 가장 먼저 가중치 상한을 의심.

## 왜 비자명한가
개별 이웃과의 separation은 각각 작아 보여도, 이웃 수가 많으면 **합이 누적**돼
target을 넘긴다. 힘 하나하나가 아니라 **합성 결과의 크기**를 통제해야 한다.

## 근거 사례
- [[separation-overpowers-target-drift]] — 이 원칙을 도출한 실제 버그
