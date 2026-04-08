using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] GameObject _enemyPrefab;
    [SerializeField] float _spawnInterval = 1.5f;
    [SerializeField] float _spawnRadius = 10f;

    Transform _player;

    void Start()
    {
        _player = FindObjectOfType<PlayerController>()?.transform;
        if (_player == null)
        {
            Debug.LogError("[EnemySpawner] Player not found");
            return;
        }

        InvokeRepeating(nameof(Spawn), 1f, _spawnInterval);
    }

    void Spawn()
    {
        if (_player == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = _player.position + (Vector3)(randomDir * _spawnRadius);

        GameObject obj = Instantiate(_enemyPrefab, spawnPos, Quaternion.identity);
        obj.GetComponent<EnemyBase>().Init(_player);
    }
}