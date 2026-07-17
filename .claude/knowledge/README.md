# Knowledge Base — Rewind Survivors

이 프로젝트의 AI 개발 하네스가 참조하는 **문제해결 지식 베이스**다.
버그를 고칠 때마다 "또 밟을 만한 패턴"을 엔트리로 축적하고, 새 문제를 만나면
**먼저 이 KB를 검색**해 유사 패턴을 참조한 뒤 구현한다.

> 이건 장식용 문서가 아니라 실제 워크플로에 연결(wiring)된 자산이다.
> 트리거는 [CLAUDE.md](../../CLAUDE.md)의 `## 하네스` 섹션에 명시돼 있다.

## 왜 만들었나

LLM은 매 세션 컨텍스트를 새로 읽는다. 과거에 이 프로젝트에서 밟은 함정을
기억하지 못하면 **같은 버그를 반복 유도**한다. KB는 그 기억을 외부화해,
다음 세션의 AI가 검색해서 재사용하게 만든다.

- Unity/Job System/Steering 특유의 함정은 재발 위험이 크다
- "증상 → 근본 원인 → 해결 → 재인식 패턴" 형식으로 검색·재사용 가능하게 저장
- 각 엔트리는 실제 커밋에 근거한다 (`commit` 프론트매터로 추적)

## 워크플로

```
버그/기능 요청
   │
   ▼
[1] KB 검색  ── tags·symptom 프론트매터로 grep, 유사 패턴 확인
   │
   ▼
[2] Planner → Coder → Reviewer  (unity-dev 파이프라인)
   │
   ▼
[3] 재발성 문제였다면 → 신규 엔트리로 KB에 저장
```

## 구조

| 폴더 | 담는 것 | 예 |
|------|--------|-----|
| `bug-patterns/` | 재발 방지용 버그 패턴 + 해결책 | 판정 틈, Job 유령 슬롯 |
| `lessons-learned/` | 게임플레이 설계 교훈 | steering 가중치 클램핑 |
| `workflows/` | 반복 작업 절차 | 버그수정 → KB 파이프라인 |

## 인덱스

### bug-patterns
- [orbit-tick-hit-gap](bug-patterns/orbit-tick-hit-gap.md) — 틱 방식 판정이 빠른 이동체를 건너뛰는 틈
- [jobsystem-uninitialized-slot-ghost](bug-patterns/jobsystem-uninitialized-slot-ghost.md) — NativeArray 전체 용량 순회 시 (0,0) 유령 슬롯이 가짜 이웃으로 작동
- [separation-overpowers-target-drift](bug-patterns/separation-overpowers-target-drift.md) — steering 가중치가 목표 추적을 압도해 반대로 드리프트
- [jsonutility-missing-field-null-collection](bug-patterns/jsonutility-missing-field-null-collection.md) — JsonUtility가 없는 필드 초기화를 건너뛰어 구버전 세이브의 컬렉션이 null
- [procedural-sprite-not-serialized](bug-patterns/procedural-sprite-not-serialized.md) — 코드 생성 Texture2D/Sprite를 프리팹에 구우면 직렬화 안 돼 런타임 흰 박스 → 런타임 컴포넌트로 재생성

### lessons-learned
- [steering-weight-clamping](lessons-learned/steering-weight-clamping.md) — 여러 steering 힘 합성 시 상한 클램프 원칙

### workflows
- [bugfix-to-knowledge-pipeline](workflows/bugfix-to-knowledge-pipeline.md) — 버그 수정 후 KB 엔트리화 절차

## 엔트리 작성 규칙

1. **1파일 = 1패턴.** 억지로 개수 채우지 않는다. 재발 위험이 실제로 있을 때만.
2. 프론트매터에 `tags`, `symptom`, `commit`을 반드시 넣어 검색 가능하게 한다.
3. `## 재인식 패턴` 섹션은 필수 — "이런 조합이 보이면 이 버그 의심"을 한 줄로.
