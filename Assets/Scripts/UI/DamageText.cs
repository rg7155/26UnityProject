using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [SerializeField] TMP_Text _text;          // Inspector에서 드래그 (같은 프리팹 내부 자식)
    [SerializeField] float _riseSpeed = 1.5f;
    [SerializeField] float _lifetime = 0.7f;

    float _timer;
    Color _baseColor;

    public void Init(int damage, Color color)
    {
        _text.text = damage.ToString();
        _baseColor = color;
        _text.color = color;
        _timer = 0f;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        transform.position += Vector3.up * _riseSpeed * Time.deltaTime;

        Color c = _baseColor;
        c.a = 1f - (_timer / _lifetime);
        _text.color = c;
    }
}
