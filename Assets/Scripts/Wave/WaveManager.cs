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

    public int   WaveIndex  { get { return _waveIndex; } }
    public float GameTime   { get { return _gameTime; } }
    public float SpawnTimer { get { return _spawnTimer; } }

    public System.Action<int> OnWaveChanged;

    WaveData CurrentWave { get { return _waves[_waveIndex]; } }

    // 상태 플래그가 아니라 _gameTime에서 매 프레임 계산 — 복원할 상태가 없으면 되감기 버그도 없다
    public bool BossWarningActive
    {
        get
        {
            if (_waves == null || _waveIndex + 1 >= _waves.Length) return false;
            WaveData next = _waves[_waveIndex + 1];
            if (!next.isBossWave) return false;
            return _gameTime >= next.startTime - next.warningLeadTime && _gameTime < next.startTime;
        }
    }

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
            if (!BossWarningActive)
                SpawnBurst();
            // 보스전 중엔 완전 정지가 아니라 완화 — 잡몹이 없으면 EXP 공급이 끊긴다
            _spawnTimer = CurrentWave.spawnInterval * (BossController.Instance != null ? 3f : 1f);
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

            // 되감기로 시간이 되돌아간 뒤 다시 전진하면 이 전환이 재발화한다 — Instance 가드로 2마리 방지
            if (CurrentWave.isBossWave && CurrentWave.bossPrefab != null && BossController.Instance == null)
            {
                _spawner.Spawn(CurrentWave.bossPrefab, -1, -1f);
                FindObjectOfType<RewindManager>()?.ResetCooldown();
            }
        }
    }

    void SpawnBurst()
    {
        for (int i = 0; i < CurrentWave.spawnCountPerBurst; i++)
            _spawner.Spawn(CurrentWave.enemyPrefab, CurrentWave.enemyHp, CurrentWave.enemySpeed);
    }

    public void RestoreSnapshot(WaveSnapshot s)
    {
        _gameTime   = s.gameTime;
        _spawnTimer = s.spawnTimer;
        _waveIndex  = s.waveIndex;
    }
}
