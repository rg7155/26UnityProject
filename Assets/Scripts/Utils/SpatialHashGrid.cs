using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

// 월드를 고정 크기 셀로 분할해 적 위치를 관리
// 가장 가까운 적 탐색 시 인접 3×3 셀만 순회 — FindObjectsOfType 대체
public class SpatialHashGrid : MonoBehaviour
{
    [SerializeField] float _cellSize = 5f;

    static readonly ProfilerMarker _findNearestMarker = new ProfilerMarker("SpatialHash.FindNearest");
    static readonly ProfilerMarker _queryNeighborsMarker = new ProfilerMarker("SpatialHash.QueryNeighbors");

    Dictionary<(int, int), List<EnemyBase>> _grid = new Dictionary<(int, int), List<EnemyBase>>();

    public static SpatialHashGrid Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    // 월드 좌표 → 셀 인덱스
    (int, int) GetCell(Vector2 pos)
    {
        return (Mathf.FloorToInt(pos.x / _cellSize),
                Mathf.FloorToInt(pos.y / _cellSize));
    }

    // 적을 그리드에 등록
    public void Add(EnemyBase enemy)
    {
        var cell = GetCell(enemy.transform.position);
        if (!_grid.ContainsKey(cell))
            _grid[cell] = new List<EnemyBase>();
        _grid[cell].Add(enemy);
    }

    // 적을 그리드에서 제거
    public void Remove(EnemyBase enemy)
    {
        var cell = GetCell(enemy.transform.position);
        if (_grid.TryGetValue(cell, out List<EnemyBase> list))
            list.Remove(enemy);
    }

    // 이동 시 셀 갱신 (이전 셀 제거 → 새 셀 등록)
    public void Move(EnemyBase enemy, Vector2 prevPos)
    {
        var prevCell = GetCell(prevPos);
        var newCell  = GetCell(enemy.transform.position);

        if (prevCell == newCell) return;  // 같은 셀이면 갱신 불필요

        if (_grid.TryGetValue(prevCell, out List<EnemyBase> list))
            list.Remove(enemy);

        if (!_grid.ContainsKey(newCell))
            _grid[newCell] = new List<EnemyBase>();
        _grid[newCell].Add(enemy);
    }

    // 주어진 위치에서 가장 가까운 적 반환 (탐지 범위 내)
    public EnemyBase FindNearest(Vector2 origin, float detectRange)
    {
        using (_findNearestMarker.Auto())
        {
            var originCell = GetCell(origin);
            int searchRadius = Mathf.CeilToInt(detectRange / _cellSize);

            EnemyBase nearest = null;
            float minDistSq = detectRange * detectRange;  // 제곱 비교로 Sqrt 생략

            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    var cell = (originCell.Item1 + dx, originCell.Item2 + dy);
                    if (!_grid.TryGetValue(cell, out List<EnemyBase> list)) continue;

                    foreach (EnemyBase enemy in list)
                    {
                        if (enemy == null || !enemy.gameObject.activeSelf) continue;

                        float distSq = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                        if (distSq < minDistSq)
                        {
                            minDistSq = distSq;
                            nearest = enemy;
                        }
                    }
                }
            }

            return nearest;
        }
    }

    public void QueryNeighbors(EnemyBase self, Vector2 origin, float radius, List<EnemyBase> result)
    {
        using (_queryNeighborsMarker.Auto())
        {
            result.Clear();
            var originCell = GetCell(origin);
            int searchRadius = Mathf.CeilToInt(radius / _cellSize);
            float radiusSq = radius * radius;

            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    var cell = (originCell.Item1 + dx, originCell.Item2 + dy);
                    if (!_grid.TryGetValue(cell, out List<EnemyBase> list)) continue;

                    foreach (EnemyBase enemy in list)
                    {
                        if (enemy == null || !enemy.gameObject.activeSelf) continue;
                        if (enemy == self) continue;
                        if (((Vector2)enemy.transform.position - origin).sqrMagnitude < radiusSq)
                            result.Add(enemy);
                    }
                }
            }
        }
    }
}
