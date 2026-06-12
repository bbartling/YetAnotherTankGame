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
}
