using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

public class ResultScene : MonoBehaviour
{
    [SerializeField] Button _retryButton;
    [SerializeField] Button _titleButton;
    [SerializeField] TMP_Text _scoreText;
    [SerializeField] TMP_Text _timeText;
    [SerializeField] TMP_Text _bestScoreText;
    [SerializeField] TMP_Text _bestTimeText;
    [SerializeField] TMP_Text _goldText;

    void Awake()
    {
        Managers.Init();
    }

    void Start()
    {
        if (_scoreText != null) _scoreText.text = $"Score: {Managers.Game.Score}";
        if (_timeText != null) _timeText.text = $"Time: {Managers.Game.SaveData.PlayTime:F1}s";
        if (_bestScoreText != null) _bestScoreText.text = $"Best Score: {Managers.Game.BestScore}";
        if (_bestTimeText != null) _bestTimeText.text = $"Best Time: {Managers.Game.BestTime:F1}s";
        if (_goldText != null) _goldText.text = $"Gold +{Managers.Game.RunGold}";

        if (_retryButton != null)
            _retryButton.onClick.AddListener(() => Managers.Scene.ChangeScene(SceneType.GameScene));
        if (_titleButton != null)
            _titleButton.onClick.AddListener(() => Managers.Scene.ChangeScene(SceneType.Title));
    }

    void OnDestroy()
    {
        if (_retryButton != null)
            _retryButton.onClick.RemoveAllListeners();
        if (_titleButton != null)
            _titleButton.onClick.RemoveAllListeners();
    }
}
