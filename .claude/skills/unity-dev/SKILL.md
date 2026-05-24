---
name: unity-dev
description: "Rewind Survivors Unity 게임의 모든 코드 작업 오케스트레이터. 기능 추가, 버그 수정, 코드 개선, 적 추가, 시스템 구현, 폴리싱 등 C# 코드를 건드리는 모든 요청에 반드시 이 스킬을 사용. 트리거 키워드: '추가해줘', '구현해줘', '만들어줘', '수정해줘', '고쳐줘', '개선해줘', '보완해줘', '다시 만들어', '리팩토링', '버그', '적 종류', '웨이브', '되감기', '리와인드', '업그레이드', 'UI', '이펙트', '사운드', '폴리싱'. 단순한 코드 설명 질문('이 코드가 뭐야?', '왜 이렇게 돼?')은 직접 답변 가능."
---

# Unity Dev 오케스트레이터

Rewind Survivors 프로젝트 코드 작업을 위한 순차 파이프라인.

## 실행 모드: 서브 에이전트 (파이프라인)

**순서:** Phase 0 (컨텍스트) → Phase 0.5 (기획 검토) → Phase 1 (계획) → Phase 2 (구현) → Phase 3 (리뷰) → Phase 4 (보고)

---

## Phase 0: 컨텍스트 확인

실행 전 먼저 확인:

1. `_workspace/` 디렉토리 존재 여부:
   - **없음** → 초기 실행
   - **있음 + 부분 수정 요청** → Phase 2(Coder)만 재실행 (`_workspace/plan.md` 재활용)
   - **있음 + 새 작업 요청** → `_workspace/`를 `_workspace_prev/`로 이름 변경 후 새 실행

2. `개발_진행상황.md` 첫 60줄 읽어 현재 단계 파악

---

## Phase 0.5: 요청 명확화 + 기획 검토 (오케스트레이터 직접 실행)

Phase 0 직후, Phase 1 진입 전에 순서대로 실행.

### Step 1: 요청 명확화
요청이 모호하거나 구현 방향이 여러 갈래면 **먼저 질문**하고 답변 받은 뒤 진행.
질문이 필요 없을 만큼 명확하면 스킵.

질문이 필요한 경우 예시:
- 범위가 불명확: "적 AI 개선해줘" → 어떤 적? 어떤 동작?
- 방법이 여러 개: 구현 방식 선택지가 있을 때
- 기존 시스템과 충돌 가능성이 있을 때

### Step 2: 기획 검토
아래 두 관점으로 간략히 평가하고 사용자에게 보고.

**관점 1 — 게임 기획자**
- 이 기능이 플레이어 경험에 어떤 가치를 더하는가?
- 게임 핵심 루프(생존 + 시간 되돌리기)와 어울리는가?
- 재미·밸런스·몰입도에 미치는 영향은?

**관점 2 — 포트폴리오 완성도**
- 어떤 기술 역량을 보여주는가?
- 현재 개발 단계(개발_진행상황.md 기준) 우선순위에 맞는가?
- 구현 비용 대비 포폴 가치는?

**보고 형식 (간결하게)**
```
[기획] 한 줄 평가 + 핵심 고려사항 1~2개
[포폴] 한 줄 평가 + 핵심 고려사항 1~2개
[권장] 진행 / 수정 후 진행 / 다른 기능 우선
```

사용자 확인 후 Phase 1 진행.

**Phase 0.5 전체 스킵 조건** (아래에 해당하면 바로 Phase 1):
- 버그 수정 요청
- 개발_진행상황.md의 기존 TODO 항목 그대로 구현
- 사용자가 이미 충분히 논의 후 "구현하자"로 확정한 경우

---

## Phase 1: 계획 수립 (game-planner)

`game-planner` 에이전트를 서브 에이전트로 호출:

```
Agent(
  description: "Unity 기능 구현 계획 수립",
  subagent_type: "Plan",
  model: "opus",
  prompt: [아래 내용 포함]
)
```

**Planner 프롬프트에 포함할 내용:**
- 사용자 요청 (원문 그대로)
- `Assets/Scripts/` 하위 파일 목록 (Glob으로 수집)
- CLAUDE.md 핵심 제약사항:
  - 네이밍: private `_camelCase` / public `PascalCase` / 메서드 `PascalCase`
  - Input: Polling 방식만 (`Keyboard.current.wKey.isPressed`)
  - 참조: 씬 간은 `FindObjectOfType`, 프리팹 내부는 Inspector
  - 금지: DI 프레임워크, Action-based Input, 과도한 추상화
- 출력 지시: `_workspace/plan.md` 생성

---

## Phase 2: 코드 구현 (game-coder)

Phase 1 완료 후 `_workspace/plan.md` 읽기. 그 후 `game-coder` 에이전트 호출:

```
Agent(
  description: "Unity C# 코드 구현",
  subagent_type: "general-purpose",
  model: "opus",
  prompt: [아래 내용 포함]
)
```

**Coder 프롬프트에 포함할 내용:**
- `_workspace/plan.md` 전체 내용
- 네이밍 컨벤션 요약 (private `_camelCase`, public `PascalCase`, 메서드 `PascalCase`)
- Input System: Polling 방식 (`Keyboard.current`) 고정
- 참조 방식: 씬 간 `FindObjectOfType`, Inspector는 프리팹 내부로 한정
- 구현 지시: plan.md 단계 순서대로, 기존 파일은 Read → Edit, 새 파일은 Write
- 리뷰 재실행 시: `_workspace/review.md` 내용도 함께 전달

---

## Phase 3: 코드 리뷰 (game-reviewer)

Phase 2 완료 후 `game-reviewer` 에이전트 호출:

```
Agent(
  description: "구현 코드 검토",
  subagent_type: "general-purpose",
  model: "opus",
  prompt: [아래 내용 포함]
)
```

**Reviewer 프롬프트에 포함할 내용:**
- `_workspace/plan.md`에서 변경/생성 파일 목록 추출
- 검토 체크리스트:
  - 네이밍 위반 (private `_camelCase`, public `PascalCase`, 메서드 `PascalCase`)
  - 금지 패턴 (DI 프레임워크, Action-based Input, 과도한 추상화)
  - Update에서 Find 반복 호출
  - 이벤트 구독/해제 쌍 확인
- 출력 지시: `_workspace/review.md` 생성

---

## Phase 4: 결과 보고

1. `_workspace/review.md` 읽기
2. **수정 필요 사항 있음:** 요약 보고 후 사용자 확인 → 승인 시 Phase 2 재실행
3. **이상 없음:** 완료 보고
4. `개발_진행상황.md` 업데이트 필요 여부 사용자에게 제안
   - CLAUDE.md 커밋 규칙: 완료 항목 `⬜ → ✅`, 비고란에 구현 방식 한 줄 기재

---

## 빠른 리뷰 경로

사용자가 "코드 리뷰해줘" / "이 파일 검토해줘" 등 **구현 없이 리뷰만 요청** 시:
- Phase 1, 2 생략
- 대상 파일을 직접 Reviewer에 전달
- Phase 3, 4만 실행

---

## 에러 핸들링

| 상황 | 대응 |
|------|------|
| Planner가 프로젝트 범위 초과 감지 | 사용자에게 알리고 범위 내 대안 제안 |
| Coder가 파일 일부 실패 | 실패 파일 명시, 해당 파일만 재시도 1회 |
| Reviewer가 심각한 위반 발견 | `review.md` 요약 보고 → 수정 승인 시 Coder 재호출 |
| _workspace/ 충돌 | 기존 `_workspace_prev/` 삭제 후 재이동 |

---

## 테스트 시나리오

**정상 흐름:** "적이 죽을 때 파티클 이펙트 추가해줘"
1. Planner: EnemyBase.OnDead 이벤트 활용 계획 → `_workspace/plan.md`
2. Coder: EnemyBase.cs 수정, DeathEffect 프리팹 연결
3. Reviewer: 네이밍/패턴 검증 → `_workspace/review.md` (합격)
4. 완료 보고 + 개발_진행상황.md 업데이트 제안

**에러 흐름:** Reviewer가 `private float speed` 네이밍 위반 발견
→ "review.md에서 수정 필요 사항 발견: EnemyBase.cs L42 — `speed` → `_speed`"
→ 사용자 승인 후 Coder 재호출
