using UnityEngine;

[DisallowMultipleComponent]
public class WheeledSuspensionController : MonoBehaviour
{
    public float halfWidth = 0.55f;
    public float frontOffset = 0.85f;
    public float wheelSpacing = 0.57f;
    public float probeHeight = 1.2f;
    public float suspensionTravel = 1f;
    public float wheelRadius = 0.34f;
    public float springStrength = 3f;
    public float damperStrength = 1.2f;
    public LayerMask groundMask = ~0;

    public int WheelCount => 8;
    public int GroundedWheelCount { get; private set; }
    public Vector3 LastGroundNormal { get; private set; } = Vector3.up;
    public float RecentBumpStrength { get; private set; }
    public float SpringBoostScale { get; set; } = 1f;
    public float DamperScale { get; set; } = 1f;
    public float BumpLaunchScale { get; set; } = 1f;
    public float WheelSpinLaunchBoost { get; set; }

    private readonly float[] _lastCompression = new float[8];

    private void OnEnable()
    {
        suspensionTravel = Mathf.Clamp(suspensionTravel, 0.55f, 1.1f);
        springStrength = Mathf.Clamp(springStrength, 2.4f, 3.6f);
        damperStrength = Mathf.Clamp(damperStrength, 0.8f, 1.8f);
    }

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
        return compression * springStrength - contactVelocity * damperStrength;
    }

    public int ApplySuspension(Rigidbody body)
    {
        GroundedWheelCount = 0;
        Vector3 normalSum = Vector3.zero;
        if (body == null)
        {
            LastGroundNormal = Vector3.up;
            return 0;
        }

        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(ReadBodyVelocity(body), transform.forward));
        float distance = probeHeight + suspensionTravel + wheelRadius;
        for (int i = 0; i < WheelCount; i++)
        {
            Vector3 mount = transform.TransformPoint(GetLocalWheelMount(i));
            Vector3 origin = mount + transform.up * probeHeight;
            if (!TryGetNearestNonSelfHit(origin, -transform.up, distance, out RaycastHit hit))
            {
                _lastCompression[i] = 0f;
                continue;
            }

            float travelDistance = Mathf.Max(0f, hit.distance - probeHeight - wheelRadius);
            float compression = Mathf.Clamp01(1f - travelDistance / Mathf.Max(0.01f, suspensionTravel));
            float contactVelocity = Vector3.Dot(body.GetPointVelocity(hit.point), hit.normal);
            float spring = springStrength * SpringBoostScale;
            float damper = damperStrength * DamperScale;
            float force = CalculateSpringForce(compression, spring, contactVelocity, damper);
            body.AddForceAtPosition(hit.normal * force, hit.point, ForceMode.Acceleration);

            float release = _lastCompression[i] - compression;
            if (release > 0.16f && _lastCompression[i] > 0.48f)
            {
                float speedFactor = Mathf.Max(forwardSpeed * 0.025f, WheelSpinLaunchBoost);
                float deltaV = release * (0.085f + speedFactor) * BumpLaunchScale;
                deltaV = Mathf.Clamp(deltaV, 0f, TankGameplayTuning.OverdriveMaxBumpDeltaV);
                if (deltaV > 0.015f)
                {
                    Vector3 launchDir = (hit.normal * 0.82f + transform.forward * 0.28f).normalized;
                    float impulse = deltaV * body.mass;
                    body.AddForceAtPosition(launchDir * impulse, hit.point, ForceMode.Impulse);
                    RecentBumpStrength = Mathf.Max(RecentBumpStrength, deltaV);
                }
            }

            _lastCompression[i] = compression;
            GroundedWheelCount++;
            normalSum += hit.normal;
        }

        LastGroundNormal = GroundedWheelCount > 0 && normalSum.sqrMagnitude > 0.0001f
            ? normalSum.normalized
            : Vector3.up;
        RecentBumpStrength = Mathf.MoveTowards(RecentBumpStrength, 0f, Time.fixedDeltaTime * 3f);
        return GroundedWheelCount;
    }

    private bool TryGetNearestNonSelfHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
    {
        nearest = default;
        float nearestDistance = float.MaxValue;
        RaycastHit[] hits = Physics.SphereCastAll(origin, wheelRadius, direction, distance, groundMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null || hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearest = hits[i];
            }
        }

        return nearest.collider != null;
    }

    private static Vector3 ReadBodyVelocity(Rigidbody body)
    {
        if (body == null)
        {
            return Vector3.zero;
        }

#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }
}
