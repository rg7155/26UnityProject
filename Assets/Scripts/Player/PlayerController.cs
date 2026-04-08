using UnityEngine;
using UnityEngine.InputSystem;   // ← 추가
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
        // ▼ 변경된 부분
        Vector2 dir = Vector2.zero;

        if (Keyboard.current != null)
        {
            float h = (Keyboard.current.dKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            float v = (Keyboard.current.wKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.sKey.isPressed ? 1f : 0f);

            dir = new Vector2(h, v).normalized;
        }

        // 게임패드 지원이 필요하면 아래 주석 해제
        // if (Gamepad.current != null)
        //     dir = Gamepad.current.leftStick.ReadValue().normalized;
        // ▲ 변경된 부분

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

    public void OnDamaged(int damage)
    {
        // 추후 HP 시스템 연결
    }
}