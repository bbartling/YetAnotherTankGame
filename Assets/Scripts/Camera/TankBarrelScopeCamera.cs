using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public class TankBarrelScopeCamera : MonoBehaviour
{
    public bool allowScope = true;
    public Transform sight;
    public Vector3 localSightOffset = Vector3.zero;
    public float scopeFieldOfView = 11f;
    public float normalFieldOfView = 58f;
    public float scopeBlendSpeed = 12f;
    public float scopeSwayDegrees = 0.08f;

    private Camera _camera;
    private float _swaySeed;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _swaySeed = Random.Range(0f, 100f);
    }

    private void LateUpdate()
    {
        if (_camera == null || sight == null)
        {
            return;
        }

        bool scoped = allowScope && Input.GetMouseButton(1) && ProjectileCameraController.ActivePlayerProjectile == null;
        float targetFov = scoped ? scopeFieldOfView : normalFieldOfView;
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, Time.deltaTime * scopeBlendSpeed);

        if (!scoped)
        {
            return;
        }

        ApplyScopePose();
    }

    public void ApplyScopePose()
    {
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        if (_camera == null || sight == null)
        {
            return;
        }

        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, scopeFieldOfView, Time.deltaTime * scopeBlendSpeed);
        transform.position = sight.TransformPoint(localSightOffset);
        transform.rotation = sight.rotation;

        if (scopeSwayDegrees > 0f)
        {
            float swayX = Mathf.Sin((Time.time + _swaySeed) * 1.7f) * scopeSwayDegrees;
            float swayY = Mathf.Cos((Time.time + _swaySeed) * 1.3f) * scopeSwayDegrees * 0.6f;
            transform.rotation *= Quaternion.Euler(swayX, swayY, 0f);
        }
    }
}
