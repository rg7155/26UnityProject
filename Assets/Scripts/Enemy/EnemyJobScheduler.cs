using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

// Separation Steering Job 파이프라인 총괄.
// 매 프레임 EnemyBase.Registry를 NativeArray로 복사 -> SeparationJob 병렬 실행 ->
// 결과를 EnemyMover가 조회해 이동에 사용. SpatialHashGrid는 건드리지 않음.
public class EnemyJobScheduler : MonoBehaviour
{
    [SerializeField] float _separationRadius = 0.8f;  // EnemyMover와 동일 값
    [SerializeField] int   _batchSize = 32;

    static readonly ProfilerMarker _gatherMarker   = new ProfilerMarker("EnemyJob.Gather");
    static readonly ProfilerMarker _scheduleMarker = new ProfilerMarker("EnemyJob.Schedule");

    NativeArray<float2> _positions;
    NativeArray<int>    _entityIds;
    NativeArray<float2> _results;
    int _capacity;
    int _count;

    // EntityId -> 이번 프레임 배열 index
    Dictionary<int, int> _indexOf = new Dictionary<int, int>(1024);

    public static EnemyJobScheduler Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        EnsureCapacity(1024);
    }

    void OnDestroy()
    {
        DisposeArrays();
        if (Instance == this) Instance = null;
    }

    // 결과가 유효한 프레임인지(이번 프레임에 Job이 돌았는지)
    public bool HasResults { get; private set; }

    // EnemyMover가 자신의 EntityId로 분리 벡터 조회. 없으면 zero.
    public float2 GetSeparation(int entityId)
    {
        if (!HasResults) return float2.zero;
        if (_indexOf.TryGetValue(entityId, out int idx)) return _results[idx];
        return float2.zero;
    }

    void Update()
    {
        HasResults = false;

        if (Managers.Game.State != Define.GameState.Playing) return;

        var registry = EnemyBase.Registry;
        int n = registry.Count;
        if (n == 0) return;

        EnsureCapacity(n);

        // 1) Gather — Registry -> NativeArray
        using (_gatherMarker.Auto())
        {
            _indexOf.Clear();
            int i = 0;
            foreach (var kv in registry)
            {
                EnemyBase e = kv.Value;
                if (e == null || !e.gameObject.activeSelf) continue;
                Vector3 p = e.transform.position;
                _positions[i] = new float2(p.x, p.y);
                _entityIds[i] = e.EntityId;
                _indexOf[e.EntityId] = i;
                i++;
            }
            _count = i;
        }

        if (_count == 0) return;

        // 2) Schedule + Complete (이번 프레임 내 동기 완료)
        using (_scheduleMarker.Auto())
        {
            var job = new SeparationJob
            {
                Positions        = _positions,
                EntityIds        = _entityIds,
                Results          = _results,
                SeparationRadius = _separationRadius,
            };
            JobHandle handle = job.Schedule(_count, _batchSize);
            handle.Complete();
        }

        HasResults = true;
    }

    // NativeArray 용량 확보(부족할 때만 재할당). Schedule 길이를 _count로 제한.
    void EnsureCapacity(int needed)
    {
        if (_positions.IsCreated && _capacity >= needed) return;

        int newCap = Mathf.Max(needed, _capacity == 0 ? 1024 : _capacity * 2);
        DisposeArrays();

        _positions = new NativeArray<float2>(newCap, Allocator.Persistent);
        _entityIds = new NativeArray<int>(newCap, Allocator.Persistent);
        _results   = new NativeArray<float2>(newCap, Allocator.Persistent);
        _capacity  = newCap;
    }

    void DisposeArrays()
    {
        if (_positions.IsCreated) _positions.Dispose();
        if (_entityIds.IsCreated) _entityIds.Dispose();
        if (_results.IsCreated)   _results.Dispose();
        _capacity = 0;
    }
}
