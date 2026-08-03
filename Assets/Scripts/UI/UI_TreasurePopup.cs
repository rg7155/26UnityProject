using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

// 보물상자 슬롯 룰렛 팝업 — 연출만 담당한다. 결과는 TreasureReward.Roll이 먼저 뽑아 인자로 넘어온다
public class UI_TreasurePopup : MonoBehaviour
{
    enum SpinPhase { Hidden, Fast, Decel, Highlight }

    const float FastDuration = 0.6f;
    const float DecelDuration = 1.0f;
    const float HighlightDuration = 0.6f;
    const float FastCellsPerSec = 20f;
    const int DecelLaps = 2;

    [SerializeField] RectTransform _popupRoot;
    [SerializeField] Image[] _cells;            // 16, 테두리 시계방향
    [SerializeField] TMP_Text[] _cellLabels;    // 16, 동일 순서
    [SerializeField] RectTransform _marker;
    [SerializeField] TMP_Text _resultText;
    [SerializeField] Button _skipButton;

    SpinPhase _phase = SpinPhase.Hidden;
    TreasureRollResult _result;
    float _elapsed;
    int _markerIndex;
    int _spinStartIndex;
    int _decelStartIndex;
    int _decelSteps;
    bool _initialized;

    void Start()
    {
        Init();

        // 씬에 활성 상태로 저장돼 있어도 시작은 항상 숨김. 단 Open()이 켜서 Start가 지금 불린 경우엔
        // 다시 꺼버리면 안 되므로 Hidden일 때만 끈다
        if (_phase == SpinPhase.Hidden)
            gameObject.SetActive(false);
    }

    void Init()
    {
        if (_initialized) return;
        _initialized = true;

        // 표와 화면이 어긋날 수 없도록 라벨은 보상 테이블에서 직접 채운다
        if (_cellLabels != null)
        {
            int count = Mathf.Min(_cellLabels.Length, TreasureReward.SlotCount);
            for (int i = 0; i < count; i++)
                if (_cellLabels[i] != null)
                    _cellLabels[i].text = TreasureReward.Layout[i].ToString();
        }

        if (_skipButton != null)
            _skipButton.onClick.AddListener(OnClickSkip);
    }

    public void Open(TreasureRollResult result)
    {
        Init();

        _result = result;
        _elapsed = 0f;
        _phase = SpinPhase.Fast;
        _spinStartIndex = _markerIndex;

        if (_resultText != null)
            _resultText.text = string.Empty;

        Managers.Game.State = GameState.Paused;
        FindObjectOfType<VirtualJoystick>()?.Cancel();  // 재개 시 옛 터치 좌표로 튐 방지

        gameObject.SetActive(true);
        if (_marker != null)
            _marker.gameObject.SetActive(true);

        SetMarkerIndex(_markerIndex, false);
    }

    void Update()
    {
        if (_phase == SpinPhase.Hidden) return;

        // 이 프로젝트는 정지 경로가 둘이다(Paused만 거는 경로 / timeScale=0까지 거는 경로).
        // deltaTime을 쓰면 timeScale=0 경로가 붙는 순간 스핀이 영원히 안 멈춘다 — UIModalTween과 같은 이유
        _elapsed += Time.unscaledDeltaTime;

        switch (_phase)
        {
            case SpinPhase.Fast:
                SetMarkerIndex((_spinStartIndex + (int)(FastCellsPerSec * _elapsed)) % TreasureReward.SlotCount, true);
                if (_elapsed >= FastDuration)
                    BeginDecel();
                break;

            case SpinPhase.Decel:
                float t = Mathf.Clamp01(_elapsed / DecelDuration);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                SetMarkerIndex((_decelStartIndex + Mathf.RoundToInt(eased * _decelSteps)) % TreasureReward.SlotCount, true);
                if (t >= 1f)
                    BeginHighlight();
                break;

            case SpinPhase.Highlight:
                if (_marker != null)
                    _marker.gameObject.SetActive(Mathf.Repeat(_elapsed, 0.2f) > 0.1f);
                if (_elapsed >= HighlightDuration)
                    Close();
                break;
        }
    }

    void BeginDecel()
    {
        // ★ 결과 역산 — 이미 뽑힌 목표 칸에 정확히 착지하도록 총 이동 칸 수를 여기서 계산한다.
        // 멈춘 자리를 결과로 읽는 구조가 아니므로 스킵·프레임드랍이 결과를 바꾸지 못한다
        int forward = ((_result.slotIndex - _markerIndex) + TreasureReward.SlotCount) % TreasureReward.SlotCount;
        _decelStartIndex = _markerIndex;
        _decelSteps = DecelLaps * TreasureReward.SlotCount + forward;

        _elapsed = 0f;
        _phase = SpinPhase.Decel;
    }

    void BeginHighlight()
    {
        _markerIndex = _result.slotIndex;   // 다음 스핀의 시작점 — 마커가 순간이동하지 않는다

        Managers.Game.RunGold += _result.gold;   // 지급은 이 한 곳뿐. 연출 도중 씬이 바뀌어도 보상이 증발하지 않는다

        if (_resultText != null)
            _resultText.text = $"+{_result.gold} G";

        Managers.Sound.PlayEffect(SoundManager.Explosion);

        _elapsed = 0f;
        _phase = SpinPhase.Highlight;
    }

    void Close()
    {
        _phase = SpinPhase.Hidden;
        if (_marker != null)
            _marker.gameObject.SetActive(true);

        gameObject.SetActive(false);
        Managers.Game.State = GameState.Playing;
    }

    // Decel/Highlight 중 스킵은 무시 — 착지 순간을 못 보면 연출이 무의미하다
    void OnClickSkip()
    {
        if (_phase == SpinPhase.Fast)
            BeginDecel();
    }

    void SetMarkerIndex(int index, bool playTick)
    {
        if (index == _markerIndex && playTick) return;

        _markerIndex = index;

        if (_marker != null && _cells != null && index < _cells.Length && _cells[index] != null)
            _marker.position = _cells[index].rectTransform.position;

        if (playTick)
            Managers.Sound.PlayEffect(SoundManager.Shoot, 0.04f);
    }
}
