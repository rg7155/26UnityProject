using UnityEngine;

// 위치 계산 + 적 생성만 담당
// 타이밍/웨이브 제어는 WaveManager가 책임
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] float _spawnRadius = 10f;

    Transform _player;

    void Start()
    {
        _player = FindObjectOfType<PlayerController>()?.transform;
        if (_player == null)
            Debug.LogError("[EnemySpawner] Player not found");
    }

    public void Spawn(GameObject prefab, int hp, float speed)
    {
        if (_player == null || prefab == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = _player.position + (Vector3)(randomDir * _spawnRadius);

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        obj.GetComponent<EnemyBase>().Init(_player, hp, speed);
    }
}
