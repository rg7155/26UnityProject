using UnityEngine;

public class CameraController : MonoBehaviour
{
    Transform _target;
    float _smoothSpeed = 8f;

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    void LateUpdate()
    {
        if (_target == null)
            return;

        Vector3 desired = new Vector3(_target.position.x, _target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime);
    }
}
