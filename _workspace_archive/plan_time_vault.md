# 기능: 타임 볼트 (Time Vault) — 보물상자 → 슬롯 룰렛 팝업 골드 획득

레퍼런스: 탕탕특공대 "전투 지원품" 슬롯머신.
필드에 간헐적으로 보물상자가 스폰되고, 접촉하면 게임이 멈추면서 슬롯 팝업이 뜬다.
마커가 5×5 격자의 바깥 테두리 16칸을 시계방향으로 돌다가 감속하며 멈추고, 그 칸의 골드를 지급한다.

**포트폴리오 어필 포인트(설계에서 반드시 드러날 것):**
> **결과를 먼저 뽑고, 그 칸에 멈추도록 총 이동 칸 수를 역산한다.**
> 애니메이션이 멈춘 자리를 결과로 읽는 구조가 아니다. `TreasureReward`(순수 로직) 와
> `UI_TreasurePopup`(연출) 이 파일 단위로 분리되며, 연출은 결과를 **입력으로 받는다.**
> 이 분리 덕분에 스킵·프레임드랍·재추첨 훅이 전부 공짜로 성립한다.

---

## 0. 사전 조사 결과 (현행 구조 확인 완료)

| 확인 대상 | 결과 | 계획에 미치는 영향 |
|---|---|---|
| `UpgradeManager` + `UI_UpgradePanel` | `GameState.Paused` 만 걸고 `timeScale` 은 1 유지. UI 는 `Start()` 에서 매니저를 스스로 찾아 구독 | **이 모달 전례를 그대로 복제**한다 |
| `Managers.Game.RunGold` | 비직렬화 런 누적값. `CommitResult()` 에서 `TotalGold` 로 뱅킹 | 팝업은 `RunGold += gold` 한 줄이면 끝 |
| `HudStats.Update()` | 매 프레임 `game.RunGold` 를 읽어 표시 | **HUD 골드 갱신 코드 0줄** — 자동 반영 |
| `WaveManager.GameTime` / `BossWarningActive` | public 프로퍼티. `BossWarningActive` 는 상태 플래그가 아니라 매 프레임 계산 | 스폰 스케줄러가 그대로 읽으면 된다 |
| `BossController.OnDead()` | `if (State == Playing)` 블록 안에서 보상 지급 (되감기 리플레이 중복 방지) | **보스 상자 드롭은 이 블록 안에 1줄** |
| `FrameSnapshot` | player / enemies / wave / boss 만 담는다. 상자도 `RunGold` 도 없음 | **되감기 대응 구현 비용 0** (아래 §5) |
| `ObjectManager` (@Pool = DontDestroyOnLoad) | 활성 오브젝트가 씬을 넘어 살아남는 이력 있음 (KB) | **상자는 풀링하지 않는다** (§4 판단 근거) |
| `SoundManager` | `PlayEffect(key, minInterval)` 로 중복 폭주 방지. 클립은 `Resources/Sounds/*.wav` 5종 | 기존 키 재사용 + pitch 로 3종 구성 |
| `UISpriteFactory` / `UIProceduralSprite` / `UIBuild` / `UITheme` / `UILayerCanvas` | 3-레이어 ui-kit 완비. `UILayer.Modal = 300` | 팝업은 Modal 레이어 |
| `UpgradePanelGenerator` / `UIGenScene` | `[MenuItem]` 생성기 전례. `UIGenScene.ResolveMainCanvas` 로 캔버스 확정 | 신규 생성기가 그대로 따른다 |
| `DebugController` | 릴리스 포함 치트키 모음 | 검증용 즉시 스폰 키를 여기 추가 |

### KB 검색 결과 — 이 기능에서 밟기 쉬운 지뢰 4개

1. **`dual-pause-timer-leak`** — 이 프로젝트는 정지 경로가 둘이다.
   `PauseController` = `Paused` + `timeScale=0`, `UpgradeManager` = `Paused` 만.
   → **팝업 애니메이션은 `Time.unscaledDeltaTime` 을 쓴다.** `deltaTime` 을 쓰면 나중에 누가
   `timeScale=0` 경로를 붙이는 순간 스핀이 영원히 안 멈춘다. (`UIModalTween` 과 동일한 이유)
   → **상자의 수명·거리 판정은 `Playing` 가드 아래**에 둔다. 시간 누적은 항상 가드 아래.
2. **`pooled-object-survives-scene-change`** — `@Pool` 은 `DontDestroyOnLoad`.
   → 상자를 풀에 넣으면 씬 전환 정리 대상이 하나 늘어난다. **일반 `Instantiate` 로 간다** (§4).
3. **`procedural-sprite-not-serialized`** — 절차 스프라이트를 씬/프리팹에 구우면 흰 박스.
   → 생성기는 `Image.sprite` 직접 대입 금지, **반드시 `UIProceduralSprite` 컴포넌트로 위임.**
4. **`editor-generator-scene-not-saved`** / **`nested-canvas-generator-duplication`**
   → 생성기는 `UIGenScene.ResolveMainCanvas` 로만 캔버스를 잡고, 실행 후 **Ctrl+S 필수**.
   → 레이아웃 컨테이너 폭 같은 값은 `Start()` 에서 런타임 불변식으로 다시 강제한다.

---

## 1. 파일 소유권 — **두 에이전트가 같은 파일을 건드리지 않는다**

### game-coder 소유 (로직)

| 경로 | 신규/수정 | 요약 |
|---|---|---|
| `Assets/Scripts/Treasure/TreasureReward.cs` | **신규** | 보상 테이블 + 결과 롤 (순수 static, MonoBehaviour 아님) |
| `Assets/Scripts/Treasure/TreasureChest.cs` | **신규** | 월드 상자 — 수명·깜빡임·거리 폴링 획득 |
| `Assets/Scripts/Treasure/TreasureSpawner.cs` | **신규** | 스폰 스케줄러 + 롤 실행 + 팝업 호출 |
| `Assets/Scripts/UI/UI_TreasurePopup.cs` | **신규** | 팝업 상태머신/스핀 애니메이션 **로직** |
| `Assets/Scripts/Enemy/BossController.cs` | 수정 | `OnDead()` 의 `Playing` 블록에 보스 드롭 1줄 |
| `Assets/Scripts/Utils/DebugController.cs` | 수정 | `T` 키 = 상자 즉시 스폰 (검증용) + 오버레이 안내 문자열 |

### game-ui-artist 소유 (비주얼)

| 경로 | 신규/수정 | 요약 |
|---|---|---|
| `Assets/Scripts/Editor/TreasurePopupGenerator.cs` | **신규** | `[MenuItem("Tools/UI/Build Treasure Popup")]` — 팝업 계층 생성 + 스타일 + **SerializeField 배선** |

> **금지:** ui-artist 는 `UI_TreasurePopup.cs` 를 **한 줄도 고치지 않는다.**
> 공유 자산(`UISpriteFactory` / `UIProceduralSprite` / `UITheme` / `UIBuild`)도 이번 MVP 에서는
> **수정하지 않는다.** 기존 shape(Rounded / Circle / Ring)만으로 충분하다.
> 반대로 game-coder 는 `Assets/Scripts/Editor/` 아래를 건드리지 않는다.

---

## 2. 확정 스펙

### 2-1. 필드 상자

| 항목 | 값 | 비고 |
|---|---|---|
| 첫 스폰 | 40초 | 스포너 자체 누적 타이머 |
| 이후 간격 | 60초 ±10 (`Random.Range(50f, 70f)`) | |
| 동시 존재 최대 | 2개 | 스포너가 들고 있는 리스트 길이로 판정 |
| 스폰 위치 | 플레이어 기준 **반경 6~10** 원형 랜덤 | `Random.insideUnitCircle.normalized * Random.Range(6f,10f)` |
| 수명 | 25초, 마지막 5초 깜빡임 | |
| 획득 | **접촉식, 반경 0.6, `sqrMagnitude <= 0.36` 비교.** 자동 흡입 없음 | Physics2D 레이어 **신규 생성 금지** |
| 금지 구간 | `WaveManager.BossWarningActive == true` 면 스폰 스킵(타이머는 다시 굴림) | |
| 보스 드롭 | 보스 처치 시 그 자리에 확정 1개 (**최대 2개 제한을 무시**) | 보상이 조용히 사라지면 안 됨 |

### 2-2. 보상 테이블 (16칸, 균등 추첨)

`TreasureReward.Layout` — **테두리 시계방향 순서와 1:1 대응하는 고정 배열.**
칸 위치가 매번 바뀌면 "잭팟이 어디 있는지 보는" 재미가 죽는다.

| 인덱스 | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 골드 | 20 | 40 | 80 | 40 | **150** | 20 | 40 | 80 | 20 | 40 | **300** | 20 | 80 | 40 | 20 | 20 |

- 개수 검증: 20×6, 40×5, 80×3, 150×1, 300×1 = 16칸 ✅
- 기대값 = (120+200+240+150+300)/16 = **63.1G** ✅
- 150(idx 4) 과 300(idx 10) 은 6칸 떨어뜨렸다 — 고가 칸이 붙어 있으면 "아깝다" 연출이 과해진다.

**시간 배율** (획득 시점 `WaveManager.GameTime` 기준):

| 구간 | 배율 |
|---|---|
| 0 ~ 120초 | ×1 |
| 120 ~ 240초 | ×1.5 |
| 240초 ~ | ×2 |

지급 골드 = `Mathf.RoundToInt(baseGold * multiplier)`.

### 2-3. 슬롯 팝업 연출 타임라인 (총 2.2초)

| 단계 | 길이 | 동작 |
|---|---|---|
| `Fast` | 0.6초 | 초당 20칸 등속 회전. 칸이 바뀔 때마다 tick 사운드 |
| `Decel` | 1.0초 | ease-out(`1-(1-t)³`) 으로 **목표 칸에 정확히 착지**. 남은 칸 수 = 2바퀴(32) + 목표까지 잔여 |
| `Highlight` | 0.6초 | 당첨 칸 강조 + `+{gold} G` 표시. **골드는 이 단계 진입 순간 지급** |
| 종료 | — | `State = Playing`, 팝업 `SetActive(false)` |

- **탭 스킵:** `Fast` 중 화면 아무 곳이나 탭하면 즉시 `Decel` 진입 (현재 칸에서 역산 다시 수행).
  `Decel`/`Highlight` 중 탭은 무시 — 착지 순간을 못 보면 연출이 무의미하다.
- **상자 1개 = 스핀 1회.** 팝업이 열린 동안 `State != Playing` 이므로 다른 상자의 `Update`
  (거리 판정 포함)가 전부 멈춘다 → **동시 획득/큐 처리 코드가 불필요하다.**

---

## 3. 두 에이전트 간 인터페이스 계약 (**이게 핵심 — 양쪽 다 이 표를 따른다**)

### 3-1. 생성기가 만들 계층 (오브젝트 이름 고정)

```
Canvas                                  ← UIGenScene.ResolveMainCanvas 로 확정한 메인 캔버스
└ TreasurePopup                         ← [UI_TreasurePopup] + UILayerCanvas(Modal) + Image(Backdrop 딤) + Button(스킵)
   └ Popup                              ← 라운드 패널 + CanvasGroup + UIModalTween
      ├ HeaderBar
      │   └ Title                       ← "TIME VAULT"
      ├ Ring                            ← GridLayoutGroup 5×5 (컨테이너, 그래픽 없음)
      │   ├ Cell00 … Cell24             ← 25개 슬롯 자리. 안쪽 9칸(6,7,8,11,12,13,16,17,18)은 비활성
      │   │   └ Label                   ← 각 테두리 칸의 TMP_Text (텍스트는 런타임에 코더가 채움)
      │   └ Marker                      ← Ring 의 **마지막 자식**(최상위 렌더). 칸 위로 이동하는 강조 프레임
      ├ CenterArt                       ← 가운데 3×3 영역 장식
      └ ResultText                      ← "+120 G" (초기 비활성 또는 빈 문자열)
```

- **`TreasurePopup` 루트는 항상 비활성으로 저장한다.** 로직이 `Start()` 에서 `SetActive(false)` 를
  한 번 더 강제하므로 씬 드리프트에도 안전하다.
- `Marker` 는 `Cell*` 과 **같은 부모(Ring)** 여야 한다 — 로직이 `_marker.position = cell.position`
  으로 옮기기 때문. GridLayoutGroup 이 Marker 까지 배치하지 않도록 `Marker` 에
  `LayoutElement.ignoreLayout = true` 를 켠다.

### 3-2. `UI_TreasurePopup` 의 `[SerializeField]` — 이름·타입·순서 고정

| 필드명 | 타입 | 생성기가 넣을 값 |
|---|---|---|
| `_popupRoot` | `RectTransform` | `Popup` |
| `_cells` | `Image[]` (길이 **16**) | 테두리 칸을 **시계방향 순서**로 (아래 3-3) |
| `_cellLabels` | `TMP_Text[]` (길이 **16**) | `_cells[i]` 의 자식 `Label` — 순서 동일 |
| `_marker` | `RectTransform` | `Marker` |
| `_resultText` | `TMP_Text` | `ResultText` |
| `_skipButton` | `Button` | `TreasurePopup` 루트의 `Button` (전체화면 백드롭) |

- 배선은 생성기가 `new SerializedObject(popup)` → `FindProperty("_cells")` → `arraySize = 16` →
  각 원소 `objectReferenceValue` 대입 → `ApplyModifiedProperties()` 로 한다
  (`UpgradePanelGenerator` 가 배열을 읽는 방식의 쓰기 버전).
- **칸의 골드 숫자 텍스트는 생성기가 정하지 않는다.** `UI_TreasurePopup.Start()` 가
  `TreasureReward.Layout[i]` 를 읽어 `_cellLabels[i].text` 를 채운다.
  → 보상 테이블을 바꿔도 UI 가 자동으로 따라오고, **표와 화면이 어긋날 수 없다.**

### 3-3. 시계방향 인덱스 정의 (양쪽이 같은 순서를 써야 한다)

5×5 격자, `row 0` = 최상단, `col 0` = 최좌측. `GridLayoutGroup` 자식 순서 = `row*5 + col`.

```
 00  01  02  03  04          0   1   2   3   4
 05  ..  ..  ..  09         15               5
 10  ..  ..  ..  14   →     14    (CenterArt) 6
 15  ..  ..  ..  19         13               7
 20  21  22  23  24         12  11  10   9   8
```

`_cells` 배열 순서 = 자식 인덱스 기준
**`[0,1,2,3,4, 9,14,19,24, 23,22,21,20, 15,10,5]`** (좌상단 → 우측 → 아래 → 좌측 → 위로).

### 3-4. 비주얼 톤 가이드 (ui-artist 재량 범위)

- 토큰만 사용: `UITheme.PanelBase`(팝업), `PanelHeader`(헤더), `CardSurface`(칸), `Outline`,
  `Gold`(마커/잭팟), `Cyan`(150 칸), `TextPrimary`/`TextSecondary`.
- 값에 따른 칸 색 위계 권장: 20/40 = `CardSurface`, 80 = 살짝 밝게, 150 = `Cyan` 계열,
  300 = `Gold` 계열. **단, 색은 `TreasureReward.Layout` 을 참조해 결정하지 말고
  생성기 내부 상수 배열로 둔다** (에디터 코드가 런타임 로직에 의존하면 순환이 생긴다).
- 세로 1080×1920 기준 권장 수치: 팝업 폭 = 화면의 86%, Ring 셀 160×160 + spacing 12
  (5×160 + 4×12 = 848 → 팝업 안에 들어감), 헤더 72, 결과 텍스트 영역 80.
- `UIModalTween` 을 `Popup` 에 부착 — 등장 연출 무료.

---

## 4. 설계 판단 (왜 이렇게 하는가)

| 판단 | 근거 |
|---|---|
| **상자를 풀링하지 않는다** (`Instantiate` / `Destroy`) | 한 판에 많아야 5~8개. 풀은 `@Pool`(DontDestroyOnLoad) 자식이 되어 씬 전환 정리 대상이 하나 늘어난다(KB `pooled-object-survives-scene-change`). 일반 씬 오브젝트는 씬 로드가 알아서 파괴한다 → **정리 코드 0줄** |
| **`Collider2D` 없이 거리 폴링** | 새 Physics 레이어를 만들면 매트릭스 4조합 규칙이 깨진다. 상자는 최대 2개 → 프레임당 비교 2회, `SpatialHash` 를 끌어들일 이유가 없다 |
| **`TreasureChest` 는 획득을 스스로 처리하지 않는다** | 롤·팝업·사운드는 `TreasureSpawner` 가 한다. 상자는 "수명 + 거리" 만 아는 순수 월드 오브젝트. `WaveManager` 참조를 상자마다 들 필요가 없어진다 |
| **`TreasureReward` 는 static 클래스** | 상태가 없다. 인스턴스도 인터페이스도 필요 없다 |
| **팝업은 `Time.unscaledDeltaTime`** | KB `dual-pause-timer-leak` — 정지 경로가 둘인 프로젝트. `UIModalTween` 이 같은 이유로 unscaled 를 쓴다 |
| **골드 지급 시점 = `Decel` 종료(=`Highlight` 진입)** | 연출 도중 씬이 바뀌어도 보상이 증발하지 않는다. `Highlight` 는 순수 표시 |
| **사운드는 기존 클립 재사용** | `Resources/Sounds/` 에 5종밖에 없다. 새 wav 는 사용자가 넣어야 하는 외부 리소스이므로 MVP 밖. `SoundManager` 를 **수정하지 않는다** |

**사운드 매핑 (MVP):**

| 용도 | 호출 |
|---|---|
| 틱 | `Managers.Sound.PlayEffect(SoundManager.Shoot, 0.04f)` — 칸이 바뀔 때마다 |
| 정지 | `Managers.Sound.PlayEffect(SoundManager.Explosion)` — `Highlight` 진입 시 |
| 획득 | `Managers.Sound.PlayEffect(SoundManager.EnemyDeath)` — 상자 접촉 시 |

> 추후 전용 wav 3종(`VaultTick` / `VaultStop` / `VaultGet`)을 `Resources/Sounds/` 에 넣으면
> `SoundManager` 에 `const string` 3개만 추가하고 위 호출의 키만 바꾸면 된다.

---

## 5. 되감기(Rewind) 연계 — **구현 비용 0**

`FrameSnapshot` 에는 상자도 `RunGold` 도 없다. 따라서:

- 되감아도 **이미 먹은 상자는 부활하지 않고, 획득한 골드도 유지된다.**
  → 무한 재추첨 루프가 **구조적으로 불가능**하다.
- 안 먹은 상자는 움직이지 않으므로 되감기 후에도 그 자리에 그대로 두면 된다 — **복원할 상태가 0.**
- 되감기 중(`State == Rewinding`)에는 상자의 `Update` 가 `Playing` 가드에 걸려 수명도 안 줄고
  거리 판정도 안 한다 → 되감기 리플레이 중 오획득 없음.
- 스포너의 스폰 타이머도 되돌리지 않는다. 되감기 5초로 상자 스폰 시점이 밀리는 정도는
  체감 불가이고, 되돌리면 **같은 상자가 두 번 스폰**되는 경로가 생긴다.

**명시할 룰 (코드 주석 + 이 문서에 남긴다):**

> **"상자의 획득 여부는 되돌아가지 않는다."**
> `BossProjectile.ClearAll()` 과 동급의 되감기 예외다. 스냅샷에 넣지 않는 것이 곧 규칙의 구현이다.

`TreasureSpawner` 클래스 상단과 `TreasureChest.Collect()` 위에 이 문장을 주석으로 남길 것.

### 씬 전환 / 게임오버 정리 검토 결과 → **추가 코드 불필요**

| 경로 | 무슨 일이 일어나나 |
|---|---|
| 게임오버 → `Result` | `SceneManagerEx.ChangeScene` 이 `Managers.Object.Clear()` + `EnemyBase.ClearRegistry()` 수행. 상자는 **풀 소속도 static 레지스트리 소속도 아니므로** 일반 씬 오브젝트로서 `LoadScene` 에 파괴된다 |
| Pause → 타이틀 | 동일 |
| `GameOver` 상태에서 씬 전환 전 몇 프레임 | 상자 `Update` 는 `Playing` 가드에 걸려 무동작 |
| 팝업이 열린 채 사망 | **불가능** — 팝업 중엔 `State == Paused` 라 플레이어가 피해를 입지 않는다 |

⚠️ **game-coder 는 `TreasureChest` 에 `static` 컬렉션을 만들지 마라.** 스포너의 인스턴스 필드
`List<TreasureChest> _alive` 로 충분하다. static 을 쓰는 순간 KB의 "씬을 넘어 사는 static 레지스트리"
버그 계열에 편입되어 `ClearRegistry` 같은 정리 코드가 필요해진다.

---

## 6. 단계별 구현 순서 — 각 단계마다 **눈으로 확인 가능**

### Step 1 — 보상 로직 (연출 없이 숫자만) · game-coder

- **신규:** `Assets/Scripts/Treasure/TreasureReward.cs`

```csharp
public struct TreasureRollResult
{
    public int slotIndex;    // 0~15, 테두리 시계방향
    public int baseGold;
    public float multiplier;
    public int gold;
}

public static class TreasureReward
{
    public const int SlotCount = 16;
    public static readonly int[] Layout = { 20,40,80,40,150,20,40,80,20,40,300,20,80,40,20,20 };

    // 연출이 결과를 만들지 않는다 — 여기서 먼저 뽑고, 팝업은 이 칸에 멈추도록 역산만 한다
    public static TreasureRollResult Roll(float gameTime) { ... }
    static float Multiplier(float gameTime) { ... }  // 120초/240초 경계
}
```

**검증:** `DebugController` 에서 임시로 `Roll` 을 100회 호출해 `Debug.Log` 합계를 찍고
평균이 63 근처인지 확인. 확인 후 임시 코드 제거. (또는 Step 5 의 `T` 키로 실측)

---

### Step 2 — 필드 상자 + 스폰 스케줄러 (팝업 없이 로그만) · game-coder

- **신규:** `Assets/Scripts/Treasure/TreasureChest.cs`

```
[SerializeField] float _lifetime = 25f;
[SerializeField] float _blinkStart = 5f;      // 남은 수명이 이 값 이하면 깜빡임
[SerializeField] float _pickupRadius = 0.6f;
[SerializeField] SpriteRenderer _sprite;      // 프리팹 내부 참조 → Inspector 드래그 (규칙 준수)

public void Init(TreasureSpawner spawner, Transform player)
void Update()   // ① Playing 가드 → ② 수명 감소·만료 → ③ 깜빡임 → ④ sqrMagnitude 거리 판정
```

- 만료: `_spawner.OnChestExpired(this)` 호출 후 `Destroy(gameObject)`
- 획득: `_spawner.OnChestCollected(this)` 호출 후 `Destroy(gameObject)`
- 깜빡임: `_sprite.enabled = Mathf.Repeat(_remain, 0.3f) > 0.15f` 정도로 단순하게

- **신규:** `Assets/Scripts/Treasure/TreasureSpawner.cs`

```
const float FirstSpawnTime = 40f, IntervalMin = 50f, IntervalMax = 70f;
const float MinSpawnDist = 6f, MaxSpawnDist = 10f;
const int   MaxAlive = 2;

GameObject _chestPrefab;              // Resources.Load<GameObject>("Treasure/TreasureChest")
List<TreasureChest> _alive;
Transform _player; WaveManager _wave; UI_TreasurePopup _popup;   // 전부 Start() 에서 자기등록

void Update()      // Playing 가드 → 타이머 → CanSpawn 이면 SpawnAtRandom
public void SpawnAt(Vector3 pos)          // 보스 드롭 — MaxAlive 무시
public void OnChestCollected(TreasureChest c)  // 리스트 제거 → Roll → 팝업 Open → 획득 사운드
public void OnChestExpired(TreasureChest c)    // 리스트 제거만
```

- `Update` 에서 `_wave.BossWarningActive` 면 스폰을 건너뛰고 타이머를 다시 굴린다.
- Step 2 시점에는 팝업이 아직 없으므로 `OnChestCollected` 에서
  `Debug.Log($"[TreasureSpawner] slot={r.slotIndex} gold={r.gold}")` + `RunGold += r.gold` 만.

**수동 작업 (Step 2 전에 사용자가 수행) → §7-A, §7-B**

**검증:**
- 40초에 상자 1개 등장, 이후 50~70초마다 추가. 화면에 3개 이상 동시에 존재하지 않음.
- 25초 지나면 사라지고, 마지막 5초 깜빡임.
- 상자에 몸으로 닿으면 콘솔에 slot/gold 가 찍히고 **HUD 골드가 즉시 오른다**(코드 0줄).
- 보스 경고(붉은 경고 UI) 중에는 새 상자가 안 생긴다.

---

### Step 3 — 팝업 로직 (임시 UI 로 동작 확인) · game-coder

- **신규:** `Assets/Scripts/UI/UI_TreasurePopup.cs`

```
enum SpinPhase { Hidden, Fast, Decel, Highlight }

const float FastDuration = 0.6f, DecelDuration = 1.0f, HighlightDuration = 0.6f;
const float FastCellsPerSec = 20f;
const int   DecelLaps = 2;

[SerializeField] RectTransform _popupRoot;
[SerializeField] Image[]   _cells;        // 16, 시계방향
[SerializeField] TMP_Text[] _cellLabels;  // 16, 동일 순서
[SerializeField] RectTransform _marker;
[SerializeField] TMP_Text _resultText;
[SerializeField] Button   _skipButton;

public void Open(TreasureRollResult result)
```

핵심 로직 (요약):

```
Open(result):
    _result = result;
    Managers.Game.State = Paused;              // UpgradeManager 와 동일 패턴
    FindObjectOfType<VirtualJoystick>()?.Cancel();   // 재개 시 옛 터치 좌표로 튐 방지
    gameObject.SetActive(true);
    _phase = Fast; _elapsed = 0f; _spinStartIndex = _markerIndex;

Update():  // 전부 Time.unscaledDeltaTime
    Fast:   idx = (_spinStartIndex + (int)(FastCellsPerSec * _elapsed)) % 16
            0.6초 경과 → BeginDecel()
    Decel:  t = _elapsed / DecelDuration
            eased = 1 - (1-t)³
            idx = (_decelStartIndex + Mathf.RoundToInt(eased * _decelSteps)) % 16
            t >= 1 → BeginHighlight()
    Highlight: 마커 깜빡임 + 0.6초 경과 → Close()

BeginDecel():
    // ★ 역산 — 목표 칸에 정확히 착지시키기 위한 총 이동 칸 수
    int forward = ((_result.slotIndex - _markerIndex) + 16) % 16;
    _decelStartIndex = _markerIndex;
    _decelSteps = DecelLaps * 16 + forward;

BeginHighlight():
    _markerIndex = _result.slotIndex;         // 다음 스핀의 시작점 — 마커가 순간이동하지 않는다
    Managers.Game.RunGold += _result.gold;    // ★ 지급은 여기 한 곳뿐
    _resultText.text = $"+{_result.gold} G";
    Managers.Sound.PlayEffect(SoundManager.Explosion);

Close():
    gameObject.SetActive(false);
    Managers.Game.State = Playing;
```

- 마커 이동: `SetMarkerIndex(int i)` 에서 `_markerIndex` 변경 시에만
  `_marker.position = _cells[i].rectTransform.position` + tick 사운드.
- `_skipButton.onClick` → `if (_phase == Fast) BeginDecel();`
- `Start()` 에서 `_cellLabels[i].text = TreasureReward.Layout[i].ToString()` 로 라벨 채움 +
  `gameObject.SetActive(false)`.
- `TreasureSpawner.OnChestCollected` 를 `_popup.Open(result)` 호출로 교체.

**임시 UI (ui-artist 작업 전 확인용):** Canvas 아래에 빈 오브젝트 + `UI_TreasurePopup` 를 붙이고
`Image` 16개를 아무렇게나 배치해 배선해도 된다. **또는** Step 4 를 먼저 끝내고 이 단계를 검증해도 된다.
게임-코더가 임시 UI 를 만들었다면 **Step 4 전에 반드시 삭제**한다(중복 `UI_TreasurePopup` 은
생성기의 캔버스 확정 로직을 방해한다).

**검증:**
- 상자 획득 → 게임이 멈추고 마커가 돈다. 적·플레이어가 정지.
- **마커가 멈춘 칸의 숫자 = 지급된 골드 / 시간배율.** 20회 반복해 단 한 번도 어긋나지 않아야 한다.
  → 이게 "결과 선행 + 역산" 이 제대로 구현됐다는 유일한 증거다.
- 탭하면 즉시 감속으로 넘어가고, **그래도 같은 칸에 멈춘다.**
- 팝업 중 ESC(Pause) 를 눌러도 아무 일도 안 일어난다(`PauseController` 가 `Playing` 아니면 무시).
- 총 연출 시간 ≤ 2.2초.

---

### Step 4 — 팝업 비주얼 생성기 · **game-ui-artist**

- **신규:** `Assets/Scripts/Editor/TreasurePopupGenerator.cs` — `[MenuItem("Tools/UI/Build Treasure Popup")]`

`ui-kit` 스킬의 3-레이어 순서를 지킨다:
1. **절차적 스프라이트** — `UISpriteFactory` 의 기존 함수만 사용 (신규 shape 추가 금지)
2. **컴포넌트** — 모든 그래픽은 `UIProceduralSprite.Configure(...)` 로 위임
   (⚠️ `Image.sprite` 직접 대입 절대 금지 — KB `procedural-sprite-not-serialized`)
3. **에디터 생성기** — idempotent (`FindOrCreateChild`), 재실행해도 중복 생성 없음

필수 준수 사항:
- 캔버스는 반드시 `UIGenScene.ResolveMainCanvas("TreasurePopupGenerator")` 로 확정.
  `FindFirstObjectByType<Canvas>()` 사용 금지 (KB `nested-canvas-generator-duplication`).
  `null` 이면 즉시 `return`.
- 루트에 `UILayerAssign.AssignLayer(root, UILayer.Modal)`.
- 백드롭 `Image.color = UITheme.Backdrop`, `raycastTarget = true` (뒤 클릭 차단 + 스킵 탭 수신).
- §3-1 계층·이름, §3-2 필드 배선, §3-3 시계방향 순서를 **정확히** 따른다.
- 마지막에 `EditorUtility.SetDirty` + `EditorSceneManager.MarkSceneDirty` + 콘솔 안내
  ("**Ctrl+S 로 씬을 저장하세요**" — KB `editor-generator-scene-not-saved`).
- 안쪽 9칸(자식 인덱스 6,7,8,11,12,13,16,17,18)은 `SetActive(false)` 하고
  `CenterArt` 를 Ring 위에 겹쳐 3×3 영역을 덮는다.
- `Ring` 컨테이너 폭 같은 레이아웃 값은 씬 드리프트 대비로 `Popup` 폭 대비 앵커 스트레치로 잡는다.

**검증:**
- 메뉴 실행 → 콘솔 에러 없음, `Canvas/TreasurePopup` 생성, **Ctrl+S**.
- **생성기를 3번 연속 실행해도 계층이 늘어나지 않는다.**
- Unity 를 껐다 켠 뒤에도 흰 박스가 없다 (절차 스프라이트 재생성 확인).
- 플레이 → 상자 획득 → 팝업이 정상 배치로 뜨고, 16칸에 20/40/80/150/300 숫자가
  §2-2 표 순서대로 찍혀 있다.

---

### Step 5 — 보스 드롭 + 디버그 키 + 사운드 마감 · game-coder

- **수정:** `Assets/Scripts/Enemy/BossController.cs` — `OnDead()` 의 `if (State == Playing)` 블록 안:
  ```csharp
  FindObjectOfType<TreasureSpawner>()?.SpawnAt(transform.position);
  ```
  (그 블록 안이어야 되감기 리플레이 중 중복 드롭이 안 생긴다 — 기존 `RunBossKills++` 와 같은 이유)
- **수정:** `Assets/Scripts/Utils/DebugController.cs`
  - `TreasureSpawner _treasureSpawner;` 캐시 (`Start()`)
  - `if (kb.tKey.wasPressedThisFrame) _treasureSpawner?.SpawnAt(_player.position + Vector3.up * 3f);`
    (`Playing` 가드 아래에 배치)
  - `OnGUI` 안내 문자열에 `T: 상자` 추가
- 사운드 3종 호출 위치 최종 확인 (§4 표).

**검증:**
- `T` 키로 즉시 상자 스폰 → 반복 획득으로 통계 확인이 쉬워진다.
- 보스 처치 → 그 자리에 상자 1개 확정 드롭. 동시 2개 제한과 무관하게 나온다.
- 보스 처치 직후 되감기 → 보스가 부활해도 **상자가 중복 드롭되지 않는다.**
- 틱/정지/획득 사운드가 각각 들리고, 고속 회전 중 소리가 뭉개지지 않는다.

---

## 7. 사용자가 Unity 에디터에서 직접 해야 하는 작업

### A. 보물상자 프리팹 생성 (Step 2 전)

1. `Assets/Resources/` 아래에 **`Treasure` 폴더** 생성.
2. Hierarchy → 우클릭 → `2D Object > Sprites > Square` → 이름을 **`TreasureChest`** 로 변경.
3. `Transform.Scale` = `(0.6, 0.6, 1)`.
4. `SpriteRenderer`
   - `Color` = 금색 `#FFC23C` (`UITheme.Gold` 와 동일 톤)
   - `Sorting Order` = **5** (플레이어가 10이므로 그 아래 — 상자에 가려지지 않는다)
5. `Layer` 는 **`Default` 그대로 둔다.** ⚠️ 새 Physics 레이어를 만들지 말 것.
6. `Collider` 를 **붙이지 않는다** (거리 폴링 방식).
7. `TreasureChest.cs` 컴포넌트 추가 → `_sprite` 필드에 **자기 자신의 SpriteRenderer 를 드래그**
   (같은 프리팹 내부 참조이므로 Inspector 드래그가 규칙에 맞다).
8. Hierarchy 의 오브젝트를 `Assets/Resources/Treasure/` 로 드래그해 **프리팹화** →
   Hierarchy 의 원본은 **삭제**.
   (경로가 정확히 `Assets/Resources/Treasure/TreasureChest.prefab` 이어야
   `Resources.Load<GameObject>("Treasure/TreasureChest")` 가 찾는다.)

### B. 스포너 배치 (Step 2 전)

1. `GameScene.unity` 를 연다.
2. Hierarchy 의 **`--- Managers ---`** 아래에 빈 GameObject 생성 → 이름 **`TreasureSpawner`**.
3. `TreasureSpawner.cs` 컴포넌트 추가. **Inspector 연결 필드는 없다**(전부 자기등록).
4. **Ctrl+S 로 씬 저장.**

### C. 팝업 생성기 실행 (Step 4 후)

1. `GameScene.unity` 를 연 상태에서 메뉴 `Tools > UI > Build Treasure Popup` 실행.
2. 콘솔에 에러가 없는지 확인 (에러가 있으면 캔버스 확정 실패 — 메시지 지시를 따른다).
3. Hierarchy 에서 `Canvas > TreasurePopup` 이 생겼고 **비활성 상태**인지 확인.
4. **Ctrl+S 로 씬 저장.** ⚠️ 이걸 빠뜨리면 며칠 뒤 레이아웃이 "이유 없이" 되돌아간다
   (KB `editor-generator-scene-not-saved` 의 실제 재발 사례).
5. 배선 확인: `TreasurePopup` 선택 → Inspector 의 `_cells` 16칸, `_cellLabels` 16칸,
   `_marker` / `_resultText` / `_skipButton` 이 모두 채워져 있어야 한다.

### D. (선택) 전용 사운드 교체

`Resources/Sounds/` 에 `VaultTick.wav` / `VaultStop.wav` / `VaultGet.wav` 를 넣었다면
`SoundManager` 상단에 `const string` 3개를 추가하고 호출 키만 교체한다.

---

## 8. AGENTS.md 제약 체크리스트 (구현 전 재확인)

- [ ] 네이밍: private `_camelCase` / public 프로퍼티 `PascalCase` / 메서드 `PascalCase` / `[SerializeField] _camelCase`
- [ ] Input: 팝업 스킵은 **UGUI `Button`** 으로 처리 (프로젝트 표준). 직접 폴링이 필요하면
      `Touchscreen.current` / `Keyboard.current` **Polling** 만. Action-based 금지
- [ ] 씬 간 참조는 `FindObjectOfType`, 프리팹 로드는 `Resources.Load`. Inspector 드래그는 프리팹 내부만
- [ ] 새 Physics 레이어 **생성 금지**, `LayerMask`/`NameToLayer` **사용 금지**
- [ ] 인터페이스·제네릭 추상화 금지. `TreasureReward` 는 static, 상자는 상속 없음
- [ ] 주석은 WHY 만. 특히 다음 3곳은 **반드시** WHY 주석을 남긴다:
      ① 팝업의 `unscaledDeltaTime` ② `BeginDecel` 의 역산 ③ "상자 획득은 되돌아가지 않는다"
- [ ] Hierarchy: 스포너는 `--- Managers ---`, 팝업은 `--- UI --- > Canvas` 하위
- [ ] `Assets/Scripts/Treasure/` 신규 폴더 (Quest / Shop / Upgrade / Rewind 와 같은 기능별 폴더 관례)
- [ ] 커밋 시 `개발_진행상황.md` 갱신 + 새 시스템이므로 `아키텍처.md` 에 구조·흐름 추가

---

## 9. 최종 검증 기준 (전 단계 완료 후 통합 확인)

| # | 항목 | 기준 |
|---|---|---|
| 1 | 스폰 리듬 | 40초 첫 상자, 이후 50~70초 간격, 동시 최대 2개 |
| 2 | 수명 | 25초 후 소멸, 마지막 5초 깜빡임 |
| 3 | 획득 | 몸으로 닿아야만 획득. 흡입 없음. 스치듯 지나가면 안 먹힘 |
| 4 | 보스 경고 | 경고 UI 표시 중 새 상자 스폰 없음 |
| 5 | 보스 드롭 | 처치 시 그 자리에 1개 확정 |
| 6 | **결과 일치** | **마커가 멈춘 칸 숫자 × 시간배율 = 지급 골드. 20회 연속 불일치 0회** |
| 7 | 스킵 | `Fast` 중 탭 → 즉시 감속, 착지 칸 동일. `Decel` 중 탭은 무시 |
| 8 | 연출 길이 | 스킵 없이 2.2초 이내 종료 |
| 9 | 정지 | 팝업 중 적·플레이어·웨이브 타이머 정지, 팝업 애니는 계속 |
| 10 | HUD | 골드 표시가 지급 즉시 갱신 (`HudStats` 무수정) |
| 11 | 시간 배율 | 0~2분 ×1 / 2~4분 ×1.5 / 4분+ ×2 (`T` 키 + `BossRushStarter` 로 시점 이동해 확인) |
| 12 | **되감기** | 상자 획득 직후 되감기 → **상자가 부활하지 않고 골드도 그대로** |
| 13 | 되감기 중 | `Rewinding` 상태에서 상자 수명 감소·획득 없음 |
| 14 | 씬 전환 | 상자를 남긴 채 게임오버/타이틀 복귀 → 콘솔 에러 0. 다음 판에 유령 상자 없음 |
| 15 | 생성기 | 3회 연속 실행해도 계층 중복 없음. Unity 재시작 후 흰 박스 없음 |
| 16 | 상호작용 | 레벨업 팝업과 슬롯 팝업이 동시에 뜨지 않음 |

---

## 10. 추후 확장 (**지금 구현하지 않는다**)

### 10-1. 재추첨 (Re-Roll) — `REWIND ↺`

사용자가 "추후 구현할 수도 있다"고 판단한 항목. **이번 MVP 범위 밖이다.**

> 팝업 하단에 `REWIND ↺` 버튼. 결과가 마음에 안 들면 1회 무르고 재추첨한다.
> 대가는 **능동 Rewind 쿨다운 풀 충전** — 게임의 정체성(되돌리기)을 보상 시스템에 얹는 훅이라
> 포트폴리오 서사와 맞는다.

**지금 해둘 준비 (설계에만 반영, 코드 추가 없음):**

- `TreasureReward.Roll(float gameTime)` 은 **상태가 없는 static 함수**다.
  → 재추첨은 그냥 한 번 더 호출하면 된다. 훅 지점이 이미 열려 있다.
- `UI_TreasurePopup.Open(TreasureRollResult)` 는 결과를 **인자로 받는다.**
  → 재추첨은 `Close()` 없이 `_result` 를 갈아끼우고 `Fast` 부터 다시 돌리면 끝.
  `BeginDecel()` 의 역산이 `_markerIndex` 기준이라 **마커가 어디에 있든 그대로 동작한다.**
- 지급이 `BeginHighlight()` 한 곳에만 있으므로, 재추첨은 `Highlight` 진입 **전에만** 허용하면
  이중 지급이 원천 차단된다.

⚠️ 구현할 때 주의: 쿨다운 충전은 `RewindManager` 에 `_cooldownTimer = _rewindCooldown` 을 세팅하는
public 메서드가 **없다**(`ResetCooldown()` 은 0으로 만드는 반대 동작이다). 그때 추가해야 한다.

### 10-2. 그 외 보류 항목

| 항목 | 비고 |
|---|---|
| 잭팟 파티클 / 화면 흔들림 | `CameraController.Shake` 재사용 가능 — 연출 폴리싱 단계에서 |
| HP 회복 칸 | 보상 축이 늘면 테이블·기대값 재설계 필요 |
| 보스 상자 3스핀 | 팝업이 연속 스핀을 지원해야 함 (`Close` 대신 재시작) |
| 화면 밖 상자 방향 화살표 | 세로 화면에서 상자를 놓치는 문제가 실제로 관측되면 추가 |
| 상자 희귀도 (일반/황금) | 테이블을 2개로 늘리는 순간 `TreasureReward` 에 데이터 분리가 필요 |
