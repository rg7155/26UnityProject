using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

// 적 타입별 GPU Instancing 렌더러
// SpriteRenderer 대신 DrawMeshInstanced로 같은 타입 적을 단일 Draw Call로 렌더링
public class EnemyInstanceRenderer : MonoBehaviour
{
    [SerializeField] GameObject _targetPrefab;  // 담당할 적 프리팹
    [SerializeField] Mesh _mesh;
    [SerializeField] Material _material;        // GPU Instancing 활성화 필요

    static readonly ProfilerMarker _buildMatricesMarker = new ProfilerMarker("EnemyInstanceRenderer.BuildMatrices");
    static readonly ProfilerMarker _drawMarker = new ProfilerMarker("EnemyInstanceRenderer.Draw");

    static Dictionary<GameObject, EnemyInstanceRenderer> _registry = new Dictionary<GameObject, EnemyInstanceRenderer>();

    List<EnemyBase> _enemies = new List<EnemyBase>();
    Matrix4x4[] _matrixBuffer = new Matrix4x4[1023];  // DrawMeshInstanced 최대 1023개

    void Awake()
    {
        if (_targetPrefab != null)
            _registry[_targetPrefab] = this;
    }

    void OnDestroy()
    {
        if (_targetPrefab != null)
            _registry.Remove(_targetPrefab);
    }

    public static void Register(EnemyBase enemy, GameObject prefab)
    {
        if (_registry.TryGetValue(prefab, out EnemyInstanceRenderer renderer))
            renderer._enemies.Add(enemy);
    }

    public static void Unregister(EnemyBase enemy, GameObject prefab)
    {
        if (_registry.TryGetValue(prefab, out EnemyInstanceRenderer renderer))
            renderer._enemies.Remove(enemy);
    }

    void LateUpdate()
    {
        if (_mesh == null || _material == null) return;

        int count = 0;
        using (_buildMatricesMarker.Auto())
        {
            foreach (EnemyBase enemy in _enemies)
            {
                if (enemy == null || !enemy.gameObject.activeSelf) continue;
                if (count >= 1023) break;

                // 피격 punch 는 연출이라 transform 이 아니라 여기서만 곱한다
                // (Collider 불변 + 좌우 반전의 localScale.x 부호와 충돌 없음)
                float punch = enemy.HitPunchScale;
                _matrixBuffer[count++] = punch == 1f
                    ? enemy.transform.localToWorldMatrix
                    : enemy.transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(punch, punch, 1f));
            }
        }

        if (count > 0)
        {
            using (_drawMarker.Auto())
                Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrixBuffer, count);
        }
    }
}
