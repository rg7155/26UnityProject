using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] protected float _speed = 2f;
    [SerializeField] protected int _hp = 3;
    [SerializeField] protected int _expReward = 1;
    [SerializeField] protected int _goldReward = 1;
    [SerializeField] GameObject _deathEffectPrefab;
    [SerializeField] float      _deathEffectLifetime = 2f;
    protected Transform _target;

    int _originHp;
    GameObject _originPrefab;

    // EntityId — Pool 재사용 시 논리적 동일성 추적
    static int _nextEntityId = 0;
    public int EntityId { get; private set; } = -1;

    // 활성 적 레지스트리 — FindObjectsOfType 대체
    static Dictionary<int, EnemyBase> _registry = new Dictionary<int, EnemyBase>();
    public static IReadOnlyDictionary<int, EnemyBase> Registry => _registry;

    // 모든 적이 공유 — 1회만 Load
    static GameObject _damageTextPrefab;

    // 피격 punch — 맞은 순간 살짝 부풀었다 돌아온다.
    // 적은 GPU 인스턴싱이라 개체별 색을 못 바꾼다(per-instance 데이터가 행렬뿐).
    // 크기는 행렬에 실리므로, 셰이더 없이 피격 피드백을 줄 수 있는 유일한 축이다.
    //
    // transform.localScale 을 쓰지 않는 이유가 둘이다.
    //   1) Collider 가 함께 커진다 (문서/개발/에디터_설정.md 크기 조절 원칙)
    //   2) 좌우 반전이 localScale.x 부호를 쓰고 있어 서로 덮어쓴다
    // 그래서 EnemyInstanceRenderer 가 행렬을 만들 때만 곱한다 — 순수 연출이다.
    //
    // 타이머를 매 프레임 깎지 않고 종료 시각만 들고 있는다. EnemyBase 에 Update 를 넣으면
    // 적 수만큼 MonoBehaviour Update 가 늘고, EnemyMover.Update 가 base 를 가려 동작하지도 않는다.
    const float HitPunchDuration = 0.12f;
    const float HitPunchAmount   = 0.22f;
    float _hitPunchEndTime = -1f;

    // 렌더 행렬에 곱할 배율. 평소 1, 피격 직후 잠깐 커졌다 돌아온다.
    public float HitPunchScale
    {
        get
        {
            float remain = _hitPunchEndTime - Time.time;
            if (remain <= 0f) return 1f;
            return 1f + HitPunchAmount * (remain / HitPunchDuration);   // 맞은 순간이 가장 크다
        }
    }

    public int        Hp           { get { return _hp; } }
    public float      Speed        { get { return _speed; } }
    public GameObject OriginPrefab { get { return _originPrefab; } }

    // 화면에 보이는 몸통의 반지름(월드 단위). 분리 조향이 이 값을 쓴다.
    //
    // transform 만 봐서는 알 수 없다 — EnemyInstanceRenderer 가 _visualScale 을 렌더 행렬에만
    // 곱하기 때문이다(HitPunchScale 과 같은 이유). 실제 크기는 localScale.x * _visualScale 이고
    // 세 적의 배율 조합이 제각각이라 전역 상수 하나로는 맞출 수 없다.
    //
    // 0.425 = 프레임 절반(0.5) * art_import.py 의 FILL_RATIO(0.85).
    // 스프라이트는 512px 프레임을 512 PPU 로 넣으므로 프레임 1장 = 월드 1유닛이고,
    // 그 안에서 내용이 85% 를 채운다. art_import.py:42 가 바뀌면 이 값도 같이 바뀐다.
    const float BodyRadiusPerUnit = 0.425f;

    public float BodyRadius { get; private set; }

    // 접촉 쿨다운은 파생 클래스가 각자 들고 있다 — Rewind 캡처가 타입 분기 없이 읽기 위한 확장 지점
    public virtual float AttackCooldown { get { return 0f; } }

    public virtual void Init(Transform target, GameObject originPrefab, int hp = -1, float speed = -1f)
    {
        _target = target;
        _originPrefab = originPrefab;
        if (hp >= 0) _hp = hp;
        if (speed >= 0f) _speed = speed;
        _originHp = _hp;

        EntityId = _nextEntityId++;
        _registry[EntityId] = this;

        if (_damageTextPrefab == null)
            _damageTextPrefab = Resources.Load<GameObject>("UI/DamageText");

        _hitPunchEndTime = -1f;   // 풀 재사용 — 직전 개체의 punch 가 남지 않게

        SpatialHashGrid.Instance?.Add(this);
        EnemyInstanceRenderer.Register(this, originPrefab);

        RecalcBodyRadius();
    }

    // Init 과 ForceRestore 양쪽에서 부른다. 되감기로 풀 개체를 복원하는 경로는 Init 을
    // 타지 않으므로, 여기 한쪽만 두면 복원된 적의 반지름이 0 이 되어 서로 완전히 겹친다.
    // 예외도 로그도 없이 조용히 깨지는 종류라 두 자리를 같이 봐야 한다.
    void RecalcBodyRadius()
    {
        float visual = EnemyInstanceRenderer.VisualScaleOf(_originPrefab);
        // localScale.x 는 좌우 반전(EnemyMover)에서 음수가 되므로 절댓값을 쓴다
        BodyRadius = BodyRadiusPerUnit * Mathf.Abs(transform.localScale.x) * visual;
    }

    public virtual void OnDamaged(int damage)
    {
        if (_damageTextPrefab != null)
        {
            GameObject fx = Instantiate(_damageTextPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            fx.GetComponent<DamageText>().Init(damage, Color.white);
            Managers.Resource.Destroy(fx, 0.7f);
        }

        _hp -= damage;
        // Rewind 리플레이 중 효과음·연출 오발생 방지
        if (Managers.Game.State == Define.GameState.Playing)
        {
            Managers.Sound.PlayEffect(SoundManager.Hit);
            _hitPunchEndTime = Time.time + HitPunchDuration;
        }
        if (_hp <= 0) OnDead();
    }

    protected virtual void OnDead()
    {
        _target?.GetComponent<PlayerController>()?.AddExp(_expReward);
        Managers.Game.Score += _expReward;
        if (Managers.Game.State == Define.GameState.Playing)
        {
            Managers.Game.RunGold += _goldReward;   // Rewind 리플레이 중복 획득 방지
            Managers.Game.RunKills++;
            Managers.Sound.PlayEffect(SoundManager.EnemyDeath);
        }
        _registry.Remove(EntityId);
        SpatialHashGrid.Instance?.Remove(this);
        EnemyInstanceRenderer.Unregister(this, _originPrefab);
        _hp = _originHp;
        if (_deathEffectPrefab != null)
        {
            GameObject fx = UnityEngine.Object.Instantiate(_deathEffectPrefab, transform.position, Quaternion.identity);
            Managers.Resource.Destroy(fx, _deathEffectLifetime);
        }
        Managers.Object.Return(gameObject, _originPrefab);
    }

    public virtual void RestoreSnapshot(EnemySnapshot s)
    {
        _hp    = s.hp;
        _speed = s.speed;
        transform.position = s.position;
    }

    // Rewind 전용 — Pool 재사용 케이스: 스냅샷 entityId를 강제 주입해 복원
    public void ForceRestore(EnemySnapshot s, Transform target)
    {
        EntityId      = s.entityId;
        _originPrefab = s.prefab;
        _target       = target;
        _originHp     = s.hp;
        _registry[EntityId] = this;
        _hitPunchEndTime = -1f;
        RecalcBodyRadius();   // Init 을 타지 않는 경로 — 빠뜨리면 복원된 적이 서로 겹친다
        RestoreSnapshot(s);
    }

    // Rewind 전용 — 레지스트리 직접 조작
    public static void UnregisterForRewind(int entityId) => _registry.Remove(entityId);
    public static void RegisterForRewind(EnemyBase e)    => _registry[e.EntityId] = e;

    // 씬 전환 시 호출 — static 레지스트리는 씬을 넘어 유지되므로, 정상 사망(OnDead) 없이
    // 파괴된 적이 남아 다음 씬에서 파괴된 참조로 접근되는 것을 막기 위해 비운다
    public static void ClearRegistry() => _registry.Clear();
}
