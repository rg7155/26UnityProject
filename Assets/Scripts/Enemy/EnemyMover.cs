using UnityEngine;

public class EnemyMover : EnemyBase
{
    [SerializeField] int _contactDamage = 10;
    float _attackCooldown = 0f;

    public float AttackCooldown { get { return _attackCooldown; } }

    void Update()
    {
        if (_target == null) return;
        if (Managers.Game.State != Define.GameState.Playing) return;

        Vector2 prevPos = transform.position;
        Vector2 dir = (_target.position - transform.position).normalized;
        transform.position += (Vector3)(dir * _speed * Time.deltaTime);

        SpatialHashGrid.Instance?.Move(this, prevPos);

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