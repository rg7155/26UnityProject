---
title: NativeArray 전체 용량 순회 시 (0,0) 유령 슬롯이 가짜 이웃으로 작동
tags: [job-system, burst, nativearray, separation, steering, count]
symptom: 적이 화면 좌상단(원점 방향)으로 미묘하게 쏠림 / 존재하지 않는 이웃에 밀려남
severity: high
commit: 1ff2f27
files: [Assets/Scripts/Enemy/SeparationJob.cs, Assets/Scripts/Enemy/EnemyJobScheduler.cs]
---

## 증상
Separation(적끼리 밀어내기) 적용 시, 활성 적이 적은데도 적들이 원점(0,0)
방향의 보이지 않는 무언가에 밀려나는 것처럼 이동이 왜곡됨.

## 근본 원인
`SeparationJob`이 NativeArray의 **논리적 활성 개수가 아니라 배열 전체 용량**을
순회했다. 풀/고정 용량 배열에서 미사용 슬롯은 기본값 `(0,0)` 위치를 갖는데,
Separation 계산은 이 (0,0) 유령 위치를 **실제 이웃으로 착각**해 그쪽에서
밀어내는 힘을 만들어냈다. 결과적으로 존재하지 않는 적이 힘을 행사.

## 해결
- 활성 개수를 담는 `Count` 필드를 도입.
- Job이 `Count`까지만 순회하도록 변경 → 미사용 슬롯 제외.

## 재인식 패턴 ⚠️
**"고정 용량 NativeArray/풀 배열" + "전체 length 순회"** 조합 → 유령 슬롯 의심.
→ 반드시 **논리적 활성 개수(Count)** 로 순회 범위를 제한하라.
기본값 슬롯이 계산에 섞이면 원점 방향 쏠림 같은 미묘한 편향으로 나타난다.

## 관련
- [[separation-overpowers-target-drift]] — 같은 Separation 시스템의 다른 버그
- [[steering-weight-clamping]] — steering 힘 합성 원칙
