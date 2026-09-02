## 코드 리뷰 결과

검토 대상: 커밋 `81f5e94` (`feat: 상점 되감기 및 기본 스탯 강화 추가`)

### 수정 필요

- **[P2] Fire Rate 상점 강화가 AoE/Orbit 무기에 적용되지 않음**
  - 위치: `Assets/Scripts/Weapon/WeaponManager.cs:22`
  - 근거: 상점 배율은 `UpgradeFireRate(m)`로 모든 무기에 전달되지만, 실제로 `UpgradeFireRate`를 구현한 무기는 `ProjectileWeapon`과 `LightningWeapon`뿐이다. `AoEWeapon`은 공격 주기를 `_interval`로 관리하면서 `UpgradeInterval`만 구현하고(`Assets/Scripts/Weapon/AoEWeapon.cs:55`), `OrbitWeapon`은 적별 공격 주기를 `_hitCooldown`으로 관리하지만 관련 override가 없다(`Assets/Scripts/Weapon/OrbitWeapon.cs:75`). 따라서 영구 골드를 사용해 Fire Rate를 구매해도 AoE와 Orbit의 공격 빈도는 전혀 오르지 않는다.
  - 영향: 시작 무기 및 추후 획득 무기 적용 경로 자체는 중복 없이 정상이나, 4개 무기 계열 중 2개에서는 구매 효과가 무효가 된다. UI는 제한 대상을 알리지 않고 공통 `Fire Rate`로 표시한다.
  - 수정안: Fire Rate를 전 무기 공통 스탯으로 유지한다면 `AoEWeapon.UpgradeFireRate`에서 `_interval /= multiplier`, `OrbitWeapon.UpgradeFireRate`에서 `_hitCooldown /= multiplier`를 적용한다. 의도적으로 발사형 무기만 지원한다면 상점 이름/설명과 적용 대상을 명시해 구매 효과 범위를 일치시킨다.

- **[P3] 되감기 버퍼가 표시 시간보다 항상 기록 간격 1회만큼 짧음**
  - 위치: `Assets/Scripts/Rewind/RewindManager.cs:50`
  - 근거: `_bufferSize = CeilToInt(duration / interval)`개의 스냅샷으로 재생 가능한 구간은 스냅샷 사이 간격이므로 `(bufferSize - 1) * interval`이다. 현재 기본값은 100개 스냅샷으로 99구간, 즉 UI의 5초 대신 최대 4.95초이며, 강화 후에도 6초/7초가 각각 5.95초/6.95초다.
  - 영향: `UI_ShopItemCell`이 표시하는 current/next/MAX 시간보다 실제 최대 되감기가 0.05초 짧다. 기록 간격이 조정되면 오차도 함께 커진다.
  - 수정안: 최대 시간 양 끝의 스냅샷을 보존하도록 버퍼 크기를 `Mathf.CeilToInt(_rewindDuration / _recordInterval) + 1`로 계산한다.

### 확인 완료

- `ShopEffectType`은 기존 `StartHp=0`, `Damage=1` 뒤에 새 값을 추가해 기존 ScriptableObject의 enum 직렬화 값을 보존한다.
- 구매 저장 키는 기존 `List<ShopPurchase>`의 문자열 `id` 방식을 유지하므로 새 항목 추가가 구버전 세이브 구조를 깨지 않는다.
- Fire Rate는 시작 무기에 한 번 적용되고, 이후 `AddWeapon`으로 획득한 무기에도 누적 배율이 한 번 적용된다.
- Move Speed는 플레이어 시작 시 적용된 뒤 레벨업 배율과 함께 `PlayerSnapshot.speed`에 기록·복원되어 메타 배율이 중복 적용되지 않는다.
- Rewind Cooldown은 `CooldownMax`를 통해 HUD 링 계산에 반영되며, 에셋 최대 티어에서 21초로 양수 범위다.
- UI는 모든 효과에 current/next/MAX 분기를 가지며 비용 인덱스와 최대 티어 판정이 일치한다.
- 신규 YAML의 effect 값 `2..5`, 비용 배열 인코딩, `valuePerTier`, Script GUID가 코드 정의와 일치하고 신규 에셋 GUID끼리 중복되지 않는다.
- 현재 `Title.unity`의 Content는 3열 `GridLayoutGroup`(280×270, 3열)과 세로 `ContentSizeFitter`/ScrollRect 구성이어서 6개 항목은 2행으로 배치되고 세로 스크롤을 유지한다.
- 새 코드에서 Action-based Input, DI 프레임워크, 불필요한 추상화 등 프로젝트 금지 패턴은 발견되지 않았다.

### Unity 수동 검증

1. 기존 2종 상점 구매 세이브를 로드해 HP/Damage 티어와 골드가 유지되는지 확인한다.
2. Fire Rate를 구매한 뒤 Projectile, Lightning, AoE, Orbit 각각의 동일 시간당 공격 횟수를 구매 전후 비교한다.
3. Rewind Duration 각 티어에서 버퍼를 완전히 채운 뒤 게임 타이머가 표시된 5/6/7초만큼 복원되는지 확인한다.
4. Rewind Cooldown 최대 티어에서 HUD 링이 21초를 기준으로 충전되고 버튼 활성화 시점과 일치하는지 확인한다.
5. Move Speed 구매 후 속도 레벨업을 적용하고 되감아, 스냅샷 시점의 속도로 정확히 복원되는지 확인한다.
6. 1080×1920 Title 씬에서 6개 셀이 3열×2행으로 보이며 current/next/비용/MAX 문구가 잘리거나 겹치지 않는지 확인한다.

### 종합 의견

**수정 후 합격.** 저장 호환성, enum 직렬화, 계산식, YAML 및 현재 상점 레이아웃은 대체로 안전하다. Fire Rate 구매가 일부 무기에서 무효인 기능 결함을 우선 수정하고, 되감기 시간의 한 샘플 경계값을 맞춘 뒤 Unity에서 수동 검증하는 것이 필요하다.
