public static class TankGameplayTuning
{
    public const float BaselineChassisMass = 18000f;
    // Heavy MBT-scale mass for planted, life-size feel (not RC-toy).
    public const float ChassisMass = 42000f;
    public const float MassTuningRatio = ChassisMass / BaselineChassisMass;

    public const float OverdriveHoldSeconds = 3f;
    public const float OverdriveSpeedMultiplier = 2.4f;
    public const float OverdriveAccelerationMultiplier = 2.6f;
    public const float OverdriveClimbMultiplier = 2.8f;
    public const float OverdriveChargeSpeedMultiplier = 1.08f;
    public const float OverdriveChargeAccelerationMultiplier = 1.12f;
    public const float OverdriveChargeClimbMultiplier = 1.18f;
    public const float OverdriveWheelVisualMultiplier = 2.2f;
    public const float OverdriveReleaseGraceSeconds = 1.2f;
    public const float OverdriveBurstPush = 0.85f;

    public const float RpmIdle = 2000f;
    public const float RpmRedline = 6000f;

    public const float OverdriveStuckSpeedThreshold = 2.2f;
    public const float OverdriveStuckPushForce = 28f;
    public const float OverdriveStuckLiftForce = 0f;
    public const float OverdriveStuckClimbMultiplier = 2.4f;

    public const float OverdriveBumpLaunchScale = 0f;
    public const float OverdriveWheelSpinLaunch = 0f;
    public const float OverdriveMaxBumpDeltaV = 0f;
    public const float BumpLaunchGlobalScale = 0f;
    public const float BumpLaunchVerticalBias = 0f;
    public const float BumpLaunchForwardBias = 1f;
    public const float OverdriveTerrainVerticalAssist = 0f;
    // Overdrive should not make suspension bouncier.
    public const float OverdriveSuspensionSpringBoost = 0.92f;
    public const float OverdriveSuspensionDamperScale = 1.55f;

    public const float PracticeBasePlanarSpeedCap = 13f;
    public const float PracticeOverdrivePlanarSpeedCap = PracticeBasePlanarSpeedCap * OverdriveSpeedMultiplier;
    public const float MaxPracticeAirborneVerticalSpeed = 1.1f;
    public const float PracticeAirborneGravityMultiplier = 3.6f;

    public const float HighSpeedTippyThreshold = 11f;
    public const float HighSpeedTippyMax = 28f;
}
