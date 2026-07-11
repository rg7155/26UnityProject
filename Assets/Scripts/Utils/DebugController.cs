#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using static Define;

// 에디터 전용 치트 도구. 전체를 #if UNITY_EDITOR로 게이트해 릴리스 빌드에서 제외
public class DebugController : MonoBehaviour
{
    [SerializeField] WeaponData[] _debugWeapons;       // 1~5 / 0 키
    [SerializeField] GameObject _debugEnemyPrefab;     // K 키 스폰 대상
    [SerializeField] int _debugSpawnCount = 20;
    [SerializeField] int _debugEnemyHp = 30;
    [SerializeField] float _debugEnemySpeed = 2f;
    [SerializeField] int _debugLevelUpExp = 99999;
    [SerializeField] int _debugGold = 1000;

    WeaponManager _weaponManager;
    EnemySpawner _spawner;
    PlayerController _player;
    UpgradeManager _upgradeManager;

    void Start()
    {
        _weaponManager = FindObjectOfType<WeaponManager>();
        _spawner = FindObjectOfType<EnemySpawner>();
        _player = FindObjectOfType<PlayerController>();
        _upgradeManager = FindObjectOfType<UpgradeManager>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Paused(패널 표시)에서도 토글되도록 Playing 가드보다 앞에 처리
        if (kb.uKey.wasPressedThisFrame && _upgradeManager != null)
            _upgradeManager.DebugSkipUpgrades = !_upgradeManager.DebugSkipUpgrades;

        // Playing일 때만 치트 동작 — Rewinding/Paused 상태 머신과 충돌 방지
        if (Managers.Game.State != GameState.Playing) return;

        if (_weaponManager != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) GiveWeapon(0);
            if (kb.digit2Key.wasPressedThisFrame) GiveWeapon(1);
            if (kb.digit3Key.wasPressedThisFrame) GiveWeapon(2);
            if (kb.digit4Key.wasPressedThisFrame) GiveWeapon(3);
            if (kb.digit5Key.wasPressedThisFrame) GiveWeapon(4);

            if (kb.digit0Key.wasPressedThisFrame)
                for (int i = 0; i < _debugWeapons.Length; i++)
                    GiveWeapon(i);
        }

        if (kb.kKey.wasPressedThisFrame && _spawner != null)
            for (int n = 0; n < _debugSpawnCount; n++)
                _spawner.Spawn(_debugEnemyPrefab, _debugEnemyHp, _debugEnemySpeed);

        if (kb.lKey.wasPressedThisFrame && _player != null)
            _player.AddExp(_debugLevelUpExp);

        if (kb.gKey.wasPressedThisFrame && _player != null)
            _player.DebugInvincible = !_player.DebugInvincible;

        if (kb.mKey.wasPressedThisFrame)
        {
            Managers.Game.AddGold(_debugGold);
            Managers.Game.SaveGame();
        }
    }

    void GiveWeapon(int index)
    {
        if (index >= _debugWeapons.Length) return;
        WeaponData data = _debugWeapons[index];
        if (data == null || _weaponManager.HasWeapon(data)) return;
        _weaponManager.AddWeapon(data);
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.yellow;

        string invincible = _player != null && _player.DebugInvincible ? "ON" : "OFF";
        string skipUpgrades = _upgradeManager != null && _upgradeManager.DebugSkipUpgrades ? "ON" : "OFF";
        GUI.Label(new Rect(10, Screen.height - 120, 640, 30), "[Debug] 1~5: 무기  0: 전체무기  K: 적스폰  L: 레벨업  G: 무적  U: 업글패널  M: 골드+", style);
        GUI.Label(new Rect(10, Screen.height - 90, 600, 30), $"[Debug] 무적: {invincible}   업글억제: {skipUpgrades}", style);
    }
}
#endif
