using UnityEngine;
using static Define;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float _speed = 5f;

    public float Speed { get { return _speed; } }

    CreatureState _state = CreatureState.Idle;
    public CreatureState State { get { return _state; } }

    void Update()
    {
        if (Managers.Game.State != GameState.Playing)
            return;

        HandleMove();
    }

    void HandleMove()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 dir = new Vector2(h, v).normalized;

        if (dir != Vector2.zero)
        {
            _state = CreatureState.Moving;
            transform.position += (Vector3)(dir * _speed * Time.deltaTime);
        }
        else
        {
            _state = CreatureState.Idle;
        }
    }

    // 외부에서 HP 등 추가 시 여기서 확장
    public void OnDamaged(int damage)
    {
        // 추후 HP 시스템 연결
    }
}
