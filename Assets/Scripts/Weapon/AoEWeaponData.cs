using UnityEngine;

[CreateAssetMenu(fileName = "AoEWeaponData", menuName = "Game/Weapon/AoE")]
public class AoEWeaponData : WeaponData
{
    public float interval;
    public float radius;
    public GameObject explosionEffectPrefab;

    public override WeaponBase AddTo(GameObject owner)
    {
        var w = owner.AddComponent<AoEWeapon>();
        w.Init(this);
        return w;
    }
}
