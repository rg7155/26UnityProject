using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] protected float _speed = 2f;
    [SerializeField] protected int _hp = 3;
    [SerializeField] protected int _expReward = 1;
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

    public int        Hp           { get { return _hp; } }
    public float      Speed        { get { return _speed; } }
    public GameObject OriginPrefab { get { return _originPrefab; } }

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

        SpatialHashGrid.Instance?.Add(this);
        EnemyInstanceRenderer.Register(this, originPrefab);
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
        if (_hp <= 0) OnDead();
    }

    protected virtual void OnDead()
    {
        _target?.GetComponent<PlayerController>()?.AddExp(_expReward);
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
        RestoreSnapshot(s);
    }

    // Rewind 전용 — 레지스트리 직접 조작
    public static void UnregisterForRewind(int entityId) => _registry.Remove(entityId);
    public static void RegisterForRewind(EnemyBase e)    => _registry[e.EntityId] = e;
}
