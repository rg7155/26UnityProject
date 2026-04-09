using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float _speed = 8f;

    int _damage;
    Vector2 _dir;
    float _range;
    Vector3 _startPos;

    public void Init(Vector2 dir, int damage, float range)
    {
        _dir = dir;
        _damage = damage;
        _range = range;
        _startPos = transform.position;
    }

    void Update()
    {
        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);

        // 사거리 초과 시 비활성화 (추후 Pool 반납으로 교체)
        if (Vector3.Distance(transform.position, _startPos) >= _range)
            gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;

        enemy.OnDamaged(_damage);
        gameObject.SetActive(false);  // 추후 Pool 반납으로 교체
    }
}
