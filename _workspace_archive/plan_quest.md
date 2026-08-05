## 기능: 메타 상점 공속·이속 강화 추가

### 변경·생성 파일 목록

- `Assets/Scripts/Shop/ShopItemData.cs` — 기존 직렬화 값이 바뀌지 않도록 enum 끝에 `FireRate`, `MoveSpeed` 효과를 추가한다.
- `Assets/Scripts/Shop/ShopService.cs` — 두 효과의 누적 배율과 상점 표기용 퍼센트 값을 계산한다.
- `Assets/Scripts/UI/UI_ShopItemCell.cs` — 공속·이속의 현재/다음 퍼센트 및 비용 표기를 추가한다.
- `Assets/Scripts/Weapon/WeaponManager.cs` — 런 시작 시 메타 공속 배율을 모든 시작 무기에 적용한다.
- `Assets/Scripts/Player/PlayerController.cs` — 런 시작 시 메타 이속 배율을 기본 이동속도에 적용한다.
- `Assets/Resources/Shop/FireRate.asset` — 공속 4티어 상점 SO를 생성한다(Unity가 생성하는 `.meta` 포함).
- `Assets/Resources/Shop/MoveSpeed.asset` — 이속 3티어 상점 SO를 생성한다(Unity가 생성하는 `.meta` 포함).

### 단계별 구현 순서

1. **상점 효과·데이터 정의**
   - 대상: `Assets/Scripts/Shop/ShopItemData.cs`, `Assets/Resources/Shop/FireRate.asset`, `Assets/Resources/Shop/MoveSpeed.asset`
   - `ShopEffectType`의 마지막에 `FireRate`, `MoveSpeed`를 추가해 기존 `StartHp`/`Damage`/되감기 SO의 enum 직렬화 번호를 보존한다.
   - `FireRate.asset`: `id: fire_rate`, 표시명 `Fire Rate`, `FireRate`, `costs: 120/240/480/960`, `valuePerTier: 0.06`으로 만든다.
   - `MoveSpeed.asset`: `id: move_speed`, 표시명 `Move Speed`, `MoveSpeed`, `costs: 90/180/360`, `valuePerTier: 0.04`로 만든다.
   - 두 SO는 `Resources/Shop`에 두므로 기존 `Resources.LoadAll`과 동적 셀 생성으로 상점에 자동 표시된다.

2. **저장 티어 기반 메타 배율 계산**
   - 대상: `Assets/Scripts/Shop/ShopService.cs`
   - 기존 `DamageMult()`와 같은 방식으로 `FireRateMult()`와 `MoveSpeedMult()`를 추가한다. 각각 `1 + (구매 티어 × valuePerTier)`이며, 최대 시 공속은 `1.24`, 이속은 `1.12`가 된다.
   - UI 전용으로 각 효과의 `+N%` 현재/다음 값을 계산하는 파생 함수를 추가한다. 저장은 기존 `Purchases`의 `id`/`tier`와 `Buy()` 흐름을 그대로 사용한다.

3. **상점 셀 표기 확장**
   - 대상: `Assets/Scripts/UI/UI_ShopItemCell.cs`
   - `FireRate`, `MoveSpeed` case를 추가해 `[{tier}/{max}]`, `+현재% -> +다음%`, 비용을 표시하고, 최대 티어는 기존 형식대로 `MAX`와 최종 `+N%`만 표시한다.
   - 별도 UI 프리팹·패널 변경은 하지 않는다. `UI_ShopPanel`이 모든 SO를 순회해 현재의 구매 후 전체 갱신 흐름을 유지한다.

4. **런 시작 보정 적용**
   - 대상: `Assets/Scripts/Weapon/WeaponManager.cs`, `Assets/Scripts/Player/PlayerController.cs`
   - `WeaponManager.Start()`에서 시작 무기 생성 뒤 `ShopService.FireRateMult()`가 1이 아닐 때 기존 `UpgradeFireRate()`를 호출한다. 이 경로는 현재 무기와 이후 레벨업으로 추가되는 무기 모두에 `_fireRateMult`를 적용한다.
   - `PlayerController.Start()`에서 시작 HP 보정과 함께 `ShopService.MoveSpeedMult()`를 `_speed`에 한 번 곱한다. `RestoreSnapshot()`은 보정된 런 중 수치를 저장·복원하므로 별도 되감기 처리는 필요 없다.
   - `UpgradeData`와 레벨업 선택지는 건드리지 않는다. 메타 상점은 작은 시작 배율만 주고, 레벨업의 큰 공속·이속 빌드 선택은 그대로 유지한다.

### 주의사항

- 기존 enum 사이에 값을 삽입하면 이미 저장된 되감기 상점 SO의 effect가 다른 효과로 해석되므로 반드시 끝에 추가한다.
- 신규 배율은 `1.06`/`1.04`처럼 곱연산으로 기존 `WeaponManager.UpgradeFireRate()`와 `PlayerController.UpgradeSpeed()`의 의미를 따른다.
- private 필드는 `_camelCase`, 공개 접근점은 PascalCase를 유지하고, DI·Action-based Input·불필요한 추상화는 추가하지 않는다.
- 이번 변경은 신규 컬렉션이나 저장 필드를 만들지 않아 `Purchases` null 컬렉션 이슈의 KB 조치 대상이 아니다.

### 검증 방법

1. Unity에서 컴파일 후 기존 시작 HP·데미지·되감기 SO의 effect와 표기가 그대로인지 확인한다.
2. 새 저장 데이터에서 Fire Rate 카드가 `0/4`, `+0% -> +6%`, `120G`로, Move Speed 카드가 `0/3`, `+0% -> +4%`, `90G`로 표시되는지 확인한다. 구매 뒤 골드, 티어, 다음 비용과 버튼 상태가 기존처럼 전체 갱신되는지 확인한다.
3. 각 항목을 최대 구매해 재실행하고 Fire Rate가 `+24%`, Move Speed가 `+12%` 및 `MAX`로 표시되는지 확인한다.
4. 게임 런에서 일시정지 스탯의 공속이 기본 대비 `124%`로, 플레이어 이동속도가 기본 `5.0` 기준 `5.6`으로 반영되는지 확인한다. 레벨업 공속/이속 선택 후에도 해당 배율이 기존 곱연산으로 추가 누적되고, 무기 추가·되감기 후에도 값이 유지되는지 확인한다.
