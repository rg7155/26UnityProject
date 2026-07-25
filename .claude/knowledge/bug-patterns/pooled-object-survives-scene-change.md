---
title: DontDestroyOnLoad 풀의 활성 오브젝트가 씬 전환 후 살아남아 유령 콜백을 쏨
tags: [object-pool, dontdestroyonload, scene-transition, nullreference, ghost-callback, projectile, physics2d, sound]
symptom: 게임 중 타이틀/결과 씬으로 전환하면 NullReferenceException. 스택이 Projectile.OnTriggerEnter2D → EnemyBase.OnDamaged → SoundManager 등 게임플레이 콜백에서 발생. 이미 떠난 씬의 로직이 새 씬에서 실행됨
severity: high
commit: (pending)
files: [Assets/Scripts/Manager/ObjectManager.cs, Assets/Scripts/Manager/SceneManager.cs]
---

## 증상
게임 플레이 중 Pause→타이틀(또는 Rewind→Result)로 씬을 바꾸면
`NullReferenceException`이 뜬다. 스택은 게임플레이 콜백:
`Projectile.OnTriggerEnter2D → EnemyBase.OnDamaged → SoundManager.Play(...)`.
떠난 GameScene의 오브젝트가 **다음 씬에서도 Update/물리 콜백을 계속 실행**하다가
정리된 매니저/오디오소스 참조를 건드려 터진다.

## 근본 원인
오브젝트 풀 루트 `@Pool`이 `DontDestroyOnLoad`다(`ObjectManager.Init`).
- 풀에서 `Get()`한 **활성 오브젝트(발사체·적)는 @Pool의 자식**으로 남는다.
- `DontDestroyOnLoad` 오브젝트의 자식도 함께 보존된다 → 씬 전환(non-additive LoadScene)이
  일반 오브젝트는 파괴해도 **@Pool 자식은 살려둔다.**
- 그 결과 잔존 발사체가 다음 씬에서 `Update`로 계속 이동하고, 살아남은 적과
  `OnTriggerEnter2D` → `OnDamaged` → 사운드 재생을 시도.
- `ObjectManager.Clear()`는 `_pools.Clear()`(딕셔너리만)라 **실제 GameObject를 안 지웠고**,
  게다가 `Managers.Clear()`(=Clear를 호출하는 유일한 지점)는 **아무 데서도 호출되지 않았다.**
  → 풀이 씬 전환 시 전혀 정리되지 않음.

## 해결
1. `ObjectManager.Clear()`가 **@Pool 자식(활성+비활성)을 실제 파괴**하도록:
   ```csharp
   public void Clear() {
       if (_root != null)
           for (int i = _root.childCount - 1; i >= 0; i--)
               Object.Destroy(_root.GetChild(i).gameObject);
       _pools.Clear();
   }
   ```
   (`Object.Destroy`는 `DontDestroyOnLoad` 보존을 무시하고 파괴한다.)
2. `SceneManagerEx.ChangeScene`에서 **LoadScene 직전에 `Managers.Object.Clear()` 호출**.
   모든 씬 전환(Title/Result 등)에 일괄 적용 → 유령 콜백 원천 차단.

## 재인식 패턴 ⚠️
- **"씬 전환 후 이전 씬 게임플레이 콜백에서 NRE"** → DontDestroyOnLoad 풀/매니저가
  들고 있는 활성 오브젝트가 살아남은 것 의심. 전환 시점에 정리하는지 확인.
- **오브젝트 풀 루트가 DontDestroyOnLoad인데 씬이 여러 개다** → 활성 풀 오브젝트가
  씬 경계를 넘는지, `Clear()`가 딕셔너리만 비우고 실물은 안 지우는지 점검.
- `Clear()`류 정리 메서드를 만들었으면 **실제 호출되는 곳이 있는지** 반드시 확인
  (여기선 `Managers.Clear()`가 데드코드였다).

## 후속 파생 (같은 전환 경로에서 연쇄로 드러난 것들)
풀 오브젝트를 전환 시 파괴하도록 고치자 **static 상태**가 씬을 넘어 유지되는 두 버그가 추가로 드러났다:

1. **`EnemyBase._registry`(static) 잔존** — 적은 `OnDead()`에서만 레지스트리에서 제거된다.
   전환 시 살아있는 적을 `Object.Destroy`하면 OnDead가 안 불려 레지스트리에 **파괴된 적이 남고**,
   static이라 다음 씬까지 유지 → `RewindManager.CaptureEnemies`가 `e.transform` 접근 시
   `MissingReferenceException`. → `EnemyBase.ClearRegistry()`를 `ChangeScene`에서 호출.
   교훈: **static 컬렉션은 씬을 넘어 산다. 전환 시 명시적으로 비워라.**

2. **`SoundManager._audioSources` 미바인딩 → `NullReferenceException`** — `Init()`이 기존
   `@SoundRoot`(DontDestroyOnLoad)를 찾는 분기에서 `_audioSources` 배열을 **재바인딩하지 않아**
   null 슬롯 접근으로 터짐. → 기존 루트 재사용 분기에서도 자식 AudioSource를 다시 바인딩.
   추가로 두 `Play` 오버로드에 `if (audioSource == null) return;` 방어(전환 레이스 대비).

## 재인식 패턴 (확장) ⚠️
- **씬 전환 후 `MissingReferenceException`(파괴된 오브젝트 접근)** → static 레지스트리/리스트가
  파괴된 참조를 물고 있는지 확인. 제거가 "정상 사망 경로"에만 있으면 강제 파괴 시 샌다.
- **DontDestroyOnLoad 매니저의 캐시 배열(_audioSources 등)이 재사용 분기에서 재바인딩 안 됨** →
  null 슬롯. 생성 분기뿐 아니라 재사용 분기도 채워라.

## 관련
- Pause 화면(`PauseController.GoTitle`)이 이 전환 경로를 처음 노출시켰다.
  `SceneManagerEx.ChangeScene`은 `Time.timeScale`도 복원 안 하므로 GoTitle에서 별도로 `1f` 복원.
