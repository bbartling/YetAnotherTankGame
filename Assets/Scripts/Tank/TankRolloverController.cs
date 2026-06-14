using UnityEngine;

public class TankRolloverController
{
    public float rolloverAngle = 75f;
    public float defeatDelay = 2f;
    public float OverturnedSeconds { get; private set; }
    public bool IsOverturned { get; private set; }

    public bool Tick(float uprightAngle, float deltaTime)
    {
        IsOverturned = uprightAngle >= rolloverAngle;
        OverturnedSeconds = IsOverturned
            ? OverturnedSeconds + Mathf.Max(0f, deltaTime)
            : 0f;
        return OverturnedSeconds >= defeatDelay;
    }

    public void Reset()
    {
        IsOverturned = false;
        OverturnedSeconds = 0f;
    }
}
