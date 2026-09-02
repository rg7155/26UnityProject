## 코드 리뷰 결과

### 합격
- `Assets/Scripts/UI/HudStats.cs` — 이상 없음
  - 네이밍 정상: `_timerText`/`_scoreText`/`_goldText`/`_wave` 모두 `_camelCase`, `Start`/`Update` `PascalCase`.
  - `Start`에서 `FindObjectOfType<WaveManager>()` 1회 캐시, `Update`에서 재탐색 없음 (성능 OK).
  - null 가드 견고: `Managers.Game == null` early-return + 각 텍스트/`_wave` 개별 null 체크.
  - 타이머 소스 `_wave.GameTime` — WaveManager.cs L51 `_gameTime += Time.deltaTime`로 매 프레임 증가하는 라이브 소스 확인. 주석의 "PlayTime은 멈춰 보임" 판단 타당(WHY 주석으로 적절).
  - `Managers.Game.Score`(int), `RunGold`(int) 실제 존재(GameManagerEx.cs L42/L58) — 참조 유효.

- `Assets/Scripts/UI/RewindButtonUI.cs` — 이상 없음
  - 네이밍 정상: SerializeField `_button`/`_cooldownRing`/`_autoPips`, 필드 `_rewind` 모두 `_camelCase`.
  - `Start`에서 `FindObjectOfType<RewindManager>()` 캐시 + null이면 LogError 후 return (Update는 `_rewind == null` 재가드).
  - 구독/해제 쌍 성립: `Start`의 `onClick.AddListener` ↔ `OnDestroy`의 `RemoveAllListeners`.
  - `CooldownMax == 0` 방어 존재 (L38 삼항 → 0이면 `fillAmount = 1f`, 0 나눗셈 회피).
  - `fillAmount` 로직 타당: 쿨다운 소진될수록 1로 차오름(`1f - remaining/max`), `Clamp01` 처리.
  - pip 갱신 null 가드(`_autoPips[i] != null`) + `interactable = CanActiveRewind` 배선 정상.

- `Assets/Scripts/Rewind/RewindManager.cs` — 이상 없음 (외과적 추가 확인)
  - 추가분은 `CanActiveRewind` 프로퍼티(L42) + `TryActiveRewind()`(L45) 2개 뿐. 되감기 로직(`DoRewind`/`Record`/`Apply*`/`Capture*`) 무수정.
  - Shift 분기(L84)가 `TryActiveRewind()` 호출로 치환됨 + 에디터 테스트용 Shift 유지 확인.
  - `CanActiveRewind`가 쿨다운·버퍼·상태(Playing) 3조건을 한곳에 응집 → 버튼/Shift/코드가 동일 게이트 공유(중복 제거, 타당).
  - 네이밍/프로퍼티 `PascalCase`, 필드 `_camelCase` 준수. `OnDestroy`에서 `_player.OnDead` 해제 유지.
  - 금지패턴 없음(New Input System Polling 방식 `Keyboard.current...wasPressedThisFrame` 유지).

- `Assets/Scripts/UI/UIProceduralSprite.cs` — 이상 없음 (회귀 안전)
  - `_preserveType` 기본값 `false`(L21) → 기존 경로 무영향.
  - 기존 시그니처 전부 보존: `Configure(bool,int,int,Color,Color)`, `ConfigureCircle(int,Color)`, `ConfigureRing(int,int,Color)`.
  - `ConfigureRing(3-arg)`(L47-48)는 신규 4-arg 오버로드에 `false` 위임 → 상점/업그레이드/타이틀/조이스틱 호출부 동작 무손상. 실제 호출부 확인:
    - `JoystickUIGenerator.cs` L58 `ConfigureRing(3-arg)`, L67 `ConfigureCircle` → 정상 라우팅.
    - `Upgrade/Title/Shop Generator` 모두 `Configure(bool,...)` 기존 시그니처 사용 → 무변경.
  - `Apply()` L83 `if (!_preserveType) img.type = type;` — preserveType일 때만 type 미덮음. 로직 정확.
  - 공유 컴포넌트라 추상화 정당(주석 명시) — CLAUDE.md "실제 재사용될 때만" 기준 부합.

- `Assets/Scripts/Editor/HudGenerator.cs` — 이상 없음
  - `#if UNITY_EDITOR` 가드(L1/L305) + `Assets/Scripts/Editor/` 폴더 위치 정상.
  - idempotent: `FindOrCreateChild`/`FindOrCreateLabel`/`EnsureProcedural`/`WireIfNull` 전부 이름·컴포넌트 재사용 → 재실행 안전.
  - Filled 링 배선 순서 정확(핵심): L131 `ConfigureRing(...,true)` (procedural 스프라이트 적용 + preserveType로 type 미덮음) → L132-136 `type=Filled`/`Radial360`/`fillOrigin=Top`/`fillClockwise`/`fillAmount=1` 세팅. procedural 이후 type 지정 순서 올바름. 런타임 `OnEnable→Apply`도 preserveType=true 직렬화로 Filled 유지.
  - 매직값은 `UITheme.*`(S2~S6, RadSm, 팔레트) + 파일 상단 `const`(StatusStripH 등)로 관리 — 하드코딩 산재 없음.
  - 절차 스프라이트 전부 `UIProceduralSprite` 위임(`ApplyProcedural`/`EnsureProcedural`) — 흰 박스/직렬화 null 회피 준수.
  - HpBar/ExpBar를 씬에서 찾아 슬라이더 참조 보존 재배치(`SetParent(strip,false)`) — 로직 파일 무수정, 스타일/배치만.

### 무수정 확인
- `HpBar.cs`, `ExpBar.cs` — `git status` 변경 없음 확인. 리스킨은 씬/슬라이더 재배치·재도색만(HudGenerator.SkinBar 경유)으로 처리됨. 기준 부합.

### 수정 필요
- 없음.

### 참고(경미, 조치 불요)
- `RewindButtonUI.OnDestroy`의 `RemoveAllListeners()`는 이 버튼의 onClick을 전용으로 쓰므로 안전. 향후 동일 버튼에 외부 리스너가 붙는다면 `RemoveListener(_rewind.TryActiveRewind)`로 좁히는 편이 안전하나 현 구조상 불필요.

### 종합 의견
CLAUDE.md 네이밍·금지패턴·자기등록·절차스프라이트 위임·idempotent 생성기 원칙을 모두 충족하며, 회귀 위험 지점(_preserveType·Filled 링 순서·기존 오버로드)까지 안전하게 처리된 합격 코드다.
