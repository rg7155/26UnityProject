## 기능: UI 레이어 시스템 리팩터 (sibling 애드혹 → 상수 sortingOrder 레이어)

### 목적
UI 렌더 순서를 `SetAsLastSibling`/`TopmostOnEnable` 같은 sibling 순서 의존 방식에서, 패널마다 부여한 **상수 `sortingOrder`** 로 결정되게 전환한다. 빈 `UIManager` 스텁을 "UI 레이어의 단일 출처"로 활용한다. **열기/닫기 스택 매니저는 만들지 않는다** (이 규모엔 과설계, 자기등록 패턴 유지).

### 핵심 메커니즘 (기술 근거)
- 각 패널 루트에 자체 `Canvas(overrideSorting=true, sortingOrder=(int)_layer)` 를 붙이면, 부모 Canvas 안에서 sibling 순서와 무관하게 `sortingOrder` 큰 것이 위에 그려진다.
- **nested Canvas + overrideSorting 은 자체 `GraphicRaycaster` 가 있어야** 버튼 입력이 동작한다. → `UILayerCanvas` 가 둘 다 GetOrAdd.
- `CanvasScaler` 는 부모(루트 Canvas)에서 상속되므로 패널에 추가 불필요.

---

### 변경/생성 파일 목록
- `Assets/Scripts/Manager/UIManager.cs` — 수정. `UILayer` enum 추가(값=sortingOrder). 기존 `Init()/Clear()` 스텁 유지.
- `Assets/Scripts/UI/UILayerCanvas.cs` — **신규**. 패널 루트 부착 런타임 컴포넌트. `[SerializeField] UILayer _layer;`, `Awake`에서 `Canvas`+`GraphicRaycaster` GetOrAdd 세팅.
- `Assets/Scripts/UI/TopmostOnEnable.cs` — **삭제**(+`.meta`). 레이어 시스템으로 대체.
- `Assets/Scripts/Editor/UpgradePanelGenerator.cs` — 수정. `TopmostOnEnable` 부착 코드(51행) 제거 → `UILayerCanvas`(Modal) GetOrAdd.
- `Assets/Scripts/Editor/ShopUIGenerator.cs` — 수정. `dim.SetAsFirstSibling()`(208행)은 딤/팝업 **내부** 형제 순서 정리이므로 유지 가능하나, 패널 루트에 `UILayerCanvas`(Modal) GetOrAdd 추가. (모달 간 상하 순서는 레이어가 결정.)
- 나머지 4개 생성기(`MenuBackgroundGenerator`, `HudGenerator`, `JoystickUIGenerator`, `TitleLobbyGenerator`) — 수정. 각 패널 루트에 `UILayerCanvas`(해당 레이어) GetOrAdd.

> 주의: `ShopUIGenerator` 내부의 `dim.SetAsFirstSibling()` 및 셀 라벨의 `label.transform.SetAsLastSibling()`은 **한 패널 내부의 자식 정렬**이라 레이어 시스템과 무관 — 건드리지 않는다. 제거 대상은 "패널을 다른 패널 위로 올리는" 애드혹(모달의 최상단화)뿐이다.

---

### 레이어 배정표
| 패널(루트 오브젝트) | 레이어 | sortingOrder | 생성기 |
|---|---|---|---|
| MenuBackground | Background | 0 | MenuBackgroundGenerator |
| TitleLobby (타이틀 콘텐츠) | Hud | 100 | TitleLobbyGenerator |
| HUD (HudRoot) | Hud | 100 | HudGenerator |
| 가상 조이스틱 | Hud | 100 | JoystickUIGenerator |
| ShopPanel (상점 모달) | Modal | 300 | ShopUIGenerator |
| UpgradePanel (업그레이드 모달) | Modal | 300 | UpgradePanelGenerator |

- **enum 정의:** `Background=0, Hud=100, Modal=300`. (Popup=200 은 현재 사용처가 없어 **추가하지 않음** — 필요해질 때 추가. 값 간격 100은 나중 삽입 여유.)
- **조이스틱 = Hud**: 배경 위, 모달 아래. HUD와 동급이면 충분 (모달 열릴 때 조이스틱이 모달 위로 튀지 않아야 함 → Modal(300) < 조이스틱이면 안 되므로 Hud 유지가 정답).
- **TitleLobby = Hud**: MenuBackground(0)보다 위, 모달(상점)보다 아래.
- **동일 레이어(같은 sortingOrder) 끼리는 sibling 순서로 결정** — 현재 겹치지 않는 패널들(Title 씬: 배경/타이틀, GameScene: HUD/조이스틱)이라 충돌 없음.

---

### 단계별 구현 순서

#### Phase A — game-coder (코드 기반)
1. **UILayer enum 추가**
   - 대상: `Assets/Scripts/Manager/UIManager.cs`
   - 변경: 파일 상단에 `public enum UILayer { Background = 0, Hud = 100, Modal = 300 }` 추가. `UIManager` 클래스의 `Init()/Clear()` 스텁은 그대로 둔다. (헬퍼는 과설계 방지 위해 추가하지 않음 — enum 소유만으로 "단일 출처" 역할.)
   - 검증: 컴파일 통과, `Managers.UI` 접근 경로 무변경.

2. **UILayerCanvas 컴포넌트 신규**
   - 대상: `Assets/Scripts/UI/UILayerCanvas.cs` (신규)
   - 변경: 아래 형태.
     ```
     [RequireComponent(typeof(RectTransform))]
     public class UILayerCanvas : MonoBehaviour
     {
         [SerializeField] UILayer _layer;
         void Awake() => Apply();
         void OnValidate() => Apply();   // 에디터에서 즉시 반영
         void Apply()
         {
             var canvas = GetComponent<Canvas>();
             if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
             canvas.overrideSorting = true;
             canvas.sortingOrder = (int)_layer;
             if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                 gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
         }
     }
     ```
   - 주의: 필드명 `_layer`(SerializeField camelCase), 메서드 PascalCase. `using UnityEngine;` 필요.
   - 검증: 패널에 붙였을 때 Inspector에 레이어 드롭다운 노출, 실행 시 Canvas.sortingOrder 반영.

3. **TopmostOnEnable 삭제 + 부착 코드 제거**
   - 대상: `Assets/Scripts/UI/TopmostOnEnable.cs`(+`.meta`) 삭제, `Assets/Scripts/Editor/UpgradePanelGenerator.cs` 51행(`if (panel.GetComponent<TopmostOnEnable>()...AddComponent<TopmostOnEnable>();`) 제거.
   - 주의: 삭제 전 `TopmostOnEnable` 참조가 이 두 곳 외 없는지 확인(grep). 있으면 보고.
   - 검증: 컴파일 통과(미참조 심볼 없음).

> Phase A 완료 후 Unity 컴파일 확인. 이 시점엔 아직 씬 패널에 `UILayerCanvas` 미부착 → 렌더 순서는 기존 sibling대로(회귀 없음).

#### Phase B — game-ui-artist (생성기 수정)
4. **6개 생성기에 UILayerCanvas GetOrAdd 배선**
   - 대상: 6개 `*Generator.cs`.
   - 변경: 각 생성기가 자신의 패널 루트를 찾은 직후(예: `ShopUIGenerator`의 `panel`, `HudGenerator`의 HudRoot, `UpgradePanelGenerator`의 `panel`), 아래 공통 헬퍼로 `UILayerCanvas`를 GetOrAdd 하고 `_layer`를 `SerializedObject`로 배정표대로 설정.
     ```
     static void EnsureLayer(GameObject root, UILayer layer)
     {
         var lc = root.GetComponent<UILayerCanvas>();
         if (lc == null) lc = root.AddComponent<UILayerCanvas>();
         var so = new SerializedObject(lc);
         so.FindProperty("_layer").enumValueIndex = ...; // 또는 intValue 매핑
         so.ApplyModifiedProperties();
     }
     ```
     (enum 값이 0/100/300 비연속이므로 `enumValueIndex`(0,1,2) 매핑 주의 — `enumValueFlag`/직접 인덱스 사용. 각 생성기 로컬 static 헬퍼로 중복 정의하거나 공용 `UIBuild`에 1개 추가 판단.)
   - `ShopUIGenerator`: 패널 루트에 `EnsureLayer(panel.gameObject, UILayer.Modal)`. 내부 `dim.SetAsFirstSibling()`은 유지.
   - `UpgradePanelGenerator`: `EnsureLayer(panel.gameObject, UILayer.Modal)`. (Phase A에서 TopmostOnEnable 부착 이미 제거됨.)
   - idempotent: 재실행 시 GetOrAdd라 중복 부착 없음.
   - 주의: 모든 편집 코드는 `#if UNITY_EDITOR` 안(기존대로). `EditorSceneManager.MarkSceneDirty` 유지.
   - 검증: 각 생성기 재실행 후 해당 패널 루트에 `UILayerCanvas`(올바른 레이어) 1개만 존재.

---

### 재실행해야 할 생성기·씬 (사용자 액션)
씬에 반영되려면 각 씬을 연 상태로 관련 생성기를 재실행해야 한다.
- **Title 씬:** `Tools/UI/Build Menu Background`, `Tools/UI/Build Title Lobby`, `Tools/UI/Build Shop Popup`
- **GameScene:** `Tools/UI/Build HUD`, `Tools/UI/Build Joystick`(메뉴명 실제 확인), `Tools/UI/Build Upgrade Panel`
- 각 실행 후 씬 저장.

---

### 주의사항 (CLAUDE.md 제약)
- **과설계 금지:** 스택/열기닫기 매니저·레이어 등록 시스템 만들지 않음. enum + 1개 컴포넌트 + 생성기 배선만.
- **네이밍:** `_layer`(SerializeField camelCase), `UILayer`/`Background`/`Hud`/`Modal`(enum PascalCase), 메서드 PascalCase.
- **매직값 상수화:** sortingOrder 는 enum 값이 곧 상수 — 생성기에 숫자 하드코딩 금지, `UILayer.Modal` 등 사용.
- **기존 UI 로직 스크립트 무수정:** `UI_ShopPanel`/`UI_UpgradePanel` 등 런타임 로직은 손대지 않음. 생성기의 리스킨/배선 코드만.
- **Editor 코드 `#if UNITY_EDITOR`** 유지.
- **DebugController(OnGUI/IMGUI)** 는 Canvas 렌더와 무관 — 건드리지 않음.

### Unity 특이사항
- nested Canvas + overrideSorting 은 `GraphicRaycaster` 필수(입력). `UILayerCanvas`가 보장.
- `CanvasScaler`는 자식 Canvas에 상속되므로 추가하면 오히려 스케일 이중 적용 위험 → 추가하지 않음.
- 동일 sortingOrder 패널끼리는 여전히 sibling 순서 — 현재 겹치는 동급 패널 없음(회귀 없음).

### 리스크
- 각 패널이 overrideSorting sub-Canvas가 되면서 **버튼 입력/딤 클릭차단/모달이 HUD 위** 를 재검증 필요.
- 6개 생성기 재실행 + 2개 씬 저장이 사용자 수동 작업 → 하나라도 누락되면 해당 패널만 구 방식으로 남음.

### 검증 방법 (구현 후)
1. **컴파일:** Phase A 후 에러 0 (TopmostOnEnable 미참조 확인).
2. **모달 상하:** GameScene에서 업그레이드 패널 열면 HUD/조이스틱 위에 그려짐. Title에서 상점 팝업이 배경·타이틀 위에 그려짐.
3. **버튼 입력:** 상점 셀/닫기, 업그레이드 카드 3개, 타이틀 버튼 정상 클릭(각 패널 자체 GraphicRaycaster 동작 확인).
4. **딤:** 모달 백드롭 딤이 뒤 클릭을 차단하고 정상 표시.
5. **재실행 idempotent:** 생성기 2회 실행해도 `UILayerCanvas`/`Canvas`/`GraphicRaycaster` 각 1개만.
