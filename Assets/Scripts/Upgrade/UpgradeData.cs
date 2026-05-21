using UnityEngine;
using static Define;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Game/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName;
    [TextArea] public string description;
    public UpgradeType type;
    public float value;  // 곱연산: 1.2 = +20%, 덧셈: 20 = +20

    public void Apply(PlayerController player, PlayerWeapon weapon)
    {
        switch (type)
        {
            case UpgradeType.MoveSpeed: player.UpgradeSpeed(value); break;
            case UpgradeType.MaxHp:     player.UpgradeMaxHp((int)value); break;
            case UpgradeType.FireRate:  weapon.UpgradeFireRate(value); break;
            case UpgradeType.Damage:    weapon.UpgradeDamage(value); break;
            case UpgradeType.Range:     weapon.UpgradeRange(value); break;
            case UpgradeType.UnlockBomb:
                player.GetComponent<BombWeapon>().enabled = true;
                break;
        }
    }
}
