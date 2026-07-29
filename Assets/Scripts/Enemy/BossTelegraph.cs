using UnityEngine;

// 보스 프리팹 내부 자식 — 장판 예고를 테두리 링(고정 크기) + 채움(0→반경)으로 표현
public class BossTelegraph : MonoBehaviour
{
    [SerializeField] SpriteRenderer _ring;
    [SerializeField] SpriteRenderer _fill;

    static readonly Color NeonRed = new Color(1f, 0.15f, 0.25f, 1f);

    Vector2 _point;
    float   _radius;

    public void Show(Vector2 worldPoint, float radius)
    {
        _point  = worldPoint;
        _radius = radius;

        _ring.color = NeonRed;
        _fill.color = NeonRed;
        _ring.gameObject.SetActive(true);
        _fill.gameObject.SetActive(true);
        SetProgress(0f);
    }

    public void SetProgress(float t01)
    {
        // 보스가 움직여도 예고원은 고정 위치에 남아야 한다 (되감기 재현성)
        transform.position = _point;

        // 보스 자식이라 부모 스케일(3~4배)이 곱해진다 — 월드 반경을 맞추려면 역수로 상쇄
        float compensate = Mathf.Abs(transform.lossyScale.x) > 0.0001f ? 1f / transform.lossyScale.x : 1f;
        float diameter = _radius * 2f * compensate;

        _ring.transform.localScale = new Vector3(diameter, diameter, 1f);

        float t = Mathf.Clamp01(t01);
        float fill = diameter * t;
        _fill.transform.localScale = new Vector3(fill, fill, 1f);

        Color c = NeonRed;
        c.a = 0.25f + 0.5f * t;
        _fill.color = c;
    }

    public void Hide()
    {
        _ring.gameObject.SetActive(false);
        _fill.gameObject.SetActive(false);
    }
}
