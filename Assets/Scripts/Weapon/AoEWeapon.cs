using System.Collections.Generic;
using UnityEngine;
using static Define;

// 플레이어 주변 반경 내 적을 일정 주기로 폭발시키는 보조 무기
// 획득 시 WeaponData.AddTo가 AddComponent로 부착
public class AoEWeapon : WeaponBase
{
    float _interval;
    float _radius;
    GameObject _explosionEffectPrefab;

    float _timer;
    List<EnemyBase> _hitBuf = new List<EnemyBase>(16);

    public override void Init(WeaponData data)
    {
        base.Init(data);
        var d = (AoEWeaponData)data;
        _interval = d.interval;
        _radius = d.radius;
        _explosionEffectPrefab = d.explosionEffectPrefab;
        _timer = _interval;
    }

    void Update()
    {
        if (Managers.Game.State != GameState.Playing) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Explode();
            _timer = _interval;
        }
    }

    void Explode()
    {
        SpatialHashGrid.Instance?.QueryNeighbors(null, transform.position, _radius, _hitBuf);
        foreach (EnemyBase each in _hitBuf)
        {
            each.OnDamaged(_damage);
        }

        if (_explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            Managers.Resource.Destroy(fx, 1f);
        }

        Managers.Sound.PlayEffect(SoundManager.Explosion);
    }

    public override void UpgradeRadius(float multiplier)   { _radius *= multiplier; }
    public override void UpgradeInterval(float multiplier) { _interval /= multiplier; }  // 빠를수록 좋으므로 나눔
}
