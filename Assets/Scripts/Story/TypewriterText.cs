using TMPro;
using UnityEngine;

// 한 글자씩 드러나는 타자기 효과.
//
// 문자열을 잘라 붙이는 방식(text = full.Substring(0, n))을 쓰지 않는다. 두 가지가 깨진다.
//   1) 매 프레임 레이아웃이 다시 계산되어, 줄바꿈이 확정되기 전까지 글자가 흔들린다
//   2) <color> 같은 리치텍스트 태그가 중간에 잘려 깨진다
// TMP 의 maxVisibleCharacters 는 전체 텍스트를 한 번에 세팅해두고 '보이는 글자 수'만
// 제어하므로 레이아웃이 처음부터 확정되고 태그도 안전하다.
//
// unscaledDeltaTime 을 쓰는 이유는 UIModalTween 과 같다 — 이 프로젝트는 정지 경로가 둘이고,
// deltaTime 을 쓰면 timeScale=0 인 경로에서 영원히 진행되지 않는다.
[RequireComponent(typeof(TMP_Text))]
public class TypewriterText : MonoBehaviour
{
    TMP_Text _label;
    float _charsPerSecond = 30f;
    float _elapsed;
    int _totalChars;
    bool _running;

    public bool IsComplete { get { return !_running; } }

    void Awake()
    {
        _label = GetComponent<TMP_Text>();
    }

    // 새 문장을 처음부터 찍기 시작한다.
    public void Play(string text, float charsPerSecond)
    {
        if (_label == null) _label = GetComponent<TMP_Text>();

        _label.text = text;
        // 글자 수는 파싱 후에 확정된다. 리치텍스트 태그를 제외한 실제 글자 수를 얻으려면
        // 강제로 한 번 갱신해야 한다 — 안 하면 직전 문장의 길이가 남는다.
        _label.ForceMeshUpdate();

        _totalChars = _label.textInfo.characterCount;
        _charsPerSecond = Mathf.Max(1f, charsPerSecond);
        _elapsed = 0f;
        _label.maxVisibleCharacters = 0;
        _running = _totalChars > 0;

        if (!_running) _label.maxVisibleCharacters = _totalChars;
    }

    // 진행 중이면 즉시 전문을 드러낸다. 이미 끝났으면 아무것도 하지 않는다.
    public void Complete()
    {
        if (!_running) return;
        _label.maxVisibleCharacters = _totalChars;
        _running = false;
    }

    void Update()
    {
        if (!_running) return;

        _elapsed += Time.unscaledDeltaTime;

        int visible = Mathf.FloorToInt(_elapsed * _charsPerSecond);
        if (visible >= _totalChars)
        {
            _label.maxVisibleCharacters = _totalChars;
            _running = false;
            return;
        }

        _label.maxVisibleCharacters = visible;
    }
}
