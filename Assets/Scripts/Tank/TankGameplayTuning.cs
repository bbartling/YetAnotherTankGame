public static class TankGameplayTuning
{
    public const float BaselineChassisMass = 18000f;
    // Heavy MBT-scale mass for planted, life-size feel (not RC-toy).
    public const float ChassisMass = 42000f;
    public const float MassTuningRatio = ChassisMass / BaselineChassisMass;

    public const float OverdriveHoldSeconds = 3f;
    public const float OverdriveSpeedMultiplier = 12.2f;
    public const float OverdriveAccelerationMultiplier = 13.2f;
    public const float OverdriveClimbMultiplier = 5.6f;
    public const float OverdriveChargeSpeedMultiplier = 1.06f;
    public const float OverdriveChargeAccelerationMultiplier = 1.14f;
    public const float OverdriveChargeClimbMultiplier = 1.36f;
    public const float OverdriveWheelVisualMultiplier = 4.4f;
    public const float OverdriveReleaseGraceSeconds = 1.2f;
    public const float OverdriveBurstPush = 1.15f;

    public const float RpmIdle = 2000f;
    public const float RpmRedline = 6000f;

    public const float OverdriveStuckSpeedThreshold = 4.4f;
    public const float OverdriveStuckPushForce = 56f;
    public const float OverdriveStuckLiftForce = 0f;
    public const float OverdriveStuckClimbMultiplier = 4.8f;

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
    public const float PlantedClimbSuspensionDamperScale = 2.35f;
    public const float PlantedClimbMaxAssistUpwardComponent = 0.16f;
    public const float PlantedClimbDownforce = 38f;
    public const float PlantedClimbMaxUpwardSpeed = 0.35f;
    public const float PlantedClimbCrawlForce = 96f;
    public const float UltraTractionClimbForceMultiplier = 3f;
    public const float UltraTractionLateralSlipDamping = 0.82f;
    public const float UltraTractionYawDamping = 0.68f;
    public const float UltraTractionSteeringMultiplier = 0.35f;
    public const float CrawlerContactSlopeDegrees = 8f;
    public const float CrawlerContactForwardAcceleration = 115f;
    public const float CrawlerContactLiftAcceleration = 18f;
    public const float CrawlerContactMaxUpwardSpeed = 1.2f;
    public const float CrawlerContactLateralDamping = 0.9f;
    public const float CrawlerContactAngularDamping = 0.72f;
    public const float CrawlerContactProbeDistance = 3.2f;
    public const float BuiltInWheelMotorTorque = 9000f;
    public const float BuiltInWheelBrakeTorque = 18000f;
    public const float BuiltInWheelClimbAssist = 24f;
    public const float BuiltInWheelSteerAngle = 24f;
    public const float BuiltInWheelSuspensionDistance = 0.85f;
    public const float BuiltInWheelSpring = 65000f;
    public const float BuiltInWheelDamper = 9000f;

    public const float PracticeBasePlanarSpeedCap = 16f;
    public const float PracticeOverdrivePlanarSpeedCap = PracticeBasePlanarSpeedCap * OverdriveSpeedMultiplier;
    public const float MaxPracticeAirborneVerticalSpeed = 1.1f;
    public const float PracticeAirborneGravityMultiplier = 3.6f;

    public const float HighSpeedTippyThreshold = 11f;
    public const float HighSpeedTippyMax = 28f;

    public const int OverdriveSmokeMinPuffs = 4;
    public const int OverdriveSmokeMaxPuffsExclusive = 9;
    public const float OverdriveSmokeMinStartSize = 3.2f;
    public const float OverdriveSmokeMaxStartSize = 8.8f;
    public const float OverdriveSmokeMinLifetime = 2.4f;
    public const float OverdriveSmokeMaxLifetime = 4.8f;
    public const int OverdriveSmokeMinParticlesPerPuff = 16;
    public const int OverdriveSmokeMaxParticlesPerPuffExclusive = 45;
    public const int OverdriveSmokeMaxParticles = 192;
}
