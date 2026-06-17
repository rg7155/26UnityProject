using UnityEngine;

// 플레이어에 부착. 두 무기에 WeaponData를 주입하고 업그레이드의 단일 진입점 제공
public class WeaponManager : MonoBehaviour
{
    [SerializeField] ProjectileWeaponData _baseWeaponData;
    [SerializeField] AoEWeaponData _bombWeaponData;

    ProjectileWeapon _projectileWeapon;
    AoEWeapon _aoeWeapon;

    void Awake()
    {
        _projectileWeapon = GetComponent<ProjectileWeapon>();
        _aoeWeapon = GetComponent<AoEWeapon>();

        _projectileWeapon.Init(_baseWeaponData);
        _aoeWeapon.Init(_bombWeaponData);
    }

    public void UpgradeFireRate(float multiplier) => _projectileWeapon.UpgradeFireRate(multiplier);
    public void UpgradeRange(float multiplier)    => _projectileWeapon.UpgradeRange(multiplier);
    public void UpgradeDamage(float multiplier)   => _projectileWeapon.UpgradeDamage(multiplier);
    public void UnlockBomb()                      => _aoeWeapon.enabled = true;
}
