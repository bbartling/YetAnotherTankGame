using UnityEngine;

public class SniperZoom : MonoBehaviour
{
    public Camera zoomCamera;
    public float zoomFOV = 15f;
    public float normalFOV = 60f;
    public float smoothSpeed = 10f;

    void Start()
    {
        if (zoomCamera == null) zoomCamera = GetComponent<Camera>();
        if (zoomCamera != null) normalFOV = zoomCamera.fieldOfView;
    }

    void Update()
    {
        if (zoomCamera == null) return;

        float targetFOV = Input.GetMouseButton(1) ? zoomFOV : normalFOV;
        zoomCamera.fieldOfView = Mathf.Lerp(zoomCamera.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
    }
}
