using UnityEngine;

// 정지 이미지에 느린 줌/팬을 주는 컴포넌트. 오프닝 배경 Image 에 붙는다.
//
// 이름은 다큐멘터리 감독 켄 번즈가 사진만으로 영상을 만들 때 쓴 기법에서 왔다.
// 배경이 완전히 멈춰 있으면 페이지가 넘어갈 때까지 화면이 죽은 것처럼 보인다.
// 배율 6% 정도의 아주 느린 이동이면 "움직인다"는 인식 없이 영상처럼 읽힌다.
//
// 시간은 UIModalTween 과 같은 이유로 unscaledDeltaTime 을 쓴다 —
// 이 프로젝트에는 timeScale 을 0 으로 만드는 정지 경로가 있다.
[RequireComponent(typeof(RectTransform))]
public class KenBurns : MonoBehaviour
{
    RectTransform _rt;
    Vector2 _size;          // Play 시점에 고정. 재생 중 rect 를 다시 읽으면 자기 이동에 물린다
    Vector2 _panFrom, _panTo;
    float _zoomFrom = 1f, _zoomTo = 1f;
    float _duration = 1f;
    float _elapsed;
    bool _running;

    void Awake() => _rt = (RectTransform)transform;

    // duration 은 그 페이지가 화면에 머무는 예상 시간이다. 사용자가 탭으로 먼저 넘기면
    // 도중에 끊기지만, 끊긴 프레임이 다음 페이지의 시작 배율로 덮이므로 티가 나지 않는다.
    public void Play(float zoomFrom, float zoomTo, Vector2 panFrom, Vector2 panTo, float duration)
    {
        if (_rt == null) _rt = (RectTransform)transform;

        _zoomFrom = Mathf.Max(1f, zoomFrom);
        _zoomTo   = Mathf.Max(1f, zoomTo);
        _panFrom  = panFrom;
        _panTo    = panTo;
        _duration = Mathf.Max(0.01f, duration);
        _elapsed  = 0f;
        _size     = _rt.rect.size;
        _running  = true;

        Apply(0f);
    }

    public void Stop() => _running = false;

    void Update()
    {
        if (!_running) return;

        _elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);
        Apply(t);
        if (t >= 1f) _running = false;
    }

    void Apply(float t)
    {
        // 끝에서 감속한다. 등속은 페이지가 넘어갈 때 뚝 끊긴 인상을 준다
        float e = 1f - (1f - t) * (1f - t);

        float zoom = Mathf.Lerp(_zoomFrom, _zoomTo, e);
        _rt.localScale = new Vector3(zoom, zoom, 1f);

        // 확대로 생긴 여백 안에서만 움직인다. 넘어가면 배경 바깥(빈 화면)이 드러난다
        float margin = Mathf.Max(0f, (zoom - 1f) * 0.5f);
        Vector2 pan = Vector2.Lerp(_panFrom, _panTo, e);
        pan.x = Mathf.Clamp(pan.x, -margin, margin);
        pan.y = Mathf.Clamp(pan.y, -margin, margin);

        _rt.anchoredPosition = new Vector2(pan.x * _size.x, pan.y * _size.y);
    }
}
