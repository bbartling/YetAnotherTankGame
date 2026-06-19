using UnityEngine;

public static class TankBallistics
{
    public readonly struct Solution
    {
        public Solution(Vector3 launchVelocity, float elevationDegrees, float timeOfFlight, Vector3 targetPoint)
        {
            LaunchVelocity = launchVelocity;
            ElevationDegrees = elevationDegrees;
            TimeOfFlight = timeOfFlight;
            TargetPoint = targetPoint;
        }

        public Vector3 LaunchVelocity { get; }
        public float ElevationDegrees { get; }
        public float TimeOfFlight { get; }
        public Vector3 TargetPoint { get; }
    }

    public static float GetMuzzleSpeed(float maxPower, float powerPercentage)
    {
        return maxPower * Mathf.Clamp01(powerPercentage / 100f);
    }

    public static bool TrySolve(Vector3 origin, Vector3 target, float muzzleSpeed, Vector3 gravity, bool highArc, out Solution solution)
    {
        solution = default;
        float gravityMagnitude = gravity.magnitude;
        if (muzzleSpeed <= 0.01f || gravityMagnitude <= 0.01f)
        {
            return false;
        }

        Vector3 up = -gravity.normalized;
        Vector3 displacement = target - origin;
        float height = Vector3.Dot(displacement, up);
        Vector3 horizontal = displacement - up * height;
        float distance = horizontal.magnitude;
        if (distance <= 0.01f)
        {
            return false;
        }

        float speedSquared = muzzleSpeed * muzzleSpeed;
        float discriminant = speedSquared * speedSquared
            - gravityMagnitude * (gravityMagnitude * distance * distance + 2f * height * speedSquared);
        if (discriminant < 0f)
        {
            return false;
        }

        float root = Mathf.Sqrt(discriminant);
        float tangent = (speedSquared + (highArc ? root : -root)) / (gravityMagnitude * distance);
        float angle = Mathf.Atan(tangent);
        float cosine = Mathf.Cos(angle);
        if (Mathf.Abs(cosine) <= 0.001f)
        {
            return false;
        }

        Vector3 horizontalDirection = horizontal / distance;
        Vector3 launchVelocity = horizontalDirection * (muzzleSpeed * cosine) + up * (muzzleSpeed * Mathf.Sin(angle));
        solution = new Solution(
            launchVelocity,
            angle * Mathf.Rad2Deg,
            distance / (muzzleSpeed * cosine),
            target);
        return true;
    }

    public static Vector3 PositionAtTime(Vector3 origin, Vector3 launchVelocity, Vector3 gravity, float time)
    {
        return origin + launchVelocity * time + 0.5f * gravity * time * time;
    }
}
