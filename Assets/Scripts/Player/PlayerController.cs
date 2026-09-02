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

    VirtualJoystick _joystick;
    SpriteFrameAnimator _spriteAnimator;
    SpriteRenderer _sprite;

    public bool DebugInvincible;

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
    public System.Action OnHit;                     // 무적/상태 가드를 통과해 실제로 피해가 들어간 순간만
    public System.Action OnDead;

    CreatureState _state = CreatureState.Idle;
    public CreatureState State { get { return _state; } }

    void Start()
    {
        _maxHp += ShopService.BonusHp();   // 메타 상점 시작 HP 강화 적용
        _speed *= ShopService.MoveSpeedMult();
        Hp = _maxHp;
        _damageTextPrefab = Resources.Load<GameObject>("UI/DamageText");
        _joystick = FindObjectOfType<VirtualJoystick>();
        Transform visual = transform.Find("PlayerVisual");
        _spriteAnimator = visual != null
            ? visual.GetComponent<SpriteFrameAnimator>()
            : GetComponent<SpriteFrameAnimator>();
        _sprite = visual != null
            ? visual.GetComponent<SpriteRenderer>()
            : GetComponent<SpriteRenderer>();
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
        Vector2 input;

        if (_joystick != null)
        {
            input = _joystick.Input;  // 터치/키보드 OR + 아날로그 반영, magnitude 0~1 — normalize 금지
        }
        else
        {
            // 안전망: 조이스틱 오브젝트가 씬에 없을 때 WASD 직접 폴백 (항상 최대속도)
            input = Vector2.zero;
            if (Keyboard.current != null)
            {
                float h = (Keyboard.current.dKey.isPressed ? 1f : 0f)
                        - (Keyboard.current.aKey.isPressed ? 1f : 0f);
                float v = (Keyboard.current.wKey.isPressed ? 1f : 0f)
                        - (Keyboard.current.sKey.isPressed ? 1f : 0f);
                input = new Vector2(h, v).normalized;
            }
        }

        bool isMoving = input.sqrMagnitude > 0.0001f;  // 아날로그 조이스틱 미세 드리프트는 정지로 본다
        _spriteAnimator?.SetMoving(isMoving);

        if (isMoving)
        {
            _state = CreatureState.Moving;

            // 좌우 반전은 transform.localScale 이 아니라 SpriteRenderer.flipX 로 한다.
            // 스케일 반전은 자식까지 미러링해서, 플레이어 자식으로 붙는 Orbit 위성이
            // (OrbitWeapon.cs:44) 반전 순간 반대편으로 순간이동하고 회전 방향도 뒤집힌다.
            // flipX 는 스프라이트 렌더링에만 적용돼 transform 계층을 건드리지 않는다.
            // 적은 SpriteRenderer 가 없어(인스턴싱) localScale 을 쓸 수밖에 없지만,
            // 적 프리팹에는 자식이 없어 같은 문제가 생기지 않는다.
            if (input.x != 0f && _sprite != null)
                _sprite.flipX = input.x < 0f;   // 아트는 오른쪽을 보는 것이 기본이다

            transform.position += (Vector3)(input * _speed * Time.deltaTime);
        }
        else
        {
            _state = CreatureState.Idle;
        }
    }

    public void OnDamaged(int damage)
    {
        if (DebugInvincible) return;
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
        CameraController.Instance?.Shake(0.18f, 0.22f);   // HitVignette 와 함께 피격을 체감시킨다
        Managers.Sound.PlayEffect(SoundManager.PlayerHit);
        OnHit?.Invoke();

        if (_hp <= 0)
            HandleDead();
    }

    // 외부에서 무적 부여 — 피격 무적이 더 길게 남아있으면 깎지 않는다
    public void GrantInvincible(float duration)
    {
        if (duration > _invincibleTimer)
            _invincibleTimer = duration;
    }

    public void AddExp(int exp)
    {
        _exp += exp;
        OnExpChanged?.Invoke(_exp, _expToNextLevel);

        while (_exp >= _expToNextLevel)  // 한 번의 AddExp가 여러 레벨 경계를 넘을 수 있음
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
