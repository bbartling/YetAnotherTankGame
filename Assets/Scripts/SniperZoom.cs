using UnityEngine;

public class SniperZoom : MonoBehaviour
{
    public Camera zoomCamera;
    public float zoomFOV = 15f;
    public float normalFOV = 58f;
    public float smoothSpeed = 15f; // Faster transition
    private TankBarrelScopeCamera _barrelScopeCamera;

    void Start()
    {
        if (zoomCamera == null) zoomCamera = GetComponent<Camera>();
        _barrelScopeCamera = GetComponent<TankBarrelScopeCamera>();
        if (zoomCamera != null) zoomCamera.fieldOfView = normalFOV;
    }

    void Update()
    {
        if (zoomCamera == null) return;
        if (_barrelScopeCamera != null && Input.GetMouseButton(1))
        {
            return;
        }

        float targetFOV = Input.GetMouseButton(1) ? zoomFOV : normalFOV;
        zoomCamera.fieldOfView = Mathf.Lerp(zoomCamera.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
    }
}
