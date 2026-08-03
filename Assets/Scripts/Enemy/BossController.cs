using System.Collections.Generic;
using UnityEngine;

public enum BossActionState { Idle, Telegraph, Execute }

// 보스 — EnemyBase 직상속. HP/위치 되감기는 EnemyBase.Registry 경로로 자동 처리된다
//
// 스탯 원본은 BossData 하나다. Inspector 에 보이는 EnemyBase 의 _hp/_speed/_expReward/_goldReward 는
// 잡몹용 필드이고 보스에겐 Init 이 전부 _data 값으로 덮으므로 **무시해도 된다**(C# 은 부모 SerializeField 를
// 자식에서 숨길 수 없다). 보스 스탯 조정은 BossData 에셋에서만 한다.
public class BossController : EnemyBase
{
    [SerializeField] BossData       _data;
    [SerializeField] BossTelegraph  _telegraph;   // 예고(장판 원·방사탄 링·돌진 조준선). 비면 예고가 안 보인다
    [SerializeField] SpriteRenderer _sprite;      // 페이즈 색 전환용 — 프리팹 루트의 SpriteRenderer

    // 풀 부활 경로(ForceRestore)는 Init을 타지 않으므로 Instance를 Init에서 잡으면 안 된다
    public static BossController Instance { get; private set; }

    float _attackCooldown;
    bool  _leashing;

    BossActionState _actionState;
    int     _phase = 1;
    int     _sequenceIndex;
    float   _stateTimer;
    float   _cooldownTimer;
    Vector2 _lockedPoint;
    Vector2 _lockedDir;

    public BossData Data { get { return _data; } }

    public string BossName { get { return _data.bossName; } }

    // EnemyBase._originHp를 쓰면 안 된다 — ForceRestore가 _originHp = s.hp로 덮으므로
    // 되감기 한 번이면 최대치가 "그 시점 HP"로 줄어 HP 바가 항상 가득 차 보인다
    public int MaxHp { get { return _data.hp; } }

    public int Phase { get { return _phase; } }

    public override float AttackCooldown { get { return _attackCooldown; } }

    BossPatternType CurrentPattern { get { return _data.patternSequence[_sequenceIndex]; } }

    void OnEnable()  { Instance = this; }
    void OnDisable()
    {
        if (Instance == this) Instance = null;
        _telegraph?.Hide();
    }

    public override void Init(Transform target, GameObject originPrefab, int hp = -1, float speed = -1f)
    {
        // 스탯 원본은 BossData — WaveData/EnemySpawner가 넘긴 hp/speed는 무시한다
        base.Init(target, originPrefab, _data.hp, _data.moveSpeed);
        _expReward  = _data.expReward;
        _goldReward = _data.goldReward;
        _attackCooldown = 0f;
        _leashing = false;
        _actionState = BossActionState.Idle;
        _sequenceIndex = 0;
        _stateTimer = 0f;
        _cooldownTimer = 0f;
        _lockedDir = Vector2.zero;
        SetPhase(1);
        _telegraph?.Hide();
    }

    void Update()
    {
        if (_target == null) return;
        if (Managers.Game.State != Define.GameState.Playing) return;

        UpdatePhase();
        UpdatePattern();

        // 텔레그래프 중엔 정지 — 예고를 보고 피할 시간을 준다
        if (_actionState == BossActionState.Execute && CurrentPattern == BossPatternType.Charge)
            ChargeMove();
        else if (_actionState != BossActionState.Telegraph)
            Move();

        if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;
    }

    void Move()
    {
        Vector2 prevPos = transform.position;
        Vector2 toTarget = (Vector2)_target.position - prevPos;
        float distance = toTarget.magnitude;

        if (_leashing) { if (distance < _data.leashReturnDistance) _leashing = false; }
        else           { if (distance > _data.leashDistance)       _leashing = true;  }

        // separation을 조회하지 않는다 — Job 결과는 프레임 순서에 의존해 되감기 재현성을 깨뜨린다
        float speed = (_leashing ? _speed * _data.leashSpeedMult : _speed) * PhaseSpeedMult;
        transform.position += (Vector3)(toTarget.normalized * speed * Time.deltaTime);

        SpatialHashGrid.Instance?.Move(this, prevPos);
    }

    // 추적 이동과 달리 방향을 다시 계산하지 않는다 — 고정된 _lockedDir로만 직진한다
    void ChargeMove()
    {
        Vector2 prevPos = transform.position;
        transform.position += (Vector3)(_lockedDir * _data.chargeSpeed * Time.deltaTime);

        SpatialHashGrid.Instance?.Move(this, prevPos);
    }

    // ── Phase ──

    // 히스테리시스 없음 — HP 비율 단일 기준이어야 되감기가 같은 페이즈를 재현한다
    int EvaluatePhase()
    {
        if (_data.phase2HpRatio <= 0f) return 1;
        return (float)Hp / _data.hp <= _data.phase2HpRatio ? 2 : 1;
    }

    // 진입 연출은 여기서만 — RestoreBossState는 SetPhase로 상태만 조용히 맞춘다
    void UpdatePhase()
    {
        int phase = EvaluatePhase();
        if (phase == _phase) return;

        bool entering = phase > _phase;
        SetPhase(phase);

        if (entering)
            FindObjectOfType<CameraController>()?.Shake(0.4f, 0.4f);
    }

    void SetPhase(int phase)
    {
        _phase = phase;
        if (_sprite != null)
            _sprite.color = _phase == 2 ? _data.phase2Color : _data.phase1Color;
    }

    float PhaseCooldownMult { get { return _phase == 2 ? _data.phase2CooldownMult : 1f; } }

    // 배율 필드는 나중에 추가돼서 기존 에셋엔 0으로 들어온다 — 0이면 보스가 멈추거나 피해가 사라진다
    float PhaseSpeedMult { get { return _phase == 2 && _data.phase2SpeedMult > 0f ? _data.phase2SpeedMult : 1f; } }

    int PhaseDamage(int damage)
    {
        if (_phase != 2 || _data.phase2DamageMult <= 0f) return damage;
        return Mathf.RoundToInt(damage * _data.phase2DamageMult);
    }

    void UpdatePattern()
    {
        if (_data.patternSequence == null || _data.patternSequence.Length == 0) return;

        switch (_actionState)
        {
            case BossActionState.Idle:
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0f) BeginPattern();
                break;

            case BossActionState.Telegraph:
                _stateTimer -= Time.deltaTime;
                ShowTelegraph();
                if (_stateTimer <= 0f) ExecutePattern();
                break;

            case BossActionState.Execute:
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f) EndPattern();
                break;
        }
    }

    // 돌진 조준선만 매 프레임 플레이어를 다시 겨눈다 — 방향 고정은 텔레그래프 종료 시점이다
    void ShowTelegraph()
    {
        if (CurrentPattern == BossPatternType.Charge)
            _telegraph?.ShowLine(transform.position, AimDir(), ChargeDistance, TelegraphProgress());
        else
            _telegraph?.SetProgress(TelegraphProgress());
    }

    // 예고선 길이를 실제 이동 거리와 같게 둔다 — 예고와 궤도가 어긋나면 회피가 불공정해진다
    float ChargeDistance { get { return _data.chargeSpeed * _data.chargeDuration; } }

    Vector2 AimDir()
    {
        Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
        // 플레이어가 보스 중심에 겹치면 normalized가 0이라 돌진이 제자리에 멈춘다
        return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;
    }

    float TelegraphProgress()
    {
        float duration = TelegraphDuration();
        if (duration <= 0f) return 1f;
        return 1f - Mathf.Clamp01(_stateTimer / duration);
    }

    float TelegraphDuration()
    {
        switch (CurrentPattern)
        {
            case BossPatternType.GroundSlam:  return _data.slamTelegraph;
            case BossPatternType.RadialBurst: return _data.radialTelegraph;
            default:                          return _data.chargeTelegraph;
        }
    }

    // 방사탄은 피해 반경 개념이 없다 — 링은 순수 시각 예고라 데이터가 아닌 상수로 둔다.
    // 보스 본체 반경(스케일 4 × 스프라이트 1유닛 = 지름 4)보다 커야 링이 가려지지 않는다
    const float RadialRingRadius = 3.4f;

    float TelegraphRadius()
    {
        return CurrentPattern == BossPatternType.RadialBurst ? RadialRingRadius : _data.slamRadius;
    }

    void BeginPattern()
    {
        switch (CurrentPattern)
        {
            case BossPatternType.GroundSlam:
                // 텔레그래프 진입 순간의 플레이어 위치를 고정 — 이후 움직여도 예고원은 그 자리(결정성)
                _lockedPoint = _target.position;
                _actionState = BossActionState.Telegraph;
                _stateTimer  = _data.slamTelegraph;
                _telegraph?.Show(_lockedPoint, TelegraphRadius());
                break;

            case BossPatternType.RadialBurst:
                // 장판과 달리 예고 링의 중심은 보스 자신 — 발사 원점이 곧 예고 지점이다
                _lockedPoint = transform.position;
                _actionState = BossActionState.Telegraph;
                _stateTimer  = _data.radialTelegraph;
                _telegraph?.Show(_lockedPoint, TelegraphRadius());
                break;

            case BossPatternType.Charge:
                _actionState = BossActionState.Telegraph;
                _stateTimer  = _data.chargeTelegraph;
                ShowTelegraph();
                break;
        }
    }

    void ExecutePattern()
    {
        _telegraph?.Hide();
        _actionState = BossActionState.Execute;
        _stateTimer  = 0f;

        if (CurrentPattern == BossPatternType.Charge)
        {
            // 방향은 여기서 고정 — 이후 플레이어가 움직여도 궤도는 그대로다(되감기 재현성)
            _lockedDir  = AimDir();
            _stateTimer = _data.chargeDuration;
            return;
        }

        if (CurrentPattern == BossPatternType.RadialBurst)
        {
            FireRadialBurst();
            return;
        }

        if (CurrentPattern != BossPatternType.GroundSlam) return;

        PlayerController player = _target.GetComponent<PlayerController>();
        if (player != null && Vector2.Distance(_lockedPoint, _target.position) <= _data.slamRadius)
            player.OnDamaged(PhaseDamage(_data.slamDamage));

        Managers.Sound.PlayEffect(SoundManager.Explosion);
        FindObjectOfType<CameraController>()?.Shake(0.25f, 0.3f);
    }

    void FireRadialBurst()
    {
        GameObject prefab = _data.radialProjectilePrefab;
        if (prefab == null) return;

        // 첫 발을 플레이어 방향으로 두는 것이 각도의 유일한 기준 — Random 없이 결정적이다
        Vector2 toPlayer = (Vector2)_target.position - (Vector2)transform.position;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        int count = _phase == 2 ? _data.radialCountPhase2 : _data.radialCount;
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float rad = (baseAngle + step * i) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            GameObject go = Managers.Object.Get(prefab);
            go.transform.position = transform.position;
            go.transform.rotation = Quaternion.identity;
            go.GetComponent<BossProjectile>().Init(dir, _data.radialSpeed, PhaseDamage(_data.radialDamage), _data.radialLifetime, prefab);
        }

        Managers.Sound.PlayEffect(SoundManager.Shoot);
    }

    void EndPattern()
    {
        float cooldown;
        switch (CurrentPattern)
        {
            case BossPatternType.GroundSlam:  cooldown = _data.slamCooldown;   break;
            case BossPatternType.RadialBurst: cooldown = _data.radialCooldown; break;
            default:                          cooldown = _data.chargeCooldown; break;
        }

        // 세 패턴이 전부 이 경로를 지나므로 페이즈 2 단축은 여기 한 곳에서만 곱한다
        _cooldownTimer = cooldown * PhaseCooldownMult;

        _actionState = BossActionState.Idle;
        _stateTimer  = 0f;
        _sequenceIndex = (_sequenceIndex + 1) % _data.patternSequence.Length;
    }

    // Enter가 아니라 Stay — 보스 콜라이더가 커서 플레이어가 안에 머무는 시간이 길다.
    // Enter면 진입 순간 1회만 발생해 "붙어 있으면 안 아픈" 상태가 된다
    void OnTriggerStay2D(Collider2D other)
    {
        if (Managers.Game.State != Define.GameState.Playing) return;
        if (_attackCooldown > 0f) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        int damage = (_actionState == BossActionState.Execute && CurrentPattern == BossPatternType.Charge)
                   ? PhaseDamage(_data.chargeDamage) : PhaseDamage(_data.contactDamage);

        player.OnDamaged(damage);
        _attackCooldown = _data.contactInterval;
    }

    // 예고원만 화면에 남는 것을 막는다 — 풀 반납 경로(OnDisable)와 함께 이중으로 정리
    protected override void OnDead()
    {
        _telegraph?.Hide();

        // EXP/골드는 _expReward/_goldReward를 통해 base.OnDead가 지급한다 — 여기선 그 밖의 보상만.
        // Playing 게이팅은 EnemyBase.OnDead와 동일 — 되감기 리플레이 중 중복 지급 방지
        if (Managers.Game.State == Define.GameState.Playing)
        {
            Managers.Game.RunBossKills++;

            FindObjectOfType<TreasureSpawner>()?.SpawnAt(transform.position);

            RewindManager rewind = FindObjectOfType<RewindManager>();
            if (rewind != null)
            {
                rewind.AddAutoRewindCharge();
                rewind.ResetCooldown();
            }

            ClearMinions();
            BossProjectile.ClearAll();   // 죽은 보스가 쏜 탄이 남아 사후 피해를 주는 것을 막는다
        }

        base.OnDead();
    }

    // Registry를 직접 순회하면 OnDead가 딕셔너리를 수정해 터진다 — 복사본으로 순회
    void ClearMinions()
    {
        List<EnemyBase> minions = new List<EnemyBase>(Registry.Values);
        foreach (EnemyBase e in minions)
        {
            if (e == this) continue;
            e.OnDamaged(e.Hp);
        }
    }

    public override void RestoreSnapshot(EnemySnapshot s)
    {
        base.RestoreSnapshot(s);
        _attackCooldown = s.attackCooldown;
    }

    // ── Rewind ──

    public BossSnapshot CaptureState()
    {
        return new BossSnapshot
        {
            active        = true,
            entityId      = EntityId,
            phase         = _phase,
            sequenceIndex = _sequenceIndex,
            actionState   = (int)_actionState,
            stateTimer    = _stateTimer,
            cooldownTimer = _cooldownTimer,
            lockedPoint   = _lockedPoint,
            lockedDir     = _lockedDir,
        };
    }

    public void RestoreBossState(BossSnapshot s)
    {
        // 색만 되돌리고 화면 흔들림은 내지 않는다 — 진입 연출은 UpdatePhase의 HP 교차 전용이다
        SetPhase(s.phase);
        _sequenceIndex = s.sequenceIndex;
        _actionState   = (BossActionState)s.actionState;
        _stateTimer    = s.stateTimer;
        _cooldownTimer = s.cooldownTimer;
        _lockedPoint   = s.lockedPoint;
        _lockedDir     = s.lockedDir;

        // Update는 State != Playing이면 조기 리턴한다 — 되감기 중 예고원을 갱신할 곳이 여기뿐이다
        if (_actionState == BossActionState.Telegraph)
        {
            // 조준선은 Show가 필요 없다 — ShowTelegraph가 위치·방향·진행도를 한 번에 갱신한다
            if (CurrentPattern != BossPatternType.Charge)
                _telegraph?.Show(_lockedPoint, TelegraphRadius());
            ShowTelegraph();
        }
        else
        {
            _telegraph?.Hide();
        }
    }
}
