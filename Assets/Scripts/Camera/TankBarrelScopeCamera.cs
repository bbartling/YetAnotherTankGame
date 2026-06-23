using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public class TankBarrelScopeCamera : MonoBehaviour
{
    public bool allowScope = true;
    public Transform sight;
    public Vector3 localSightOffset = new Vector3(0.2f, 0.12f, 2.5f);
    public float scopeFieldOfView = 16f;
    public float normalFieldOfView = 58f;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (_camera == null || sight == null)
        {
            return;
        }

        bool scoped = allowScope && Input.GetMouseButton(1) && ProjectileCameraController.ActivePlayerProjectile == null;
        _camera.fieldOfView = scoped ? scopeFieldOfView : normalFieldOfView;
        if (scoped)
        {
            ApplyScopePose();
        }
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

        _camera.fieldOfView = scopeFieldOfView;
        transform.position = sight.TransformPoint(localSightOffset);
        transform.rotation = sight.rotation;
    }
}
