using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float _speed = 8f;

    int _damage;
    Vector2 _dir;
    float _range;
    Vector3 _startPos;
    GameObject _originPrefab;

    public void Init(Vector2 dir, int damage, float range, GameObject originPrefab)
    {
        _dir = dir;
        _damage = damage;
        _range = range;
        _startPos = transform.position;
        _originPrefab = originPrefab;
    }

    void Update()
    {
        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _startPos) >= _range)
            ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;

        enemy.OnDamaged(_damage);
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (!gameObject.activeSelf) return;  // 이미 반납됐으면 무시
        Managers.Object.Return(gameObject, _originPrefab);
    }
}
