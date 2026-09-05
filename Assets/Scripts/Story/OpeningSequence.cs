using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 오프닝 시퀀스 재생. 타이틀 씬 위에 덮이는 풀스크린 오버레이로 동작한다.
//
// 별도 씬을 쓰지 않는 이유: 씬을 늘리면 EditorBuildSettings·Define.SceneType·씬 라우팅이
// 따라오는데, 배경 5장은 씬을 나눌 만한 메모리 경계가 아니다. 상용 모바일 프로젝트가
// 컷씬을 오버레이나 Additive 프리팹으로 처리하는 것과 같은 이유다.
//
// 계층은 Tools/UI/Build Opening Overlay 생성기가 만든다.
[RequireComponent(typeof(CanvasGroup))]
public class OpeningSequence : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] OpeningData _data;
    [SerializeField] Image _background;
    [SerializeField] TMP_Text _speakerLabel;
    [SerializeField] TypewriterText _body;
    [SerializeField] Button _skipButton;
    [SerializeField] Image[] _pageDots;
    [SerializeField] KenBurns _kenBurns;
    [SerializeField] OpeningBlinkDot _blinkDot;

    // 재생이 끝났을 때(끝까지 봤든 스킵했든) 호출된다. 타이틀이 여기서 오버레이를 끈다.
    public System.Action OnFinished;

    CanvasGroup _group;
    int _pageIndex;
    int _lineIndex;
    float _lineHoldTimer;
    bool _finished;
    Coroutine _fade;

    void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        if (_skipButton != null)
            _skipButton.onClick.AddListener(Finish);
    }

    void OnDestroy()
    {
        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(Finish);
    }

    public void Play()
    {
        // 오버레이는 평소 꺼져 있다. 활성화가 먼저여야 Awake 가 돌아 _group 이 잡힌다.
        gameObject.SetActive(true);

        if (_data == null || _data.pages == null || _data.pages.Length == 0)
        {
            Debug.LogWarning("[OpeningSequence] OpeningData 가 비어 있다. 즉시 종료한다");
            Finish();
            return;
        }

        _finished = false;
        _pageIndex = 0;
        _lineIndex = 0;
        _group.alpha = 1f;
        _group.blocksRaycasts = true;

        ShowPage(0, instant: true);
    }

    void Update()
    {
        if (_finished || _body == null) return;

        // 페이지 전환(암전) 중에는 진행하지 않는다.
        // CrossFade 는 암전 절반이 지난 뒤에야 ShowLine 을 부르는데, 그 사이 _body 는
        // 직전 줄을 끝낸 상태 그대로다. 막지 않으면 같은 조건이 계속 참이라 Advance 가
        // 반복 호출되어, 줄을 건너뛰고 타자기가 도중에 처음부터 다시 찍힌다.
        if (_fade != null) return;

        // 타자기가 끝난 뒤에만 자동 진행 타이머가 흐른다.
        if (!_body.IsComplete) return;

        _lineHoldTimer += Time.unscaledDeltaTime;
        if (_lineHoldTimer >= _data.autoAdvanceDelay)
            Advance();
    }

    // 화면 전체가 탭 영역이다.
    //   진행 중 → 즉시 전문 표시
    //   완료 후 → 다음 줄
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_finished || _body == null) return;
        if (_fade != null) return;   // 암전 중 탭도 같은 이유로 막는다

        if (!_body.IsComplete)
        {
            _body.Complete();
            _lineHoldTimer = 0f;
            return;
        }

        Advance();
    }

    void Advance()
    {
        OpeningPage page = _data.pages[_pageIndex];

        if (_lineIndex + 1 < page.lines.Length)
        {
            _lineIndex++;
            ShowLine();
            return;
        }

        if (_pageIndex + 1 < _data.pages.Length)
        {
            ShowPage(_pageIndex + 1, instant: false);
            return;
        }

        Finish();
    }

    void ShowPage(int index, bool instant)
    {
        _pageIndex = index;
        _lineIndex = 0;
        _lineHoldTimer = 0f;   // 전환 직후 자동 진행이 곧바로 다시 걸리지 않게 한다

        UpdateDots();

        if (instant)
        {
            ApplyBackground();
            ShowLine();
            return;
        }

        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(CrossFade());
    }

    // 배경 전환은 암전을 거친다. 두 장을 겹쳐 섞는 것보다 단순하고,
    // 어두운 팔레트라 암전이 톤에도 맞는다.
    IEnumerator CrossFade()
    {
        float half = Mathf.Max(0.01f, _data.pageFadeDuration * 0.5f);

        yield return FadeBackground(1f, 0f, half);
        ApplyBackground();
        ShowLine();
        yield return FadeBackground(0f, 1f, half);

        _fade = null;
    }

    IEnumerator FadeBackground(float from, float to, float duration)
    {
        if (_background == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;   // 정지 경로가 둘이라 unscaled 를 쓴다
            SetBackgroundAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetBackgroundAlpha(to);
    }

    void SetBackgroundAlpha(float a)
    {
        Color c = _background.color;
        c.a = a;
        _background.color = c;

        // 표시등은 배경의 자식이지만 알파는 따로 계산하므로 암전에 같이 묻히지 않는다.
        // 배경 알파를 그대로 넘겨 한 덩어리로 사라지게 한다.
        if (_blinkDot != null) _blinkDot.MasterAlpha = a;
    }

    void ApplyBackground()
    {
        if (_background == null) return;

        OpeningPage page = _data.pages[_pageIndex];

        _background.sprite = page.background;
        // 배경이 아직 없어도(아트 생성 전) 대사와 흐름은 검증할 수 있어야 한다.
        _background.enabled = page.background != null;

        if (_kenBurns != null)
            _kenBurns.Play(page.zoomFrom, page.zoomTo, page.panFrom, page.panTo, PageDuration(page));

        if (_blinkDot != null)
        {
            if (page.blink && page.background != null) _blinkDot.Show(page.blinkAnchor);
            else _blinkDot.Hide();
        }
    }

    // 이 페이지가 화면에 머무는 예상 시간. 켄 번즈가 페이지가 넘어가는 순간에
    // 정확히 끝나도록 맞춘다. 탭으로 먼저 넘기면 도중에 끊기는데, 그 편이
    // 고정 길이를 써서 중간에 멈춰 버리는 것보다 낫다.
    float PageDuration(OpeningPage page)
    {
        if (page.lines == null) return 1f;

        float total = 0f;
        foreach (OpeningLine line in page.lines)
            total += line.text.Length / _data.charsPerSecond + _data.autoAdvanceDelay;

        return Mathf.Max(1f, total);
    }

    void ShowLine()
    {
        OpeningLine line = _data.pages[_pageIndex].lines[_lineIndex];

        if (_speakerLabel != null)
        {
            _speakerLabel.text = line.speaker.ToString();
            _speakerLabel.color = SpeakerColor(line.speaker);
        }

        _body.Play(line.text, _data.charsPerSecond);
        _lineHoldTimer = 0f;
    }

    static Color SpeakerColor(Speaker speaker)
    {
        switch (speaker)
        {
            case Speaker.ARC:      return UITheme.Cyan;
            case Speaker.MONOLITH: return UITheme.Danger;
            default:               return UITheme.TextPrimary;
        }
    }

    // 남은 길이가 보이면 스킵률이 내려간다 — 인디케이터를 두는 이유다.
    void UpdateDots()
    {
        if (_pageDots == null) return;

        for (int i = 0; i < _pageDots.Length; i++)
        {
            if (_pageDots[i] == null) continue;
            _pageDots[i].color = i == _pageIndex ? UITheme.TextPrimary : UITheme.TextDisabled;
        }
    }

    void Finish()
    {
        if (_finished) return;   // SKIP 과 마지막 줄 진행이 겹쳐도 한 번만 끝난다
        _finished = true;

        if (_fade != null) { StopCoroutine(_fade); _fade = null; }
        if (_kenBurns != null) _kenBurns.Stop();

        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        gameObject.SetActive(false);

        OnFinished?.Invoke();
    }
}
