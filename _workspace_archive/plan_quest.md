# QUEST(퀘스트) 시스템 구현 계획 — Rewind Survivors

Unity 6 / URP / New Input System(Polling) / 모바일 세로. 기존 Shop(ShopService/ShopItemData) · GameData(JsonUtility) 패턴을 복제한다. DI·Action기반·과추상화 금지, 단순함 우선.

## 0. 확정 기획 요약
메타 누적 도전과제(Career Challenges) — 영구 티어형, **수동 수령**.
- 매 판 종료 시 평생 스탯 누적: 총 처치 / 총 되감기 / 총 골드 / 최고 생존시간
- 목표 티어 도달 → "수령" 버튼 클릭 시 골드 획득(자동 아님) → 상점으로 순환
- 데이터 주도: `QuestData`(SO)를 `Resources/Quests/`에 넣으면 자동 등장
- UI: 타이틀 로비 퀘스트 패널(읽기+수령), 기존 상점 셀 UI 패턴 재사용
- 일일/리셋은 MVP 제외. `QuestData`만 나중에 `resetPeriod` 드롭인 가능하게 열어둠(구현은 안 함)

## 1. 코드 재검증 결과 (확정 시그니처)
- 골드: `GameManagerEx.AddGold(int)` / `SpendGold(int)` / `TotalGold{get}` / `RunGold{get;set;}`(비직렬화). `CommitResult()`가 런 종료 정산 단일 지점 — `TotalGold += RunGold; SaveGame();`.
- 저장: `GameData`(JsonUtility). `LoadGame()`에 null-guard 패턴 존재(`Purchases == null → new`).
- 적 처치 단일 지점: `EnemyBase.OnDead()`. `if (State == Playing)` 가드 안에서 `RunGold += _goldReward` — Rewind 리플레이 중복 방지. 여기 옆에 `RunKills++`.
- 되감기 단일 지점: `RewindManager.DoRewind()` 코루틴. 능동(`TryActiveRewind`)·자동부활(`OnPlayerDead`) 둘 다 진입. 현재 카운터 없음 → 신설. 코루틴 시작부에서 `RunRewinds++` 1회.
- 생존시간: `WaveManager.GameTime` → `OnPlayerDead`에서 `PlayTime` 기록 → `CommitResult`가 `BestTime` 갱신. **생존 퀘스트는 `BestTime`(최고기록)을 stat 값으로 사용**.
- SO 자동로드: `ShopService.Items` → `Resources.LoadAll<ShopItemData>("Shop")`. `QuestService`도 동형 `LoadAll<QuestData>("Quests")`.
- Run 카운터 리셋: `GameScene.Awake()`에서 `Score = 0; RunGold = 0` 옆.
- UI: `UI_ShopPanel`(자기등록, Start에서 Items 순회 셀 생성, RefreshAll) + `UI_ShopItemCell`(Bind/Refresh/버튼 interactable). 복제.
- 타이틀 토글: `TitleScene` `_shopButton`/`_shopPanel` — 퀘스트도 `_questButton`/`_questPanel` 추가.

## 2. 데이터 계약 (고정)
- `GameData` 추가: `int LifetimeKills; int LifetimeRewinds; int LifetimeGold; List<QuestProgress> QuestClaims = new();`
- `QuestProgress`(`[Serializable]`, ShopPurchase 옆): `string id; int claimedTier;`
- `GameManagerEx` Run 카운터(비직렬화, RunGold 옆): `public int RunKills {get;set;}` / `public int RunRewinds {get;set;}`
- `QuestStat` enum: `Kills, SurviveTime, Rewinds, Gold`
- `QuestData`(SO): `string id; string displayName; QuestStat stat; int[] targets; int[] rewards;` (targets/rewards 동일 길이). `[CreateAssetMenu(fileName="Quest", menuName="Game/Quest")]`
- `QuestService`(static, ShopService 형태):
  - `Quests` → `Resources.LoadAll<QuestData>("Quests")` 지연 캐시
  - `Current(QuestData)` → stat별 누적값(Kills→LifetimeKills, Rewinds→LifetimeRewinds, Gold→LifetimeGold, SurviveTime→`Mathf.RoundToInt(BestTime)`)
  - `ReachedTier(QuestData)` → `targets` 중 `Current >= target` 개수
  - `ClaimedTier(id)` → QuestClaims 조회(없으면 0)
  - `Claimable(QuestData)` → `ReachedTier > ClaimedTier`
  - `Claim(QuestData)` → 미수령 도달분 `rewards` 합산 `AddGold`, `claimedTier = ReachedTier`, `SaveGame()`. 반환 bool
  - 표시 헬퍼: `NextTarget`, `IsMaxed`, `RewardOf`

## 3. 퀘스트 에셋 4종
`Resources/Quests/`에 배치. 3티어.
| id | displayName | stat | targets | rewards |
|---|---|---|---|---|
| kills_total | 처치의 달인 | Kills | 100 / 1000 / 10000 | 200 / 1000 / 5000 |
| survive_best | 생존 전문가 | SurviveTime | 60 / 180 / 300 | 200 / 1000 / 5000 |
| rewind_total | 시간 여행자 | Rewinds | 10 / 100 / 500 | 200 / 1000 / 5000 |
| gold_total | 수집가 | Gold | 1000 / 10000 / 50000 | 300 / 1500 / 7000 |

## 4. .asset 생성 방법 (결정: 에디터 [MenuItem] 생성기)
`Assets/Scripts/Editor/QuestAssetGenerator.cs`에 `[MenuItem("Tools/Quests/Create Quest Assets")]`로 4종 코드 생성.
- `ScriptableObject.CreateInstance<QuestData>()` → 필드 세팅 → `AssetDatabase.CreateAsset(so, "Assets/Resources/Quests/{id}.asset")`
- 폴더 없으면 `CreateFolder`, 있으면 스킵(idempotent), 끝에 `SaveAssets`/`Refresh`
- 근거: 프로젝트 에디터 [MenuItem] 생성기 관행. 재현·재실행 가능.

## 5. 단계별 구현 (각 단계 검증 가능)

### 단계 A — 데이터 계층 [game-coder]
1. `GameManagerEx.cs`: `QuestProgress` 클래스 + `GameData`에 Lifetime 3필드 + `List<QuestProgress> QuestClaims`. `LoadGame()`에 `if (QuestClaims == null) QuestClaims = new()` null-guard.
2. `RunKills`/`RunRewinds` 프로퍼티(RunGold 옆).
3. `CommitResult()` 말미(SaveGame 전): `LifetimeKills += RunKills; LifetimeRewinds += RunRewinds; LifetimeGold += RunGold;`. Best 기존 유지.
- 검증: 컴파일. 구버전 세이브 로드 시 새 필드 0/빈 리스트.

### 단계 B — Run 카운터 리셋 & 후킹 [game-coder]
4. `GameScene.Awake()`: `RunKills = 0; RunRewinds = 0;`
5. `EnemyBase.OnDead()`: Playing 가드 안(RunGold 옆) `RunKills++;`
6. `RewindManager.DoRewind()`: 코루틴 진입부 `RunRewinds++;` (능동·자동부활 모두 통과)
- 검증: 처치/되감기 후 GameOver→CommitResult로 Lifetime 증가. 리플레이 중 처치는 미카운트.

### 단계 C — QuestData / QuestService [game-coder]
7. `Assets/Scripts/Quest/QuestData.cs`: enum + SO.
8. `Assets/Scripts/Quest/QuestService.cs`: 계약 API. Claim은 미수령 도달분만 지급+저장.
- 검증: 컴파일. `QuestService.Quests` 로드.

### 단계 D — 퀘스트 에셋 [game-coder]
9. `Assets/Scripts/Editor/QuestAssetGenerator.cs` [MenuItem] → `Resources/Quests/*.asset` 4종.
- 검증: 에셋 4개, `QuestService.Quests`가 4개 반환.

### 단계 E — 타이틀 로비 퀘스트 패널 [game-ui-artist]
10. `UI_QuestPanel.cs`(UI_ShopPanel 복제): Start에서 Quests 순회 셀 생성, RefreshAll, 닫기.
11. `UI_QuestCell.cs`(UI_ShopItemCell 복제): 이름/진행바(Current vs NextTarget)/보상/**수령버튼**(interactable=Claimable, 클릭→Claim→onChanged→RefreshAll). MAX/미도달/수령가능 3상태.
12. `TitleScene.cs`: `_questButton`/`_questPanel` 추가+토글+해제(_shopButton 복제). **coder 미터치.**
13. `Assets/Scripts/Editor/QuestUIGenerator.cs` [MenuItem](ShopUIGenerator 패턴): ui-kit+UITheme+UIProceduralSprite로 타이틀 씬에 패널/셀 생성·리스킨(세로, 외부0). 로직 무수정.
- 검증: 타이틀 퀘스트 버튼→패널. 도달 티어 수령버튼 활성→클릭 시 골드 증가+상점 반영, 재클릭 불가, 재시작 후 claimedTier 유지.

## 6. 파일별 목록
### game-coder
- 수정 `Assets/Scripts/Manager/GameManagerEx.cs`
- 수정 `Assets/Scripts/Scene/GameScene.cs`
- 수정 `Assets/Scripts/Enemy/EnemyBase.cs`
- 수정 `Assets/Scripts/Rewind/RewindManager.cs`
- 신규 `Assets/Scripts/Quest/QuestData.cs`
- 신규 `Assets/Scripts/Quest/QuestService.cs`
- 신규 `Assets/Scripts/Editor/QuestAssetGenerator.cs` → `Assets/Resources/Quests/*.asset` 4종
### game-ui-artist
- 신규 `Assets/Scripts/UI/UI_QuestPanel.cs`
- 신규 `Assets/Scripts/UI/UI_QuestCell.cs`
- 수정 `Assets/Scripts/Scene/TitleScene.cs`
- 신규 `Assets/Scripts/Editor/QuestUIGenerator.cs` → 타이틀 씬 퀘스트 패널/셀

## 7. 충돌 방지
공유 편집 파일 없음. 로직=coder 단독, TitleScene/UI_Quest*=artist 단독. 순서: A→B→C→D(coder) 완료 후 E(artist). QuestService API가 UI 선행 의존.

## 8. 제약 준수
네이밍/Polling/Resources.Load·FindObjectOfType/단순 static 서비스. 기존 스타일 유지.
주의: `Resources/Shop/StartHp.asset`에 stale `weaponToUnlock` 필드 있으나 무해 — QuestData 에셋에 복제 금지.
