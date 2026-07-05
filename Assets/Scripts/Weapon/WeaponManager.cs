using System.Collections.Generic;
using UnityEngine;

// 플레이어에 부착. 무기 목록을 관리하고 업그레이드의 단일 진입점 제공
public class WeaponManager : MonoBehaviour
{
    [SerializeField] WeaponData[] _startingWeapons;

    List<WeaponBase> _weapons = new List<WeaponBase>();
    float _damageMult = 1f, _fireRateMult = 1f, _rangeMult = 1f;

    // Managers/SpatialHashGrid 초기화(Awake) 이후 무기 생성 보장 위해 Start
    void Start()
    {
        foreach (var d in _startingWeapons)
            AddWeapon(d);

        float m = ShopService.DamageMult();   // 메타 상점 기본 데미지 강화 적용
        if (m != 1f) UpgradeDamage(m);
    }

    public WeaponBase AddWeapon(WeaponData data)
    {
        var w = data.AddTo(gameObject);
        if (_damageMult != 1f) w.UpgradeDamage(_damageMult);
        if (_fireRateMult != 1f) w.UpgradeFireRate(_fireRateMult);
        if (_rangeMult != 1f) w.UpgradeRange(_rangeMult);
        _weapons.Add(w);
        return w;
    }

    public bool HasWeapon(WeaponData data)
    {
        foreach (var w in _weapons)
            if (w.Data == data) return true;
        return false;
    }

    public void UpgradeDamage(float multiplier)   { _damageMult *= multiplier; foreach (var w in _weapons) w.UpgradeDamage(multiplier); }
    public void UpgradeFireRate(float multiplier) { _fireRateMult *= multiplier; foreach (var w in _weapons) w.UpgradeFireRate(multiplier); }
    public void UpgradeRange(float multiplier)    { _rangeMult *= multiplier; foreach (var w in _weapons) w.UpgradeRange(multiplier); }
}
