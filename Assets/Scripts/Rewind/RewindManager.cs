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
        var registry = EnemyBase.Registry;
        EnemySnapshot[] snapshots = new EnemySnapshot[registry.Count];

        int i = 0;
        foreach (EnemyBase e in registry.Values)
        {
            EnemyMover mover = e as EnemyMover;
            snapshots[i++] = new EnemySnapshot
            {
                entityId       = e.EntityId,
                enemy          = e,
                prefab         = e.OriginPrefab,
                position       = e.transform.position,
                hp             = e.Hp,
                speed          = e.Speed,
                attackCooldown = mover != null ? mover.AttackCooldown : 0f,
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
        // 스냅샷 entityId 집합
        var snapshotIds = new HashSet<int>();
        foreach (var s in snapshots)
            snapshotIds.Add(s.entityId);

        // Case C: 현재 살아있지만 스냅샷에 없음 → 풀 반납
        var toRemove = new List<EnemyBase>(EnemyBase.Registry.Values);
        foreach (EnemyBase e in toRemove)
        {
            if (snapshotIds.Contains(e.EntityId)) continue;
            EnemyInstanceRenderer.Unregister(e, e.OriginPrefab);
            SpatialHashGrid.Instance?.Remove(e);
            EnemyBase.UnregisterForRewind(e.EntityId);
            Managers.Object.Return(e.gameObject, e.OriginPrefab);
        }

        // Case A / B: 스냅샷의 적 복원
        foreach (EnemySnapshot s in snapshots)
        {
            if (EnemyBase.Registry.ContainsKey(s.entityId))
            {
                // Case A: 현재 살아있음 → 상태만 복원
                EnemyBase enemy = EnemyBase.Registry[s.entityId];
                SpatialHashGrid.Instance?.Remove(enemy);
                enemy.RestoreSnapshot(s);
                SpatialHashGrid.Instance?.Add(enemy);
            }
            else
            {
                // Case B: 현재 죽어있음 → 부활
                if (s.enemy == null) continue;

                if (s.enemy.EntityId == s.entityId)
                {
                    // B1: Pool 재사용 없음 → 직접 활성화
                    s.enemy.gameObject.SetActive(true);
                    s.enemy.RestoreSnapshot(s);
                    EnemyBase.RegisterForRewind(s.enemy);
                    EnemyInstanceRenderer.Register(s.enemy, s.prefab);
                    SpatialHashGrid.Instance?.Add(s.enemy);
                }
                else
                {
                    // B2: Pool 재사용됨 → 새 오브젝트에 entityId 강제 주입
                    GameObject obj = Managers.Object.Get(s.prefab);
                    EnemyBase restored = obj.GetComponent<EnemyBase>();
                    restored.ForceRestore(s, _player.transform);
                    EnemyInstanceRenderer.Register(restored, s.prefab);
                    SpatialHashGrid.Instance?.Add(restored);
                }
            }
        }
    }

    void ApplyWave(WaveSnapshot s)
    {
        _waveManager.RestoreSnapshot(s);
    }
}
