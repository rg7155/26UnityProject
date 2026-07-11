---
title: JsonUtility가 JSON에 없는 필드의 초기화를 건너뛰어 컬렉션이 null
tags: [save, json, jsonutility, serialization, null, migration, list]
symptom: 새 필드(List/배열)를 GameData에 추가한 뒤, 구버전 세이브 로드 시 그 필드가 null → NRE
severity: high
commit: 90a5734
files: [Assets/Scripts/Manager/GameManagerEx.cs]
---

## 증상
상점 기능으로 `GameData`에 `List<ShopPurchase> Purchases = new()` 를 추가했다.
구버전 `SaveData.json`(그 키가 없는 파일)을 로드하면 `Purchases`가 `null`이 되고,
게임 시작 순간 `ShopService.BonusHp()` → `TierOf()` → `foreach (var p in D.Purchases)`
에서 **NullReferenceException**으로 크래시. 상점을 열기도 전에 터진다.

## 근본 원인
`JsonUtility.FromJson<T>` 은 **JSON에 존재하는 필드만** 채운다.
JSON에 없는 필드는 **필드 초기화식(`= new List<>()`)을 실행하지 않고** CLR 기본값으로 둔다.
→ 참조 타입은 `null`. 즉 클래스에 초기화를 써 놨어도 구버전 세이브엔 적용되지 않는다.
(이것은 신규 필드를 추가한 **세이브 마이그레이션** 상황에서 반드시 발생한다.)

## 해결
로드 직후 컬렉션 필드를 명시적으로 null 가드:
```csharp
if (_gameData.Purchases == null)
    _gameData.Purchases = new List<ShopPurchase>();
```
`LoadGame()` 의 `FromJson` 직후에 배치. 새 세이브엔 영향 없고 구버전만 복구된다.

## 재인식 패턴 ⚠️
**"기존 저장 포맷에 새 컬렉션/참조 필드 추가"** 가 보이면 구버전 로드 시 null 의심.
→ `JsonUtility.FromJson` 은 필드 초기화식을 실행하지 않는다. 로드 직후 **null 가드**로
   기본값을 보장하라. (필드 초기화나 생성자에 의존하지 말 것.)
값 타입(int/bool)은 0/false로 안전하지만 **List·배열·클래스 참조는 반드시 가드**.

## 관련
- 세이브 스키마 변경 시 항상 마이그레이션 관점으로 로드 경로를 점검할 것.
