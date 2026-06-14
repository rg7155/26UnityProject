using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

public class TitleScene : MonoBehaviour
{
    [SerializeField] Button _startButton;
    [SerializeField] TMP_Text _bestText;

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
    }

    void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveAllListeners();
    }
}
