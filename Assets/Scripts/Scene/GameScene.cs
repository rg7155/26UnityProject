using UnityEngine;

public class GameScene : MonoBehaviour
{
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
    }
}
