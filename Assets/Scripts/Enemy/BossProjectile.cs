using System.Collections.Generic;
using UnityEngine;

// 보스 방사탄 — 되감기 스냅샷 대상이 아니라 되감기 시작 시 ClearAll()로 전부 소거된다
public class BossProjectile : MonoBehaviour
{
    // 풀 오브젝트라 Destroy를 거치지 않는다 — 씬 전환 훅 대신 OnEnable/OnDisable로 목록을 자체 관리
    static readonly List<BossProjectile> _active = new List<BossProjectile>();

    Vector2 _dir;
    float _speed;
    int _damage;
    float _lifetime;
    GameObject _originPrefab;

    public void Init(Vector2 dir, float speed, int damage, float lifetime, GameObject originPrefab)
    {
        _dir = dir;
        _speed = speed;
        _damage = damage;
        _lifetime = lifetime;
        _originPrefab = originPrefab;
    }

    void OnEnable()  { _active.Add(this); }
    void OnDisable() { _active.Remove(this); }

    void Update()
    {
        if (Managers.Game.State != Define.GameState.Playing) return;

        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);

        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
            ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Managers.Game.State != Define.GameState.Playing) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        player.OnDamaged(_damage);
        ReturnToPool();
    }

    // 역순 순회 — Return이 OnDisable을 타고 _active를 순회 도중 수정한다
    public static void ClearAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
            _active[i].ReturnToPool();
    }

    void ReturnToPool()
    {
        if (!gameObject.activeSelf) return;  // 이미 반납됐으면 무시
        Managers.Object.Return(gameObject, _originPrefab);
    }
}
