using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] float _duration = 0.4f;
    [SerializeField] float _maxRadius = 3f;
    [SerializeField] Color _color = Color.white;

    SpriteRenderer _renderer;
    Vector3 _baseScale;
    float _elapsed;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);
        float scale = Mathf.Lerp(0.2f, _maxRadius * 2f, t);
        transform.localScale = _baseScale * scale;

        Color c = _color;
        c.a *= 1f - t;
        _renderer.color = c;

        if (t >= 1f)
            Destroy(gameObject);
    }
}
