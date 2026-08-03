using UnityEngine;
using TMPro;
using static Define;

public class GameScene : MonoBehaviour
{
    [SerializeField] TMP_Text _gameOverText;

    // 타이틀의 두 버튼이 모두 값을 명시적으로 설정한다(START=false / BOSS RUSH=true).
    // 소비형 의미론이 아니므로 잔류 상태로 다음 판이 오염될 여지가 없다
    public static bool BossRush;

    void Awake()
    {
        Time.timeScale = 1f;
        Managers.Init();
        Managers.Game.State = GameState.Playing;
        Managers.Game.Score = 0;
        Managers.Game.RunGold = 0;
        Managers.Game.RunKills = 0;
        Managers.Game.RunRewinds = 0;
        Managers.Game.RunBossKills = 0;
    }

    void Start()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        CameraController cam = FindObjectOfType<CameraController>();

        if (player == null) { Debug.LogError("[GameScene] Player not found"); return; }
        if (cam == null) { Debug.LogError("[GameScene] CameraController not found"); return; }

        cam.SetTarget(player.transform);
        Managers.Game.OnStateChanged += OnGameStateChanged;

        if (_gameOverText != null)
            _gameOverText.gameObject.SetActive(false);
    }

    void OnGameStateChanged(GameState state)
    {
        if (_gameOverText == null) return;
        _gameOverText.gameObject.SetActive(state == GameState.GameOver);
    }

    void OnDestroy()
    {
        Managers.Game.OnStateChanged -= OnGameStateChanged;
    }
}
