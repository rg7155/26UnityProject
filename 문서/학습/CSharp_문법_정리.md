# C# 문법 정리 — C++ 개발자 관점

C++과 다른 개념 위주. 이 프로젝트 코드에서 실제로 쓰인 것만 정리.

---

## 1. struct vs class — 가장 중요한 차이

```csharp
struct PlayerSnapshot { ... }   // 값 타입 (Value Type)
class  EnemyBase       { ... }  // 참조 타입 (Reference Type)
```

| | struct | class |
|---|---|---|
| 메모리 위치 | 스택 (또는 인라인) | 힙 |
| 대입 | **복사** | **참조 복사** (포인터처럼) |
| GC 대상 | 아니오 | 예 |
| C++ 유사 개념 | `struct` / POD | `new`로 힙 할당한 객체 |

**Unity 성능 관점:**
```csharp
// struct → 스택에 복사됨, GC 없음
FrameSnapshot frame = _buffer[i];   // 복사 발생 (의도된 것)

// class → 힙에 있는 것의 참조만 복사됨
EnemyBase enemy = registry[id];    // 참조 복사, 같은 객체 가리킴
```

> FrameSnapshot에 `EnemySnapshot[]`(배열, 참조 타입)가 들어있어서
> 배열 자체는 heap에 있음. 매 Record()마다 배열 새로 할당 → GC 부하 주의.

---

## 2. Property (프로퍼티)

C++에 없는 개념. getter/setter를 언어 수준에서 지원.

```csharp
// 선언
public int Hp { get { return _hp; } }           // 읽기 전용
public int EntityId { get; private set; } = -1; // 외부 읽기 O, 외부 쓰기 X

// C++로 치면
// int GetHp() const { return _hp; }
// int EntityId; (외부 write 막으려면 private로 이동 + getter 함수)
```

**자동 프로퍼티:**
```csharp
public float Speed { get; private set; }
// 내부에 backing field(_speed)를 컴파일러가 자동 생성
```

---

## 3. delegate / event / Action

C++의 함수 포인터 + std::function 을 언어 수준에서 지원.

```csharp
// delegate 타입 정의 (보통 Action/Func으로 대체)
public event Action OnDead;          // 반환값 없는 콜백
public event Action<int, int> OnHpChanged;  // int 2개 받는 콜백

// 구독 (+=)
_player.OnDead += OnPlayerDead;

// 발행
OnDead?.Invoke();   // 구독자가 없으면 null → ?.로 null 체크

// 해제 (-=)
_player.OnDead -= OnPlayerDead;
```

**`event` 키워드의 의미:**
- `event` 없으면 외부에서 `OnDead = null`로 전체 초기화 가능 (위험)
- `event` 있으면 외부에서 `+=` / `-=` 만 가능

**C++ 유사 코드:**
```cpp
std::vector<std::function<void()>> onDead;
for (auto& f : onDead) f();
```

---

## 4. ?. 연산자 (Null 조건부 연산자)

```csharp
SpatialHashGrid.Instance?.Add(this);
// 풀이: if (SpatialHashGrid.Instance != null) SpatialHashGrid.Instance.Add(this);

_target?.GetComponent<PlayerController>()?.AddExp(_expReward);
// 체이닝 가능. 어느 하나라도 null이면 전체 short-circuit
```

C++에는 없음. Unity에서 null 체크 코드 줄이는 데 자주 씀.

---

## 5. 제네릭 (Generics)

C++의 template과 유사하지만 **런타임에 동작** (template은 컴파일 타임 코드 생성).

```csharp
Dictionary<int, EnemyBase>   // C++: std::unordered_map<int, EnemyBase*>
List<EnemyBase>              // C++: std::vector<EnemyBase*>
Queue<GameObject>            // C++: std::queue<GameObject*>
HashSet<int>                 // C++: std::unordered_set<int>
```

**성능 차이 — Boxing:**
```csharp
// Value type을 object(참조 타입)로 바꾸면 힙 할당 발생 → GC 부하
List<object> list = new List<object>();
list.Add(42);   // int → object로 Boxing 발생! 힙 할당

List<int> list = new List<int>();
list.Add(42);   // Boxing 없음. OK
```

`HashSet<int>`, `List<EnemyBase>` 등 타입 명시하면 Boxing 없음.

---

## 6. IEnumerator / Coroutine

Unity Coroutine의 기반. C++ iterator와 비슷하지만 `yield`로 실행을 일시 정지.

```csharp
IEnumerator DoRewind()
{
    Managers.Game.State = GameState.Rewinding;

    for (int i = _count - 1; i >= 0; i--)
    {
        // ... 프레임 적용 ...

        yield return new WaitForSecondsRealtime(0.05f);
        // ↑ 여기서 함수 실행을 멈추고 Unity에 제어권 반환
        //   0.05초 후 다음 줄부터 재개
    }
}
```

**내부 동작:**
컴파일러가 상태 머신(state machine) 클래스로 변환. `yield return` 지점마다 상태 저장.

**성능:**
- `StartCoroutine()` 호출마다 힙에 상태 머신 객체 할당 → GC 대상
- 매 프레임 호출이 아닌 간헐적 사용이면 허용 가능한 수준

---

## 7. interface

C++의 순수 가상 클래스와 유사. 다중 상속 가능.

```csharp
// IReadOnlyDictionary — 읽기만 허용하는 Dictionary 뷰
public static IReadOnlyDictionary<int, EnemyBase> Registry => _registry;

// 실제 타입은 Dictionary지만, 외부에서는 읽기 전용 인터페이스만 노출
// ContainsKey(), [], .Values 만 사용 가능. Add/Remove 불가
```

**C++ 유사:**
```cpp
// const std::unordered_map<int, EnemyBase*>& GetRegistry() const;
```

---

## 8. virtual / override

C++과 거의 동일. 차이점:

```csharp
// C#: virtual 없으면 기본이 non-virtual (C++과 동일)
public virtual void RestoreSnapshot(EnemySnapshot s) { ... }   // EnemyBase
public override void RestoreSnapshot(EnemySnapshot s) { ... }  // EnemyMover

// C++과 다른 점: override 키워드가 강제됨 (오타 시 컴파일 에러)
// C++에서 override는 선택적이었음 (C++11부터 지원)
```

---

## 9. => 표현식 바디 (Expression-bodied member)

람다 스타일로 메서드/프로퍼티를 한 줄로 표현.

```csharp
public static void UnregisterForRewind(int entityId) => _registry.Remove(entityId);
// 동일:
public static void UnregisterForRewind(int entityId) { _registry.Remove(entityId); }

public float CooldownRemaining => _cooldownTimer;
// 동일:
public float CooldownRemaining { get { return _cooldownTimer; } }
```

---

## 10. var — 타입 추론

```csharp
var registry = EnemyBase.Registry;   // IReadOnlyDictionary<int, EnemyBase>로 추론
var toRemove = new List<EnemyBase>(); // List<EnemyBase>로 추론
```

C++의 `auto`와 동일. 타입이 우변에서 명확할 때만 쓰는 게 관례.

---

## 11. GC (Garbage Collector) — Unity 성능의 핵심

C++은 직접 `delete`. C#은 GC가 자동으로 힙 메모리 해제.

**문제:** GC가 실행될 때 프레임 스파이크 발생 가능.

**힙 할당이 발생하는 코드 패턴:**
```csharp
// 1. new로 참조 타입 생성
new List<EnemyBase>()        // 힙 할당
new HashSet<int>()           // 힙 할당
new WaitForSecondsRealtime() // 힙 할당 → 캐싱 권장

// 2. 배열 생성
EnemySnapshot[] snapshots = new EnemySnapshot[n];  // 힙 할당

// 3. 클로저 캡처
Action a = () => Debug.Log(someLocalVar);  // someLocalVar를 힙에 캡처

// 4. 문자열 연산
string s = "pos: " + transform.position;  // 힙 할당
```

**이 프로젝트에서 주의할 부분:**
```csharp
// Record()가 0.05초마다 호출됨
EnemySnapshot[] snapshots = new EnemySnapshot[registry.Count];  // 매 호출마다 힙 할당
// → 1000마리 기준 20fps = 초당 20번 배열 생성 → GC 주의
// 개선: 미리 배열 할당해두고 재사용 (현재는 포폴 규모라 허용)
```

---

## 12. foreach 와 boxing 주의

```csharp
// Dictionary.Values 순회 — IEnumerable<V> 반환 → 내부적으로 enumerator 객체 생성
foreach (EnemyBase e in EnemyBase.Registry.Values) { ... }
// 힙에 enumerator 할당 발생 (미미한 수준)

// List<T> foreach — JIT 최적화로 boxing 없음
foreach (EnemyBase e in toRemove) { ... }  // OK
```

---

## 요약 — C++ 개발자가 주의할 것

| C++ | C# | 주의점 |
|-----|-----|--------|
| 포인터/참조 | class (참조 타입) | 대입해도 복사 안 됨, 같은 객체 |
| 값 타입 struct | struct | 대입 시 복사됨 |
| 소멸자 | GC | 언제 해제될지 모름, UnityEngine.Object는 Destroy() 필요 |
| std::function | delegate / Action | event 키워드로 캡슐화 |
| template | Generic | 런타임 동작, Boxing 주의 |
| 순수 가상 클래스 | interface | 다중 구현 가능 |
| delete | 없음 (GC) | Unity Object는 Managers.Object.Return() / Destroy() |
