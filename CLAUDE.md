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
| 충돌 감지 | 현재 Physics2D → 3개월차에 Spatial Hashing으로 교체 예정 | 지금은 Physics2D 써도 됨 |
| 오브젝트 관리 | 현재 Instantiate/SetActive → 3개월차에 Object Pool로 교체 예정 | 지금은 직접 생성/비활성화 써도 됨 |

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
```

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

## 앞으로 만들 핵심 시스템 (월별)
- **2개월차:** 웨이브 시스템, 레벨업/업그레이드, 적 3종, 기본 UI
- **3개월차:** Object Pool → Spatial Hashing → GPU Instancing (최적화)
- **4개월차:** Time Rewind (Circular Buffer + State Snapshot)
- **5개월차:** 폴리싱
- **6개월차:** GitHub README, Profiler 캡처, GIF 문서화

## 커밋 규칙
- 커밋 + 푸시 시 `개발_진행상황.md` 도 함께 업데이트해서 포함할 것
- 완료된 항목은 `⬜ → ✅` 로 변경, 비고란에 핵심 구현 방식 한 줄 기재
- 커밋 메시지 형식: `feat: 한 줄 요약` (간략하게)

## 구현 방식
- **복잡한 기능은 단계별로 나눠서 구현** — 한 번에 여러 파일을 바꾸지 않고, 단계마다 동작 확인 후 다음 단계 진행

## 코드 작성 시 주의사항
- 현재 `Instantiate` / `SetActive(false)` 로 처리하는 부분은 3개월차에 Object Pool로 교체 예정. 지금은 그대로 둘 것
- `FindObjectsOfType` 사용 중인 부분 (`PlayerWeapon.FindNearest`)은 3개월차 Spatial Hashing으로 교체 예정
- 성능 최적화 관련 제안은 3개월차 이전에는 하지 않아도 됨
- 진행 상황은 `개발_진행상황.md` 참고
- 기획 및 기술 상세는 `탕탕_포트폴리오_기획서.md` 참고

---

## 하네스: Unity Game Dev

**목표:** 기능 구현/버그 수정/코드 개선 요청 시 Planner → Coder → Reviewer 파이프라인으로 품질 보장

**트리거:** C# 코드를 건드리는 모든 작업 요청 시 `unity-dev` 스킬을 사용하라. 단순 코드 설명/질문은 직접 답변 가능.

**변경 이력:**
| 날짜 | 변경 내용 | 대상 | 사유 |
|------|----------|------|------|
| 2026-05-17 | 초기 구성 | 전체 | 5개월차 폴리싱 단계 돌입, 코드 품질 파이프라인 구축 |
