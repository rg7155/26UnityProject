# 기능: 보스 시스템 (MONOLITH / SWARMLORD)

작성일 2026-07-28 · 마감 2026-08-09 (12일) · 1인 · 퇴근 후 작업

---

## 0. 코드베이스 조사 결과 (설계 근거)

계획을 세우기 전에 확인한 사실. 이게 아래 설계 판단의 전제다.

| 확인한 것 | 결과 | 계획에 미치는 영향 |
|---|---|---|
| `EnemyInstanceRenderer.Register(enemy, prefab)` | `_registry.TryGetValue(prefab, ...)` 실패 시 **조용히 no-op** | 보스 프리팹용 렌더러를 씬에 두지 않으면 자동으로 GPU 인스턴싱에서 빠진다. **EnemyBase 수정 불필요.** SpriteRenderer 그대로 사용 |
| `EnemyJobScheduler` | 매 프레임 `EnemyBase.Registry` 전체를 순회 | 보스도 Registry에 있으므로 **separation 계산에 이웃으로 포함**된다(잡몹을 밀어냄 — 오히려 바람직). 보스 자신은 `GetSeparation`을 **조회하지 않으면** 이동에 영향 없음 → "SeparationJob 제외" 요구를 코드 수정 0으로 만족 |
| `EnemyBase.Init(target, prefab, hp=-1, speed=-1)` | hp/speed가 음수면 SerializeField 값 유지 | 보스는 `Init`을 override해 `BossData`에서 hp/speed를 읽으면 됨 → **`EnemySpawner` 수정 불필요** |
| `RewindManager.CaptureEnemies()` | `EnemyBase.Registry` 전체를 훑어 `hp/position` 저장 | 보스가 `EnemyBase`를 상속하기만 하면 **HP·위치 되감기는 코드 0줄로 자동 확보** (요구사항 3-2 충족) |
| `ApplyEnemies` Case B2 | 풀 재사용 시 `ForceRestore`로 EntityId 강제 주입 | 보스가 죽은 뒤 되감으면 풀에서 부활 → `BossController`가 `OnEnable/OnDisable`로 static Instance를 관리해야 안전 |
| `WaveManager.RestoreSnapshot` | `_gameTime/_spawnTimer/_waveIndex` 복원, `CheckWaveTransition`은 `Update`에서만 호출 | 되감기로 시간이 되돌아간 뒤 다시 전진하면 **보스가 두 번 스폰될 수 있음** → 스폰 지점에 `BossController.Instance != null` 가드 필수 |
| `Projectile.Update()` | **GameState 가드 없음** (기존 코드) | 신규 `BossProjectile`은 반드시 가드를 넣는다. 기존 `Projectile`은 스코프 밖 — 건드리지 않음 |
| `EnemyMover`의 접촉 데미지 | `OnTriggerEnter2D` + `_attackCooldown 1f`. Enter는 **진입 순간 1회만** 발생 | 보스는 덩치가 커서 플레이어가 콜라이더 **안에 머무는** 시간이 길다 → Enter로 짜면 "붙어 있으면 안 아픈" 버그. 보스는 **`OnTriggerStay2D`** 를 쓴다 (§2.6) |
| `EnemySnapshot.attackCooldown` | 이미 존재하고 `EnemyMover.RestoreSnapshot`이 복원 중 | 보스 접촉 쿨다운을 **이 기존 필드에 태운다** → 신규 스냅샷 필드 0 |
| 현재 `WaveData` 에셋 | startTime 0 / 5 / 10 (테스트값) | 보스 웨이브는 startTime 180 / 360으로 신규 에셋 추가 |

### Knowledge Base 반영 (`.claude/knowledge/`)

| 엔트리 | 이 기능에서의 적용 |
|---|---|
| `dual-pause-timer-leak` | 이 프로젝트엔 정지 경로가 2개(`GameState.Paused`만 / `Paused`+`timeScale=0`). **신규 타이머 전부**(패턴 쿨다운·텔레그래프·돌진·탄환 수명·WARNING 점멸)를 `if (Managers.Game.State != GameState.Playing) return;` **아래**에 둔다. 이건 스타일이 아니라 **결정성 요구사항** — 업그레이드 패널이 뜬 동안 패턴 타이머가 새면 되감기 재현이 깨진다 |
| `pooled-object-survives-scene-change` | 보스 탄환·텔레그래프 오브젝트가 씬 전환 후 유령 콜백을 쏘지 않도록, **탄환은 `Managers.Object` 풀 사용**(`Clear()`가 실제 파괴함), 텔레그래프는 보스 프리팹 **내부 자식**으로 두고 SetActive만 토글 |
| `jsonutility-missing-field-null-collection` | `GameData.LifetimeBossKills`는 **int(값 타입)** 이라 구버전 세이브에서 0으로 안전. 새 `List<>`를 추가하지 않는다 |
| `orbit-tick-hit-gap` | 돌진(속도 12)은 빠른 이동체 → 접촉 판정을 틱이 아니라 **프레임 단위 트리거**로. 장판은 폭발 순간 1회 거리 판정이라 무관 |
| `separation-overpowers-target-drift` | 보스는 separation을 조회하지 않으므로 해당 없음 (되감기 결정성을 위해 의도적으로 제외) |

---

## 1. 신규/수정 파일 목록

### 신규 — game-coder

| 경로 | 역할 |
|---|---|
| `Assets/Scripts/Enemy/BossData.cs` | 보스 SO. `BossPatternType` enum + `patternSequence[]` + 패턴별 파라미터 블록. **SWARMLORD를 코드 0줄로 만드는 핵심** |
| `Assets/Scripts/Enemy/BossController.cs` | `EnemyBase` 직상속. 이동/리쉬/패턴 상태머신/페이즈/보상/스냅샷 복원. 이 기능의 몸통 |
| `Assets/Scripts/Enemy/BossTelegraph.cs` | 예고 시각(원 확대·링·조준선)을 스케일/알파로 표현하는 초경량 컴포넌트. 보스 프리팹 내부 자식 |
| `Assets/Scripts/Enemy/BossProjectile.cs` | 방사탄. 풀 반납 + `static ClearAll()`(되감기 시 소거) |
| `Assets/Scripts/UI/BossHpBar.cs` | **로직만** — `BossController.Instance`를 폴링해 값 갱신·표시 토글 |
| `Assets/Scripts/UI/BossWarningUI.cs` | **로직만** — `WaveManager.BossWarningActive` 폴링해 점멸 토글 |
| `Assets/Scripts/Scene/BossRushStarter.cs` | 데모 모드. 프리셋 지급 + 보스 직전 시점으로 점프 |

### 수정 — game-coder

| 경로 | 변경 |
|---|---|
| `Assets/Scripts/Rewind/RewindSnapshot.cs` | `BossSnapshot` 구조체 추가 + `FrameSnapshot.boss` 필드 1개 |
| `Assets/Scripts/Rewind/RewindManager.cs` | `CaptureBoss()`/`ApplyBoss()` 배선, 되감기 시작 시 `BossProjectile.ClearAll()`, `ResetCooldown()`·`AddAutoRewindCharge()` public 메서드 |
| `Assets/Scripts/Wave/WaveData.cs` | `isBossWave`, `bossPrefab`, `warningLeadTime` 필드 |
| `Assets/Scripts/Wave/WaveManager.cs` | 보스 웨이브 스폰(중복 가드), WARNING 구간 스폰 정지, 보스전 중 `spawnInterval ×3`, `JumpTo(float)` |
| `Assets/Scripts/Quest/QuestData.cs` | `QuestStat.BossKills` 추가 |
| `Assets/Scripts/Quest/QuestService.cs` | `Current()`에 `BossKills` case 1줄 |
| `Assets/Scripts/Manager/GameManagerEx.cs` | `GameData.LifetimeBossKills`, `RunBossKills`, `CommitResult()` 합산 |
| `Assets/Scripts/Scene/GameScene.cs` | `public static bool BossRush` 플래그, `RunBossKills = 0` 초기화 |
| `Assets/Scripts/Scene/TitleScene.cs` | `_bossRushButton` SerializeField + 리스너 |
| `Assets/Scripts/Controllers/CameraController.cs` | `Shake(duration, magnitude)` — 페이즈 전환·장판 폭발용 (10줄) |

### 신규/수정 — game-ui-artist (**이 계획의 구현 대상 아님. 인계만**)

| 경로 | 역할 |
|---|---|
| `Assets/Scripts/Editor/BossHudGenerator.cs` (신규) | 상단 보스 HP 바 + 이름 + WARNING 배너의 **비주얼 생성기**. ui-kit 토큰 준수, `BossHpBar`/`BossWarningUI`를 붙이고 프리팹 내부 자식을 SerializeField에 연결 |
| `Assets/Scripts/Editor/TitleLobbyGenerator.cs` (수정) | 타이틀에 `BOSS RUSH` 버튼 추가 후 `TitleScene._bossRushButton`에 연결 |

### 파일 충돌 방지 규약 (두 에이전트 동시 작업 금지 구역)

- `BossHpBar.cs` / `BossWarningUI.cs` — **game-coder만** 작성. artist는 **읽고 SerializeField에 오브젝트를 연결만** 한다
- `Assets/Scripts/Editor/*` — **game-ui-artist만** 수정
- `TitleScene.cs`(coder) → `TitleLobbyGenerator.cs`(artist) 순서 의존. **coder가 필드를 먼저 만든 뒤** artist를 돌린다
- 두 UI 로직 스크립트가 artist에게 노출하는 계약(필드명 고정):
  - `BossHpBar`: `[SerializeField] GameObject _root; [SerializeField] Slider _slider; [SerializeField] TMP_Text _nameText; [SerializeField] TMP_Text _hpText;`
  - `BossWarningUI`: `[SerializeField] GameObject _root;`
  - artist는 **이 4+1개 필드만** 채우면 되고, 값 계산·표시 토글은 전부 로직 스크립트가 한다

---

## 2. Rewind 스냅샷 확장 설계 ★

### 2.1 선택: `FrameSnapshot`에 `BossSnapshot` 1개 추가 (프레임당 1개)

세 가지 안을 비교했다.

| 안 | 내용 | 판단 |
|---|---|---|
| A. `EnemySnapshot` 확장 | 페이즈/시퀀스/타이머 필드를 모든 적 스냅샷에 추가 | ❌ **기각.** `CaptureEnemies`는 매 0.05초 **적 전체**에 대해 배열을 새로 만든다(적 300 × 100프레임 = 3만 구조체). 1개체에만 의미 있는 필드 6개(≈28B)를 전원에게 붙이면 매 기록마다 수백 KB 추가 할당. 구조체 의미론도 오염 |
| B. **`FrameSnapshot`에 `BossSnapshot boss` 1필드** | 프레임당 정확히 1개 | ✅ **채택** |
| C. 보스 전용 별도 링버퍼 | `BossController`나 `RewindManager`가 두 번째 원형 버퍼 운영 | ❌ **기각.** `_head/_tail/_count` 인덱스, 리셋(`DoRewind` 끝의 `_count=0`), 되감기 루프의 프레임 정렬을 **두 벌 유지**해야 한다. 두 버퍼가 어긋나는 순간 "같은 패턴이 같은 타이밍에" 요구가 조용히 깨진다. 결정성이 최우선인 기능에서 동기화 지점을 늘리는 건 최악의 트레이드오프 |

**B의 근거 요약:** 보스는 런당 최대 1개체이므로 프레임당 1레코드가 자연스러운 카디널리티다. 기존 `WaveSnapshot`이 이미 같은 자리(프레임당 1개 시스템 상태)를 차지하고 있어 **선례가 존재**한다 — `wave` 옆에 `boss`를 두는 것이 이 코드베이스의 기존 문법이다. 비용은 프레임당 ~32B × 100프레임 = 3KB, 기록/복원/리셋 경로는 전부 기존 것을 그대로 탄다. 신규 동기화 지점 0.

### 2.2 구조체

```csharp
// RewindSnapshot.cs 에 추가
public struct BossSnapshot
{
    public bool    active;         // 이 프레임에 보스가 존재했는가
    public int     entityId;       // 복원 대상이 같은 개체인지 확인
    public int     phase;          // 1 or 2
    public int     sequenceIndex;  // patternSequence 인덱스
    public int     actionState;    // BossActionState (Idle/Telegraph/Execute)
    public float   stateTimer;     // 현재 상태 경과 시간
    public float   cooldownTimer;  // 다음 패턴까지
    public Vector2 lockedPoint;    // 장판 예고 중심 (텔레그래프 시점 고정)
    public Vector2 lockedDir;      // 돌진 방향 (텔레그래프 종료 시 고정)
}

public struct FrameSnapshot
{
    public PlayerSnapshot  player;
    public EnemySnapshot[] enemies;
    public WaveSnapshot    wave;
    public BossSnapshot    boss;   // 추가
}
```

> `lockedPoint`/`lockedDir`을 넣는 이유: 텔레그래프 도중에 되감으면 **예고원의 위치까지 그때 그 자리로** 돌아와야 화면과 로직이 일치한다. 이게 없으면 되감기 직후 예고원이 순간이동한다.
>
> HP/위치/속도는 **일부러 넣지 않는다.** 보스는 `EnemyBase.Registry`에 있으므로 기존 `EnemySnapshot` 경로가 이미 기록·복원한다(요구 3-2). 두 곳에서 같은 값을 관리하면 어긋난다.

### 2.3 기록 지점

`RewindManager.Record()` — 기존 3줄 옆에 1줄:

```csharp
FrameSnapshot frame = new FrameSnapshot
{
    player  = CapturePlayer(),
    enemies = CaptureEnemies(),
    wave    = CaptureWave(),
    boss    = CaptureBoss(),      // 추가
};

BossSnapshot CaptureBoss()
{
    BossController b = BossController.Instance;
    if (b == null) return default;          // active=false
    return b.CaptureState();                // 보스가 자기 상태를 채워 반환
}
```

### 2.4 복원 지점 — **순서가 중요**

`DoRewind()`의 스텝 루프와 마지막 프레임 양쪽에서, **`ApplyEnemies` 다음에** `ApplyBoss`를 호출한다.

```csharp
ApplyPlayer(toFrame.player);
ApplyEnemies(toFrame.enemies);   // 여기서 보스가 풀에서 부활(Case B2)하거나 반납(Case C)될 수 있음
ApplyBoss(toFrame.boss);         // 그 다음에야 Instance가 확정됨
ApplyWave(toFrame.wave);

void ApplyBoss(BossSnapshot s)
{
    BossController b = BossController.Instance;
    if (b == null || !s.active) return;
    if (b.EntityId != s.entityId) return;   // 다른 개체면 적용 금지
    b.RestoreBossState(s);
}
```

- `RestoreBossState`는 상태 대입 후 **텔레그래프 비주얼을 즉시 동기화**한다. `BossController.Update`는 `State != Playing`이면 조기 리턴하므로, 되감기 중 예고원이 갱신되려면 복원 시점에 직접 갱신해줘야 한다
- **되감기 시작 시점**(`DoRewind` 진입 직후, `State = Rewinding` 설정 직전/직후)에 `BossProjectile.ClearAll()` — 요구 5. 탄환은 스냅샷에 없으므로 남겨두면 복원된 세계와 불일치

### 2.5 결정성 체크리스트 (요구 1 — 절대 못 버림)


- `patternSequence`는 배열 인덱스 순회. **`Random` 호출 전면 금지** — 보스 관련 코드 어디에도 `Random.` 이 등장하면 안 된다
- 방사탄 각도는 `첫 발 = 플레이어 방향`, 나머지는 `360/count` 균등. 플레이어 위치가 복원되므로 결정적
- 장판 목표점은 텔레그래프 **시작 순간의 플레이어 위치**를 고정 → 스냅샷에 저장
- 돌진 방향은 텔레그래프 **종료 순간**의 플레이어 방향을 고정 → 스냅샷에 저장
- 페이즈는 HP 비율에서 파생 + 스냅샷 복원. HP가 50% 위로 되감기면 페이즈 1로 돌아가고 다음 교차에서 연출이 다시 발생한다 — **의도된 동작**(재도전 성립)
- 모든 신규 타이머는 `Playing` 가드 아래 (KB `dual-pause-timer-leak`)

### 2.6 상시 접촉 데미지 (사용자 확정)

보스는 **평소에도 몸에 닿으면 피해를 준다.** 돌진 중에는 `chargeDamage`로 대체된다.

```csharp
void OnTriggerStay2D(Collider2D other)   // Enter가 아니라 Stay ★
{
    if (Managers.Game.State != Define.GameState.Playing) return;
    if (_attackCooldown > 0f) return;

    PlayerController player = other.GetComponent<PlayerController>();
    if (player == null) return;

    int damage = (_actionState == BossActionState.Execute
               && CurrentPattern == BossPatternType.Charge)
               ? _data.chargeDamage : _data.contactDamage;

    player.OnDamaged(damage);
    _attackCooldown = _data.contactInterval;
}
```

- **`OnTriggerEnter2D`를 쓰면 안 되는 이유:** 보스 콜라이더가 커서 플레이어가 **안에 머무는** 시간이 길다. Enter는 진입 순간 1회뿐이라 "보스 몸에 붙어 있으면 아무 일도 안 일어나는" 상태가 된다. 잡몹은 작아서 밀려나며 Enter가 재발생하니 기존 `EnemyMover` 방식이 통했던 것 — **보스에 그대로 복제하지 말 것.**
- `_attackCooldown`은 **기존 `EnemySnapshot.attackCooldown`에 태운다** (`EnemyMover.RestoreSnapshot` 선례 그대로). `BossSnapshot`에 새 필드를 추가하지 않는다 — 되감기 복원은 코드 0줄로 따라온다.
- 플레이어의 `_invincibleDuration`(1s)과 `contactInterval`(1s)이 이중 게이트가 된다. 둘 다 두는 이유: 무적시간은 **모든 피해원 공통**이라, 보스 접촉이 무적시간을 다 먹으면 장판·탄막 피해가 무효화된다. 보스 쪽에도 자체 쿨다운을 둬 접촉이 다른 패턴 피해를 잡아먹지 않게 한다.
- 밸런스 영향: **보스에 밀착 딜이 불가능해진다.** 오토에임 사거리 안에서 거리를 유지하는 플레이가 정답이 되므로, 리쉬(거리 18)와 함께 "너무 멀지도 가깝지도 않게" 유지하는 압박이 생긴다. 의도된 방향.

---

## 3. `BossData` SO 설계 ★

```csharp
public enum BossPatternType { GroundSlam, RadialBurst, Charge }

[CreateAssetMenu(fileName = "BossData", menuName = "Game/BossData")]
public class BossData : ScriptableObject
{
    [Header("정체")]
    public string bossName;
    public int    hp;
    public float  moveSpeed;
    public int    expReward;
    public int    goldReward;
    public int    contactDamage;    // 25 — 상시 접촉 피해
    public float  contactInterval;  // 1.0 — 접촉 피해 자체 쿨다운
    public Color  phase1Color;
    public Color  phase2Color;

    [Header("패턴 시퀀스 — 이 순서대로 무한 반복 (랜덤 없음)")]
    public BossPatternType[] patternSequence;

    [Header("장판 GroundSlam")]
    public float slamTelegraph;      // 1.0
    public float slamRadius;         // 2.5
    public int   slamDamage;         // 35
    public float slamCooldown;       // 5

    [Header("방사탄 RadialBurst")]
    public float radialTelegraph;    // 0.6
    public int   radialCount;        // 12
    public int   radialCountPhase2;  // 16
    public int   radialDamage;       // 20
    public float radialSpeed;        // 4.5
    public float radialLifetime;     // 3
    public float radialCooldown;     // 6

    [Header("돌진 Charge")]
    public float chargeTelegraph;    // 0.8
    public float chargeSpeed;        // 12
    public float chargeDuration;     // 1.2
    public int   chargeDamage;       // 30
    public float chargeCooldown;     // 8

    [Header("페이즈 2 — hpRatio 0이면 비활성")]
    public float phase2HpRatio;         // 0.5
    public float phase2CooldownMult;    // 0.7

    [Header("리쉬")]
    public float leashDistance;       // 18
    public float leashReturnDistance; // 12
    public float leashSpeedMult;      // 2.5
}
```

### 자체 검증 — SWARMLORD가 정말 코드 0줄인가?

| 요구 | 충족 수단 | 신규 코드 |
|---|---|---|
| 6:00 등장 | `WaveData4.asset` (startTime 360, isBossWave, bossPrefab) | 0 |
| 시퀀스 = 장판 → 장판 → 방사탄 | `patternSequence = [GroundSlam, GroundSlam, RadialBurst]` | 0 |
| 돌진 미사용 | 시퀀스에 `Charge`를 넣지 않으면 charge 파라미터는 **읽히지도 않음** | 0 |
| 방사탄 대량·저속 | `radialCount 24`, `radialSpeed 2.5` | 0 |
| 더 강함 | `hp`, `expReward 2레벨분`, `goldReward 300` | 0 |
| 거대 마름모 | **프리팹 복제 후 사각 스프라이트 z축 45° 회전 + scale** — 에디터 에셋 작업 | 0 |
| 페이즈 2 없이 가려면 | `phase2HpRatio = 0` → 전환 로직이 자동 비활성 | 0 |

→ **SWARMLORD = 프리팹 1개 복제 + `BossData` 에셋 1개 + `WaveData` 에셋 1개. C# 신규 0줄.** ✔

> 검증 포인트: `BossController`는 `switch (patternSequence[i])` 로만 분기하고, **패턴 개수·순서·타입에 대한 어떤 가정도 코드에 넣지 않는다.** `patternSequence.Length`가 1이든 5이든 동작해야 한다. 이걸 어기는 구현(예: "시퀀스는 항상 3개"라는 상수)이 들어오면 셀링포인트가 무너진다 — 리뷰 항목으로 명시.

---

## 4. 단계별 구현 순서

각 단계는 **끝에서 커밋 가능**해야 한다. 작업량은 1인 저녁 작업 기준 체감치.

---

### Stage 1 — 보스 골격 (등장·이동·리쉬·사망)

- **목표:** 보스가 웨이브로 등장하고, 플레이어를 쫓고, **몸에 닿으면 피해를 주고**, 기존 무기로 죽고, EXP/골드를 준다. 패턴은 아직 없음
- **파일:** `BossData.cs`(신규) · `BossController.cs`(신규, 패턴 없이) · `WaveData.cs`(+3필드) · `WaveManager.cs`(스폰·경고·간격완화)
- **핵심 내용:**
  - `BossController : EnemyBase` — `Init` override로 `_data`에서 hp/speed 주입, `OnEnable/OnDisable`로 `static Instance` 관리, `Update`에서 `Playing` 가드 → 플레이어 추적 이동 + `SpatialHashGrid.Move` 호출 + 리쉬 판정
  - **상시 접촉 데미지** — §2.6 그대로. `OnTriggerStay2D`(Enter 아님), `_attackCooldown`은 `RestoreSnapshot`에서 `EnemySnapshot.attackCooldown` 복원(`EnemyMover` 선례 복제)
  - `OnDead` override: 보상 지급(EXP/골드) 후 `base.OnDead()`. 리플레이 중복 방지를 위해 보상은 `State == Playing`일 때만 (`EnemyBase.OnDead`와 동일 패턴)
  - `WaveManager`: `CheckWaveTransition`에서 `CurrentWave.isBossWave && BossController.Instance == null`일 때만 `_spawner.Spawn(bossPrefab, -1, -1f)`. **중복 스폰 가드가 이 단계의 진짜 요점** (되감기 시 2마리 방지)
  - `WaveManager.BossWarningActive` — 상태 플래그가 아니라 `gameTime`에서 매 프레임 **계산하는 프로퍼티**. 되감기 시 자동으로 올바른 값이 된다(복원할 상태가 없음)
  - WARNING 구간엔 `SpawnBurst` 건너뜀, `BossController.Instance != null`이면 `_spawnTimer = spawnInterval * 3f`
- **검증:** 임시로 보스 WaveData의 startTime을 15초로 낮춰 테스트 → 보스 등장, 추적, 사살, 골드/EXP 증가. 멀리 도망가면 속도 ×2.5. **보스 몸에 계속 붙어 있으면 1초마다 반복 피해**(Enter 방식이면 여기서 1회만 들어옴 — 이 케이스로 Stay 구현을 검증). Shift 되감기 → 보스 HP·위치가 되돌아감(기존 경로만으로 동작하는지 확인)
- **여기서 멈춰도 되는가:** ✅ "거대한 강적 1마리"로 성립. 되감기 HP 복원도 이미 동작
- **작업량:** 신규 2 + 수정 2 파일 / 난이도 중 / 저녁 1~2회

---

### Stage 2 — 텔레그래프 + 장판(GroundSlam) ★못 버림

- **목표:** 시퀀스 상태머신 + 예고 → 폭발이 동작
- **파일:** `BossController.cs`(상태머신) · `BossTelegraph.cs`(신규) · 보스 프리팹(수동)
- **핵심 내용:**
  - `enum BossActionState { Idle, Telegraph, Execute }` + `_sequenceIndex` + `_stateTimer` + `_cooldownTimer`
  - Idle에서 쿨다운 소진 → `patternSequence[_sequenceIndex]` 시작 → Telegraph(보스 **정지**) → Execute → Idle, `_sequenceIndex = (_sequenceIndex + 1) % Length`
  - `BossTelegraph`: 프리팹 내부 자식 SpriteRenderer 2개(테두리 링·채움)를 `SetActive` + `transform.localScale`/`color.a` 로 표현. 붉은 네온 색 상수 1개
  - 장판 폭발: 중심-플레이어 거리 ≤ `slamRadius` → `player.OnDamaged(slamDamage)` + `CameraController.Shake` 소폭
- **검증:** 예고원 1초 차오름 → 폭발. 예고 중 보스가 멈춤. 예고원 밖으로 나가면 무피해. 업그레이드 패널이 뜬 동안 예고가 **얼어붙는지** (KB dual-pause 확인)
- **여기서 멈춰도 되는가:** ✅ 보스전의 정체성(예고→회피)이 이미 성립
- **작업량:** 신규 1 + 수정 1 / 난이도 중 / 저녁 1~2회

---

### Stage 3 — Rewind 연계 ★★ 이 기능의 셀링포인트

패턴을 더 늘리기 **전에** 한다. 여기가 무너지면 뒤 작업이 전부 재작업이 된다.

- **목표:** 되감기 후 같은 패턴이 같은 타이밍에 재현된다
- **파일:** `RewindSnapshot.cs` · `RewindManager.cs` · `BossController.cs`(Capture/Restore) · `BossProjectile.cs`(ClearAll 스텁만 먼저)
- **핵심 내용:** 2장 설계 그대로. `CaptureState()`/`RestoreBossState()`, `ApplyBoss`는 `ApplyEnemies` **뒤**, `RewindManager.ResetCooldown()` 추가 후 **보스 등장 순간 호출**(요구 4)
- **검증(가장 중요):**
  1. 장판 예고 도중 Shift → 예고원 위치·차오름 진행도가 그 시점으로 복원되고, 되감기 종료 후 **같은 자리에** 다시 떨어진다
  2. 보스를 절반 깎고 되감기 → HP·시퀀스 인덱스가 함께 되돌아간다
  3. 보스를 죽인 직후 되감기 → 보스가 풀에서 부활하고 패턴 상태가 복원된다 (`Instance` null 아님)
  4. 보스 등장 **이전**으로 되감기 → 보스가 사라지고(Case C), 시간이 다시 흐르면 **1마리만** 재등장
  5. 보스 등장 순간 되감기 쿨타임 게이지가 0이 된다
- **여기서 멈춰도 되는가:** ✅ 그리고 **여기까지가 최소 출하선.** 데모/면접에서 보여줄 것은 여기 다 있다
- **작업량:** 수정 3 + 신규 1 / 난이도 **상**(이 계획에서 가장 어려움) / 저녁 2회

---

### Stage 4 — 방사탄(RadialBurst)

- **목표:** 2번째 패턴. 탄막 + 되감기 시 소거
- **파일:** `BossProjectile.cs`(완성) · `BossController.cs`(패턴 분기 추가) · 탄환 프리팹(수동)
- **핵심 내용:**
  - `BossProjectile`: `Managers.Object` 풀 사용, `Update` 맨 위 `Playing` 가드, 수명 만료/피격 시 `Return`, `OnEnable/OnDisable`로 `static List<BossProjectile> _active` 자체 관리 → **씬 전환 훅 불필요**(KB pooled-object 대응)
  - `ClearAll()`은 `_active` 역순 순회하며 Return (순회 중 컬렉션 변경 주의)
  - 각도: `first = 플레이어 방향`, `step = 360f / count` — 랜덤 없음
- **검증:** 링 예고 0.6초 → 12발 균등 발사, 3초 후 소멸, 피격 20. 발사 직후 되감기 → **탄환 전부 사라짐**. 업그레이드 패널 중 탄환 정지
- **여기서 멈춰도 되는가:** ✅ 패턴 2종이면 시퀀스 반복이 눈에 보인다
- **작업량:** 신규 1 + 수정 1 / 난이도 중 / 저녁 1회

---

### Stage 5 — 보상 패키지 + 퀘스트

- **목표:** 처치 보상이 실제로 체감된다
- **파일:** `BossController.OnDead` · `RewindManager.cs`(+`AddAutoRewindCharge`) · `GameManagerEx.cs` · `QuestData.cs` · `QuestService.cs`
- **핵심 내용:**
  - 대량 EXP + 골드 + `AddAutoRewindCharge()` + `ResetCooldown()` + **화면 정화**
  - 화면 정화 주의: `EnemyBase.Registry`를 순회하면서 `OnDamaged`를 호출하면 `OnDead`가 딕셔너리를 수정한다 → **`new List<EnemyBase>(Registry.Values)`로 복사한 뒤** 순회. 보스 자신은 제외
  - `RunBossKills` → `CommitResult()`에서 `LifetimeBossKills`에 합산, `GameScene.Awake`에서 0 초기화
- **검증:** 처치 시 레벨업 팝업(1.5레벨분), 골드 +150, Auto-Rewind pip +1, 잡몹 전멸, 쿨타임 0. 결과 씬 → 타이틀 → 퀘스트 패널에 BOSS KILLS 진행도 1
- **여기서 멈춰도 되는가:** ✅
- **작업량:** 수정 5(전부 소폭) / 난이도 하 / 저녁 1회

---

### Stage 6 — UI 로직 훅 + game-ui-artist 인계

- **목표:** 보스 HP 바·WARNING이 **값으로는 동작**하고, 비주얼은 넘길 준비가 된다
- **파일(coder):** `BossHpBar.cs` · `BossWarningUI.cs` · `TitleScene.cs`(BOSS RUSH 버튼 필드)
- **핵심 내용:**
  - 둘 다 `Update`에서 폴링. **이벤트 구독을 쓰지 않는 이유:** 보스는 풀에서 부활할 수 있어(되감기 Case B2) 구독 해제/재구독 타이밍이 생기고, 그게 정확히 `pooled-object` 계열 버그의 온상이다. 값 2개 폴링은 비용이 무의미하게 싸다
  - `BossController`가 노출할 것: `BossName`, `Hp`(EnemyBase 기존), `MaxHp`, `Phase`
  - ⚠️ **`MaxHp`는 `_data.hp`에서 읽어라.** `EnemyBase._originHp`를 쓰면 안 된다 — `EnemyBase.RestoreSnapshot`이 `_originHp = s.hp`로 덮으므로, 되감기 한 번이면 최대치가 "그 시점 HP"로 줄어들어 HP 바가 항상 가득 찬 것처럼 보인다
  - `_root.SetActive(Instance != null)` 만으로 표시 토글
  - 임시로 기본 UGUI Slider/Text를 붙여 값 검증까지만 (예쁘게는 artist)
- **인계 내용:** "상단 보스 HP 바 + 이름 + WARNING 배너를 ui-kit 토큰으로. `BossHpBar`/`BossWarningUI`를 붙이고 `_root/_slider/_nameText/_hpText`, `_root`를 프리팹 내부 자식으로 연결. 타이틀에 BOSS RUSH 버튼 추가 후 `TitleScene._bossRushButton` 연결. **로직 스크립트 내용은 수정 금지.**"
- **검증:** 보스 등장 시 바가 뜨고 깎이며, 처치 시 사라진다. WARNING이 3초간 뜬다
- **여기서 멈춰도 되는가:** ✅ (못생겼지만 기능 완성)
- **작업량:** 신규 2 + 수정 1 / 난이도 하 / 저녁 1회 + artist 별도

---

### Stage 7 — BOSS RUSH 데모 모드

- **목표:** 타이틀 버튼 1개로 보스전 즉시 시연 (심사·면접·GIF)
- **파일:** `GameScene.cs`(static 플래그) · `TitleScene.cs`(리스너) · `BossRushStarter.cs`(신규) · `WaveManager.JumpTo`
- **플래그 전달 방식 (확정):**
  - `GameScene`에 `public static bool BossRush;` 단 하나
  - 타이틀의 **두 버튼이 모두 값을 명시적으로 설정**한다 — START는 `false`, BOSS RUSH는 `true`. 소비형(consume-once) 의미론을 쓰지 않으므로 잔류 상태 버그가 원천 차단된다
  - **새 씬을 만들지 않는다.** GameScene 그대로
- **프리셋 적용 지점 (확정):**
  - 신규 `BossRushStarter` 컴포넌트 + `[DefaultExecutionOrder(100)]`. 기존 `GameScene`/`WeaponManager`/`PlayerController`의 실행 순서를 **건드리지 않고** 확실히 뒤에서 실행하기 위함. `GameScene.Start`에 끼워 넣으면 `WeaponManager.Start`와의 순서가 미보장이라 회피
  - `Start()`: `if (!GameScene.BossRush) { enabled = false; return; }` → `Resources.Load<WeaponData>("Weapons/...")` 2~3개를 `WeaponManager.AddWeapon` → `UpgradeDamage/UpgradeFireRate`/`player.UpgradeMaxHp` 몇 번 → `WaveManager.JumpTo(_startTime)`
  - `[SerializeField] float _startTime = 176f;` — 3:00 보스 4초 전. **Inspector에서 356으로 바꾸면 SWARMLORD 데모** (추가 코드 0)
  - `JumpTo(float t)`: `_gameTime = t`, `_waveIndex`를 `startTime <= t`인 마지막 웨이브로 재계산, `_spawnTimer = 0`. 보스는 **정식 경로(CheckWaveTransition)로** 등장 → WARNING까지 그대로 재현되고 코드 중복 0
- **검증:** 타이틀 BOSS RUSH → 무기 3개 보유 상태로 시작 → 4초 뒤 WARNING → 보스. 그 다음 타이틀 START로 시작하면 **평범한 1레벨**로 시작(플래그 잔류 없음)
- **여기서 멈춰도 되는가:** ✅
- **작업량:** 신규 1 + 수정 3 / 난이도 하~중 / 저녁 1회

---

### Stage 8 — 돌진(Charge)  ※ 시간 부족 시 여기부터 잘라냄 (3순위 컷)

- **목표:** MONOLITH 3번째 패턴
- **파일:** `BossController.cs`만
- **핵심 내용:** 텔레그래프 0.8s 조준선(`BossTelegraph` 재사용, 스케일 늘린 가는 사각) → 종료 시 방향 고정(스냅샷 `lockedDir`) → 1.2초간 `chargeSpeed`로 직선 이동. 돌진 중 접촉은 §2.6의 분기로 `contactDamage` 대신 `chargeDamage 30`이 적용된다 — **접촉 판정 코드는 Stage 1에서 이미 만들어져 있으므로 여기선 분기 한 줄뿐**
- **검증:** 조준선이 보이고, 옆으로 피하면 안 맞는다. 돌진 중 되감기 → 같은 방향으로 다시 돌진
- **여기서 멈춰도 되는가:** ✅
- **작업량:** 수정 1 / 난이도 중 / 저녁 1회

---

### Stage 9 — 페이즈 2  ※ 2순위 컷

- **목표:** HP 50% 이하에서 긴장 상승
- **파일:** `BossController.cs` · `CameraController.cs`(Shake)
- **핵심 내용:** 모든 쿨다운 × `phase2CooldownMult`, 방사탄 `radialCountPhase2`, SpriteRenderer 색 → `phase2Color`, 진입 1회 화면 흔들림. **신규 패턴 없음.** `phase2HpRatio == 0`이면 전체 비활성
- **검증:** 50% 교차 시 색 전환 + 흔들림 1회. 되감기로 50% 위로 올라가면 페이즈 1로 복귀하고 다시 교차 시 재연출
- **여기서 멈춰도 되는가:** ✅
- **작업량:** 수정 2 / 난이도 하~중 / 저녁 1회

---

### Stage 10 — SWARMLORD  ※ 1순위 컷 (그러나 가장 쌈)

- **목표:** 데이터 주도 설계 증명
- **파일:** **C# 0줄.** 프리팹 1 + `BossData` 에셋 1 + `WaveData` 에셋 1 (전부 수동 작업)
- **검증:** 6:00에 마름모 보스 등장, 시퀀스가 장판·장판·방사탄으로 반복, 돌진 없음. **git diff에 .cs 파일이 없어야 성공** ← 이게 포트폴리오 증거물
- **여기서 멈춰도 되는가:** ✅ (마지막 단계)
- **작업량:** 에셋 3 / 난이도 하 / 저녁 0.5회

> **컷 순서 주석:** 지시받은 컷 순서(2번째 보스 → 페이즈2 → 돌진)를 그대로 뒤집어 빌드 순서로 삼았다. 다만 Stage 10은 **코드 0줄·30분**이고 포트폴리오 서사에서 가치가 가장 크므로, 실제로 시간이 빠듯해지면 Stage 9(페이즈2)를 건너뛰고 Stage 10을 먼저 하는 것도 합리적이다. 판단은 그 시점에.

---

## 5. Unity 에디터 수동 작업 목록 (사용자 직접 수행)

계획 실행 중 **코드로 할 수 없는 것들**. 단계별로 필요한 시점에 안내한다.

### Stage 1 시점

1. **보스 프리팹 생성** — 기존 잡몹 프리팹을 **복제**해서 시작할 것 (Rigidbody2D / Collider2D(isTrigger) / Layer 설정이 이미 되어 있어 `Projectile`·`AoEWeapon` 피격이 그대로 동작한다. 맨땅에서 만들면 이 배선이 빠져 "보스가 안 맞는" 증상이 난다)
   - `EnemyMover` 컴포넌트 **제거** → `BossController` 부착
   - ⚠️ **`Rigidbody2D.Sleeping Mode`를 `Never Sleep`으로** — `OnTriggerStay2D`는 리지드바디가 잠들면 호출이 끊긴다. 텔레그래프 중 보스가 정지하고 플레이어도 멈춰 있으면 접촉 피해가 조용히 사라진다(§2.6)
   - ⚠️ **`SpriteRenderer`를 새로 추가해야 한다** — 잡몹 프리팹엔 SpriteRenderer가 **없다**(3개월차 GPU 인스턴싱 작업 때 제거, `EnemyInstanceRenderer.DrawMeshInstanced`가 대신 그린다). 복제만 하면 보스가 **화면에 안 보인다**. Sprite는 내장 `Knob`, Color 시안, Order in Layer는 플레이어보다 낮게
   - `transform.scale` 3~4배
   - `Assets/Resources/` 밖(예: `Assets/Prefabs/`)에 두어도 됨 — `WaveData`가 직접 참조
2. **`BossData` 에셋 생성** — `Create > Game > BossData` → `MONOLITH` 이름. 3장 표의 기본값 입력, `patternSequence = [GroundSlam, RadialBurst, Charge]`
3. 보스 프리팹의 `BossController._data`에 위 에셋 드래그 (**같은 프리팹 외부 SO 참조 — 허용 범위**)
4. **`WaveData4.asset` 생성** — `Resources/Waves/`, `startTime 180`, `isBossWave ✔`, `bossPrefab` 연결, `spawnInterval`/`enemyPrefab`은 보스전 중 잡몹용으로 채울 것
   - ⚠️ `WaveManager`가 `startTime` 오름차순 정렬하므로 기존 WaveData1~3(0/5/10초, 테스트값)의 시간도 **본편 밸런스로 재조정 필요** — 별도 밸런싱 TODO와 함께
5. 테스트 편의: `WaveData4.startTime`을 임시 15초로 두고 개발, 완료 후 180으로 복구

### Stage 2 시점

6. 보스 프리팹 **자식 오브젝트 2개** 추가 — `Telegraph_Circle`(테두리 링), `Telegraph_Fill`(채움). SpriteRenderer + `BossTelegraph` 부착, 기본 비활성
   - 스프라이트는 Unity 내장 `Knob`(원형)으로 충분. 붉은 네온은 색 + Bloom(URP Volume은 이미 씬에 있음)
   - `BossController`의 SerializeField에 **프리팹 내부 자식이므로 Inspector 드래그** (AGENTS 규칙 부합)

### Stage 4 시점

7. **보스 탄환 프리팹** — 작은 원형 SpriteRenderer + Collider2D(isTrigger) + `BossProjectile`. Layer는 플레이어와 충돌 가능하도록(잡몹 발사체가 없으므로 Physics2D 레이어 매트릭스 확인 필요)
8. `BossData`가 아닌 **보스 프리팹**에서 탄환 프리팹 참조 연결(또는 `Resources/` 배치 후 로드 — 구현 시 확정)

### Stage 5 시점

9. **`boss_kills.asset` 생성** — `Resources/Quests/`, `Create > Game > Quest`. `id = boss_kills`, `stat = BossKills`, `targets = [1,5,20]`, `rewards = [150,400,1000]`

### Stage 7 시점

10. GameScene에 빈 오브젝트 `BossRushStarter` 추가(`--- Managers ---` 하위) + 컴포넌트 부착
11. 타이틀 씬 `BOSS RUSH` 버튼 오브젝트 → `TitleScene._bossRushButton` 연결 (**game-ui-artist가 생성기로 처리하면 이 항목은 자동**)

### Stage 10 시점

12. 보스 프리팹 복제 → `Boss_Swarmlord`. 스프라이트를 사각으로 교체하고 **z축 45° 회전** → 마름모(추가 아트 리소스 0)
13. `BossData` 에셋 복제 → `SWARMLORD`. `patternSequence = [GroundSlam, GroundSlam, RadialBurst]`, `radialCount 24`, `radialSpeed 2.5`, `goldReward 300`
14. `WaveData5.asset` — `startTime 360`, `isBossWave`, `bossPrefab = Boss_Swarmlord`

### 상시

15. URP Global Volume의 Bloom이 켜져 있는지 확인 (예고·네온 표현의 전제)
16. 보스 SpriteRenderer의 `sortingOrder`를 잡몹보다 낮게(뒤로) 두면 잡몹에 가려지지 않는 큰 실루엣이 나온다 — 취향

---

## 6. 주의사항 (구현자 체크리스트)

- **`Random` 금지** — 보스 관련 신규 코드에 `Random.` 이 등장하면 결정성 위반. 리뷰 시 grep으로 확인
- **모든 신규 타이머는 `if (Managers.Game.State != GameState.Playing) return;` 아래** (KB `dual-pause-timer-leak`). 업그레이드 패널은 `timeScale`을 0으로 만들지 **않는다**
- **`patternSequence.Length`에 대한 가정 금지** — 길이 1~N에서 모두 동작해야 SWARMLORD 요구가 성립
- **보스 보상·사운드는 `State == Playing`일 때만** (`EnemyBase.OnDead`의 기존 패턴 그대로). 되감기 리플레이 중 중복 획득 방지
- **화면 정화 시 Registry 복사 후 순회** — `OnDead`가 딕셔너리를 수정한다
- **`OnEnable/OnDisable`로 `BossController.Instance` 관리** — 풀 부활 경로(`ForceRestore`)는 `Init`을 타지 않는다
- **접촉 데미지는 `OnTriggerStay2D`** — `OnTriggerEnter2D`로 짜면 큰 보스 안에 머무는 동안 피해가 1회만 들어온다(§2.6). `EnemyMover` 복붙 금지
- **텔레그래프/탄환 정리** — 보스가 죽거나 풀로 반납될 때 예고 오브젝트 비활성 필수. 안 하면 예고원만 화면에 남는다
- Inspector 연결은 **보스 프리팹 내부 자식**까지만. `WaveManager`↔`BossController`↔`RewindManager`↔UI는 전부 `FindObjectOfType` / static Instance
- `EnemyBase.cs`는 **수정하지 않는다** — 조사 결과 확장 지점(virtual Init/OnDead/RestoreSnapshot)이 이미 충분하다
- 신규 SFX 없음 — 기존 `SoundManager.Explosion` / `Hit` 재사용

---

## 7. 리스크

| 리스크 | 영향 | 완화 |
|---|---|---|
| **되감기 상태 복원(Stage 3)이 가장 난이도 높음** — 순서 의존(ApplyEnemies→ApplyBoss), 풀 부활, EntityId 일치 | 높음. 셀링포인트 직결 | Stage 3을 패턴 확장보다 **먼저** 배치했다. 검증 시나리오 5개를 미리 정의해 둠 |
| 보스가 `EnemyBase.Registry`에 있어 `EnemyJobScheduler`/`SpatialHashGrid`/`AoEWeapon` 모두에 노출 | 중. 예기치 못한 상호작용 | 의도된 설계(무기 적중에 필요). 단 보스는 separation 결과를 **조회하지 않음**으로 이동 결정성 확보 |
| 되감기 후 보스 2마리 스폰 | 높음(즉시 눈에 띔) | `BossController.Instance != null` 가드. Stage 1 검증 항목에 포함 |
| 예고 오브젝트/탄환이 씬 전환 후 잔존 | 중 (KB에 실제 전례) | 탄환은 `@Pool` 사용(`Clear()`가 실제 파괴), 예고는 프리팹 내부 자식, `static _active`는 `OnDisable` 자체 정리 |
| 기존 WaveData가 0/5/10초 테스트값 | 보스 3:00/6:00이 의미를 가지려면 본편 웨이브 밸런싱 필요 | 별도 밸런싱 TODO. 보스 기능 자체와는 분리 |
| 12일 마감 + 잔여 TODO(밸런스/GIF/README) | 높음 | Stage 6까지가 최소 출하선. Stage 8~10은 잘라도 됨 |
| Physics2D 레이어 매트릭스 — 보스 탄환이 플레이어를 맞히는 설정 부재 가능 | 중. Stage 4에서 "탄이 안 맞음"으로 발현 | 수동 작업 7번에 명시. 잡몹 발사체 선례가 없으므로 새 레이어가 필요할 수 있음 |

## 8. 미확정 항목 (구현 중 결정 필요)

1. **보스 HP 수치** — 제안: MONOLITH 1200 / SWARMLORD 2000. 플레이어 DPS를 실측해서 "60~90초 교전" 목표로 조정. 밸런싱 단계로 이월
2. **보스 등장 연출** — 현재 계획은 `EnemySpawner.Spawn`의 화면 밖 랜덤 위치(코드 0줄). 등장 연출이 밋밋하면 화면 상단 고정 위치로 바꿀지 여부. **일단 무료 경로로 가고 나중에 판단**
3. **BOSS RUSH 프리셋 구성** — 무기 3종 조합과 배율 구체값. 실제 시연해보며 "적당히 강한" 느낌을 잡는 게 빠름
4. ~~보스 상시 접촉 데미지 여부~~ → **확정: 상시 접촉 데미지 있음** (2026-07-28 사용자 결정). `contactDamage 25` / `contactInterval 1.0`, 돌진 중에만 `chargeDamage 30`으로 대체. 구현은 §2.6 — **`OnTriggerStay2D` 필수**
5. **SWARMLORD 페이즈 2 유무** — `phase2HpRatio`로 켜고 끌 수 있게 설계됨. 기획 미명시 → 일단 MONOLITH와 동일하게 켜두는 것을 제안
6. **육각형 스프라이트** — 절차적 생성이 필요. 원형(내장 Knob)으로 시작하고, 여유가 있으면 game-ui-artist에게 월드용 다각형 스프라이트를 추가 요청하는 것을 권장(마름모는 사각 45° 회전으로 무료)
