using UnityEngine;

// Inspector에서 웨이브 데이터 편집 가능
// Assets/Data/Waves/ 에 에셋으로 저장
[CreateAssetMenu(fileName = "WaveData", menuName = "Game/WaveData")]
public class WaveData : ScriptableObject
{
    [Header("타이밍")]
    public float startTime;         // 게임 시작 후 이 웨이브가 시작되는 시간 (초)
    public float spawnInterval;     // 스폰 간격 (초)
    public int spawnCountPerBurst;  // 한 번에 스폰할 적 수

    [Header("잡몹 프리팹")]
    public GameObject enemyPrefab;   // 이 웨이브에서 스폰할 적 프리팹

    [Header("잡몹 스탯 — 보스에는 적용되지 않음(BossData가 원본)")]
    public int enemyHp;
    public float enemySpeed;

    [Header("보스 — 위 잡몹 설정은 보스전 중에도 계속 쓰인다")]
    public bool isBossWave;
    public GameObject bossPrefab;    // 스탯은 BossData가 원본 — 여기선 등장만 지정
    public float warningLeadTime;    // 웨이브 시작 이 초 전부터 WARNING 표시 + 잡몹 스폰 중단
}
