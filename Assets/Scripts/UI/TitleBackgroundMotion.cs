using UnityEngine;

// 타이틀 배경 원화에만 적용하는 저강도 대기 연출.
// UI·버튼의 위치는 건드리지 않고 배경 이미지의 확대와 세로 이동만 반복한다.
[DisallowMultipleComponent]
public class TitleBackgroundMotion : MonoBehaviour
{
    [SerializeField] float _zoomAmount = 0.03f;
    [SerializeField] float _verticalDrift = 18f;
    [SerializeField] float _period = 28f;

    RectTransform _rectTransform;
    Vector3 _baseScale;
    Vector2 _basePosition;

    void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _baseScale = _rectTransform.localScale;
        _basePosition = _rectTransform.anchoredPosition;
    }

    void Update()
    {
        float phase = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / _period);
        float zoom = 1f + _zoomAmount * ((phase + 1f) * 0.5f);
        _rectTransform.localScale = _baseScale * zoom;
        _rectTransform.anchoredPosition = _basePosition + Vector2.up * (phase * _verticalDrift);
    }
}
