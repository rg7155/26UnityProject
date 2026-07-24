---
name: unity-dev
description: "Rewind Survivors Unity 게임의 모든 코드 작업 오케스트레이터. 기능 추가, 버그 수정, 코드 개선, 적 추가, 시스템 구현, 폴리싱 등 C# 코드를 건드리는 모든 요청에 반드시 이 스킬을 사용. 트리거 키워드: '추가해줘', '구현해줘', '만들어줘', '수정해줘', '고쳐줘', '개선해줘', '보완해줘', '다시 만들어', '리팩토링', '버그', '적 종류', '웨이브', '되감기', '리와인드', '업그레이드', '이펙트', '사운드', '폴리싱', 그리고 UI 관련: 'UI', '상점', '팝업', 'HUD', '타이틀 화면', '결과 화면', '레이아웃', '리스킨', '버튼', '패널', '이쁘게', '디자인'. 후속: '다시 만들어', '재실행', '이 화면도', '스크린샷 반영'. 단순한 코드 설명 질문('이 코드가 뭐야?', '왜 이렇게 돼?')은 직접 답변 가능."
---

# Unity Dev 오케스트레이터

Rewind Survivors 프로젝트 코드 작업을 위한 순차 파이프라인.

## 실행 모드: 서브 에이전트 (파이프라인)

**순서:** Phase 0 (컨텍스트) → Phase 0.5 (명확화 · 게임 기획 · 검토) → Phase 1 (계획) → Phase 2 (구현) → **Phase 3 (리뷰 — 선택, 기본 스킵)** → Phase 4 (보고)

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

### Step 2: 게임 기획 (game-designer) — 신규 콘텐츠/룰일 때만
새 적·무기·룰·메커니즘·진행구조·밸런스처럼 **"무엇을 만들지"가 열려있는** 요청이면 `game-designer` 에이전트를 호출해 기획 제안을 받는다.

```
Agent(
  description: "게임 기획 제안",
  subagent_type: "game-designer",
  model: "opus",
  prompt: [사용자 요청 원문 + 현재 게임 구성 요약 + "핵심 루프(생존+시간 되돌리기)에 맞는 룰·플레이어 경험 설계, Rewind 연계, 밸런스·페이싱, 스코프/우선순위, 대안 제시" 지시]
)
```

- 반환된 기획 제안을 사용자에게 보고 → **사용자가 방향 확정**한 뒤 Step 3로.
- **스킵 조건:** "무엇을"이 이미 명확 — 버그 수정, 스펙 확정된 기능, 기존 TODO 그대로 구현, 이미 충분히 논의해 확정된 경우.

### Step 3: 기획 검토 (포트폴리오 관점 — 오케스트레이터 직접)
Step 2를 했든 안 했든, 진행 전 포폴 관점으로 간략 점검 후 보고.

**관점 — 포트폴리오 완성도**
- 어떤 기술 역량을 보여주는가?
- 현재 개발 단계(개발_진행상황.md 기준) 우선순위에 맞는가?
- 구현 비용 대비 포폴 가치는?

**보고 형식 (간결하게)**
```
[기획] game-designer 제안 요약(했다면) 또는 한 줄 평가
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
- **UI 작업이면:** 구현 주체는 game-ui-artist(코드기반 UGUI). 계획은 `ui-kit`의
  3-레이어(절차적 스프라이트 → 컴포넌트 → `[MenuItem]` 생성기) 순서로 파일을 배치하고,
  기존 UI 로직 스크립트는 무수정 대상으로 명시. 로직+비주얼 혼합이면 파일을 분담.
- 출력 지시: `_workspace/plan.md` 생성

---

## Phase 2: 코드 구현 (game-coder / game-ui-artist)

Phase 1 완료 후 `_workspace/plan.md` 읽기.

### 라우팅 — UI 작업이면 game-ui-artist
요청이 **UI 비주얼**(화면 리스킨, 팝업/HUD/타이틀/결과/상점 UI, 레이아웃·스타일·
폴리싱)이면 `game-coder` 대신 **`game-ui-artist`** 를 호출한다. 이 에이전트는
코드기반 UGUI(절차적 스프라이트 + 컴포넌트 + `[MenuItem]` 에디터 생성기)로 구현하며
`ui-kit` 스킬을 참조한다.

- **순수 UI 비주얼** → game-ui-artist만
- **순수 게임플레이 로직** → game-coder만
- **혼합**(UI + 로직) → game-coder(로직) 먼저 → game-ui-artist(비주얼). 두 에이전트가
  같은 파일을 동시에 건드리지 않도록 plan.md에서 파일을 분담시킨다.

```
Agent(
  description: "UGUI 화면 구현",
  subagent_type: "general-purpose",
  model: "opus",
  prompt: [game-ui-artist 역할 + ui-kit 스킬 참조 지시 + plan.md 내용 + 아래 공통 내용]
)
```

game-ui-artist 호출 시 프롬프트에 반드시 포함: `ui-kit` 스킬을 먼저 읽을 것, 디자인
토큰만 사용(매직값 금지), 외부 리소스 0, 기존 UI 로직 스크립트 무수정, 구현 후 사용자에게
`Tools/UI/...` 메뉴 실행 + 스크린샷 요청 안내.

### 게임플레이 로직이면 game-coder
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

## Phase 3: 코드 리뷰 (game-reviewer) — **선택 (기본 스킵)**

리뷰는 토큰 비용이 크므로 **기본적으로 실행하지 않는다.** UI 리스킨은 스크린샷으로
눈검증되고, 로직 오류는 컴파일·계획 단계에서 대부분 걸러진다. **아래에 해당할 때만** 실행:

- **사용자가 명시적으로 요청** ("리뷰해줘"/"검토해줘"/`/code-review`)
- **위험도 높은 변경**(핵심 게임플레이 로직·아키텍처·세이브/직렬화·되감기 등 눈으로 검증
  안 되는 것)일 때 → Phase 4에서 **"리뷰 권장" 한 줄만 제안**하고, 사용자가 원할 때만 실행.
  **자동 실행 금지.** (비용은 사용자의 것이다 — 기본은 절약.)

리뷰를 실행하는 경우에만 `game-reviewer` 에이전트 호출:

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

1. **리뷰를 실행했으면** `_workspace/review.md` 읽기:
   - **수정 필요 사항 있음:** 요약 보고 후 사용자 확인 → 승인 시 Phase 2 재실행
   - **이상 없음:** 완료 보고
2. **리뷰를 스킵했으면(기본):** 구현 요약 + 사용자 검증 안내(메뉴 실행/스크린샷 등).
   위험도 높은 변경이었으면 "필요하면 '리뷰해줘'로 검토 가능" 한 줄만 덧붙인다(자동 실행 X).
3. `개발_진행상황.md` 업데이트 필요 여부 사용자에게 제안
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
