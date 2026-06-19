using UnityEngine;

public class TankAimController : MonoBehaviour
{
    public Transform barrelPitchPivot;
    public float mouseWheelPitchSensitivity = 18f;
    public float keyboardPitchSpeed = 24f;
    public float minElevation = -8f;
    public float maxElevation = 35f;
    public KeyCode elevateUpKey = KeyCode.E;
    public KeyCode elevateDownKey = KeyCode.Q;

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
        if (Input.GetKey(KeyCode.PageUp) || Input.GetKey(elevateUpKey)) input += keyboardPitchSpeed * deltaTime;
        if (Input.GetKey(KeyCode.PageDown) || Input.GetKey(elevateDownKey)) input -= keyboardPitchSpeed * deltaTime;
        SetElevation(ElevationDegrees + input);
    }

    public void ApplyKeyboardElevationInput(float direction, float deltaTime)
    {
        SetElevation(ElevationDegrees + Mathf.Clamp(direction, -1f, 1f) * keyboardPitchSpeed * Mathf.Max(0f, deltaTime));
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
