using UnityEngine;

public class TankSuspensionVisual : MonoBehaviour
{
    public Transform visualRoot;
    public float alignmentDegreesPerSecond = 45f;

    public Vector3 SmoothedGroundNormal { get; private set; } = Vector3.up;
    public float SlopeAngle => Vector3.Angle(SmoothedGroundNormal, Vector3.up);

    public void RecordGroundNormal(Vector3 groundNormal, float deltaTime)
    {
        if (groundNormal.sqrMagnitude < 0.001f)
        {
            return;
        }

        SmoothedGroundNormal = Vector3.Slerp(
            SmoothedGroundNormal,
            groundNormal.normalized,
            Mathf.Clamp01(deltaTime * 8f));

        if (visualRoot != null)
        {
            Quaternion target = CalculateTargetRotation(visualRoot.rotation, transform.forward, SmoothedGroundNormal);
            visualRoot.rotation = Quaternion.RotateTowards(
                visualRoot.rotation,
                target,
                alignmentDegreesPerSecond * deltaTime);
        }
    }

    public static Quaternion CalculateTargetRotation(Quaternion currentRotation, Vector3 forward, Vector3 groundNormal)
    {
        Vector3 planarForward = Vector3.ProjectOnPlane(forward, groundNormal);
        if (planarForward.sqrMagnitude < 0.001f)
        {
            planarForward = Vector3.ProjectOnPlane(currentRotation * Vector3.forward, groundNormal);
        }

        if (planarForward.sqrMagnitude < 0.001f)
        {
            return currentRotation;
        }

        return Quaternion.LookRotation(planarForward.normalized, groundNormal.normalized);
    }
}
