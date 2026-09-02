using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LightningEffect : MonoBehaviour
{
    [SerializeField] float _duration = 0.15f;
    [SerializeField] Color _color = new Color(0.6f, 0.8f, 1f, 1f);
    [SerializeField] int _segmentsPerSpan = 6;
    [SerializeField] float _jitter = 0.4f;
    [SerializeField] Sprite _sourceNodeSprite;
    [SerializeField] Sprite _hitSparkSprite;

    LineRenderer _lr;
    SpriteRenderer _sourceNode;
    SpriteRenderer _hitSpark;
    float _elapsed;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = true; // 경로(_path)가 월드 좌표
        _lr.loop = false;
        _lr.startWidth = 0.1f;
        _lr.endWidth = 0.1f;
    }

    public void Init(List<Vector3> points)
    {
        if (points == null || points.Count < 2)
        {
            Destroy(gameObject);
            return;
        }

        var poly = new List<Vector3>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            Vector3 dir = (b - a).normalized;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

            for (int s = 0; s < _segmentsPerSpan; s++)
            {
                float t = s / (float)_segmentsPerSpan;
                Vector3 p = Vector3.Lerp(a, b, t);
                if (s != 0)
                    p += perp * Random.Range(-_jitter, _jitter);
                poly.Add(p);
            }
        }
        poly.Add(points[points.Count - 1]);

        _lr.positionCount = poly.Count;
        for (int i = 0; i < poly.Count; i++)
            _lr.SetPosition(i, poly[i]);

        _sourceNode = CreateMarker("SourceNode", _sourceNodeSprite, points[0], 0.56f);
        _hitSpark = CreateMarker("HitSpark", _hitSparkSprite, points[points.Count - 1], 0.76f);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        Color c = _color;
        c.a = 1f - t;
        _lr.startColor = c;
        _lr.endColor = c;
        SetMarkerColor(_sourceNode, c);
        SetMarkerColor(_hitSpark, c);

        if (t >= 1f)
            Destroy(gameObject);
    }

    SpriteRenderer CreateMarker(string markerName, Sprite sprite, Vector3 position, float scale)
    {
        if (sprite == null)
            return null;

        var marker = new GameObject(markerName);
        marker.transform.SetParent(transform);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * scale;

        var renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = _color;
        renderer.sortingOrder = 6;
        return renderer;
    }

    void SetMarkerColor(SpriteRenderer marker, Color color)
    {
        if (marker != null)
            marker.color = color;
    }
}
