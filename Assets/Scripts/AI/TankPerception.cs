using UnityEngine;

public sealed class TankPerception
{
    public bool HasLineOfSight { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public float LastAwarenessTime { get; private set; } = float.NegativeInfinity;

    private readonly float _memorySeconds;

    public TankPerception(float memorySeconds)
    {
        _memorySeconds = Mathf.Max(0f, memorySeconds);
    }

    public void Observe(bool hasLineOfSight, Vector3 targetPosition, float now)
    {
        HasLineOfSight = hasLineOfSight;
        if (!hasLineOfSight)
        {
            return;
        }

        LastKnownPosition = targetPosition;
        LastAwarenessTime = now;
    }

    public void HearNoise(Vector3 position, float now, float memorySeconds)
    {
        HasLineOfSight = false;
        LastKnownPosition = position;
        LastAwarenessTime = now - Mathf.Max(0f, _memorySeconds - memorySeconds);
    }

    public bool HasRecentMemory(float now)
    {
        return now - LastAwarenessTime <= _memorySeconds;
    }
}
