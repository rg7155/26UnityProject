using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Define;

// Time Rewind 핵심 로직
// Circular Buffer로 매 0.05초 스냅샷 저장
// Shift: 능동 되감기 (5초, 쿨타임 30초)
// 사망 시: Auto-Rewind (스테이지당 1회)
public class RewindManager : MonoBehaviour
{
    [SerializeField] float _recordInterval  = 0.05f;  // 스냅샷 저장 간격
    [SerializeField] float _rewindDuration  = 5f;     // 최대 되감기 시간
    [SerializeField] float _rewindCooldown  = 30f;    // 능동 되감기 쿨타임

    int _bufferSize;  // _rewindDuration / _recordInterval
    FrameSnapshot[] _buffer;
    int _head;   // 가장 오래된 인덱스
    int _tail;   // 다음에 쓸 인덱스
    int _count;

    float _recordTimer;
    float _cooldownTimer;
    int   _autoRewindCharges = 1;  // 사망 Auto-Rewind 횟수

    PlayerController _player;
    WaveManager      _waveManager;

    public float CooldownRemaining { get { return _cooldownTimer; } }
    public float CooldownMax       { get { return _rewindCooldown; } }
    public int   AutoRewindCharges { get { return _autoRewindCharges; } }

    void Start()
    {
        _bufferSize = Mathf.CeilToInt(_rewindDuration / _recordInterval);
        _buffer     = new FrameSnapshot[_bufferSize];

        _player      = FindObjectOfType<PlayerController>();
        _waveManager = FindObjectOfType<WaveManager>();

        if (_player != null)
            _player.OnDead += OnPlayerDead;
    }

    void OnDestroy()
    {
        if (_player != null)
            _player.OnDead -= OnPlayerDead;
    }

    void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (Managers.Game.State != GameState.Playing) return;

        // 스냅샷 기록
        _recordTimer += Time.deltaTime;
        if (_recordTimer >= _recordInterval)
        {
            _recordTimer = 0f;
            Record();
        }

        // Shift 입력 — 능동 되감기
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            if (_cooldownTimer <= 0f && _count > 0)
                StartCoroutine(DoRewind());
        }
    }

    // ── 스냅샷 저장 ──────────────────────────────────────────

    void Record()
    {
        FrameSnapshot frame = new FrameSnapshot
        {
            player  = CapturePlayer(),
            enemies = CaptureEnemies(),
            wave    = CaptureWave(),
        };

        _buffer[_tail] = frame;
        _tail = (_tail + 1) % _bufferSize;

        if (_count < _bufferSize)
            _count++;
        else
            _head = (_head + 1) % _bufferSize;  // 가득 찼으면 가장 오래된 것 덮어씀
    }

    PlayerSnapshot CapturePlayer()
    {
        return new PlayerSnapshot
        {
            position     = _player.transform.position,
            hp           = _player.Hp,
            exp          = _player.Exp,
            level        = _player.Level,
            expToNextLevel = _player.ExpToNextLevel,
            speed        = _player.Speed,
            maxHp        = _player.MaxHp,
        };
    }

    EnemySnapshot[] CaptureEnemies()
    {
        EnemyBase[] allEnemies = FindObjectsOfType<EnemyBase>(true);  // 비활성 포함
        EnemySnapshot[] snapshots = new EnemySnapshot[allEnemies.Length];

        for (int i = 0; i < allEnemies.Length; i++)
        {
            EnemyBase e = allEnemies[i];
            EnemyMover mover = e as EnemyMover;
            snapshots[i] = new EnemySnapshot
            {
                enemy          = e,
                prefab         = e.OriginPrefab,
                position       = e.transform.position,
                hp             = e.Hp,
                speed          = e.Speed,
                attackCooldown = mover != null ? mover.AttackCooldown : 0f,
                isActive       = e.gameObject.activeSelf,
            };
        }

        return snapshots;
    }

    WaveSnapshot CaptureWave()
    {
        return new WaveSnapshot
        {
            gameTime   = _waveManager.GameTime,
            spawnTimer = _waveManager.SpawnTimer,
            waveIndex  = _waveManager.WaveIndex,
        };
    }

    // ── 되감기 실행 ──────────────────────────────────────────

    void OnPlayerDead()
    {
        if (_autoRewindCharges > 0 && _count > 0)
        {
            _autoRewindCharges--;
            StartCoroutine(DoRewind());
        }
        else
        {
            Managers.Game.State = Define.GameState.GameOver;
        }
    }

    IEnumerator DoRewind()
    {
        Managers.Game.State = GameState.Rewinding;

        // 가장 오래된 것부터 최신 순으로 스냅샷 인덱스 배열 생성
        int[] indices = new int[_count];
        for (int i = 0; i < _count; i++)
            indices[i] = (_head + i) % _bufferSize;

        // 역순으로 재생 (최신 → 과거)
        for (int i = _count - 1; i >= 0; i--)
        {
            FrameSnapshot frame = _buffer[indices[i]];

            ApplyPlayer(frame.player);
            ApplyEnemies(frame.enemies);
            ApplyWave(frame.wave);

            yield return new WaitForSecondsRealtime(_recordInterval);
        }

        _count = 0;
        _head  = 0;
        _tail  = 0;
        _cooldownTimer = _rewindCooldown;

        Managers.Game.State = GameState.Playing;
    }

    void ApplyPlayer(PlayerSnapshot s)
    {
        _player.RestoreSnapshot(s);
    }

    void ApplyEnemies(EnemySnapshot[] snapshots)
    {
        foreach (EnemySnapshot s in snapshots)
        {
            if (s.enemy == null) continue;

            if (s.isActive && !s.enemy.gameObject.activeSelf)
            {
                // 죽어있던 적 부활
                GameObject obj = Managers.Object.Get(s.prefab);
                // Get이 새 오브젝트를 줄 수 있으므로 s.enemy 직접 활성화
                s.enemy.gameObject.SetActive(true);
                EnemyInstanceRenderer.Register(s.enemy, s.prefab);
                SpatialHashGrid.Instance?.Add(s.enemy);
            }
            else if (!s.isActive && s.enemy.gameObject.activeSelf)
            {
                // 살아있던 적 제거
                EnemyInstanceRenderer.Unregister(s.enemy, s.prefab);
                SpatialHashGrid.Instance?.Remove(s.enemy);
                s.enemy.gameObject.SetActive(false);
            }

            if (s.enemy.gameObject.activeSelf)
                s.enemy.RestoreSnapshot(s);
        }
    }

    void ApplyWave(WaveSnapshot s)
    {
        _waveManager.RestoreSnapshot(s);
    }
}
