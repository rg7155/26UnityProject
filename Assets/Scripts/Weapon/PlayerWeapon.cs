using UnityEngine;

// 플레이어에 붙이는 자동 발사 무기
// 가장 가까운 적을 향해 일정 주기로 발사체 발사
public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] GameObject _projectilePrefab;
    [SerializeField] float _fireRate = 1f;      // 초당 발사 횟수
    [SerializeField] int _damage = 20;
    [SerializeField] float _range = 8f;         // 발사체 사거리
    [SerializeField] float _detectRange = 10f;  // 적 탐지 범위

    float _fireCooldown;

    void Update()
    {
        if (Managers.Game.State != Define.GameState.Playing) return;

        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown <= 0f)
        {
            TryFire();
            _fireCooldown = 1f / _fireRate;
        }
    }

    void TryFire()
    {
        EnemyBase nearest = FindNearest();
        if (nearest == null) return;

        Vector2 dir = (nearest.transform.position - transform.position).normalized;
        Fire(dir);
    }

    void Fire(Vector2 dir)
    {
        if (_projectilePrefab == null) return;

        GameObject go = Managers.Object.Get(_projectilePrefab);
        go.transform.position = transform.position;
        go.transform.rotation = Quaternion.identity;
        Projectile proj = go.GetComponent<Projectile>();
        proj.Init(dir, _damage, _range, _projectilePrefab);
        Managers.Sound.PlayEffect(SoundManager.Shoot);
    }

    // --- 업그레이드 적용 메서드 ---
    public void UpgradeFireRate(float multiplier) { _fireRate *= multiplier; }
    public void UpgradeDamage(float multiplier)   { _damage = Mathf.RoundToInt(_damage * multiplier); }
    public void UpgradeRange(float multiplier)    { _range *= multiplier; _detectRange *= multiplier; }

    EnemyBase FindNearest()
    {
        return SpatialHashGrid.Instance?.FindNearest(transform.position, _detectRange);
    }
}
