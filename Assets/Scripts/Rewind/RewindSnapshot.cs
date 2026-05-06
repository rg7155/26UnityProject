using UnityEngine;

// Time Rewind 시스템에서 사용하는 스냅샷 자료구조
public struct PlayerSnapshot
{
    public Vector3 position;
    public int     hp;
    public int     exp;
    public int     level;
    public int     expToNextLevel;
    public float   speed;
    public int     maxHp;
}

public struct EnemySnapshot
{
    public int        entityId;     // 논리적 동일성 추적 (Pool 재사용 감지용)
    public EnemyBase  enemy;        // 오브젝트 참조
    public GameObject prefab;       // Pool 반납/복원용
    public Vector3    position;
    public int        hp;
    public float      speed;
    public float      attackCooldown;
}

public struct WaveSnapshot
{
    public float gameTime;
    public float spawnTimer;
    public int   waveIndex;
}

public struct FrameSnapshot
{
    public PlayerSnapshot  player;
    public EnemySnapshot[] enemies;
    public WaveSnapshot    wave;
}
