using UnityEngine;
using static Define;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Game/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName;
    [TextArea] public string description;
    public UpgradeType type;
    public float value;  // 곱연산: 1.2 = +20%, 덧셈: 20 = +20
    public WeaponData weaponToAcquire;  // AcquireWeapon 전용

    public void Apply(PlayerController player, WeaponManager wm)
    {
        switch (type)
        {
            case UpgradeType.MoveSpeed: player.UpgradeSpeed(value); break;
            case UpgradeType.MaxHp:     player.UpgradeMaxHp((int)value); break;
            case UpgradeType.FireRate:  wm.UpgradeFireRate(value); break;
            case UpgradeType.Damage:    wm.UpgradeDamage(value); break;
            case UpgradeType.Range:     wm.UpgradeRange(value); break;
            case UpgradeType.AcquireWeapon: wm.AddWeapon(weaponToAcquire); break;
        }
    }
}
