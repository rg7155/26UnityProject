using System.Collections.Generic;
using UnityEngine;
using static Define;

// 플레이어 주위를 도는 위성. 각 위성 위치에서 주기적으로 근접 적에게 데미지
// 위성은 player 자식이라 씬/플레이어 파괴 시 함께 소멸 (별도 정리 불필요)
public class OrbitWeapon : WeaponBase
{
    int _orbiterCount;
    float _orbitRadius;
    float _rotationSpeed;
    float _hitRadius;
    float _hitInterval;

    List<Transform> _orbiters = new List<Transform>();
    float _angle;
    float _tickTimer;
    List<EnemyBase> _hitBuf = new List<EnemyBase>(16);

    public override void Init(WeaponData data)
    {
        base.Init(data);
        var d = (OrbitWeaponData)data;
        _orbiterCount = d.orbiterCount;
        _orbitRadius = d.orbitRadius;
        _rotationSpeed = d.rotationSpeed;
        _hitRadius = d.hitRadius;
        _hitInterval = d.hitInterval;

        for (int i = 0; i < _orbiterCount; i++)
        {
            GameObject orb = Instantiate(d.orbiterPrefab, transform);
            _orbiters.Add(orb.transform);
        }
        _tickTimer = _hitInterval;
    }

    void Update()
    {
        if (Managers.Game.State != GameState.Playing) return;

        _angle += _rotationSpeed * Time.deltaTime;
        float step = 360f / _orbiterCount;
        for (int i = 0; i < _orbiters.Count; i++)
        {
            float ang = (_angle + i * step) * Mathf.Deg2Rad;
            _orbiters[i].localPosition = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * _orbitRadius;
        }

        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            foreach (Transform orb in _orbiters)
            {
                SpatialHashGrid.Instance?.QueryNeighbors(null, orb.position, _hitRadius, _hitBuf);
                foreach (EnemyBase each in _hitBuf)
                    each.OnDamaged(_damage);
            }
            _tickTimer = _hitInterval;
        }
    }
}
