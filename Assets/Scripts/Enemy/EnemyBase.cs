using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] protected float _speed = 2f;
    [SerializeField] protected int _hp = 3;
    [SerializeField] protected int _expReward = 1;
    protected Transform _target;

    int _originHp;
    GameObject _originPrefab;

    public int        Hp          { get { return _hp; } }
    public float      Speed       { get { return _speed; } }
    public GameObject OriginPrefab { get { return _originPrefab; } }

    public virtual void Init(Transform target, GameObject originPrefab, int hp = -1, float speed = -1f)
    {
        _target = target;
        _originPrefab = originPrefab;
        if (hp >= 0) _hp = hp;
        if (speed >= 0f) _speed = speed;
        _originHp = _hp;

        SpatialHashGrid.Instance?.Add(this);
        EnemyInstanceRenderer.Register(this, originPrefab);
    }

    public virtual void OnDamaged(int damage)
    {
        _hp -= damage;
        if (_hp <= 0) OnDead();
    }

    protected virtual void OnDead()
    {
        _target?.GetComponent<PlayerController>()?.AddExp(_expReward);
        SpatialHashGrid.Instance?.Remove(this);
        EnemyInstanceRenderer.Unregister(this, _originPrefab);
        _hp = _originHp;
        Managers.Object.Return(gameObject, _originPrefab);
    }

    public virtual void RestoreSnapshot(EnemySnapshot s)
    {
        _hp    = s.hp;
        _speed = s.speed;
        transform.position = s.position;
    }
}
