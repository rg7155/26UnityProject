using UnityEngine;
using UnityEngine.InputSystem;
using static Define;

// 포트폴리오 빌드에서도 치트·계측이 필요해 릴리스에 포함한다(에디터 전용 게이트 없음).
// 디버그 오버레이 좌표는 이 클래스가 단독 소유한다 — GameScene이 따로 그리던 시절
// 상단 HUD 밴드와 겹쳤다.
public class DebugController : MonoBehaviour
{
    [SerializeField] WeaponData[] _debugWeapons;       // 1~5 / 0 키
    [SerializeField] GameObject _debugEnemyPrefab;     // K 키 스폰 대상
    [SerializeField] int _debugSpawnCount = 20;
    [SerializeField] int _debugEnemyHp = 30;
    [SerializeField] float _debugEnemySpeed = 2f;
    [SerializeField] int _debugLevelUpExp = 99999;
    [SerializeField] int _debugGold = 1000;

    // 릴리스 빌드를 심사자가 그대로 실행하므로 기본 숨김 — 치트키는 토글과 무관하게 항상 동작
    bool _showOverlay;

    WeaponManager _weaponManager;
    EnemySpawner _spawner;
    PlayerController _player;
    UpgradeManager _upgradeManager;
    TreasureSpawner _treasureSpawner;

    void Start()
    {
        _weaponManager = FindObjectOfType<WeaponManager>();
        _spawner = FindObjectOfType<EnemySpawner>();
        _player = FindObjectOfType<PlayerController>();
        _upgradeManager = FindObjectOfType<UpgradeManager>();
        _treasureSpawner = FindObjectOfType<TreasureSpawner>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Paused(패널 표시)에서도 토글되도록 Playing 가드보다 앞에 처리
        if (kb.f1Key.wasPressedThisFrame)
            _showOverlay = !_showOverlay;

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

        if (kb.tKey.wasPressedThisFrame && _treasureSpawner != null && _player != null)
            _treasureSpawner.SpawnAt(_player.transform.position + Vector3.up * 3f);

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
        if (!_showOverlay) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.yellow;

        // 세로 화면은 폭이 좁아 긴 한 줄이 자동 줄바꿈되며 아랫줄과 겹친다 — 치트 목록만 작게 두 줄로 나눈다
        GUIStyle small = new GUIStyle(style);
        small.fontSize = 13;

        float w = Screen.width - 20;

        // timeScale=0인 Pause·Rewind 중 deltaTime이 0이 되어 FPS가 Infinity로 찍힌다
        GUI.Label(new Rect(10, Screen.height - 190, 300, 26), $"Enemies: {EnemyBase.Registry.Count}", style);
        GUI.Label(new Rect(10, Screen.height - 164, 300, 26), $"FPS: {(1f / Time.unscaledDeltaTime):F0}", style);

        string invincible = _player != null && _player.DebugInvincible ? "ON" : "OFF";
        string skipUpgrades = _upgradeManager != null && _upgradeManager.DebugSkipUpgrades ? "ON" : "OFF";

        // OnGUI 내장 폰트에 한글 글리프가 없다 — 에디터는 시스템 폰트로 대체되지만
        // WebGL 빌드에는 동봉되지 않아 빈 칸으로 나온다. 그래서 영문으로 고정한다.
        GUI.Label(new Rect(10, Screen.height - 132, w, 22), "F1:Hide  1-5:Weapon  0:AllWeapons  K:Spawn", small);
        GUI.Label(new Rect(10, Screen.height - 110, w, 22), "L:LevelUp  G:Godmode  U:UpgradePanel  M:Gold+  T:Chest", small);
        GUI.Label(new Rect(10, Screen.height - 84, w, 26), $"Godmode: {invincible}   SkipUpgrades: {skipUpgrades}", style);
    }
}
