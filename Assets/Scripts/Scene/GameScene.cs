using UnityEngine;
using TMPro;
using static Define;

public class GameScene : MonoBehaviour
{
    [SerializeField] TMP_Text _gameOverText;

    void Awake()
    {
        Managers.Init();
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
