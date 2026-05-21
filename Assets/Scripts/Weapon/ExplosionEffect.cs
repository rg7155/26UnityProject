using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] float _duration = 0.4f;
    [SerializeField] float _maxRadius = 3f;
    [SerializeField] Color _color = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField] int _segments = 32;

    LineRenderer _lr;
    float _elapsed;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.loop = true;
        _lr.positionCount = _segments;
        _lr.useWorldSpace = false;
        _lr.startWidth = 0.15f;
        _lr.endWidth = 0.15f;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);
        float r = Mathf.Lerp(0f, _maxRadius, t);

        for (int i = 0; i < _segments; i++)
        {
            float angle = i / (float)_segments * Mathf.PI * 2f;
            _lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
        }

        Color c = _color;
        c.a = 1f - t;
        _lr.startColor = c;
        _lr.endColor = c;

        if (t >= 1f)
            Destroy(gameObject);
    }
}
