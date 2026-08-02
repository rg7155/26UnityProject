---
title: 에디터 생성기 결과를 씬에 저장하지 않아 유실 — 며칠 뒤 "고쳤던 UI가 다시 깨짐"
tags: [ui, ugui, editor-script, generator, scene-save, serialization, safearea, canvasscaler, clipping, regression]
symptom: "[MenuItem] 생성기를 돌려 UI가 정상 표시되는 걸 확인했는데, Unity를 껐다 켜거나 며칠 뒤 열면 같은 레이아웃 버그가 다시 나타남. 커밋된 씬 파일에는 고치기 전 값이 들어있음"
severity: high
commit: (pending)
files: [Assets/Scripts/UI/UI_QuestPanel.cs, Assets/Scripts/UI/SafeAreaFitter.cs, Assets/Scripts/Editor/QuestUIGenerator.cs]
---

## 증상
`Tools/UI/Build Quest Panel` 실행 → 화면 정상 확인 → 커밋. 며칠 뒤 다시 열자
**퀘스트 셀이 좌우로 대칭 클리핑**(이름 첫 글자와 우측 버튼이 잘림). 프리팹에 저장된 수정
(진행바 fillAmount)은 멀쩡한데 **씬에 저장돼야 하는 수정만 되돌아가 있었다.**

## 근본 원인
**에디터 생성기의 변경은 "씬 메모리"에만 적용된다. `Ctrl+S`로 씬을 저장하지 않으면 디스크에 안 남는다.**
`EditorSceneManager.MarkSceneDirty`는 더티 표시만 할 뿐 저장하지 않는다.

- 생성기가 `content.sizeDelta.x = 0`(리스트 컨테이너 폭 = 뷰포트 폭)으로 고쳤고 화면상 정상이었다.
- 그러나 씬을 저장하지 않아 **디스크의 `Title.unity`에는 Unity 기본값 `{x: 100, y: 100}`이 그대로** 남았다.
- 즉 커밋된 씬은 처음부터 깨진 상태였고, 다음에 Unity가 그 파일을 로드하자 컨테이너가
  뷰포트보다 100px 넓어져 셀이 좌우 50px씩 잘렸다.

**진단 팁 — 씬 파일에서 직접 확인한다.** 추측하지 말고 ground truth를 읽어라:
```bash
# 오브젝트 이름 → GameObject id → 그 id를 참조하는 RectTransform 블록의 값
grep -n "m_Name: Content" Assets/Scenes/Title.unity
# 부모 체인을 타고 올라가 어느 패널 소속인지 확정
```

### 같이 드러난 2차 원인: SafeAreaFitter가 화면 초과 앵커를 씬에 구움
`SafeAreaFitter`는 `[ExecuteAlways]`라 **에디터에서도 실행되어 앵커를 씬에 저장**한다.
Device Simulator 기기 해상도(예: 1170×2532)의 `Screen.safeArea`와 게임뷰 해상도(1080×1920)의
`Screen.width/height`가 **서로 다른 출처**일 때 비율이 1을 넘는다:
`1170/1080 = 1.0833`, `2391/1920 = 1.2453` → `anchorMax: {x: 1.0833, y: 1.2453}`.
컨테이너가 화면보다 커져 자식이 잘린다. 같은 계열 버그를 다른 패널에서도 유발한다.

## 해결
1. **씬 직렬화 값에 의존하지 말고 런타임에 불변식을 강제한다.** (핵심)
   레이아웃 컨테이너 폭처럼 패널이 소유한 값은 `Start()`에서 직접 고정 — 생성기 실행/씬 저장
   여부와 무관해진다.
   ```csharp
   RectTransform rt = _cellContainer as RectTransform;
   rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
   rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
   rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);   // 폭 = 뷰포트 폭
   ```
2. **SafeAreaFitter는 `Mathf.Clamp01`** — 안전 영역은 화면을 넘을 수 없다. 뒤집힌 값이면 전체 화면으로 폴백.
3. 생성기 실행 후에는 **반드시 씬 저장(Ctrl+S)**. 프리팹(`SaveAsPrefabAsset`)은 즉시 디스크에 쓰이지만
   **씬 변경은 저장 전까지 휘발**이라는 비대칭을 기억할 것.

## 재인식 패턴 ⚠️
- **"고쳤고 눈으로 확인했는데 나중에 다시 깨짐"** → 프리팹/에셋 수정은 남고 **씬 수정만 유실**된 것 의심.
  커밋된 `.unity`를 직접 grep해 값이 실제로 반영됐는지 확인하라(에디터 화면을 믿지 마라).
- **UI가 좌우/상하로 대칭 클리핑** → 컨테이너가 뷰포트보다 크다. `sizeDelta`(스트레치 앵커에서 0이어야 함)와
  `anchorMin/Max`(0~1 범위 밖인지)를 먼저 본다.
- **`[ExecuteAlways]` 컴포넌트가 트랜스폼을 쓴다** → 에디터 상태(시뮬레이터·게임뷰 해상도)가 씬에 구워진다.
  입력값 범위를 반드시 클램프하고, 씬에 남아도 안전한 값만 쓰게 하라.
- 에디터 생성기로 만든 UI는 **런타임 불변식(자기 방어)** 을 함께 두면 씬 드리프트에 면역이 된다.

## 관련
- 절차 스프라이트 직렬화: [[procedural-sprite-not-serialized]]
- 중첩 Canvas 오탐: [[nested-canvas-generator-duplication]]
