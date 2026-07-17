# Karpathy 코딩 가이드라인

LLM 코딩 실수를 줄이기 위한 행동 지침. 사소한 작업은 판단에 따라 적용.

## 1. 코딩 전 먼저 생각

- 가정을 명시적으로 서술. 불확실하면 질문.
- 여러 해석이 가능하면 제시하고 물어봄 — 혼자 선택하지 않음.
- 더 단순한 방법이 있으면 말함. 필요시 반박.
- 불명확한 것이 있으면 멈추고 명확히 질문.

## 2. 단순함 우선

- 요청된 것만 구현. 추측성 기능 없음.
- 한 번만 쓰이는 코드에 추상화 없음.
- 요청하지 않은 유연성/설정가능성 없음.
- 불가능한 시나리오에 대한 에러 핸들링 없음.
- 200줄로 쓸 수 있는 걸 50줄로 쓸 수 있다면 다시 씀.

## 3. 외과적 변경

- 필요한 것만 건드림. 자신이 만든 orphan만 정리.
- 인접 코드, 주석, 포맷 개선 금지.
- 망가지지 않은 것 리팩토링 금지.
- 기존 스타일 유지, 내 방식이 달라도.
- 관련 없는 dead code 발견 시 언급만, 삭제 금지.

## 4. 목표 기반 실행

- 검증 기준을 먼저 정의하고 진행.
- 멀티 스텝 작업은 간단한 계획을 먼저 서술.

---

# Rewind Survivors — CLAUDE.md

## 프로젝트 개요
- **장르:** 탕탕특공대 스타일 탑다운 2D 서바이버 + 시간 되돌리기
- **목적:** 클라이언트 게임플레이 프로그래머 포트폴리오
- **개발 규모:** 1인, 퇴근 후 작업
- **엔진:** Unity 6, URP, New Input System

## 기술 결정사항 (변경 금지)
| 항목 | 결정 | 이유 |
|------|------|------|
| 렌더 파이프라인 | URP (Unity 6 기본값) | 변경 없이 진행 |
| Input System | New Input System — **Polling 방식** | `Keyboard.current.wKey.isPressed` 형태. Action-based 사용 안 함 |
| 리소스 로딩 | `Resources.Load` | Addressables 미사용. 포트폴리오 규모에 충분 |
| 충돌 감지 | Spatial Hashing (탐색·범위 판정) + Physics2D (접촉·피격) | 병행 사용 |
| 오브젝트 관리 | Object Pool (타입별 Generic Pool) | Get/Return |

## 코딩 스타일

### 네이밍 컨벤션
```
private 필드:     _camelCase     (예: _speed, _hp, _target)
public 프로퍼티:  PascalCase     (예: Hp, MaxHp, State)
메서드:           PascalCase     (예: OnDamaged, HandleMove)
SerializeField:  _camelCase     (예: [SerializeField] float _speed)
```

### 1인 소규모 프로젝트 구조 원칙
- **UI는 자기 자신이 등록** — UI 컴포넌트가 `Start()`에서 직접 플레이어/매니저를 찾아 이벤트 구독. Inspector 드래그 연결 최소화
- **Inspector 연결은 Prefab 내부로 한정** — 씬 간 참조는 코드로 처리
- **Inspector vs 코드 참조 기준:**
  - 같은 오브젝트/프리팹 내부 자식 → **Inspector 드래그** (항상 함께 존재하므로 끊길 일 없음)
  - 씬의 다른 오브젝트 → **`FindObjectOfType` / `Resources.Load`** (씬 구성이 바뀌어도 코드가 알아서 찾음)
  - 예: UI_UpgradePanel 안의 Button[], TMP_Text[] → Inspector / UpgradeManager, PlayerController → FindObjectOfType
- **추상화는 실제로 재사용될 때만** — 한 번만 쓰이는 코드는 추상화하지 않음
- **스텁은 주석으로 교체 시점 명시** — `// 추후 Pool 반납으로 교체` 형태

### 주석 규약
- **WHY가 비자명할 때만** 주석 작성 — 숨겨진 제약, 미묘한 불변식, 버그 우회, 독자가 놀랄 동작
- **WHAT 주석 금지** — 잘 명명된 식별자가 이미 설명함 (`// 타이머 감소` 같은 주석 불필요)
- **섹션 구분** — `// ── 제목 ──` 형태 허용 (메서드가 길어질 때 가독성용)
- **한 줄 최대** — 멀티라인 주석 블록 금지

### 금지 사항
- DI 프레임워크 (Zenject, VContainer 등) 제안 금지 — 규모 초과
- Action-based Input System 제안 금지 — Polling 방식으로 통일
- 과도한 인터페이스/제네릭 추상화 금지
- 없어도 되는 디자인 패턴 추가 금지

## Hierarchy 구조 및 네이밍 규칙

### 네이밍
- `--- GroupName ---` — 씬 정리용 빈 오브젝트 (Separator)
- `@Name` — 씬 전역 매니저/시스템 싱글톤 (예: @Managers, @Pool, @Renderers)
- `PascalCase` — 일반 게임플레이 오브젝트 (예: Player, EnemySpawner)

### 씬 Hierarchy 구성
```
--- Managers ---
  @Managers          (Managers 스크립트 — DontDestroyOnLoad)
  GameScene
  EnemySpawner
  WaveManager
  UpgradeManager
  SpatialHashGrid
  @Renderers
    Renderer_Basic
    Renderer_Tanker
    Renderer_Speeder

--- World ---
  Player

--- UI ---
  Canvas
    HpBar
    UpgradePanel
    GameOverText
    ExpBar
    LevelText
  EventSystem
  Main Camera
  Global Light 2D
  RewindVolume
```

### 신규 오브젝트 배치 규칙
| 추가할 오브젝트 | 배치 위치 | 예시 |
|----------------|----------|------|
| 게임 시스템/매니저 | `--- Managers ---` | WaveManager, EnemySpawner |
| GPU 인스턴싱 렌더러 | `--- Managers ---` > `@Renderers` 하위 | Renderer_Basic |
| 게임플레이 오브젝트 | `--- World ---` | Player, Boss |
| Canvas 안 UI 요소 | `--- UI ---` > `Canvas` 하위 | HpBar, 새 패널 |
| 씬 전역 시각 효과 | `--- UI ---` > Canvas 밖 | Global Light 2D, RewindVolume |
| 카메라 | `--- UI ---` > Canvas 밖 | Main Camera |

## 폴더 구조
```
Assets/Scripts/
├── Controllers/     CameraController
├── Enemy/           EnemyBase, EnemyMover, EnemySpawner
├── Manager/         Managers, GameManagerEx, ObjectManager 등
├── Player/          PlayerController
├── Scene/           GameScene
├── UI/              HpBar 등
├── Utils/           Define, Utils, GridBackground
└── Weapon/          Projectile, PlayerWeapon
```

## 커밋 규칙
- 커밋 + 푸시 시 `개발_진행상황.md` 도 함께 업데이트해서 포함할 것
- 완료된 항목은 `⬜ → ✅` 로 변경, 비고란에 핵심 구현 방식 한 줄 기재
- 새 시스템/아키텍처 변경이 있으면 `아키텍처.md` 에도 구조·흐름·설계 결정을 추가해서 함께 커밋할 것 (단순 버그 수정·밸런스는 제외)
- 커밋 메시지 형식: `feat: 한 줄 요약` (간략하게)

## 구현 방식
- **복잡한 기능은 단계별로 나눠서 구현** — 한 번에 여러 파일을 바꾸지 않고, 단계마다 동작 확인 후 다음 단계 진행

## 코드 작성 시 주의사항
- 진행 상황은 `개발_진행상황.md` 참고
- 기획 및 기술 상세는 `탕탕_포트폴리오_기획서.md` 참고

---

## 하네스: Unity Game Dev

**목표:** 기능 구현/버그 수정/코드 개선 요청 시 Planner → Coder → Reviewer 파이프라인으로 품질 보장

**트리거:** C# 코드를 건드리는 모든 작업 요청 시 `unity-dev` 스킬을 사용하라. 단순 코드 설명/질문은 직접 답변 가능.

**Knowledge Base:** 버그 수정·기능 구현에 착수하기 전, 먼저 `.claude/knowledge/`를 검색해 유사 패턴을 참조하라 (`symptom`·`tags` 프론트매터 기준). 재발 위험이 있는 문제를 새로 해결했다면 `.claude/knowledge/workflows/bugfix-to-knowledge-pipeline.md` 절차에 따라 엔트리로 저장하라.

**변경 이력:**
| 날짜 | 변경 내용 | 대상 | 사유 |
|------|----------|------|------|
| 2026-05-17 | 초기 구성 | 전체 | 5개월차 폴리싱 단계 돌입, 코드 품질 파이프라인 구축 |
| 2026-07-01 | game-designer 기획자 에이전트 추가 + Phase 0.5에 게임 기획 단계 편입 | unity-dev, agents | 신규 콘텐츠/룰 요청 시 실제 기획자 관점 설계 강화 |
| 2026-07-10 | Knowledge Base 도입 (bug-patterns / lessons-learned / workflows) + 착수 전 KB 검색 트리거 배선 | .claude/knowledge, CLAUDE.md | 재발성 버그 패턴 재사용 + AI 워크플로 자산화 |
| 2026-07-13 | game-ui-artist 에이전트 + ui-kit 스킬 추가, unity-dev Phase 2에 UI 라우팅 배선 | agents, skills/ui-kit, skills/unity-dev | UI 비주얼 폴리싱 전문화 — 코드기반 UGUI(절차적 스프라이트·에디터 생성기, 외부 리소스 0) |
