// Shared tank driving feel for WAR, Driving Practice, and Cannon Practice.
// Physics baseline: YetAnotherTankGame commit 4b6842e7 (2026-06-30).
public static class TankDrivingProfile
{
    public const float MaxForwardSpeed = 12f;
    public const float MaxReverseSpeed = 6f;
    public const float AccelerationSeconds = 0.62f;
    public const float BrakingSeconds = 1.1f;
    public const float MinimumUphillSpeedMultiplier = 0.72f;
    public const float TractionLossSlopeDegrees = 44f;
    public const float MaxClimbSlopeDegrees = 60f;

    public const float TrackDriveResponse = 6.5f;
    public const float ForwardAcceleration = 65f;
    public const float ReverseAcceleration = 42f;

    public static bool ShouldApplyToScene(string sceneName)
    {
        return sceneName == "Practice"
            || sceneName == "TankDrivingPractice"
            || sceneName == "TankTargetPractice";
    }

    public static void ApplyToDriveController(TankDriveController drive)
    {
        if (drive == null)
        {
            return;
        }

        drive.maxForwardSpeed = MaxForwardSpeed;
        drive.maxReverseSpeed = MaxReverseSpeed;
        drive.accelerationSeconds = AccelerationSeconds;
        drive.brakingSeconds = BrakingSeconds;
        drive.minimumUphillSpeedMultiplier = MinimumUphillSpeedMultiplier;
        drive.tractionLossSlopeDegrees = TractionLossSlopeDegrees;
        drive.maxClimbSlopeDegrees = MaxClimbSlopeDegrees;
    }

    public static void ApplyToTankController(TankController tank)
    {
        if (tank == null)
        {
            return;
        }

        tank.ApplySharedDrivingHandling();
    }
}
