using System.Collections.Generic;
using UnityEngine;
using static Define;

// 플레이어 주변 반경 내 적을 일정 주기로 폭발시키는 보조 무기
// 기본 비활성화 상태이며, UnlockBomb 업그레이드 선택 시 enabled = true
public class BombWeapon : MonoBehaviour
{
    [SerializeField] float _interval = 4f;
    [SerializeField] int _damage = 30;
    [SerializeField] float _radius = 3f;
    [SerializeField] GameObject _explosionEffectPrefab;

    float _timer;
    List<EnemyBase> _hitBuf = new List<EnemyBase>(16);

    void Awake()
    {
        enabled = false;
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
    }

    // --- 업그레이드 적용 메서드 ---
    public void UpgradeDamage(float multiplier)   { _damage = Mathf.RoundToInt(_damage * multiplier); }
    public void UpgradeRadius(float multiplier)   { _radius *= multiplier; }
    public void UpgradeInterval(float multiplier) { _interval /= multiplier; }  // 빠를수록 좋으므로 나눔
}
