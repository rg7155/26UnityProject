using UnityEngine;

public class EnemyMover : EnemyBase
{
    void Update()
    {
        if (_target == null) return;

        Vector2 dir = (_target.position - transform.position).normalized;
        transform.position += (Vector3)(dir * _speed * Time.deltaTime);
    }
}