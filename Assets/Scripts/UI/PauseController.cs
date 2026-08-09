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
        // 정산은 사망 경로에만 있어, 중간 퇴장 시 그 판의 골드·기록·퀘스트 진행이 통째로 버려졌다.
        // Pause 는 Playing 중에만 열리므로 사망 정산과 겹쳐 이중 반영될 일은 없다.
        // BossRush 제외와 PlayTime 갱신은 사망 경로(RewindManager)와 동일한 규칙이다 —
        // PlayTime 은 여기서 넣지 않으면 이전 판의 낡은 값과 비교돼 최고 기록이 갱신되지 않는다.
        if (!GameScene.BossRush)
        {
            WaveManager wave = FindObjectOfType<WaveManager>();
            if (wave != null) Managers.Game.SaveData.PlayTime = wave.GameTime;
            Managers.Game.CommitResult();
        }

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
