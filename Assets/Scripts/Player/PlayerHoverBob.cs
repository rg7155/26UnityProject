using UnityEngine;

public class PlayerHoverBob : MonoBehaviour
{
    [SerializeField] float _frequency = 1.6f;
    [SerializeField] float _amplitude = 0.06f;

    Vector3 _baseLocalPosition;
    float _time;

    void Awake()
    {
        _baseLocalPosition = transform.localPosition;
    }

    void Update()
    {
        _time += Time.deltaTime;
        transform.localPosition = _baseLocalPosition + Vector3.up * (Mathf.Sin(_time * _frequency) * _amplitude);
    }
}
