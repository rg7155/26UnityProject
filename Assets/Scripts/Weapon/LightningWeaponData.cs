using UnityEngine;

[CreateAssetMenu(fileName = "LightningWeaponData", menuName = "Game/Weapon/Lightning")]
public class LightningWeaponData : WeaponData
{
    public float fireRate = 1f;
    public float detectRange = 8f;
    public int chainCount = 4;
    public float chainRange = 4f;
    [Range(0f, 1f)] public float damageFalloff = 0.8f;
    public GameObject boltPrefab;

    public override WeaponBase AddTo(GameObject owner)
    {
        var w = owner.AddComponent<LightningWeapon>();
        w.Init(this);
        return w;
    }
}
