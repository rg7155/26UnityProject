---
title: 값 타입 신규 필드는 null 가드로 못 막는다 — 스키마 버전이 필요
tags: [save, json, jsonutility, serialization, migration, float, schema-version, audio]
symptom: 새 float/int 필드를 GameData에 추가한 뒤, 구버전 세이브 로드 시 0으로 남아 기능이 조용히 죽음(무음·수치 0). 크래시가 없어 발견이 늦다
severity: high
files: [Assets/Scripts/Manager/GameManagerEx.cs]
---

## 증상
BGM/효과음 볼륨을 `GameData`에 `float BgmVolume = 0.5f` 로 추가했다.
구버전 `SaveData.json`(그 키가 없는 파일)을 로드하면 `BgmVolume`이 **0.0f**로 남아
게임 전체가 무음이 된다. **예외도 로그도 없다.** 사용자는 "사운드 기능이 안 만들어졌다"고
판단하게 되고, 개발자는 재생 코드부터 의심하며 시간을 버린다.

## 근본 원인
`JsonUtility.FromJson<T>` 은 JSON에 있는 필드만 채우고, 없는 필드는 **초기화식을 실행하지
않은 채** CLR 기본값으로 둔다. 이는 [[jsonutility-missing-field-null-collection]] 과 같은
원인이지만, 값 타입에서는 훨씬 고약하다.

**참조 타입은 `null`이라는 "값이 없음" 신호가 남지만, 값 타입은 안 남는다.**
`0.0f`는 "저장된 적 없음"과 "사용자가 일부러 0으로 내림"이 **구분 불가능한 상태**다.
따라서 `if (volume == 0) volume = 0.5f` 같은 가드는 쓸 수 없다 — 음소거를 저장한
사용자의 설정을 매번 되돌려버린다.

## 해결
필드 단위 가드가 원리적으로 불가능하므로 **레코드 단위 버전 표식**을 둔다.

```csharp
// GameData
public int SchemaVersion = GameManagerEx.CurrentSchemaVersion;
public float BgmVolume    = GameManagerEx.DefaultBgmVolume;
public float EffectVolume = GameManagerEx.DefaultEffectVolume;

// LoadGame() — FromJson 직후, 기존 null 가드 옆
if (_gameData.SchemaVersion < 1)
{
    _gameData.BgmVolume    = DefaultBgmVolume;
    _gameData.EffectVolume = DefaultEffectVolume;
    _gameData.SchemaVersion = 1;
}
```

구버전 세이브는 `SchemaVersion`도 없으므로 0이 되고, 이 값이 곧 "구버전" 판별자가 된다.
값 타입의 기본값 0을 **버그가 아니라 신호로 활용**하는 것이 핵심.

기본값은 **반드시 상수 하나를 필드 초기화식과 마이그레이션 양쪽이 참조**하게 한다.
두 군데 하드코딩하면 나중에 한쪽만 바뀌어 신규 유저와 기존 유저의 기본값이 갈린다.

## 재인식 패턴 ⚠️
**"기존 저장 포맷에 새 값 타입(float/int/bool/enum) 필드 추가"** 가 보이면,
구버전 로드 시 0/false로 남아 **조용히 기능이 죽는지** 먼저 따져라.
→ 0이 유효한 값이면(볼륨·배율·좌표) 필드 가드는 **원리적으로 불가능**하다. `SchemaVersion`을 도입하라.
→ 0이 절대 유효하지 않으면(MaxHp 등) 필드 가드로 충분하다. 판단 기준은 **"0이 정상 값일 수 있는가"**.

크래시가 나는 null 버그와 달리 이 계열은 **무증상으로 배포까지 통과**한다.
세이브 스키마를 늘렸다면 반드시 **구버전 세이브로 실제 로드 테스트**를 하라
(키를 지운 json을 만들어 넣어보는 것으로 충분하다).

## 관련
- [[jsonutility-missing-field-null-collection]] — 같은 원인의 참조 타입 버전.
  그 엔트리의 "값 타입(int/bool)은 0/false로 안전하다"는 서술은 **0이 유효 값이 아닐 때만** 참이다.
