using UnityEngine;
using UnityEngine.InputSystem;
using static Define;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float _speed = 5f;
    [SerializeField] int _maxHp = 100;
    [SerializeField] float _invincibleDuration = 1f;

    int _hp;
    float _invincibleTimer;

    GameObject _damageTextPrefab;

    // 경험치 / 레벨
    int _exp;
    int _level = 1;
    int _expToNextLevel = 10;

    public int Hp
    {
        get { return _hp; }
        private set
        {
            _hp = Mathf.Clamp(value, 0, _maxHp);
            OnHpChanged?.Invoke(_hp, _maxHp);
        }
    }

    public int MaxHp   { get { return _maxHp; } }
    public float Speed { get { return _speed; } }
    public int Level   { get { return _level; } }
    public int Exp     { get { return _exp; } }
    public int ExpToNextLevel { get { return _expToNextLevel; } }
    public bool IsInvincible  { get { return _invincibleTimer > 0f; } }

    public System.Action<int, int> OnHpChanged;     // (current, max)
    public System.Action<int, int> OnExpChanged;    // (current, expToNext)
    public System.Action<int> OnLevelUp;            // (newLevel)
    public System.Action OnDead;

    CreatureState _state = CreatureState.Idle;
    public CreatureState State { get { return _state; } }

    void Start()
    {
        Hp = _maxHp;
        _damageTextPrefab = Resources.Load<GameObject>("UI/DamageText");
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

        if (_damageTextPrefab != null)
        {
            GameObject fx = Instantiate(_damageTextPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            fx.GetComponent<DamageText>().Init(damage, Color.red);
            Managers.Resource.Destroy(fx, 0.7f);
        }

        Hp -= damage;
        _invincibleTimer = _invincibleDuration;
        Managers.Sound.PlayEffect(SoundManager.PlayerHit);

        if (_hp <= 0)
            HandleDead();
    }

    public void AddExp(int exp)
    {
        _exp += exp;
        OnExpChanged?.Invoke(_exp, _expToNextLevel);

        if (_exp >= _expToNextLevel)
            HandleLevelUp();
    }

    void HandleLevelUp()
    {
        _exp -= _expToNextLevel;
        _level++;
        _expToNextLevel = Mathf.RoundToInt(_expToNextLevel * 1.3f); // 레벨마다 요구 경험치 30% 증가

        OnExpChanged?.Invoke(_exp, _expToNextLevel);  // ExpBar 리셋용
        OnLevelUp?.Invoke(_level);  // Pause는 구독자(UpgradeManager)가 담당
    }

    void HandleDead()
    {
        _state = CreatureState.Dead;
        OnDead?.Invoke();  // RewindManager가 구독 — Auto-Rewind 없으면 GameOver 처리
    }

    // --- 업그레이드 적용 메서드 ---
    public void UpgradeSpeed(float multiplier)  { _speed *= multiplier; }
    public void UpgradeMaxHp(int amount)
    {
        _maxHp += amount;
        Hp += amount;   // 즉시 회복 포함
    }

    // --- Rewind 복원 ---
    public void RestoreSnapshot(PlayerSnapshot s)
    {
        _speed         = s.speed;
        _maxHp         = s.maxHp;
        _exp           = s.exp;
        _level         = s.level;
        _expToNextLevel = s.expToNextLevel;
        _state         = CreatureState.Idle;

        transform.position = s.position;

        _hp = Mathf.Clamp(s.hp, 0, _maxHp);
        OnHpChanged?.Invoke(_hp, _maxHp);
        OnExpChanged?.Invoke(_exp, _expToNextLevel);

        if (Managers.Game.State == Define.GameState.GameOver)
            Managers.Game.State = Define.GameState.Rewinding;
    }
}
