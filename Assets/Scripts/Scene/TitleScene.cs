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

    // 오프닝 오버레이. Tools/UI/Build Opening Overlay 가 만든다.
    // 비어 있어도 타이틀은 정상 동작해야 한다 — 이 참조는 선택 사항이다.
    [SerializeField] OpeningSequence _opening;
    [SerializeField] TMP_Text _statusText;


    void Awake()
    {
        Managers.Init();
    }

    void Start()
    {
        if (_startButton != null)
            _startButton.onClick.AddListener(() =>
            {
                GameScene.BossRush = false;
                Managers.Scene.ChangeScene(SceneType.GameScene);
            });

        if (_bestText != null)
            _bestText.text = $"Best Score: {Managers.Game.BestScore}\nBest Time: {Managers.Game.BestTime:F1}s";

        if (_shopButton != null && _shopPanel != null)
            _shopButton.onClick.AddListener(() => _shopPanel.SetActive(true));

        if (_questButton != null && _questPanel != null)
            _questButton.onClick.AddListener(() => _questPanel.SetActive(true));

        if (_bossRushButton != null)
            _bossRushButton.onClick.AddListener(() =>
            {
                GameScene.BossRush = true;
                Managers.Scene.ChangeScene(SceneType.GameScene);
            });

        TryPlayOpening();
    }

    // 오프닝은 최초 1회만 재생한다.
    //
    // 오버레이는 타이틀 전체를 덮고 raycast 를 막는다. 여기서 잘못되면 심사자가 PLAY 버튼을
    // 누르지 못해 게임에 진입조차 못 한다 — 컴파일 에러보다 훨씬 나쁜 실패다.
    // 그래서 어떤 경로로 실패하든 오버레이를 끄고 타이틀로 떨어지게 만든다.
    void TryPlayOpening()
    {
        try
        {
            if (_opening == null)
            {
                ShowStatusLine();
                return;
            }

            if (Managers.Game.SeenOpening)
            {
                _opening.gameObject.SetActive(false);
                ShowStatusLine();
                return;
            }

            // 재생 시작 시점에 저장한다. 도중에 브라우저를 닫거나 오류가 나도
            // 다음 실행에서 다시 오프닝에 갇히지 않는다.
            Managers.Game.SeenOpening = true;

            if (_statusText != null)
                _statusText.gameObject.SetActive(false);

            _opening.OnFinished = ShowStatusLine;   // Play 보다 먼저 걸어야 한다. 즉시 종료하는 경로가 있다
            _opening.Play();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TitleScene] 오프닝 재생 실패 — 타이틀로 진행한다: {e}");
            if (_opening != null) _opening.gameObject.SetActive(false);
            ShowStatusLine();
        }
    }

    // P5 의 「세는 건 내가 할게」가 여기서 회수된다.
    // 이 줄이 없으면 오프닝의 마지막 약속이 지켜지지 않는다.
    void ShowStatusLine()
    {
        if (_statusText == null) return;

        _statusText.text = OpeningStatusLines.For(Managers.Game.SaveData.LifetimeRewinds);
        _statusText.gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveAllListeners();
        if (_shopButton != null)
            _shopButton.onClick.RemoveAllListeners();
        if (_questButton != null)
            _questButton.onClick.RemoveAllListeners();
        if (_bossRushButton != null)
            _bossRushButton.onClick.RemoveAllListeners();
        if (_opening != null)
            _opening.OnFinished = null;
    }
}
