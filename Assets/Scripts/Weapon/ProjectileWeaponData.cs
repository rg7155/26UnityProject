using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileWeaponData", menuName = "Game/Weapon/Projectile")]
public class ProjectileWeaponData : WeaponData
{
    public GameObject projectilePrefab;
    public float fireRate;
    public float range;
    public float detectRange;
}
