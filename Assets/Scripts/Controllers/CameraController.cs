using UnityEngine;

public class CameraController : MonoBehaviour
{
    Transform _target;
    float _smoothSpeed = 8f;

    float _shakeTimer;
    float _shakeDuration;
    float _shakeMagnitude;

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void Shake(float duration, float magnitude)
    {
        _shakeTimer     = duration;
        _shakeDuration  = duration;
        _shakeMagnitude = magnitude;
    }

    void LateUpdate()
    {
        if (_target == null)
            return;

        Vector3 desired = new Vector3(_target.position.x, _target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime);

        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            // Random 대신 사인파 — 흔들림은 연출뿐이지만 보스 코드 전반의 Random 금지 규약을 따른다
            float damp = _shakeDuration > 0f ? Mathf.Clamp01(_shakeTimer / _shakeDuration) : 0f;
            float offset = _shakeMagnitude * damp;
            transform.position += new Vector3(Mathf.Sin(Time.time * 90f), Mathf.Cos(Time.time * 70f), 0f) * offset;
        }
    }
}
