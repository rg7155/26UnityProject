---
title: 중첩 Canvas(overrideSorting) 때문에 FindFirstObjectByType<Canvas>가 오탐 → 에디터 생성기가 루트를 중복 생성
tags: [ui, ugui, editor-script, generator, canvas, overridesorting, uilayercanvas, idempotent, duplication, destroyimmediate]
symptom: UILayer 시스템 도입 후 에디터 생성기를 재실행할 때마다 루트 오브젝트(ResultRoot 등)가 계속 중복 생성됨. dedup으로 파괴하려다 그 안의 SerializeField 오브젝트(버튼/텍스트)가 같이 사라짐
severity: high
commit: 90a4fc0
files: [Assets/Scripts/Editor/ResultSceneGenerator.cs, Assets/Scripts/Editor/HudGenerator.cs, Assets/Scripts/Editor/TitleLobbyGenerator.cs, Assets/Scripts/Editor/JoystickUIGenerator.cs, Assets/Scripts/Editor/MenuBackgroundGenerator.cs]
---

## 증상
`[MenuItem]` UI 생성기가 `FindOrCreateChild(canvas, "루트이름")`으로 idempotent하게
동작하는데, **UI 레이어 시스템(`UILayerCanvas`) 도입 후** 재실행마다 루트가 하나씩
더 생겨 화면에 빈 카드/중복 UI가 쌓였다. 이를 정리하려고 만든 dedup이 **중복 루트를
파괴하면서 그 안의 버튼/텍스트까지 destroy** → `ResultScene._retryButton` 등 SerializeField
참조가 전부 null이 되어 화면이 텅 빔.

## 근본 원인
두 개가 겹쳤다.

1. **중첩 Canvas 오탐** — `UILayerCanvas`가 패널 루트에 `overrideSorting` Canvas를
   붙인다. 그러면 씬에 Canvas가 여러 개가 되고, `Object.FindFirstObjectByType<Canvas>()`가
   **루트 Canvas가 아니라 패널의 중첩 Canvas를 반환**할 수 있다(반환 순서 비보장). 그 잘못된
   Canvas 밑에서 `FindOrCreateChild("루트")`는 못 찾아 **또 하나를 그 안에 중첩 생성** → 매 실행 누적.

2. **파괴 전 대피 조건 오류** — dedup이 중복 루트를 파괴하기 전 보존 대상을 canonical로
   옮길 때 `if (t.IsChildOf(dup) && !t.IsChildOf(canonical))` 같은 조건을 걸었는데, 중첩 구조
   (canonical ⊃ dup ⊃ 보존대상)에서는 보존 대상이 **이미 canonical의 자손**이라 `!IsChildOf(canonical)`가
   false → 대피 스킵 → `DestroyImmediate(dup)`가 보존 대상을 함께 파괴.

## 해결
1. **항상 루트 Canvas 기준** — Canvas 취득 직후 `canvas = canvas.rootCanvas;`.
   중첩 Canvas가 잡혀도 `.rootCanvas`가 최상위를 돌려주므로 오탐 제거. (모든 UI 생성기에 적용.)
2. **파괴 전 "무조건 먼저 대피"** — 조건부 구제 금지. 순서를 바꾼다:
   ```
   // 1) 보존 대상을 루트로 먼저 대피 (어디 있든)
   foreach (preserve) t.SetParent(rootParent, false);
   // 2) 같은 이름 전부(중첩 포함) 파괴
   foreach (match) DestroyImmediate(match);
   // 3) 깨끗한 새 루트 하나 생성
   ```
   중첩까지 스캔하려면 `rootParent.GetComponentsInChildren<RectTransform>(true)`로 이름 매칭.

## 재인식 패턴 ⚠️
- **"nested Canvas(overrideSorting)를 쓰는데 생성기가 `FindFirstObjectByType<Canvas>`로
  루트를 잡는다"** → 오탐 의심. 즉시 `canvas.rootCanvas`로 정규화.
- **에디터 생성기 idempotent 정리에서 오브젝트를 파괴할 때** → SerializeField가 물고 있는
  대상이 그 서브트리에 있으면 **파괴 전에 무조건 밖으로 먼저 빼라.** 조건부(IsChildOf 비교)
  구제는 중첩에서 새기 쉽다. destroy는 서브트리 전체를 지운다는 걸 항상 전제.
- 데이터 소실 사고 시: **작업이 미커밋이면 `git checkout HEAD -- 씬.unity`로 원본 복구** 가능
  (씬을 아직 커밋 안 했으면 원본에 참조가 살아있다).

## 재발 사례 2 — **형제** 루트 캔버스 오인 (`.rootCanvas`로는 못 막는다)

위 해결책 1(`canvas.rootCanvas`)은 **중첩**만 막는다. 그 뒤 `PauseScreenGenerator`가
`@PauseCanvas`라는 **별도 루트 캔버스**(Reference 1080×1920, 메인은 800×600)를 만들면서
같은 버그가 다시 났다.

### 증상
보스 HUD 작업 후 `Build HUD → Build Boss HUD → Build Pause Screen` 순서로 실행하자
**HUD가 통째로 두 벌** 생성됐다. 보스 HP 바·REWIND 버튼·Score/Timer/Gold·Lv 배지가
전부 2개씩, **서로 다른 배율로** 겹쳐 보였다(두 캔버스의 Reference 해상도가 달라서).

### 근본 원인
`FindFirstObjectByType<Canvas>`는 **반환 순서를 보장하지 않는다.** `@PauseCanvas`가 씬에
이미 존재하는 상태에서 `Build HUD`를 돌리면 그걸 메인 캔버스로 오인하고, 그 아래에
`HudRoot` 한 벌을 새로 만든다. `.rootCanvas`는 이미 루트인 `@PauseCanvas`를 그대로 돌려주므로
**정규화가 아무 방어도 못 한다.**

`BossHudGenerator`만 `ResolveHudCanvas()`(HpBar 역추적)로 방어하고 있었으나, `HudGenerator`가
`@PauseCanvas` 아래에 HpBar를 만들어버리면 그 역추적도 함께 뚫린다.

**방아쇠는 실행 순서였다.** 이전엔 Pause 생성기를 마지막에 돌려서 우연히 안 터졌을 뿐이다.

### 해결
공용 리졸버 `UIGenScene.ResolveMainCanvas(tag)` 하나로 통일하고 **모든** 생성기가 그것만 쓴다.
- 씬의 Canvas를 전부 모아 `rootCanvas`로 정규화·중복 제거 → **캔버스 "집합"만 보므로 순서 비의존**
- **`@` 접두사 루트는 후보에서 제외**(생성기 전용 오버레이 규약 = Hierarchy 네이밍 규칙과 동일)
- 후보 1개면 확정, 여러 개면 이름이 `Canvas`인 유일한 하나
- **못 정하면 조용히 아무거나 고르지 않고 `LogError` 후 중단** ← 이 버그의 본질이 "조용히 틀린 걸 골랐다"였다

이미 생긴 중복은 `UIGenScene.PurgeStrays`가 자가 치유한다. 파괴 전 안전장치로
**그 서브트리에만 존재하는 로직 컴포넌트(씬의 유일본)가 있으면 지우지 않고 경고**한다
(사례 1의 "참조 소실" 재발 방지).

`Tools/UI/Check HUD Overlap`에 **"메인 캔버스 밖에 HUD 컴포넌트가 있으면 경고"** 를 추가했다 —
겹침 검사만으로는 다른 캔버스에 생긴 유령 UI를 못 본다.

### 재인식 패턴 ⚠️
- **생성기가 전용 루트 캔버스(`@PauseCanvas` 등)를 만드는 프로젝트에서 `FindFirstObjectByType<Canvas>`는
  실행 순서에 따라 결과가 달라진다.** `.rootCanvas` 정규화는 중첩만 막고 형제는 못 막는다.
- **"같은 UI가 두 가지 크기로 겹쳐 보인다"** → 서로 다른 Reference 해상도를 가진 **두 캔버스**에
  같은 것이 생성된 것. 한쪽을 지우기 전에 어느 쪽이 올바른 캔버스인지부터 확정하라.
- **생성기를 실행하는 순서를 바꿨더니 결과가 달라졌다** → 씬 조회가 순서 비의존인지 점검.
  "지금까지 잘 됐다"는 우연일 수 있다.
- 일반 규칙: **씬 조회로 대상을 하나 고르는 코드는 후보가 여럿일 때 침묵하면 안 된다.**
  확정 규칙을 명시하고, 확정 실패 시 중단하라.

## 관련
- UI 레이어 시스템: `UILayerCanvas`/`UILayerAssign`, `UIManager.UILayer`.
- 절차 스프라이트 직렬화 이슈는 별도: [[procedural-sprite-not-serialized]].
- 상단 HUD 밴드 좌표 충돌(같은 슬롯을 두 생성기가 계산)은 `UIHudLayout` 단일 소유자로 해결 — 같은 계열의
  "좌표·대상 소유권이 분산되면 조용히 충돌한다" 패턴.
