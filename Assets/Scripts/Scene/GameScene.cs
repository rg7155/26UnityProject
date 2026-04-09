using UnityEngine;
using TMPro;

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
        player.OnDead += OnGameOver;

        if (_gameOverText != null)
            _gameOverText.gameObject.SetActive(false);
    }

    void OnGameOver()
    {
        if (_gameOverText != null)
            _gameOverText.gameObject.SetActive(true);

        Debug.Log("[GameScene] Game Over");
    }
}
