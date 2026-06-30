public static class TankGameplayTuning
{
    public const float BaselineChassisMass = 18000f;
    public const float ChassisMass = 22000f;
    public const float MassTuningRatio = ChassisMass / BaselineChassisMass;

    public const float OverdriveHoldSeconds = 3f;
    public const float OverdriveSpeedMultiplier = 3f * MassTuningRatio;
    public const float OverdriveAccelerationMultiplier = 3.5f * MassTuningRatio;
    public const float OverdriveClimbMultiplier = 3f * MassTuningRatio;
    public const float OverdriveChargeSpeedMultiplier = 1.05f * MassTuningRatio;
    public const float OverdriveChargeAccelerationMultiplier = 1.15f * MassTuningRatio;
    public const float OverdriveChargeClimbMultiplier = 1.2f * MassTuningRatio;
    public const float OverdriveWheelVisualMultiplier = 3f;
    public const float OverdriveReleaseGraceSeconds = 1.2f;
    public const float OverdriveBurstPush = 1.8f;

    public const float RpmIdle = 2000f;
    public const float RpmRedline = 6000f;

    public const float OverdriveStuckSpeedThreshold = 2.5f;
    public const float OverdriveStuckPushForce = 48f * MassTuningRatio;
    public const float OverdriveStuckLiftForce = 0f;
    public const float OverdriveStuckClimbMultiplier = 4f * MassTuningRatio;

    public const float OverdriveBumpLaunchScale = 1f;
    public const float OverdriveWheelSpinLaunch = 0f;
    public const float OverdriveMaxBumpDeltaV = 0f;
    public const float BumpLaunchGlobalScale = 0f;
    public const float BumpLaunchVerticalBias = 0f;
    public const float BumpLaunchForwardBias = 1f;
    public const float OverdriveTerrainVerticalAssist = 0f;
    public const float OverdriveSuspensionSpringBoost = 1.04f;
    public const float OverdriveSuspensionDamperScale = 1.15f;

    public const float PracticeBasePlanarSpeedCap = 13f;
    public const float PracticeOverdrivePlanarSpeedCap = PracticeBasePlanarSpeedCap * OverdriveSpeedMultiplier;
    public const float MaxPracticeAirborneVerticalSpeed = 1.8f;
    public const float PracticeAirborneGravityMultiplier = 2.8f;

    public const float HighSpeedTippyThreshold = 9f;
    public const float HighSpeedTippyMax = 24f;
}
