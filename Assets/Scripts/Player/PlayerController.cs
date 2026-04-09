using UnityEngine;
using UnityEngine.InputSystem;
using static Define;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float _speed = 5f;
    [SerializeField] int _maxHp = 100;
    [SerializeField] float _invincibleDuration = 1f;    // 피격 후 무적시간

    int _hp;
    float _invincibleTimer;

    public int Hp
    {
        get { return _hp; }
        private set
        {
            _hp = Mathf.Clamp(value, 0, _maxHp);
            OnHpChanged?.Invoke(_hp, _maxHp);
        }
    }

    public int MaxHp { get { return _maxHp; } }
    public float Speed { get { return _speed; } }
    public bool IsInvincible { get { return _invincibleTimer > 0f; } }

    // HP 변경 시 UI에 알림 (HpBar가 구독)
    public System.Action<int, int> OnHpChanged;
    public System.Action OnDead;

    CreatureState _state = CreatureState.Idle;
    public CreatureState State { get { return _state; } }

    void Start()
    {
        Hp = _maxHp;
    }

    void Update()
    {
        if (Managers.Game.State != GameState.Playing)
            return;

        if (_invincibleTimer > 0f)
            _invincibleTimer -= Time.deltaTime;

        HandleMove();
    }

    void HandleMove()
    {
        Vector2 dir = Vector2.zero;

        if (Keyboard.current != null)
        {
            float h = (Keyboard.current.dKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            float v = (Keyboard.current.wKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.sKey.isPressed ? 1f : 0f);

            dir = new Vector2(h, v).normalized;
        }

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
        if (IsInvincible) return;
        if (Managers.Game.State != GameState.Playing) return;

        Hp -= damage;
        _invincibleTimer = _invincibleDuration;

        if (_hp <= 0)
            HandleDead();
    }

    void HandleDead()
    {
        _state = CreatureState.Dead;
        Managers.Game.State = GameState.GameOver;
        OnDead?.Invoke();
    }
}
