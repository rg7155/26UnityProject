using UnityEngine;

// 보스 프리팹 내부 자식 — 장판 예고는 테두리 링(고정 크기) + 채움(0→반경),
// 돌진 예고는 같은 렌더러 2개를 늘려 조준선(윤곽 + 뻗는 채움)으로 표현
public class BossTelegraph : MonoBehaviour
{
    [SerializeField] SpriteRenderer _ring;
    [SerializeField] SpriteRenderer _fill;

    static readonly Color NeonRed = new Color(1f, 0.15f, 0.25f, 1f);

    const float LineWidth = 0.9f;

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

        // 조준선 모드가 남긴 회전·오프셋을 되돌린다 — 원형 예고는 중심 정렬이어야 한다
        transform.rotation = Quaternion.identity;
        _ring.transform.localPosition = Vector3.zero;
        _fill.transform.localPosition = Vector3.zero;

        SetProgress(0f);
    }

    // 돌진 조준선 — 링이 전체 경로 윤곽, 채움이 진행도만큼 뻗는다.
    // 원형 스프라이트를 비균등 스케일로 늘려 쓰므로 프리팹에 새 자식이 필요 없다
    public void ShowLine(Vector2 origin, Vector2 dir, float length, float t01)
    {
        _ring.gameObject.SetActive(true);
        _fill.gameObject.SetActive(true);

        transform.position = origin;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        // 보스 자식이라 부모 스케일(3~4배)이 곱해진다 — 월드 길이를 맞추려면 역수로 상쇄
        float compensate = Mathf.Abs(transform.lossyScale.x) > 0.0001f ? 1f / transform.lossyScale.x : 1f;
        float len   = length * compensate;
        float width = LineWidth * compensate;

        _ring.transform.localScale    = new Vector3(len, width, 1f);
        _ring.transform.localPosition = new Vector3(len * 0.5f, 0f, 0f);

        float t = Mathf.Clamp01(t01);
        _fill.transform.localScale    = new Vector3(len * t, width, 1f);
        _fill.transform.localPosition = new Vector3(len * t * 0.5f, 0f, 0f);

        Color ring = NeonRed;
        ring.a = 0.35f;
        _ring.color = ring;

        Color c = NeonRed;
        c.a = 0.25f + 0.5f * t;
        _fill.color = c;
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
