using UnityEngine;

[DisallowMultipleComponent]
public class TankBarrelScopeCamera : MonoBehaviour
{
    public Transform sight;
    public Vector3 localSightOffset = new Vector3(0.45f, 0.35f, -0.6f);
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

        bool scoped = Input.GetMouseButton(1) && ProjectileCameraController.ActivePlayerProjectile == null;
        _camera.fieldOfView = scoped ? scopeFieldOfView : normalFieldOfView;
        if (scoped)
        {
            transform.position = sight.TransformPoint(localSightOffset);
            transform.rotation = sight.rotation;
        }
    }
}
