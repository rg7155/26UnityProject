using System.Collections.Generic;
using UnityEngine;

public class EnemyMover : EnemyBase
{
    [SerializeField] int _contactDamage = 10;
    [SerializeField] float _separationRadius = 0.8f;
    [SerializeField] float _separationWeight = 1.5f;
    List<EnemyBase> _neighborBuf = new List<EnemyBase>(8);
    float _attackCooldown = 0f;

    public float AttackCooldown { get { return _attackCooldown; } }

    void Update()
    {
        if (_target == null) return;
        if (Managers.Game.State != Define.GameState.Playing) return;

        Vector2 prevPos = transform.position;
        Vector2 targetDir = ((Vector2)_target.position - (Vector2)transform.position).normalized;

        // Separation Steering
        Vector2 separation = Vector2.zero;
        SpatialHashGrid.Instance?.QueryNeighbors(this, transform.position, _separationRadius, _neighborBuf);
        foreach (EnemyBase other in _neighborBuf)
        {
            Vector2 diff = (Vector2)transform.position - (Vector2)other.transform.position;
            float dist = diff.magnitude;
            if (dist < 0.0001f)
            {
                // 완전 겹침 — EntityId 기반 결정적 오프셋으로 NaN 방지
                diff = new Vector2((EntityId & 1) == 0 ? 1f : -1f, (EntityId & 2) == 0 ? 1f : -1f);
                dist = 1f;
            }
            float strength = 1f - (dist / _separationRadius);
            separation += diff.normalized * strength;
        }

        Vector2 moveDir = (targetDir + separation * _separationWeight).normalized;
        transform.position += (Vector3)(moveDir * _speed * Time.deltaTime);

        SpatialHashGrid.Instance?.Move(this, prevPos);

        if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;
    }

    public override void RestoreSnapshot(EnemySnapshot s)
    {
        base.RestoreSnapshot(s);
        _attackCooldown = s.attackCooldown;
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