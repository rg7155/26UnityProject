using UnityEngine;
using static Define;

// 시간 기반 웨이브 시스템
// WaveData는 Resources/Waves/ 에서 자동 로드 — Inspector 연결 불필요
public class WaveManager : MonoBehaviour
{
    float _gameTime;
    float _spawnTimer;
    int _waveIndex;

    WaveData[] _waves;
    EnemySpawner _spawner;

    public int WaveIndex { get { return _waveIndex; } }
    public float GameTime { get { return _gameTime; } }

    public System.Action<int> OnWaveChanged;

    WaveData CurrentWave { get { return _waves[_waveIndex]; } }

    void Start()
    {
        // WaveData 에셋을 Resources/Waves/ 에서 로드 (이름 오름차순 정렬)
        _waves = Resources.LoadAll<WaveData>("Waves");
        if (_waves == null || _waves.Length == 0)
        {
            Debug.LogError("[WaveManager] Resources/Waves/ 에 WaveData 에셋이 없습니다");
            return;
        }

        System.Array.Sort(_waves, (a, b) => a.startTime.CompareTo(b.startTime));

        _spawner = FindObjectOfType<EnemySpawner>();
        if (_spawner == null)
        {
            Debug.LogError("[WaveManager] EnemySpawner not found");
            return;
        }

        _waveIndex = 0;
        _spawnTimer = CurrentWave.spawnInterval;
        Debug.Log($"[WaveManager] Wave 1 시작 — 총 {_waves.Length}개 웨이브 로드됨");
    }

    void Update()
    {
        if (Managers.Game.State != GameState.Playing) return;

        _gameTime += Time.deltaTime;
        _spawnTimer -= Time.deltaTime;

        CheckWaveTransition();

        if (_spawnTimer <= 0f)
        {
            SpawnBurst();
            _spawnTimer = CurrentWave.spawnInterval;
        }
    }

    void CheckWaveTransition()
    {
        if (_waveIndex + 1 >= _waves.Length) return;

        if (_gameTime >= _waves[_waveIndex + 1].startTime)
        {
            _waveIndex++;
            _spawnTimer = 0f;
            OnWaveChanged?.Invoke(_waveIndex + 1);
            Debug.Log($"[WaveManager] Wave {_waveIndex + 1} 시작 ({_gameTime:F0}초)");
        }
    }

    void SpawnBurst()
    {
        for (int i = 0; i < CurrentWave.spawnCountPerBurst; i++)
            _spawner.Spawn(CurrentWave.enemyPrefab, CurrentWave.enemyHp, CurrentWave.enemySpeed);
    }
}
