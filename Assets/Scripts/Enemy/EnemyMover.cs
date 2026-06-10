using System.Collections.Generic;
using UnityEngine;

public class EnemyMover : EnemyBase
{
    [SerializeField] int _contactDamage = 10;
    [SerializeField] float _separationRadius = 0.8f;
    [SerializeField] float _separationWeight = 1.5f;
    // _neighborBuf 제거 — 이웃 탐색은 EnemyJobScheduler가 담당
    float _attackCooldown = 0f;
    EnemyJobScheduler _scheduler;   // 씬 간 참조 캐시

    public float AttackCooldown { get { return _attackCooldown; } }

    void Start()
    {
        _scheduler = FindObjectOfType<EnemyJobScheduler>();
    }

    void Update()
    {
        if (_target == null) return;
        if (Managers.Game.State != Define.GameState.Playing) return;

        Vector2 prevPos = transform.position;
        Vector2 targetDir = ((Vector2)_target.position - (Vector2)transform.position).normalized;

        // Separation은 EnemyJobScheduler(Job+Burst)가 미리 계산 — index 조회만
        Vector2 separation = Vector2.zero;
        if (_scheduler != null)
        {
            Unity.Mathematics.float2 s = _scheduler.GetSeparation(EntityId);
            separation = new Vector2(s.x, s.y);
        }

        Vector2 moveDir = (targetDir + separation * _separationWeight).normalized;
        transform.position += (Vector3)(moveDir * _speed * Time.deltaTime);

        SpatialHashGrid.Instance?.Move(this, prevPos);   // grid 유지(BombWeapon/Rewind 의존)

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