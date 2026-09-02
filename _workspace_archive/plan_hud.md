## 기능: 인게임 HUD (모바일 세로 1080×1920)

기존 UI 로직 스크립트(`HpBar`/`ExpBar`) **무수정**, 슬라이더 비주얼만 리스킨.
로직 추가는 (1) `RewindManager` 능동 발동 public API, (2) 신규 HUD 표시 컴포넌트 `HudStats` + `RewindButtonUI` 두 개뿐.
비주얼/배선은 신규 에디터 생성기 `HudGenerator` 하나로 in-place 생성.

---

### 확인된 사실 (Read로 검증)

- `PlayerController` 이벤트: `OnHpChanged(int,int)`, `OnExpChanged(int,int)`, `OnLevelUp(int)`. 게터 `Hp/MaxHp/Exp/ExpToNextLevel/Level`. `HpBar`(`_slider`,`_hpText`)·`ExpBar`(`_slider`,`_levelText`)가 이미 자기등록 구독 중 → **무수정**.
- `Managers.Game`(GameManagerEx): `Score`(int), `PlayTime`(float 초), `RunGold`(int). 모두 게터 존재.
- `RewindManager`: `CooldownRemaining`(=`_cooldownTimer`), `CooldownMax`(=`_rewindCooldown`, 30f), `AutoRewindCharges`(int) 게터 존재. 능동 발동은 `Update`의 Shift 분기에서 `if (_cooldownTimer<=0f && _count>0) StartCoroutine(DoRewind())`. `_count`는 private → 외부에서 발동 가능 여부를 알 수 없음 → **public API 필요**.
- `UITheme`: `Danger`(HP), `Cyan`(XP/Rewind), `Gold`, `Positive`, 여백 `S1~S7`, 라운드 `RadSm/Md/Lg`, 폰트 `Display/Header/Button/Body/Caption`.
- `UIProceduralSprite`: `ConfigureCircle(radius, fill)`, `ConfigureRing(outerRadius, thickness, fill)`, `Configure(outlined,...)`. 런타임 재생성(직렬화 null 방지 — KB).
- `SafeAreaFitter`: RectTransform을 `Screen.safeArea`로 인셋(anchor 비율). 자식은 offset 0 스트레치.
- 기존 생성기 관례(`JoystickUIGenerator`): `[MenuItem("Tools/UI/...")]`, `GetOrCreate`(idempotent), `EnsureImage`/`EnsureProcedural`, `SerializedObject.FindProperty(...).objectReferenceValue`로 배선, `EditorSceneManager.MarkSceneDirty`. **동일 패턴 재사용**.

---

### 변경/생성 파일 목록

| 파일 | 신규/수정 | 담당 | 요약 |
|------|-----------|------|------|
| `Assets/Scripts/Rewind/RewindManager.cs` | **수정(최소)** | game-coder | `CanActiveRewind` 프로퍼티 + `TryActiveRewind()` 추가. Shift 분기를 이 메서드 호출로 치환(중복 제거). 그 외 되감기 로직 무수정. |
| `Assets/Scripts/UI/HudStats.cs` | **신규** | game-coder | 타이머/스코어/골드 매 프레임 갱신(자기등록, 캐시 참조). |
| `Assets/Scripts/UI/RewindButtonUI.cs` | **신규** | game-coder | 쿨다운 링 `fillAmount` + Auto 차지 pip 갱신, 버튼 `onClick→TryActiveRewind`, `interactable=CanActiveRewind`. |
| `Assets/Scripts/Editor/HudGenerator.cs` | **신규** | game-ui-artist | `[MenuItem("Tools/UI/Build HUD")]`. 상단 상태 스트립(SafeArea) + HP/XP 슬라이더 리스킨 + 우하단 Rewind 버튼/링/pip 생성 + 신규 컴포넌트 참조 SerializedObject 배선. |
| `Assets/Scripts/UI/HpBar.cs` | **무수정** | — | 슬라이더 스타일만 생성기가 재도색. |
| `Assets/Scripts/UI/ExpBar.cs` | **무수정** | — | 동일. |

---

### 세로 레이아웃 앵커 규약 (1080×1920)

```
┌──────────── SafeArea 상단 인셋 ────────────┐
│ [StatusStrip]  (anchor top-stretch, SafeAreaFitter 하위)
│   ├ HpBar     (상단, Danger fill)
│   ├ ExpBar    (HP 아래, Cyan fill) + Lv 텍스트
│   └ 한 줄: [Score 좌] · [Timer 중앙] · [Gold칩 우]
│
│            (플레이 영역 — HUD 오브젝트 없음)
│
│                              [RewindButton] ← 우하단
│  (좌하단 = 조이스틱 존, 비워둠)      원형+Ring+pip
└──── SafeArea 하단 인셋(홈 인디케이터) ─────┘
```

- 상단 상태 스트립: `SafeAreaFitter` 부착 컨테이너의 **상단** 자식. anchor top-stretch.
- Rewind 버튼: SafeArea 컨테이너 하단, anchor **우하단**(1,0). 조이스틱(좌하단 `RadiusScreenRatio=0.12`) 반대편이라 터치 충돌 없음.
- 좌하단은 **빈 공간 유지**(조이스틱 페이드 존).

---

### 단계별 구현 순서

**1단계 — RewindManager 발동 API (game-coder)**
- 대상: `Assets/Scripts/Rewind/RewindManager.cs`
- 추가:
  ```csharp
  public bool CanActiveRewind => _cooldownTimer <= 0f && _count > 0
                                 && Managers.Game.State == GameState.Playing;
  public void TryActiveRewind()
  {
      if (CanActiveRewind) StartCoroutine(DoRewind());
  }
  ```
- Shift 분기 치환(중복 제거):
  ```csharp
  if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
      TryActiveRewind();
  ```
- **키보드 Shift 유지**(에디터 테스트용). 그 외 `DoRewind`/`Record`/`Apply*` 무수정.

**2단계 — HudStats 표시 컴포넌트 (game-coder)**
- 대상: `Assets/Scripts/UI/HudStats.cs` (신규)
- 자기등록·캐시 참조. `Update`에서 `Find` 금지 — `Managers.Game`은 정적 접근이라 참조 불필요.
- 확정 필드명(생성기가 배선):
  ```csharp
  [SerializeField] TMP_Text _timerText;
  [SerializeField] TMP_Text _scoreText;
  [SerializeField] TMP_Text _goldText;
  ```
- `Update()`에서 `Managers.Game` 읽어 세팅. 타이머는 `mm:ss` 포맷(아래 주의사항). Playing이 아니어도 마지막 값 유지(별도 분기 불필요 — 과설계 금지).

**3단계 — RewindButtonUI 표시 컴포넌트 (game-coder)**
- 대상: `Assets/Scripts/UI/RewindButtonUI.cs` (신규)
- 확정 필드명:
  ```csharp
  [SerializeField] Button _button;      // onClick 배선
  [SerializeField] Image  _cooldownRing; // type=Filled, Radial360, fillAmount 구동
  [SerializeField] Image[] _autoPips;    // Auto 차지 표시(1개 기준, 배열로 여유)
  ```
- `Start()`: `_rewind = FindObjectOfType<RewindManager>();`, `_button.onClick.AddListener(_rewind.TryActiveRewind);`
- `Update()`:
  - `_cooldownRing.fillAmount = 1f - Mathf.Clamp01(_rewind.CooldownRemaining / _rewind.CooldownMax);` (쿨다운 소진→채워짐)
  - `_button.interactable = _rewind.CanActiveRewind;`
  - Auto pip: `_rewind.AutoRewindCharges > 0` 이면 pip enabled/색상 on.
- `CooldownMax==0` 방어(0 나눗셈)만 최소 처리, 그 외 방어 없음.

**4단계 — HudGenerator 생성·리스킨·배선 (game-ui-artist)**
- 대상: `Assets/Scripts/Editor/HudGenerator.cs` (신규, `#if UNITY_EDITOR`)
- `ui-kit` 스킬 참조. `JoystickUIGenerator` 패턴 복제(GetOrCreate/EnsureImage/EnsureProcedural/SerializedObject).
- 작업:
  1. Canvas 하위 `HudRoot`(SafeAreaFitter 부착, 스트레치) 생성.
  2. **상단 StatusStrip**: HP 슬라이더·XP 슬라이더를 SafeArea 상단으로 재배치. 기존 `HpBar._slider`/`ExpBar._slider`의 Fill/Background Image에 `UIProceduralSprite`로 라운드 바 재도색(HP=`Danger`, XP=`Cyan`), **핸들 오브젝트 비활성화**(`handleRect` GameObject SetActive(false)). 기존 슬라이더가 없으면 경고만.
  3. 상태 한 줄: `_scoreText`(좌, `Body`), `_timerText`(중앙, `Header`), Gold칩(우 — 라운드 배경 `CardSurface` + `_goldText` `Gold`색).
  4. **우하단 RewindButton**: `Button`(원형 `UIProceduralSprite.ConfigureCircle`, `Cyan` 계열) + 자식 `CooldownRing`(Image, `type=Filled`, `fillMethod=Radial360`, `fillOrigin=Top`, 링 스프라이트 `ConfigureRing`) + Auto pip(작은 `ConfigureCircle`). anchor (1,0), 조이스틱 회피.
  5. `HudStats`/`RewindButtonUI` 컴포넌트를 각 오브젝트에 부착, `SerializedObject`로 위 확정 필드명 전부 배선.
- idempotent(이름 재사용), `MarkSceneDirty`.

---

### game-coder vs game-ui-artist 분담

- **game-coder** (1→2→3, 먼저): `RewindManager` API, `HudStats.cs`, `RewindButtonUI.cs`. **필드명 확정이 산출물** — 아래 배선 계약을 artist에 넘김.
- **game-ui-artist** (4, coder 이후): `HudGenerator.cs`. coder가 확정한 필드명으로만 배선. 로직 파일 무수정.
- **순차 의존**: coder 완료(필드명 fix) → artist 배선. 병렬 금지.

**배선 계약(필드명 고정):**
```
HudStats:        _timerText, _scoreText, _goldText
RewindButtonUI:  _button, _cooldownRing, _autoPips[]
```

---

### 주의사항 (CLAUDE.md 제약)

- **네이밍**: private/SerializeField `_camelCase`, 메서드/프로퍼티 `PascalCase`. 준수.
- **Polling 고정**: Rewind 버튼은 UGUI `Button.onClick`이라 Input System과 무관(허용). 키보드는 기존 `Keyboard.current.leftShiftKey` 유지 — Polling 위반 아님.
- **금지**: DI/Action-based/과설계/외부리소스 없음. 컴포넌트는 `HudStats`·`RewindButtonUI` 2개로만 분할(통합 안 함 — 표시 대상·갱신 성격 다름, 그러나 그 이상 쪼개지 않음).
- **매직값**: 색/여백/폰트는 `UITheme` 토큰만. 타이머 포맷·pip 개수 등 상수는 각 파일 `const`.
- **절차 스프라이트 런타임 적용**: 직렬화 null(흰 박스) 방지 — `UIProceduralSprite`가 `OnEnable`/`Start`에서 재생성. 생성기는 sprite를 씬에 굽지 않음(KB `procedural-sprite-not-serialized`).
- **Filled 링 주의**: `UIProceduralSprite`가 Image.sprite를 세팅하므로, 생성기는 procedural 적용 **후** `image.type = Image.Type.Filled; image.fillMethod = Radial360`을 세팅해야 함(순서 뒤집히면 type이 링에 안 걸림). 런타임 재생성 시에도 type/fillMethod는 Image 필드라 유지됨.
- **타이머 포맷**: `PlayTime`(초) → `mm:ss`. `TimeSpan` 또는 `Mathf.FloorToInt` 정수 나눗셈. 매 프레임 문자열 생성이나 GC는 포트폴리오 규모상 무시(과최적화 금지).
- **씬 참조**: `RewindButtonUI`→`RewindManager`는 씬 다른 오브젝트 → `FindObjectOfType`. HpBar/ExpBar는 기존대로 자기등록.

---

### 검증 방법 (무엇이 보이면/동작하면 성공)

**1단계(RewindManager API)**
- 컴파일 성공. 에디터에서 Shift 눌렀을 때 기존과 동일하게 되감기 발동(회귀 없음).

**2단계(HudStats)**
- 컴파일 성공. (배선 전이라 화면엔 아직 안 보임 — Null 안전.)

**3단계(RewindButtonUI)**
- 컴파일 성공. `CooldownMax` 0 나눗셈 없음.

**4단계(HudGenerator 실행 후 Play)**
- **상단**: SafeArea 아래에 HP바(붉음)·XP바(cyan)+`Lv.N`, 중앙 타이머가 `00:03`처럼 증가, 좌측 Score 증가, 우측 Gold칩이 골드 획득 시 증가.
- **핸들 없음**: 슬라이더 드래그 핸들 안 보임.
- **우하단 Rewind 버튼**: 원형 버튼 + 쿨다운 링. 되감기 사용 직후 링이 비었다가 30초에 걸쳐 서서히 참(`fillAmount` 0→1), 다 차면 `interactable` 활성(눌러짐). Auto 차지 1개일 때 pip 켜짐, 사망 Auto-Rewind 후 pip 꺼짐.
- **터치 발동**: 우하단 버튼 탭 시 Shift와 동일하게 되감기 실행.
- **조이스틱 비충돌**: 좌하단 조이스틱 드래그가 Rewind 버튼과 겹치지 않음.
- **세로 안전영역**: 시뮬레이터 노치 기기에서 상단 상태바가 노치에 안 가림, 하단 버튼이 홈 인디케이터에 안 겹침.
