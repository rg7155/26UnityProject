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

    [Header("적 프리팹")]
    public GameObject enemyPrefab;   // 이 웨이브에서 스폰할 적 프리팹

    [Header("적 스탯")]
    public int enemyHp;
    public float enemySpeed;
}
