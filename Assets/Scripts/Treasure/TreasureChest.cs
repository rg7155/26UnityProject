using UnityEngine;
using static Define;

// 필드에 놓이는 보물상자. 수명과 거리만 안다 — 롤·팝업·사운드는 TreasureSpawner가 담당한다
public class TreasureChest : MonoBehaviour
{
    [SerializeField] float _lifetime = 25f;
    [SerializeField] float _blinkStart = 5f;      // 남은 수명이 이 값 이하면 깜빡임
    [SerializeField] float _pickupRadius = 0.6f;
    [SerializeField] SpriteRenderer _sprite;

    TreasureSpawner _spawner;
    Transform _player;
    float _remain;

    public void Init(TreasureSpawner spawner, Transform player)
    {
        _spawner = spawner;
        _player = player;
        _remain = _lifetime;
    }

    void Update()
    {
        // 시간 누적·획득 판정은 항상 Playing 아래 — Rewinding 리플레이 중 오획득과 수명 누수를 막는다
        if (Managers.Game.State != GameState.Playing) return;

        _remain -= Time.deltaTime;
        if (_remain <= 0f)
        {
            _spawner.OnChestExpired(this);
            Destroy(gameObject);
            return;
        }

        if (_sprite != null)
            _sprite.enabled = _remain > _blinkStart || Mathf.Repeat(_remain, 0.3f) > 0.15f;

        if (_player == null) return;

        // 상자 획득 여부는 되돌아가지 않는다 — 스냅샷에 넣지 않는 것이 곧 그 규칙의 구현이다
        // (BossProjectile.ClearAll 과 동급의 되감기 예외. 되감아도 먹은 상자는 부활하지 않는다)
        Vector2 delta = _player.position - transform.position;
        if (delta.sqrMagnitude <= _pickupRadius * _pickupRadius)
        {
            _spawner.OnChestCollected(this);
            Destroy(gameObject);
        }
    }
}
