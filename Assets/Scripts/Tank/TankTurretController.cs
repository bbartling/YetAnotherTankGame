using UnityEngine;

public class TankTurretController : MonoBehaviour
{
    public Transform turretYawPivot;
    public float mouseYawDegreesPerSecond = 32f;
    public float turretYawSpeed = 55f;

    public float DesiredYawDegrees { get; private set; }

    public void Bind(Transform pivot, float yawSpeed, float inputSpeed)
    {
        turretYawPivot = pivot;
        turretYawSpeed = yawSpeed;
        mouseYawDegreesPerSecond = inputSpeed;
        DesiredYawDegrees = pivot != null ? NormalizeSignedAngle(pivot.localEulerAngles.y) : 0f;
    }

    public void TickPlayerInput(float deltaTime)
    {
        DesiredYawDegrees += Input.GetAxisRaw("Mouse X") * mouseYawDegreesPerSecond * deltaTime;
        TickRotation(deltaTime);
    }

    public void TickRotation(float deltaTime)
    {
        if (turretYawPivot == null)
        {
            return;
        }

        Quaternion target = Quaternion.Euler(0f, DesiredYawDegrees, 0f);
        turretYawPivot.localRotation = Quaternion.RotateTowards(
            turretYawPivot.localRotation,
            target,
            turretYawSpeed * deltaTime);
    }

    public void SetDesiredYawForTest(float yawDegrees)
    {
        SetDesiredYaw(yawDegrees);
    }

    public void SetDesiredYaw(float yawDegrees)
    {
        DesiredYawDegrees = yawDegrees;
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        return angle > 180f ? angle - 360f : angle;
    }
}
