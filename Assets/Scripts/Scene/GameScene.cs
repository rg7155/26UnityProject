using UnityEngine;
using TMPro;
using static Define;

public class GameScene : MonoBehaviour
{
    [SerializeField] TMP_Text _gameOverText;

    void Awake()
    {
        Time.timeScale = 1f;
        Managers.Init();
        Managers.Game.State = GameState.Playing;
        Managers.Game.Score = 0;
        Managers.Game.RunGold = 0;
        Managers.Game.RunKills = 0;
        Managers.Game.RunRewinds = 0;
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

    // 성능 측정용 — 측정 완료 후 삭제
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(10, 10, 300, 35), $"Enemies: {EnemyBase.Registry.Count}", style);
        GUI.Label(new Rect(10, 50, 300, 35), $"FPS: {(1f / Time.deltaTime):F0}", style);
    }
}
