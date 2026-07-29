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

// HP/위치/접촉쿨다운은 담지 않는다 — 보스도 EnemyBase.Registry에 있어 EnemySnapshot 경로가 이미 기록한다
public struct BossSnapshot
{
    public bool    active;         // 이 프레임에 보스가 존재했는가
    public int     entityId;       // 복원 대상이 같은 개체인지 확인
    public int     sequenceIndex;
    public int     actionState;    // BossActionState
    public float   stateTimer;
    public float   cooldownTimer;
    public Vector2 lockedPoint;    // 텔레그래프 시작 시점에 고정한 장판 중심
}

public struct FrameSnapshot
{
    public PlayerSnapshot  player;
    public EnemySnapshot[] enemies;
    public WaveSnapshot    wave;
    public BossSnapshot    boss;
}
