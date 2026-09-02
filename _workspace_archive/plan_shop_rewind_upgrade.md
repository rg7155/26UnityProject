## 기능: 상점 영구 업그레이드 — RewindCooldown / RewindDuration

### 가정과 완료 기준
- 현재 기본 능동 Rewind는 지속 시간 5초, 쿨다운 30초이며, 구매 티어는 다음 게임 시작 시 적용한다.
- `RewindCooldown`은 티어당 쿨다운을 3초 줄인다. T3 비용은 600 / 1200 / 2400G이다.
- `RewindDuration`은 티어당 기록·되감기 가능 시간을 1초 늘린다. T2 비용은 900 / 1800G이다.
- 완료 시 새 SO 두 개가 기존 상점에 자동 표시되고, 구매 티어가 저장되며, 게임 시작 RewindManager의 버퍼 크기·쿨다운과 HUD 링이 해당 값으로 동작한다.

### 변경·생성 파일 목록
- `Assets/Scripts/Shop/ShopItemData.cs` — `RewindCooldown`, `RewindDuration` 효과 종류를 기존 enum에 추가한다.
- `Assets/Scripts/Shop/ShopService.cs` — 기본 Rewind 수치와 티어별 파생 지속 시간·쿨다운 계산, 상점 표시용 수치 계산을 추가한다.
- `Assets/Scripts/UI/UI_ShopItemCell.cs` — 두 효과를 초 단위의 현재 값 → 다음 값으로 표기한다.
- `Assets/Scripts/Rewind/RewindManager.cs` — `Start()`에서 영구 업그레이드 값을 적용한 뒤 버퍼 크기를 계산한다.
- `Assets/Resources/Shop/RewindCooldown.asset` — `id: rewind_cooldown`, `valuePerTier: 3`, 비용 600/1200/2400의 신규 상점 SO.
- `Assets/Resources/Shop/RewindDuration.asset` — `id: rewind_duration`, `valuePerTier: 1`, 비용 900/1800의 신규 상점 SO.
- 위 SO의 `.meta` 파일 — Unity가 함께 생성·추적한다.

### 단계별 구현 순서
1. **상점 효과와 SO 데이터 추가**
   - 대상: `ShopItemData.cs`, `Assets/Resources/Shop/RewindCooldown.asset`, `Assets/Resources/Shop/RewindDuration.asset`
   - 기존 `ShopEffectType`에 두 효과만 보태고, Resources 자동 로드 규칙에 맞춰 SO를 배치한다. 별도의 상점 목록 등록 코드는 만들지 않는다.
   - 확인: Unity 재임포트 후 `ShopService.Items`가 기존 2종과 신규 2종을 모두 반환하고, 새 셀 두 개가 동적으로 생성된다.

2. **티어 기반 수치 계산과 셀 표기 확장**
   - 대상: `ShopService.cs`, `UI_ShopItemCell.cs`
   - `BaseMaxHp` 패턴처럼 기본 Rewind 지속 시간(5초)·쿨다운(30초)을 ShopService의 단일 기준으로 두고, 각 티어의 최종값을 계산하는 간단한 메서드를 추가한다.
   - 셀의 switch에 명시적 case를 추가한다. Duration은 `5s -> 6s`, Cooldown은 `30s -> 27s`처럼 감소 방향을 보존해 표시하고 MAX 표기도 기존 형식을 따른다.
   - 확인: 각 티어에서 가격, `[tier/max]`, 현재/다음 초 수치, MAX 상태와 구매 버튼 비활성화가 기존 HP·Damage와 동일하게 갱신된다.

3. **게임 시작 Rewind 반영**
   - 대상: `RewindManager.cs`
   - `Start()`의 버퍼 할당보다 먼저 ShopService에서 구매 티어를 반영한 최종 지속 시간과 쿨다운을 가져온다. 이후 기존 `CeilToInt(_rewindDuration / _recordInterval)` 경로를 그대로 사용한다.
   - 쿨다운 타이머 재설정과 `CooldownMax`는 이미 `_rewindCooldown`을 참조하므로, RewindButtonUI나 입력·HUD 로직은 수정하지 않는다.
   - 확인: 미구매 시 5초/30초 동작 유지, Duration T2에서는 7초 버퍼, Cooldown T3에서는 21초 재사용 대기시간 및 HUD 링 진행이 동작한다. Shift Polling과 터치 버튼, Auto-Rewind의 기존 상태 흐름도 회귀 확인한다.

### 주의사항
- `GameData.Purchases`는 ID·tier 범용 목록이므로 새 컬렉션이나 저장 필드를 추가하지 않는다. JsonUtility 구버전 세이브의 새 컬렉션 null KB 항목은 검토했지만 이번 범위에는 적용 대상이 없다.
- UI는 `Resources.LoadAll` 기반 동적 셀을 그대로 사용한다. 프리팹, ShopUIGenerator, UI 비주얼 및 씬 참조는 변경하지 않는다.
- 기본 수치와 ShopService 기준 수치가 어긋나지 않도록 RewindManager의 직렬화 기본값도 같은 기준을 사용한다. 신규 추상화·DI·Action-based Input은 도입하지 않는다.
- 변경 후 커밋할 경우에만 `개발_진행상황.md`의 상점 다음 항목을 갱신한다. 이번 계획 작성 단계에서는 수정하지 않는다.

### 검증 방법
- 새 저장과 기존 저장 모두에서 타이틀 상점을 열어 4개 항목이 표시되는지 확인한다.
- 각 신규 항목을 한 티어씩 구매한 뒤 상점 재오픈·게임 재시작으로 비용, 티어, 영속 저장을 확인한다.
- 게임에서 Rewind를 충분히 기록한 뒤 지속 시간 증가분만큼 과거 프레임이 늘어나는지, 사용 후 쿨다운이 감소된 값으로 시작하고 HUD 링이 0→1로 채워지는지 확인한다.
- Console에 누락된 SO, null 저장 목록, 버퍼 인덱스 오류가 없는지 확인한다.
