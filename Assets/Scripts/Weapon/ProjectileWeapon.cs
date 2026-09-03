using UnityEngine;

// 가장 가까운 적을 향해 일정 주기로 발사체 발사
public class ProjectileWeapon : WeaponBase
{
    GameObject _projectilePrefab;
    float _fireRate;
    float _range;
    float _detectRange;
    Transform _fireOrigin;

    float _fireCooldown;

    public override void Init(WeaponData data)
    {
        base.Init(data);
        var d = (ProjectileWeaponData)data;
        _projectilePrefab = d.projectilePrefab;
        _fireRate = d.fireRate;
        _range = d.range;
        _detectRange = d.detectRange;
        _fireOrigin = transform.Find("PlayerVisual/FireOrigin");
    }

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
        go.transform.position = _fireOrigin != null ? _fireOrigin.position : transform.position;
        go.transform.rotation = Quaternion.identity;
        Projectile proj = go.GetComponent<Projectile>();
        proj.Init(dir, _damage, _range, _projectilePrefab);
        Managers.Sound.PlayEffect(SoundManager.Shoot);
    }

    public override void UpgradeFireRate(float multiplier) { _fireRate *= multiplier; }
    public override void UpgradeRange(float multiplier)    { _range *= multiplier; _detectRange *= multiplier; }

    EnemyBase FindNearest()
    {
        return SpatialHashGrid.Instance?.FindNearest(transform.position, _detectRange);
    }
}
