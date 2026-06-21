using UnityEngine;

[CreateAssetMenu(fileName = "OrbitWeaponData", menuName = "Game/Weapon/Orbit")]
public class OrbitWeaponData : WeaponData
{
    public int orbiterCount;
    public float orbitRadius;
    public float rotationSpeed;   // 각속도 (도/초)
    public float hitRadius;
    public float hitInterval;
    public GameObject orbiterPrefab;

    public override WeaponBase AddTo(GameObject owner)
    {
        var w = owner.AddComponent<OrbitWeapon>();
        w.Init(this);
        return w;
    }
}
