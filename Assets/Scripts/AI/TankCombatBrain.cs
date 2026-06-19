public sealed class TankCombatBrain
{
    public enum State
    {
        Patrol,
        Suspicious,
        HaltToAim,
        Firing,
        Reloading,
        Repositioning,
        Retreating,
        Disabled,
        Destroyed
    }

    public float RetreatDistance { get; set; } = 100f;
    public float PreferredDistance { get; set; } = 220f;
    public float RepositionDistance { get; set; } = 300f;
    public float HaltSpeed { get; set; } = 0.5f;

    public State Decide(
        bool hasLineOfSight,
        float distance,
        float speed,
        bool aimAligned,
        bool reloading,
        bool remembersTarget = false,
        bool disabled = false,
        bool destroyed = false)
    {
        if (destroyed)
        {
            return State.Destroyed;
        }

        if (disabled)
        {
            return State.Disabled;
        }

        if (!hasLineOfSight)
        {
            return remembersTarget ? State.Suspicious : State.Patrol;
        }

        if (distance < RetreatDistance)
        {
            return State.Retreating;
        }

        if (distance > RepositionDistance)
        {
            return State.Repositioning;
        }

        if (reloading)
        {
            return State.Reloading;
        }

        if (speed > HaltSpeed || !aimAligned)
        {
            return State.HaltToAim;
        }

        return State.Firing;
    }
}
