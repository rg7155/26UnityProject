## 코드 리뷰 결과

검토 기준: CLAUDE.md (네이밍/금지패턴/참조방식/Unity 패턴 + 요청 중점 체크 5항목)

### 합격

- `Assets/Scripts/Manager/UIManager.cs` — 이상 없음
  - enum 단일 출처 `UILayer { Background=0, Hud=100, Modal=300 }` 정의 명확. 값=sortingOrder, 간격 100은 후속 삽입 여유(주석으로 WHY 명시 — 규약 부합). 스텁 클래스도 주석으로 교체 시점 명시. 매직값 없음.

- `Assets/Scripts/Editor/UILayerAssign.cs` — **enum 배선 정확성 합격 (핵심 판정)**
  - L18 `prop.enumValueIndex = Array.IndexOf((UILayer[])Enum.GetValues(typeof(UILayer)), layer)` — `Enum.GetValues`는 underlying 값 오름차순으로 반환하므로 `[Background(0), Hud(100), Modal(300)]`, `IndexOf`는 **멤버 인덱스** Background→0 / Hud→1 / Modal→2 를 산출. 선언 순서와 값 순서가 일치(0<100<300)하므로 Unity `enumValueIndex`(선언 순서 기준)와 정확히 매칭됨. 비연속 enum에서 `intValue`(=0/100/300) 오사용을 피한 것이 정확. **레이어 오배정 위험 없음.**
  - GetOrAdd(L12-13) idempotent, `#if UNITY_EDITOR` 가드 정상, prop null 방어(L16), `SetDirty` 호출.

- `Assets/Scripts/Editor/MenuBackgroundGenerator.cs` — 이상 없음
  - L42 `AssignLayer(..., UILayer.Background)` enum 심볼 사용(매직값 없음). L40 `SetAsFirstSibling()`는 내부 배경 정렬(최하단)로 유지 — 애드혹 모달 최상단화가 아니므로 정당하게 보존됨.

- `Assets/Scripts/Editor/JoystickUIGenerator.cs` — 이상 없음
  - L45 `AssignLayer(..., UILayer.Hud)`. CanvasGroup(blocksRaycasts=false) 세팅은 조이스틱 고유 동작. nested canvas 위 CanvasGroup 정상.

- `Assets/Scripts/Editor/HudGenerator.cs` — 이상 없음
  - L53 `AssignLayer(..., UILayer.Hud)`. SafeAreaFitter + UILayerCanvas 공존 정상.

- `Assets/Scripts/Editor/TitleLobbyGenerator.cs` — 이상 없음
  - L61 `AssignLayer(..., UILayer.Hud)`. L210 `SetSiblingIndex(menuBg+1)`는 배경 위 로비 배치용 내부 정렬로 유지(주석 L208: 모달 상하순서는 UILayer가 결정 — 애드혹 최상단화 제거됨). 정당한 잔존 정렬.

- `Assets/Scripts/Editor/ShopUIGenerator.cs` — 이상 없음
  - L45 `AssignLayer(..., UILayer.Modal)`로 모달 최상단화를 레이어에 위임(애드혹 제거 정확). L210 `dim.SetAsFirstSibling()`(딤 최하단), L296 `label.SetAsLastSibling()`(장식 위 텍스트)는 **패널 내부 자식 정렬**이므로 유지가 정답 — 잘못 제거된 내부 정렬 없음.
  - L48-51 기존 `_goldText/_closeButton/_cellContainer` 참조 읽어 재부모화만, L203-205 `WireIfNull`로 null일 때만 배선(로직 무수정 보존). 미사용 `shopPanel` 지역변수 없음(grep 확인).

- `Assets/Scripts/Editor/UpgradePanelGenerator.cs` — 이상 없음
  - L42 `AssignLayer(..., UILayer.Modal)`. L104-117 `_buttons/_nameTexts/_descTexts` 배열 재부모화만, `so.ApplyModifiedProperties()`로 배열 미변경(보존). UI 로직 무수정.

- **삭제 확인 합격** — `Assets/Scripts/UI/TopmostOnEnable.cs` 및 `.cs.meta` 삭제됨. `Assets/` 전체 grep 결과 참조 0건(고아 없음). (세션 시작 시 git 스냅샷엔 untracked로 남아 있었으나 이후 삭제 완료됨.)

### 수정 필요

- `Assets/Scripts/UI/UILayerCanvas.cs`
  - **[Unity 패턴 / 요청 중점 1]** `OnValidate()`(L12)가 `Apply()`를 호출하고 `Apply()` 내부에서 `AddComponent<Canvas>()`(L17)·`AddComponent<GraphicRaycaster>()`(L22)를 수행함. Unity는 `OnValidate` 중 `AddComponent` 호출 시 "AddComponent ... cannot be called during OnValidate" 경고를 뱉음(컴포넌트 추가 지연/무시 가능).
    → 수정: OnValidate에서는 이미 존재하는 컴포넌트의 값 세팅(overrideSorting/sortingOrder)만 하고 컴포넌트 추가는 하지 않도록 분리하거나, 최소한 `#if UNITY_EDITOR` 가드로 감쌀 것(체크리스트 요구사항). 참고로 `OnValidate`는 플레이어 빌드에서 자동 제거되어 런타임 비용은 없으나, 에디터 콘솔 경고와 가드 규약 위반은 정리 필요.

  - 그 외 항목은 정상: `[RequireComponent(RectTransform)]` 적절(L6), `overrideSorting=true`/`sortingOrder=(int)_layer` 세팅 올바름(L18-19, 값=100/300도 sortingOrder로 유효), nested canvas 입력용 GraphicRaycaster GetOrAdd 존재(L21-22), CanvasScaler 중복 추가 없음, Canvas GetOrAdd null 방어(L16-17), `[SerializeField] UILayer _layer` 네이밍(`_camelCase`) 정상, WHY 주석(L4-5) 규약 부합.

### 종합 의견
enum→enumValueIndex 매핑과 애드혹 정렬 제거/내부 정렬 보존이 모두 정확하고 과설계(스택 매니저) 없이 깔끔하나, `UILayerCanvas.OnValidate`의 AddComponent 경고 1건만 정리하면 합격 수준이다.
