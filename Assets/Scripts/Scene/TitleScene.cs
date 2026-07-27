using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

public class TitleScene : MonoBehaviour
{
    [SerializeField] Button _startButton;
    [SerializeField] TMP_Text _bestText;
    [SerializeField] Button _shopButton;
    [SerializeField] GameObject _shopPanel;
    [SerializeField] Button _questButton;
    [SerializeField] GameObject _questPanel;

    void Awake()
    {
        Managers.Init();
    }

    void Start()
    {
        if (_startButton != null)
            _startButton.onClick.AddListener(() => Managers.Scene.ChangeScene(SceneType.GameScene));

        if (_bestText != null)
            _bestText.text = $"Best Score: {Managers.Game.BestScore}\nBest Time: {Managers.Game.BestTime:F1}s";

        if (_shopButton != null && _shopPanel != null)
            _shopButton.onClick.AddListener(() => _shopPanel.SetActive(true));

        if (_questButton != null && _questPanel != null)
            _questButton.onClick.AddListener(() => _questPanel.SetActive(true));
    }

    void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveAllListeners();
        if (_shopButton != null)
            _shopButton.onClick.RemoveAllListeners();
        if (_questButton != null)
            _questButton.onClick.RemoveAllListeners();
    }
}
