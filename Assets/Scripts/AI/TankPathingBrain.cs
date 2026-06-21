using UnityEngine;

public sealed class TankPathingBrain
{
    public enum Motion
    {
        Stop,
        Advance,
        Retreat
    }

    public float MaxTravelSpeed { get; set; } = 2.25f;
    public float MaxSlopeDegrees { get; set; } = 35f;

    public Motion DecideMotion(float distance, float preferredDistance, float retreatDistance)
    {
        if (distance < retreatDistance)
        {
            return Motion.Retreat;
        }

        if (distance > preferredDistance)
        {
            return Motion.Advance;
        }

        return Motion.Stop;
    }

    public Vector3 ClampPlanarVelocity(Vector3 velocity)
    {
        Vector2 planar = Vector2.ClampMagnitude(new Vector2(velocity.x, velocity.z), MaxTravelSpeed);
        return new Vector3(planar.x, velocity.y, planar.y);
    }

    public bool CanAdvanceOnSlope(float slopeDegrees)
    {
        return slopeDegrees <= MaxSlopeDegrees;
    }

    public Vector3 ChooseSlopeAwareDirection(Vector3 desiredDirection, Vector3 right, float forwardSlope, float leftSlope, float rightSlope)
    {
        Vector3 forward = Vector3.ProjectOnPlane(desiredDirection, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        Vector3 side = Vector3.ProjectOnPlane(right, Vector3.up);
        if (side.sqrMagnitude < 0.0001f)
        {
            side = Vector3.Cross(Vector3.up, forward);
        }

        side.Normalize();
        if (CanAdvanceOnSlope(forwardSlope))
        {
            return forward;
        }

        bool canLeft = CanAdvanceOnSlope(leftSlope);
        bool canRight = CanAdvanceOnSlope(rightSlope);
        if (canRight && (!canLeft || rightSlope <= leftSlope))
        {
            return side;
        }

        if (canLeft)
        {
            return -side;
        }

        return -forward;
    }
}
