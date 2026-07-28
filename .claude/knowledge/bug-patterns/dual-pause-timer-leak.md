---
title: 정지 방식이 두 갈래(GameState / timeScale)일 때 한쪽 가드만 탄 타이머가 샌다
tags: [pause, timer, cooldown, gamestate, timescale, rewind, update-guard]
symptom: 업그레이드 패널로 게임이 멈춘 동안에도 되감기 쿨타임이 계속 줄어듦
severity: medium
commit: fbc78a3
files: [Assets/Scripts/Rewind/RewindManager.cs]
---

## 증상
능동 되감기 사용 후 쿨타임이 도는 중에 레벨업 업그레이드 패널이 뜨면,
게임은 멈춰 있는데 쿨다운 링만 계속 채워진다.
반면 메뉴 Pause(일시정지 버튼)에서는 정상적으로 멈춘다 — 재현 경로가 갈린다.

## 근본 원인
이 프로젝트에는 "정지"가 **두 가지 경로**로 존재한다.

| 경로 | 하는 일 |
|------|--------|
| `PauseController.Pause()` | `GameState.Paused` **+ `Time.timeScale = 0f`** |
| `UpgradeManager.HandleLevelUp()` | `GameState.Paused` **만** (timeScale은 1 유지) |

`RewindManager.Update()`는 쿨타임 감소를 `if (State != Playing) return;` **가드보다 위**에
두고 있었다. 메뉴 Pause에서는 `Time.deltaTime`이 0이라 우연히 같이 멈춰서 버그가 가려졌고,
업그레이드 패널에서만 드러났다. 즉 **가드 누락 버그를 timeScale이 절반만 덮고 있던** 상황.

프로젝트 내 다른 Update 타이머(WaveManager, 무기 4종, EnemyMover, EnemyJobScheduler)는
모두 가드가 맨 위에 있어 동일 문제가 없었다. RewindManager만 예외였다.

## 해결
쿨타임 감소를 상태 가드 **아래**로 이동. 3줄 순서 교체가 전부.

```csharp
void Update()
{
    if (Managers.Game.State != GameState.Playing) return;

    // 업그레이드 패널은 timeScale=0을 쓰지 않으므로, 쿨타임도 상태 가드 아래에 있어야 멈춘다
    if (_cooldownTimer > 0f)
        _cooldownTimer -= Time.deltaTime;
    ...
}
```

UI(`RewindButtonUI.Update`)는 매 프레임 `CooldownRemaining`을 읽어 링을 그리므로,
타이머가 멈추면 링도 자동으로 멈춘다 — UI 측 수정 불필요.

## 재인식 패턴 ⚠️
**"정지 경로가 2개 이상(상태머신 / timeScale / 스크립트 비활성화)"** 인 프로젝트에서
Update 안의 시간 누적(`+= Time.deltaTime`)이 **상태 가드보다 위**에 있으면 의심하라.
→ 한쪽 경로에서만 재현되는 "멈췄는데 뭔가 흐른다" 버그가 된다.

- 시간 누적은 **항상 상태 가드 아래**에 둔다. `timeScale=0`에 기대지 말 것.
- 반대로 정지 중에도 **돌아야 하는** 것(입력 폴링, 디버그 토글, UI 연출)만
  의도적으로 가드 위에 두고, 왜 위인지 주석을 남긴다
  (예: `DebugController`의 업그레이드 스킵 토글).
- 새 "정지" 상태를 추가할 때는 두 경로를 모두 세팅하든지, 한쪽으로 통일하라.

## 관련
- [[pooled-object-survives-scene-change]] — 상태 전환 시 정리 누락 계열
