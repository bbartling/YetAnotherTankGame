using UnityEngine;

public class SniperZoom : MonoBehaviour
{
    public Camera zoomCamera;
    public float zoomFOV = 15f;
    public float normalFOV = 58f;
    public float smoothSpeed = 15f; // Faster transition

    void Start()
    {
        if (zoomCamera == null) zoomCamera = GetComponent<Camera>();
        if (zoomCamera != null) zoomCamera.fieldOfView = normalFOV;
    }

    void Update()
    {
        if (zoomCamera == null) return;

        float targetFOV = Input.GetMouseButton(1) ? zoomFOV : normalFOV;
        zoomCamera.fieldOfView = Mathf.Lerp(zoomCamera.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
    }
}
