using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static Define;

// Pause 오버레이 루트에 부착. 자기등록.
// 정지는 GameState.Paused(로직 가드) + Time.timeScale=0(애니/파티클) 병행.
public class PauseController : MonoBehaviour
{
    [SerializeField] GameObject _panelRoot;
    [SerializeField] Button _resumeButton;
    [SerializeField] Button _titleButton;
    [SerializeField] Button _pauseButton;

    bool _isPaused;

    void Start()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);

        if (_resumeButton != null) _resumeButton.onClick.AddListener(Resume);
        if (_titleButton != null)  _titleButton.onClick.AddListener(GoTitle);
        if (_pauseButton != null)  _pauseButton.onClick.AddListener(Pause);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    void Toggle()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (Managers.Game == null || Managers.Game.State != GameState.Playing) return;

        FindObjectOfType<VirtualJoystick>()?.Cancel();  // 재개 시 터치 튐 방지
        Managers.Game.State = GameState.Paused;
        Time.timeScale = 0f;
        if (_panelRoot != null) _panelRoot.SetActive(true);
        _isPaused = true;
    }

    public void Resume()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);
        Time.timeScale = 1f;
        if (Managers.Game != null) Managers.Game.State = GameState.Playing;
        _isPaused = false;
    }

    public void GoTitle()
    {
        Time.timeScale = 1f;  // SceneManagerEx.ChangeScene은 timeScale 복원 안 함
        Managers.Scene.ChangeScene(SceneType.Title);
    }

    void OnDestroy()
    {
        if (_resumeButton != null) _resumeButton.onClick.RemoveAllListeners();
        if (_titleButton != null)  _titleButton.onClick.RemoveAllListeners();
        if (_pauseButton != null)  _pauseButton.onClick.RemoveAllListeners();
    }
}
