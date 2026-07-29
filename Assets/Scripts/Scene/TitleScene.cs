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
    [SerializeField] Button _bossRushButton;

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

        // Stage 7(BOSS RUSH 모드)에서 _bossRushButton 리스너를 여기에 등록한다.
        // 필드를 먼저 만들어 두는 이유는 game-ui-artist가 생성기로 버튼을 만들어 연결해야 하기 때문
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
