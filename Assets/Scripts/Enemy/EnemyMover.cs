using UnityEngine;

public class EnemyMover : EnemyBase
{
    [SerializeField] int _contactDamage = 10;
    float _attackCooldown = 0f;

    void Update()
    {
        if (_target == null) return;

        Vector2 dir = (_target.position - transform.position).normalized;
        transform.position += (Vector3)(dir * _speed * Time.deltaTime);

        if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_attackCooldown > 0f) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        player.OnDamaged(_contactDamage);
        _attackCooldown = 1f;
    }
}