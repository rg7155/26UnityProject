using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] protected float _speed = 2f;
    [SerializeField] protected int _hp = 3;
    [SerializeField] protected int _expReward = 1;
    protected Transform _target;

    public virtual void Init(Transform target, int hp = -1, float speed = -1f)
    {
        _target = target;
        if (hp >= 0) _hp = hp;
        if (speed >= 0f) _speed = speed;
        // 미전달 시 Inspector SerializeField 값 유지
    }

    public virtual void OnDamaged(int damage)
    {
        _hp -= damage;
        if (_hp <= 0) OnDead();
    }

    protected virtual void OnDead()
    {
        _target?.GetComponent<PlayerController>()?.AddExp(_expReward);
        gameObject.SetActive(false);  // 추후 Pool 반납으로 교체
    }
}
