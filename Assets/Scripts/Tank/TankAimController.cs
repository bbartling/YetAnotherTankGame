using UnityEngine;

public class TankAimController : MonoBehaviour
{
    public Transform barrelPitchPivot;
    public float mouseWheelPitchSensitivity = 18f;
    public float keyboardPitchSpeed = 24f;
    public float minElevation = -8f;
    public float maxElevation = 35f;

    public float ElevationDegrees { get; private set; }

    public void Bind(Transform pivot, float currentElevation, float minimum, float maximum)
    {
        barrelPitchPivot = pivot;
        minElevation = minimum;
        maxElevation = maximum;
        SetElevation(currentElevation);
    }

    public void TickPlayerInput(float deltaTime)
    {
        float input = Input.GetAxisRaw("Mouse ScrollWheel") * mouseWheelPitchSensitivity;
        if (Input.GetKey(KeyCode.PageUp)) input += keyboardPitchSpeed * deltaTime;
        if (Input.GetKey(KeyCode.PageDown)) input -= keyboardPitchSpeed * deltaTime;
        SetElevation(ElevationDegrees + input);
    }

    public void SetElevation(float elevation)
    {
        ElevationDegrees = Mathf.Clamp(elevation, minElevation, maxElevation);
        if (barrelPitchPivot != null)
        {
            barrelPitchPivot.localRotation = Quaternion.Euler(-ElevationDegrees, 0f, 0f);
        }
    }
}
