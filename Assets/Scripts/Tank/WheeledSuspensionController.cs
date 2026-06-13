using UnityEngine;

[DisallowMultipleComponent]
public class WheeledSuspensionController : MonoBehaviour
{
    public float halfWidth = 0.55f;
    public float frontOffset = 0.85f;
    public float wheelSpacing = 0.57f;
    public float probeHeight = 0.75f;
    public float suspensionTravel = 0.75f;
    public float wheelRadius = 0.28f;
    public float springStrength = 30f;
    public float damperStrength = 8f;
    public LayerMask groundMask = ~0;

    public int WheelCount => 8;
    public int GroundedWheelCount { get; private set; }

    public Vector3 GetLocalWheelMount(int index)
    {
        int clamped = Mathf.Clamp(index, 0, WheelCount - 1);
        bool rightSide = clamped >= 4;
        int axle = clamped % 4;
        float z = frontOffset - axle * wheelSpacing;
        return new Vector3(rightSide ? halfWidth : -halfWidth, 0f, z);
    }

    public static float CalculateSpringForce(float compression, float springStrength, float contactVelocity, float damperStrength)
    {
        return Mathf.Max(0f, compression * springStrength - contactVelocity * damperStrength);
    }

    public int ApplySuspension(Rigidbody body)
    {
        GroundedWheelCount = 0;
        if (body == null)
        {
            return 0;
        }

        float distance = probeHeight + suspensionTravel + wheelRadius;
        for (int i = 0; i < WheelCount; i++)
        {
            Vector3 mount = transform.TransformPoint(GetLocalWheelMount(i));
            Vector3 origin = mount + transform.up * probeHeight;
            if (!Physics.SphereCast(origin, wheelRadius, -transform.up, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            float travelDistance = Mathf.Max(0f, hit.distance - probeHeight - wheelRadius);
            float compression = Mathf.Clamp01(1f - travelDistance / Mathf.Max(0.01f, suspensionTravel));
            float contactVelocity = Vector3.Dot(body.GetPointVelocity(hit.point), hit.normal);
            float force = CalculateSpringForce(compression, springStrength, contactVelocity, damperStrength);
            body.AddForceAtPosition(hit.normal * force, hit.point, ForceMode.Acceleration);
            GroundedWheelCount++;
        }

        return GroundedWheelCount;
    }
}
