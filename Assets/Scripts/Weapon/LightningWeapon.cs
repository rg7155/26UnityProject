using System.Collections.Generic;
using UnityEngine;
using static Define;

public class LightningWeapon : WeaponBase
{
    float _fireRate;
    float _detectRange;
    int _chainCount;
    float _chainRange;
    float _damageFalloff;
    GameObject _boltPrefab;

    float _fireCooldown;
    readonly List<EnemyBase> _hitBuf = new List<EnemyBase>(16);
    readonly HashSet<int> _visited = new HashSet<int>();
    readonly List<Vector3> _path = new List<Vector3>(8);

    public override void Init(WeaponData data)
    {
        base.Init(data);
        var d = (LightningWeaponData)data;
        _fireRate = d.fireRate;
        _detectRange = d.detectRange;
        _chainCount = d.chainCount;
        _chainRange = d.chainRange;
        _damageFalloff = d.damageFalloff;
        _boltPrefab = d.boltPrefab;
    }

    void Update()
    {
        if (Managers.Game.State != GameState.Playing)
            return;

        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown <= 0f)
        {
            TryFire();
            _fireCooldown = 1f / _fireRate;
        }
    }

    void TryFire()
    {
        EnemyBase target = SpatialHashGrid.Instance?.FindNearest(transform.position, _detectRange);
        if (target == null)
            return;

        _visited.Clear();
        _path.Clear();
        _path.Add(transform.position);

        float falloff = 1f;
        for (int i = 0; i < _chainCount; i++)
        {
            if (target == null)
                break;

            int dmg = Mathf.RoundToInt(_damage * falloff);
            target.OnDamaged(dmg);
            _visited.Add(target.EntityId);
            _path.Add(target.transform.position);

            target = NextTarget(target);
            falloff *= _damageFalloff;
        }

        if (_path.Count >= 2)
            SpawnBolt();

        // TODO: Lightning 효과음 (사용자 확인 후 추가)
    }

    EnemyBase NextTarget(EnemyBase from)
    {
        SpatialHashGrid.Instance?.QueryNeighbors(from, from.transform.position, _chainRange, _hitBuf);

        EnemyBase best = null;
        float bestSqr = float.MaxValue;
        Vector3 origin = from.transform.position;
        for (int i = 0; i < _hitBuf.Count; i++)
        {
            EnemyBase e = _hitBuf[i];
            if (_visited.Contains(e.EntityId))
                continue;

            float sqr = (e.transform.position - origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = e;
            }
        }
        return best;
    }

    void SpawnBolt()
    {
        if (_boltPrefab == null)
            return;

        GameObject fx = Instantiate(_boltPrefab, transform.position, Quaternion.identity);
        fx.GetComponent<LightningEffect>()?.Init(_path);
    }

    public override void UpgradeFireRate(float multiplier) { _fireRate *= multiplier; }
    public override void UpgradeRange(float multiplier) { _detectRange *= multiplier; _chainRange *= multiplier; }
}
