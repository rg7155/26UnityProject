using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// 좌측 화면 플로팅 아날로그 조이스틱 (터치 우선, WASD 병행)
// 출력 _input은 magnitude 0~1 — PlayerController가 normalize 없이 소비한다.
public class VirtualJoystick : MonoBehaviour
{
    const float RadiusScreenRatio = 0.12f;   // maxRadius = Screen.width * 0.12 (≈130px@1080)
    const float DeadzoneRatio     = 0.15f;   // deadzone = maxRadius * 0.15
    const float FadeInDuration    = 0.1f;
    const float FadeOutDuration   = 0.15f;
    const float LeftHalfRatio     = 0.5f;    // 좌측 절반만 조이스틱 존
    const float DirEpsilon        = 0.0001f; // 0 나눗셈 방지

    [SerializeField] RectTransform _baseRing;    // Inspector/생성기 배선 — 반투명 도넛 베이스
    [SerializeField] RectTransform _knob;        // Inspector/생성기 배선 — 중앙 노브
    [SerializeField] CanvasGroup   _canvasGroup; // Inspector/생성기 배선 — 페이드용
    [SerializeField] RectTransform _canvasRect;  // Inspector/생성기 배선 — 스크린→로컬 변환용

    int     _activeTouchId = -1;
    bool    _isTouchActive;
    Vector2 _center;   // 터치 시작 스크린 좌표
    Vector2 _input;
    float   _alpha;

    public Vector2 Input { get { return _input; } }  // magnitude 0~1

    public void Cancel()
    {
        _activeTouchId = -1;
        _isTouchActive = false;
        _input = Vector2.zero;
        _alpha = 0f;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    void Update()
    {
        PollTouch();

        if (!_isTouchActive)
            PollKeyboard();

        UpdateFade();
    }

    void PollTouch()
    {
        var ts = Touchscreen.current;
        if (ts == null) return;

        if (_activeTouchId < 0)
        {
            foreach (var t in ts.touches)
            {
                if (!t.press.wasPressedThisFrame) continue;

                Vector2 pos = t.position.ReadValue();
                if (pos.x >= Screen.width * LeftHalfRatio) continue;

                int id = t.touchId.ReadValue();
                // fingerId 인자 필수 — 안 넘기면 모바일에서 조용히 오작동. 터치다운 순간 1회만 검사.
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id))
                    continue;

                BeginTouch(id, pos);
                break;
            }
            return;
        }

        bool found = false;
        foreach (var t in ts.touches)
        {
            if (t.touchId.ReadValue() != _activeTouchId) continue;
            found = true;

            if (t.press.wasReleasedThisFrame || !t.press.isPressed)
                EndTouch();
            else
                UpdateAnalog(t.position.ReadValue());
            break;
        }
        if (!found) EndTouch();  // 터치가 사라짐
    }

    void BeginTouch(int id, Vector2 center)
    {
        _activeTouchId = id;
        _isTouchActive = true;
        _center = center;
        _input  = Vector2.zero;

        SetAnchoredFromScreen(_baseRing, center);
        SetAnchoredFromScreen(_knob, center);
    }

    void EndTouch()
    {
        _activeTouchId = -1;
        _isTouchActive = false;
        _input = Vector2.zero;
        // 페이드아웃은 UpdateFade가 FadeOutDuration으로 처리
    }

    void UpdateAnalog(Vector2 touchPos)
    {
        float maxRadius = Screen.width * RadiusScreenRatio;
        float deadzone  = maxRadius * DeadzoneRatio;

        Vector2 delta = touchPos - _center;
        float   dist  = delta.magnitude;
        Vector2 dirN  = dist > DirEpsilon ? delta / dist : Vector2.zero;
        float   mag   = Mathf.Clamp01((dist - deadzone) / (maxRadius - deadzone));

        _input = dirN * mag;  // deadzone 내부는 0

        Vector2 knobScreen = _center + dirN * Mathf.Min(dist, maxRadius);  // maxRadius 클램프
        SetAnchoredFromScreen(_knob, knobScreen);
    }

    void PollKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) { _input = Vector2.zero; return; }

        float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        _input = new Vector2(h, v).normalized;  // 키보드는 항상 최대속도
    }

    void UpdateFade()
    {
        float target   = _isTouchActive ? 1f : 0f;
        float duration = _isTouchActive ? FadeInDuration : FadeOutDuration;
        _alpha = Mathf.MoveTowards(_alpha, target, Time.deltaTime / duration);

        if (_canvasGroup != null)
            _canvasGroup.alpha = _alpha;
    }

    void SetAnchoredFromScreen(RectTransform target, Vector2 screenPos)
    {
        if (target == null || _canvasRect == null) return;

        // Overlay 캔버스이므로 camera 인자 null
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, null, out Vector2 local))
            target.anchoredPosition = local;
    }
}
