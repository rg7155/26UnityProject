using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] protected float _speed = 2f;
    [SerializeField] protected int _hp = 3;
    protected Transform _target;  // 플레이어

    public virtual void Init(Transform target, int hp = -1)
    {
        _target = target;
        if (hp >= 0)
            _hp = hp;
        // hp 미전달 시 Inspector SerializeField 값 유지
    }

    public virtual void OnDamaged(int damage)
    {
        _hp -= damage;
        if (_hp <= 0) OnDead();
    }

    protected virtual void OnDead()
    {
        gameObject.SetActive(false);  // 나중에 Pool 반납으로 교체
    }
}