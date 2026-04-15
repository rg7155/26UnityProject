using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] protected float _speed = 2f;
    [SerializeField] protected int _hp = 3;
    [SerializeField] protected int _expReward = 1;
    protected Transform _target;

    int _originHp;  // Init 시 전달받은 HP 저장 (반납 후 재사용 시 리셋용 — Init은 항상 hp >= 0으로 호출할 것)
    GameObject _originPrefab;  // 풀 반납 시 사용

    public virtual void Init(Transform target, GameObject originPrefab, int hp = -1, float speed = -1f)
    {
        _target = target;
        _originPrefab = originPrefab;
        if (hp >= 0) _hp = hp;
        if (speed >= 0f) _speed = speed;
        _originHp = _hp;
    }

    public virtual void OnDamaged(int damage)
    {
        _hp -= damage;
        if (_hp <= 0) OnDead();
    }

    protected virtual void OnDead()
    {
        _target?.GetComponent<PlayerController>()?.AddExp(_expReward);
        _hp = _originHp;  // HP 리셋 후 반납
        Managers.Object.Return(gameObject, _originPrefab);
    }
}
