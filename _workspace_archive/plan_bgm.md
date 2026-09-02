# 기능: BGM 재생 + 볼륨 슬라이더 2개 (BGM/효과음)

작성: 2026-08-07 (해커톤 제출 D-2)

## 0. 사전 조사로 확정된 사실 (추측 아님)

| 확인 항목 | 실물 근거 |
|---|---|
| `Managers.Init()`는 **최초 1회만** 실제 실행 | `Managers.cs:31` — `if (s_instance != null) return;` |
| `@SoundRoot`는 DontDestroyOnLoad, 씬 전환에 파괴되지 않음 | `SoundManager.cs:28` |
| **씬 전환 시 사운드를 멈추는 코드가 없음** | `Managers.Clear()`·`SoundManager.Clear()` 호출자 0개 |
| `BGMOn`/`EffectSoundOn`을 **set 하는 코드가 없음** | grep 0건 — 현재 항상 true인 죽은 스위치 |
| 기존 사운드 호출부는 전부 `Managers.Sound.PlayEffect(key)` | 8개 파일 9군데 |
| `AudioSource.volume`·`AudioListener.*` 사용 0건 | grep |
| Slider 생성 선례 | `BossHudGenerator.cs:66-100` |
| Pause UI 여유 공간 | StatsCard 하단 ≈ y-848, BottomBar 상단 y-1600 → 약 750px |
| mp3 임포트 | DecompressOnLoad, preload off, **7.35 MB** |

## 1. 설계 결정

### 쟁점 1 — 상태 모델: bool과 float의 중복 제거

**결론: float 볼륨을 유일한 상태로. `BGMOn`/`EffectSoundOn`은 GameData 필드·프로퍼티 모두 삭제.**

- 두 불리언은 **아무도 set 하지 않는다**. 남기면 "못 끄는 bool"과 "끄는 float" 두 벌이 되어 반드시 어긋난다.
- 삭제 영향 범위는 `SoundManager` 게이트 6줄 + `GameManagerEx` 뿐. 게임플레이 호출부(`PlayEffect`)는 무수정.

탈락 대안:
- bool 유지 + float 추가 → 요구사항이 금지한 두 벌 상태.
- `BGMOn => BgmVolume > 0f` 파생 → **치명적**. 게이트가 false면 `Play()` 자체를 안 하므로, 볼륨 0으로 저장 후 재시작하면 소스가 정지 상태가 되고 슬라이더를 올려도 소리가 안 난다.

**핵심 규칙: BGM 소스는 볼륨과 무관하게 항상 `Play()` 상태. 음소거 = `volume 0f`이지 `Stop()`이 아니다.**

### 세이브 스키마 마이그레이션

구 세이브에 `BgmVolume` 키가 없으면 `JsonUtility`가 필드 초기화식을 실행하지 않아 **0.0f → 전 게임 무음**이 되고, 사용자가 일부러 0으로 내린 것과 구분 불가. float는 null 가드로 못 잡는다.

**결론: `GameData`에 `SchemaVersion` 추가. 로드 직후 `< 1`이면 볼륨 기본값 주입 후 1로 승격.**

기본값 `BgmVolume = 0.5f`, `EffectVolume = 0.8f`. 상수로 두고 필드 초기화식과 마이그레이션이 **같은 상수**를 참조.

### 쟁점 2 — BGM 재생 지점

**결론: 씬 단위가 아니라 전역 1회 재생. `SoundManager`가 소유.**

- `@SoundRoot`가 DontDestroyOnLoad이고 아무도 정지시키지 않으므로 한 번 `Play()`하면 전 씬에 걸쳐 이어진다.
- 중복 방지: `Managers.Init()` 조기 리턴 + `PlayBgm()` 자체를 멱등하게.
- `Time.timeScale = 0`은 AudioSource를 멈추지 않으므로 Pause 중에도 BGM이 흘러 슬라이더 조절 결과를 바로 들을 수 있다.

탈락: 씬마다 `Start()`에서 재생 → 씬 전환마다 곡이 처음부터 다시 시작(끊김) + 배선 3배.

### 쟁점 3 — WebGL 오디오 컨텍스트 잠금

**결론: "첫 사용자 입력 시 재생"으로 통일. 플랫폼 분기 없이 단일 경로.**

잠긴 채 `Play()`하면 재생 헤드만 흘러가 **도입부가 유실**되고, `isPlaying`은 true라 감지도 불가능하다. 그래서 애초에 잠긴 상태에서 Play하지 않는다.

`SoundManager.Init()`이 `@SoundRoot`에 `BgmStarter`를 부착 → 첫 입력(클릭/터치/키) Polling 감지 시 `PlayBgm()` 후 자기 자신 Destroy.

- 단일 경로라 **에디터에서 검증한 것이 곧 WebGL 동작** — D-2에 가장 중요한 성질.
- 타이틀에서 어차피 PLAY를 눌러야 하므로 체감 지연 없음.

탈락: `#if UNITY_WEBGL` 분기 → 에디터에서 한 번도 실행 안 되는 경로를 D-2에 만드는 건 최악.

### 쟁점 4 — 볼륨 즉시 반영

**결론: `SoundManager.ApplyVolume()` 단일 진입점.**

```
슬라이더 onValueChanged → Managers.Game.BgmVolume = v → Managers.Sound.ApplyVolume()
```

- BGM: 재생 중인 소스에 즉시 반영 — 드래그 중 실시간.
- Effect: `PlayOneShot`은 호출 시점의 `AudioSource.volume`을 곱하므로 미리 세팅해두면 이후 모든 효과음에 자동 반영. 호출부 무수정.
- 세이브: `onValueChanged`마다 저장하면 드래그 중 초당 60회 IO. **패널이 닫힐 때(`OnDisable`) 1회 저장.**

## 2. 단계별 구현 순서

**1~4는 game-coder, 5는 game-ui-artist.** 파일 교집합 0.

### 단계 1 — 상태 모델 정리 + 세이브 마이그레이션 【game-coder】
`Assets/Scripts/Manager/GameManagerEx.cs` (수정)

1. `GameData`에서 `BGMOn`/`EffectSoundOn` **삭제**
2. `GameData`에 추가: `SchemaVersion`(int), `BgmVolume`(float), `EffectVolume`(float)
3. `GameManagerEx`: 두 bool 프로퍼티 삭제 → `DefaultBgmVolume`/`DefaultEffectVolume` const + `BgmVolume`/`EffectVolume` 프로퍼티(기존 Score/PlayTime과 같은 `_gameData` 위임 스타일, setter에서 `Mathf.Clamp01`)
4. `LoadGame()`의 `FromJson` 직후 기존 null 가드 옆에 마이그레이션 블록

> 이 시점 컴파일 에러 발생(SoundManager가 BGMOn 참조). **단계 2와 원자적.**

**멈춤 가능:** ❌ 단계 2와 한 묶음

### 단계 2 — SoundManager: 게이트 제거 → 볼륨 적용 + BGM API 【game-coder】
`Assets/Scripts/Manager/SoundManager.cs` (수정)

1. `public const string MainBgm = "Neon Static Loop";` (공백 포함 — 조회 경로 `Sounds/Neon Static Loop`). 오타 방지를 위해 **이 상수만 사용**. `LoadAudioClip`이 실패를 침묵하므로 `PlayBgm()`에 clip null 시 `Debug.LogError` 한 줄 추가
2. 두 `Play` 오버로드에서 `if (Managers.Game.BGMOn)` / `EffectSoundOn` 게이트 **6군데 제거** → 대신 `audioSource.volume` 세팅
3. 신규: `ApplyVolume()` (요소별 null 체크), `PlayBgm()` (멱등 — 이미 재생 중이면 리턴)
4. `Init()`의 **@SoundRoot 신규 생성 분기에서만** `AddComponent<BgmStarter>()`

**멈춤 가능:** ✅ 컴파일 통과, 효과음 정상. BGM은 아직 호출자 없음

### 단계 3 — BGM 시동 컴포넌트 【game-coder】
`Assets/Scripts/Manager/BgmStarter.cs` (**신규**)

- `Update()`에서 첫 입력 Polling 감지 → `Managers.Sound.PlayBgm()` → `Destroy(this)`
- `Mouse.current` / `Touchscreen.current` / `Keyboard.current.anyKey` — **전부 Polling** (AGENTS.md)
- `Destroy(this)`로 컴포넌트만 제거. `@SoundRoot`는 절대 파괴 금지

**멈춤 가능:** ✅ **요구사항 1 완성.** 볼륨은 세이브 값 고정, UI 없이도 정상

### 단계 4 — Pause 볼륨 UI 로직 【game-coder】
`Assets/Scripts/UI/PauseVolumeView.cs` (**신규**)

`PauseStatsView`와 동일한 자기등록 패턴. `[SerializeField] Slider _bgmSlider, _effectSlider`

- `Start()`: **value 세팅 → 그 다음 AddListener** (순서 고정 — 초기값이 리스너를 때려 세이브를 더럽히지 않게) → `_wired = true`
- 핸들러: `Managers.Game.BgmVolume = v; Managers.Sound.ApplyVolume();`
- `OnDisable()`: `if (!_wired) return;` 후 `SaveGame()`
  > **가드 필수** — `PauseController.Start()`가 패널을 끄는데, 스크립트 실행 순서상 `PauseVolumeView.Start()`보다 먼저 돌 수 있다
- `OnDestroy()`: `RemoveAllListeners()`
- **`PauseController.cs`, `PauseStatsView.cs` 무수정**

**멈춤 가능:** ✅ 배선 전이라 동작 변화 없음

### 단계 5 — Pause 패널에 슬라이더 배치 【game-ui-artist】
`Assets/Scripts/Editor/PauseScreenGenerator.cs` (수정 — **artist 전용**)

기존 생성기의 in-place·idempotent 규약 준수. StatsCard와 BottomBar **사이**에 SoundCard 추가.

```
SafeArea
 ├ Title / StatsCard   (기존 — 무수정)
 ├ SoundCard  ★신규
 │   ├ Header "SOUND"
 │   ├ BgmRow    [ "BGM" | BgmSlider ]
 │   └ EffectRow [ "SFX" | EffectSlider ]
 └ BottomBar           (기존)
```

- `SoundCard`: `ApplyProcedural(RadLg, OutlineWidth, PanelBase, Outline)` + `VerticalLayoutGroup`(padding S5, spacing S3) + `ContentSizeFitter(vertical=PreferredSize)` — StatsCard와 동일 조리법
- 배치: StatsCard 높이가 런타임 결정이므로 **위에서 쌓지 말고 SafeArea 하단 기준** — `anchor/pivot (0.5, 0)`, `anchoredPosition = (0, BottomBarH + UITheme.S6)`. StatsCard 행 수가 바뀌어도 안 겹친다
- 폭 `CardWidth`(760) 재사용. 신규 상수는 `VolumeRowH = 88f` 하나만(터치 타깃)
- 슬라이더는 `BossHudGenerator.cs:66-100` 참조하되 **차이 4가지**: `interactable = true` / `handleRect` 실제 핸들 지정(없으면 모바일 조작감 0) / fill 색 `Accent`(BGM)·`Cyan`(SFX) / `min 0, max 1, wholeNumbers false`, `targetGraphic = handle`
- 절차 스프라이트는 반드시 `UIProceduralSprite.Configure()` 위임 (`Image.sprite` 직접 대입 시 런타임 흰 박스)
- 배선: `SoundCard`에 `PauseVolumeView` AddComponent 후 기존 `WireIfNull` 헬퍼로 슬라이더 2개 연결 + `EditorUtility.SetDirty`

**절대 금지 (회귀 방지):**
- `ResolveMainCanvas`/`GetOrCreatePauseCanvas`/`PurgeStrays` 호출 순서 변경 (`nested-canvas-generator-duplication` 재발 트리거)
- SoundCard용 **별도 `[MenuItem]` 생성기 금지** — 캔버스 리졸버 진입점이 하나 더 생기면 실행 순서 의존 버그 재발. 기존 `Build Pause Screen` 안에만
- StatsCard·PauseStatsView 배선·PauseEntryButton 기하 무수정

**멈춤 가능:** ✅ 요구사항 2 완성

## 3. 파일 목록

### 신규
| 경로 | 담당 |
|---|---|
| `Assets/Scripts/Manager/BgmStarter.cs` | game-coder |
| `Assets/Scripts/UI/PauseVolumeView.cs` | game-coder |

### 수정
| 경로 | 담당 | 내용 |
|---|---|---|
| `Assets/Scripts/Manager/GameManagerEx.cs` | game-coder | bool 2개 삭제, float 2개 + SchemaVersion, 마이그레이션 |
| `Assets/Scripts/Manager/SoundManager.cs` | game-coder | 게이트 6곳 제거→볼륨, `MainBgm`, `ApplyVolume()`, `PlayBgm()`, BgmStarter 부착 |
| `Assets/Scripts/Editor/PauseScreenGenerator.cs` | **game-ui-artist 단독** | SoundCard + 슬라이더 2개 + 배선 |

### 무수정 (보호 대상)
`PauseController.cs`, `PauseStatsView.cs`, `Managers.cs`, `Define.cs`, 각 Scene 스크립트, **`PlayEffect` 호출 8개 파일 전부**

**충돌 없음:** coder = `Manager/` + `UI/PauseVolumeView.cs` / artist = `Editor/PauseScreenGenerator.cs`. 단계 4가 5보다 먼저 끝나야 함(생성기가 `PauseVolumeView` 타입 참조).

## 4. 사용자 수동 작업 (순서 준수)

1. **오디오 임포트 설정** — `Neon Static Loop.mp3` Inspector:
   - `Load In Background` **체크** (7.35MB 디코드 프리즈 방지)
   - `Preload Audio Data` **체크** (꺼두면 첫 Play 시 무음 가능)
   - `Load Type` = **Compressed In Memory** (DecompressOnLoad는 7MB mp3를 수십 MB PCM으로 전개 — WebGL 메모리에 치명적)
   - Quality 100% → 50~70% (선택, 빌드 용량 절감)
2. `GameScene.unity` 열기
3. 메뉴 **`Tools/UI/Build Pause Screen`** 실행 (다른 생성기는 실행 금지. 꼭 여러 개면 `Build HUD` → `Build Boss HUD` → `Build Pause Screen` 순서)
4. **`Ctrl+S` 씬 저장** — MarkSceneDirty는 저장하지 않는다. 빠뜨리면 며칠 뒤 슬라이더 소실
5. (선택) `Tools/UI/Check HUD Overlap`
6. Inspector 수동 연결: **없음** (생성기가 자동 배선)
7. 제출용: `Tools/Build/WebGL Build` → `docs/` 갱신 → 커밋·푸시

## 5. 검증

| # | 조작 | 기대 결과 |
|---|---|---|
| V1 | Title Play → 아무 키/클릭 | BGM 도입부부터 루프 시작. 첫 입력 전 무음은 **사양** |
| V2 | PLAY → 사망 → Result → TITLE | BGM 끊김·되감기 없이 연속. `@SoundRoot` 1개 |
| V3 | Pause 진입 | BGM 계속 재생(timeScale 0). 슬라이더 2개, 핸들이 현재 볼륨과 일치 |
| V4 | BGM 슬라이더 0으로 드래그 | 드래그 중 실시간 감소, 0에서 무음. 다시 올리면 **같은 지점부터** 재개 |
| V5 | SFX 0 → RESUME → 발사 | 발사음 없음. 1로 올리면 정상 |
| V6 | 조절 → TITLE → PLAY → Pause | 값 유지 (OnDisable 저장) |
| V7 | 조절 → Stop → 재Play | 값 유지. `SaveData.json`에 `BgmVolume`, `SchemaVersion:1` |
| V8 | **구 세이브 호환** — json에서 볼륨·SchemaVersion 키 삭제 후 Play | 무음이 아니라 **기본값 재생**. 골드·상점·퀘스트 **전부 보존** |
| V9 | 슬라이더 5초 흔들기 | 프레임 드랍 없음 |
| V10 | WebGL 빌드 → 10초 대기 → 클릭 | 대기 중 무음, 클릭 순간 **처음부터** 시작 |

**실패 시 첫 진단:** 무음이면 콘솔의 `PlayBgm` clip null 에러 확인 → 있으면 `MainBgm` 오타/경로. 없으면 볼륨 0 또는 임포트 설정.

## 6. 회귀 위험

| # | 위험 | 심각도 | 완화 |
|---|---|---|---|
| R1 | 구 세이브 로드 시 볼륨 0 → 전 게임 무음 | 🔴 치명 | `SchemaVersion` 마이그레이션. **V8 필수 검증** |
| R2 | 세이브 스키마 변경이 골드/상점/퀘스트를 날림 | 🔴 치명 | 필드는 추가·삭제만, 기존 필드명·타입 불변. V8 검증 |
| R3 | 생성기 재실행이 UI를 두 벌로 (`nested-canvas-generator-duplication`) | 🔴 높음 | 신규 `[MenuItem]` 금지, 리졸버 순서 불변 |
| R4 | 생성기 실행 후 씬 미저장 → 슬라이더 소실 | 🟠 높음 | `Ctrl+S` 명시. 커밋 전 `git diff`에 `SoundCard` 확인 |
| R5 | 슬라이더 초기값이 리스너를 발화시켜 세이브 오염 | 🟠 중간 | value 세팅 → AddListener 순서 고정 |
| R6 | `Start()` 전에 `OnDisable` 발화 | 🟠 중간 | `_wired` 가드 |
| R7 | 볼륨 0 저장 → 재시작 시 소스 Stop → 복구 불가 | 🟠 중간 | BGM은 항상 Play, 게이트 완전 제거. V4 검증 |
| R8 | 7.35MB mp3 DecompressOnLoad → WebGL 메모리/프리즈 | 🟠 중간 | 임포트 설정 변경. 최악의 경우 클립 단축 |
| R9 | 첫 클릭 전 무음을 버그로 오인 | 🟡 낮음 | 설계 의도. 제출 노트에 한 줄 추가 권장 |
| R10 | `PlayEffect` 호출부 9곳 회귀 | 🟡 낮음 | 시그니처·호출부 무수정. V5 검증 |
| R11 | `SoundManager.Clear()`가 null 요소에 Stop → NRE | ⚪ 정보 | **호출자 0개인 기존 dead code.** AGENTS.md에 따라 손대지 않고 기록만 |

## 7. 커밋

- 커밋 1: `feat: BGM 재생 + 볼륨 상태 단일화` (단계 1~3)
- 커밋 2: `feat: Pause 패널 BGM/SFX 볼륨 슬라이더` (단계 4~5 + 씬)
- 둘 다 `개발_진행상황.md` 갱신 동반. 상태 모델 bool→float 단일화는 설계 결정이므로 `아키텍처.md`에도 한 문단
- R1은 재발성이 높으므로 완료 후 `bug-patterns/jsonutility-float-default-needs-schema-version.md` 엔트리 저장 권장
