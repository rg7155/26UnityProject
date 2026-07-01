using UnityEngine;

[CreateAssetMenu(fileName = "OrbitWeaponData", menuName = "Game/Weapon/Orbit")]
public class OrbitWeaponData : WeaponData
{
    public int orbiterCount;
    public float orbitRadius;
    public float rotationSpeed;   // 각속도 (도/초)
    public float hitRadius;
    public float hitCooldown = 0.3f;   // 적별 재타격 쿨다운 — 낮을수록 DPS↑
    public GameObject orbiterPrefab;

    public float spinBaseSpeed;        // 자전 기본 각속도 (도/초)
    public float spinAmplitude;        // 자전 속도 변동 진폭
    public float spinFrequency;        // 자전 속도 변동 주기 계수
    public float radiusPulseAmplitude; // 거리 펄스 진폭 (orbitRadius보다 작게 — 음수 방지)
    public float radiusPulseFrequency; // 거리 펄스 주기 계수

    public override WeaponBase AddTo(GameObject owner)
    {
        var w = owner.AddComponent<OrbitWeapon>();
        w.Init(this);
        return w;
    }
}
