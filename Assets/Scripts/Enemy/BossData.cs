using UnityEngine;

public enum BossPatternType { GroundSlam, RadialBurst, Charge }

[CreateAssetMenu(fileName = "BossData", menuName = "Game/BossData")]
public class BossData : ScriptableObject
{
    [Header("정체")]
    public string bossName;
    public int    hp = 1200;
    public float  moveSpeed = 1.6f;
    public int    expReward = 100;
    public int    goldReward = 150;
    public int    contactDamage = 25;
    public float  contactInterval = 1f;
    public Color  phase1Color = Color.cyan;
    public Color  phase2Color = Color.red;

    [Header("패턴 시퀀스 — 이 순서대로 무한 반복 (랜덤 없음)")]
    public BossPatternType[] patternSequence;

    [Header("장판 GroundSlam")]
    public float slamTelegraph = 1f;
    public float slamRadius = 2.5f;
    public int   slamDamage = 35;
    public float slamCooldown = 5f;

    [Header("방사탄 RadialBurst")]
    public GameObject radialProjectilePrefab;
    public float radialTelegraph = 0.6f;
    public int   radialCount = 12;
    public int   radialCountPhase2 = 16;
    public int   radialDamage = 20;
    public float radialSpeed = 4.5f;
    public float radialLifetime = 3f;
    public float radialCooldown = 6f;

    [Header("돌진 Charge")]
    public float chargeTelegraph = 0.8f;
    public float chargeSpeed = 12f;
    public float chargeDuration = 1.2f;
    public int   chargeDamage = 30;
    public float chargeCooldown = 8f;

    [Header("페이즈 2 — hpRatio 0이면 비활성")]
    public float phase2HpRatio = 0.5f;
    public float phase2CooldownMult = 0.7f;

    [Header("리쉬")]
    public float leashDistance = 18f;
    public float leashReturnDistance = 12f;
    public float leashSpeedMult = 2.5f;
}
