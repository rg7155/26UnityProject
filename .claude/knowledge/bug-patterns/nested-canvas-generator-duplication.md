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

## 관련
- UI 레이어 시스템: `UILayerCanvas`/`UILayerAssign`, `UIManager.UILayer`.
- 절차 스프라이트 직렬화 이슈는 별도: [[procedural-sprite-not-serialized]].
