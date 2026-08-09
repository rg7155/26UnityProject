using UnityEngine;

// 위치 계산 + 적 생성만 담당
// 타이밍/웨이브 제어는 WaveManager가 책임
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] float _spawnMargin = 2f;   // 화면 밖으로 이만큼 여유를 두고 스폰
    [SerializeField] float _fallbackRadius = 12f; // 직교 카메라가 없을 때 폴백

    // 후반 웨이브는 처치 속도보다 스폰이 빨라 개체가 무한히 쌓인다. 되감기 버퍼가 프레임당
    // 전 개체를 복사하므로 상한이 없으면 메모리·프레임이 함께 무너진다.
    const int MaxAliveEnemies = 300;

    Transform _player;
    Camera _cam;

    void Start()
    {
        _player = FindObjectOfType<PlayerController>()?.transform;
        if (_player == null)
            Debug.LogError("[EnemySpawner] Player not found");
        _cam = Camera.main;
    }

    public void Spawn(GameObject prefab, int hp, float speed)
    {
        if (_player == null || prefab == null) return;
        if (EnemyBase.Registry.Count >= MaxAliveEnemies) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = _player.position + (Vector3)(randomDir * SpawnRadius());

        GameObject obj = Managers.Object.Get(prefab);
        obj.transform.position = spawnPos;
        obj.GetComponent<EnemyBase>().Init(_player, prefab, hp, speed);
    }

    // 화면(직교 카메라) 코너 밖까지 거리 — 화면비·줌이 바뀌어도 항상 화면 밖에서 스폰
    float SpawnRadius()
    {
        if (_cam == null || !_cam.orthographic) return _fallbackRadius;
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;
        return Mathf.Sqrt(halfH * halfH + halfW * halfW) + _spawnMargin;
    }
}
