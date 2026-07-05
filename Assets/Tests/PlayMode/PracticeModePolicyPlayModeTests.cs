#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class PracticeModePolicyPlayModeTests
{
    [Test]
    public void DrivingPractice_AllowsDriveOnlyAndLocksGunSystems()
    {
        GameObject root = CreateMinimalTank();
        TankController tank = root.GetComponent<TankController>();
        PracticeModeInputPolicy policy = root.AddComponent<PracticeModeInputPolicy>();
        policy.mode = PracticeModeInputPolicy.PracticeControlMode.DrivingOnly;

        policy.ApplyTo(tank);

        Assert.That(tank.allowDrivingInput, Is.True);
        Assert.That(tank.allowTurretInput, Is.True);
        Assert.That(tank.allowCannonInput, Is.False);
        Assert.That(tank.leftClickFiresCannon, Is.False);
        Assert.That(root.GetComponent<TankMachineGun>().allowInputFire, Is.False);
        Assert.That(root.GetComponent<SniperRangeFinder>().allowScope, Is.False);
        Assert.That(tank.gameplayCamera.GetComponent<TankBarrelScopeCamera>().allowScope, Is.False);
        Assert.That(tank.disableRolloverDefeat, Is.True);
        TankVoidFallController voidFall = root.GetComponent<TankVoidFallController>();
        Assert.That(voidFall, Is.Not.Null);
        Assert.That(voidFall.fallScreamClip, Is.Not.Null);
        Assert.That(voidFall.useHorizontalBounds, Is.False);
        Assert.That(voidFall.fallScreamDelaySeconds, Is.GreaterThanOrEqualTo(1.1f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void TargetPractice_AllowsDriveAndTurretScopeAndCannon()
    {
        GameObject root = CreateMinimalTank();
        TankController tank = root.GetComponent<TankController>();
        PracticeModeInputPolicy policy = root.AddComponent<PracticeModeInputPolicy>();
        policy.mode = PracticeModeInputPolicy.PracticeControlMode.TargetPractice;

        policy.ApplyTo(tank);

        Assert.That(tank.allowDrivingInput, Is.True);
        Assert.That(tank.allowTurretInput, Is.True);
        Assert.That(tank.allowCannonInput, Is.True);
        Assert.That(root.GetComponent<TankMachineGun>().allowInputFire, Is.False);
        Assert.That(root.GetComponent<SniperRangeFinder>().allowScope, Is.True);
        Assert.That(tank.gameplayCamera.GetComponent<TankBarrelScopeCamera>().allowScope, Is.True);
        Assert.That(tank.turretYawSpeed, Is.EqualTo(TankCombatProfile.TurretYawSpeed));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void WarPolicy_AllowsFullPlayerControl()
    {
        GameObject root = CreateMinimalTank();
        TankController tank = root.GetComponent<TankController>();
        PracticeModeInputPolicy policy = root.AddComponent<PracticeModeInputPolicy>();
        policy.mode = PracticeModeInputPolicy.PracticeControlMode.War;

        policy.ApplyTo(tank);

        Assert.That(tank.allowDrivingInput, Is.True);
        Assert.That(tank.allowTurretInput, Is.True);
        Assert.That(tank.allowCannonInput, Is.True);
        Assert.That(root.GetComponent<TankMachineGun>().allowInputFire, Is.True);
        Assert.That(root.GetComponent<SniperRangeFinder>().allowScope, Is.True);

        Object.DestroyImmediate(root);
    }

    [Test]
    public void WarPolicy_AppliesSharedDrivingHandling()
    {
        GameObject root = CreateMinimalTank();
        TankController tank = root.GetComponent<TankController>();
        PracticeModeInputPolicy policy = root.AddComponent<PracticeModeInputPolicy>();
        policy.mode = PracticeModeInputPolicy.PracticeControlMode.War;

        policy.ApplyTo(tank);

        TankDriveController drive = tank.GetComponent<TankDriveController>();
        Assert.That(drive, Is.Not.Null);
        Assert.That(drive.maxForwardSpeed, Is.EqualTo(TankDrivingProfile.MaxForwardSpeed));
        Assert.That(drive.maxClimbSlopeDegrees, Is.EqualTo(TankDrivingProfile.MaxClimbSlopeDegrees));
        BuiltInWheelTankDrive wheelDrive = tank.GetComponent<BuiltInWheelTankDrive>();
        Assert.That(wheelDrive, Is.Not.Null);
        Assert.That(wheelDrive.enabled, Is.True);
        Assert.That(wheelDrive.WheelColliderCount, Is.GreaterThanOrEqualTo(4));
        Assert.That(tank.turretYawSpeed, Is.EqualTo(TankCombatProfile.TurretYawSpeed));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void TargetPractice_AppliesSharedDrivingAndCombatProfiles()
    {
        GameObject root = CreateMinimalTank();
        TankController tank = root.GetComponent<TankController>();
        PracticeModeInputPolicy policy = root.AddComponent<PracticeModeInputPolicy>();
        policy.mode = PracticeModeInputPolicy.PracticeControlMode.TargetPractice;

        policy.ApplyTo(tank);

        TankDriveController drive = tank.GetComponent<TankDriveController>();
        Assert.That(drive.maxForwardSpeed, Is.EqualTo(TankDrivingProfile.MaxForwardSpeed));
        BuiltInWheelTankDrive wheelDrive = tank.GetComponent<BuiltInWheelTankDrive>();
        Assert.That(wheelDrive, Is.Not.Null);
        Assert.That(wheelDrive.WheelColliderCount, Is.GreaterThanOrEqualTo(4));
        Assert.That(tank.turretYawSpeed, Is.EqualTo(TankCombatProfile.TurretYawSpeed));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void SharedDrivingProfile_UsesModerateSpeedAndBoostedClimbBaseline()
    {
        Assert.That(TankDrivingProfile.MaxForwardSpeed, Is.EqualTo(15f));
        Assert.That(TankDrivingProfile.MaxReverseSpeed, Is.EqualTo(7.5f));
        Assert.That(TankDrivingProfile.AccelerationSeconds, Is.EqualTo(0.52f));
        Assert.That(TankDrivingProfile.TractionLossSlopeDegrees, Is.EqualTo(52f));
        Assert.That(TankDrivingProfile.MaxClimbSlopeDegrees, Is.EqualTo(75f));
        Assert.That(TankDrivingProfile.ForwardAcceleration, Is.EqualTo(82f));
        Assert.That(TankDrivingProfile.ReverseAcceleration, Is.EqualTo(52f));
        Assert.That(TankGameplayTuning.PracticeBasePlanarSpeedCap, Is.EqualTo(16f));
        Assert.That(TankGameplayTuning.OverdriveSpeedMultiplier, Is.EqualTo(2.2f));
        Assert.That(TankGameplayTuning.OverdriveClimbMultiplier, Is.EqualTo(5.6f));
        Assert.That(TankGameplayTuning.OverdriveStuckPushForce, Is.EqualTo(56f));
        Assert.That(TankGameplayTuning.OverdriveStuckClimbMultiplier, Is.EqualTo(4.8f));
        Assert.That(TankGameplayTuning.PlantedClimbMaxAssistUpwardComponent, Is.EqualTo(0.16f));
        Assert.That(TankGameplayTuning.PlantedClimbDownforce, Is.EqualTo(38f));
        Assert.That(TankGameplayTuning.PlantedClimbMaxUpwardSpeed, Is.EqualTo(0.35f));
        Assert.That(TankGameplayTuning.PlantedClimbCrawlForce, Is.EqualTo(96f));
        Assert.That(TankGameplayTuning.UltraTractionClimbForceMultiplier, Is.EqualTo(3f));
        Assert.That(TankGameplayTuning.UltraTractionLateralSlipDamping, Is.EqualTo(0.82f));
        Assert.That(TankGameplayTuning.UltraTractionYawDamping, Is.EqualTo(0.68f));
        Assert.That(TankGameplayTuning.UltraTractionSteeringMultiplier, Is.EqualTo(0.35f));
        Assert.That(TankGameplayTuning.CrawlerContactForwardAcceleration, Is.EqualTo(115f));
        Assert.That(TankGameplayTuning.CrawlerContactLiftAcceleration, Is.EqualTo(18f));
        Assert.That(TankGameplayTuning.CrawlerContactMaxUpwardSpeed, Is.EqualTo(1.2f));
        Assert.That(TankGameplayTuning.CrawlerContactLateralDamping, Is.EqualTo(0.9f));
        Assert.That(TankGameplayTuning.CrawlerContactAngularDamping, Is.EqualTo(0.72f));
        Assert.That(TankGameplayTuning.BuiltInWheelMotorTorque, Is.EqualTo(9000f));
        Assert.That(TankGameplayTuning.BuiltInWheelBrakeTorque, Is.EqualTo(18000f));
        Assert.That(TankGameplayTuning.BuiltInWheelClimbAssist, Is.EqualTo(24f));
    }

    [Test]
    public void BuiltInWheelTankDrive_CreatesUnityWheelCollidersAndKeepsOverdrive()
    {
        GameObject root = CreateMinimalTank();
        TankController tank = root.GetComponent<TankController>();

        tank.ApplySharedDrivingHandling();

        BuiltInWheelTankDrive wheelDrive = root.GetComponent<BuiltInWheelTankDrive>();
        TankOverdriveController overdrive = root.GetComponent<TankOverdriveController>();

        Assert.That(wheelDrive, Is.Not.Null);
        Assert.That(wheelDrive.WheelColliderCount, Is.GreaterThanOrEqualTo(4));
        Assert.That(wheelDrive.motorTorque, Is.EqualTo(TankGameplayTuning.BuiltInWheelMotorTorque));
        Assert.That(wheelDrive.climbAssistAcceleration, Is.EqualTo(TankGameplayTuning.BuiltInWheelClimbAssist));
        Assert.That(overdrive, Is.Not.Null);
        Assert.That(overdrive.allowOverdrive, Is.True);

        Object.DestroyImmediate(root);
    }

    [Test]
    public void PlantedClimbDirection_CapsUpwardLaunchComponent()
    {
        Vector3 steepGroundNormal = Quaternion.AngleAxis(-72f, Vector3.right) * Vector3.up;
        Vector3 direction = TankController.CalculatePlantedClimbDirection(
            Vector3.forward,
            steepGroundNormal,
            TankGameplayTuning.PlantedClimbMaxAssistUpwardComponent);

        Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
        Assert.That(direction.y, Is.LessThanOrEqualTo(TankGameplayTuning.PlantedClimbMaxAssistUpwardComponent + 0.001f));
        Assert.That(Vector3.Dot(Vector3.ProjectOnPlane(direction, Vector3.up).normalized, Vector3.forward), Is.GreaterThan(0.95f));
    }

    [Test]
    public void UltraTractionVelocity_DampsSideSlipAndKeepsForwardClimb()
    {
        Vector3 groundNormal = Quaternion.AngleAxis(-35f, Vector3.right) * Vector3.up;
        Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, groundNormal).normalized;
        Vector3 right = Vector3.ProjectOnPlane(Vector3.right, groundNormal).normalized;
        Vector3 velocity = forward * 5f + right * 6f + Vector3.up * 0.2f;

        Vector3 damped = TankController.CalculateUltraTractionVelocity(
            velocity,
            groundNormal,
            Vector3.forward,
            TankGameplayTuning.UltraTractionLateralSlipDamping);

        Assert.That(Vector3.Dot(damped, right), Is.EqualTo(6f * (1f - TankGameplayTuning.UltraTractionLateralSlipDamping)).Within(0.01f));
        Assert.That(Vector3.Dot(damped, forward), Is.GreaterThan(4.9f));
    }

    [Test]
    public void CrawlerContactVelocity_DampsSideSlipWithoutRemovingGradeForwardMotion()
    {
        Vector3 groundNormal = Quaternion.AngleAxis(-25f, Vector3.right) * Vector3.up;
        Vector3 forward = TankController.CalculateCrawlerForward(Vector3.forward, groundNormal);
        Vector3 right = Vector3.Cross(groundNormal.normalized, forward).normalized;
        Vector3 velocity = forward * 3.5f + right * 5f + Vector3.up * 0.15f;

        Vector3 damped = TankController.CalculateCrawlerDampedVelocity(
            velocity,
            groundNormal,
            forward,
            TankGameplayTuning.CrawlerContactLateralDamping);

        Assert.That(Vector3.Dot(damped, right), Is.EqualTo(5f * (1f - TankGameplayTuning.CrawlerContactLateralDamping)).Within(0.01f));
        Assert.That(Vector3.Dot(damped, forward), Is.GreaterThan(3.4f));
    }

    [Test]
    public void VoidFallPinnedCamera_HoldsPositionAndLooksAtFallingTank()
    {
        GameObject cameraObject = new GameObject("VoidCamera");
        GameObject tankObject = new GameObject("FallingTank");
        cameraObject.transform.position = new Vector3(100f, 100f, 100f);
        tankObject.transform.position = new Vector3(0f, -20f, 20f);

        VoidFallPinnedCamera pinned = cameraObject.AddComponent<VoidFallPinnedCamera>();
        pinned.target = tankObject.transform;
        pinned.pinnedPosition = new Vector3(0f, 6f, -16f);

        pinned.ApplyNow();

        Assert.That(cameraObject.transform.position, Is.EqualTo(pinned.pinnedPosition));
        Assert.That(Vector3.Dot(cameraObject.transform.forward, (tankObject.transform.position - pinned.pinnedPosition).normalized), Is.GreaterThan(0.99f));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(tankObject);
    }

    [Test]
    public void VoidFall_DefaultsDelayScreenAndAccelerateFallingTank()
    {
        GameObject root = CreateMinimalTank();
        TankVoidFallController voidFall = TankVoidFallController.Ensure(root.GetComponent<TankController>(), useRangeBounds: false);

        Assert.That(voidFall.fallScreamDelaySeconds, Is.GreaterThanOrEqualTo(1.1f));
        Assert.That(voidFall.voidFallScreenDelaySeconds, Is.GreaterThanOrEqualTo(1.0f));
        Assert.That(voidFall.voidFallReturnDelaySeconds, Is.GreaterThanOrEqualTo(4.4f));
        Assert.That(voidFall.voidFallDownwardSpeed, Is.EqualTo(32f));
        Assert.That(voidFall.voidFallSpinSpeed, Is.EqualTo(10f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void OverdriveSmokeProfile_UsesBiggerLongerRandomPuffs()
    {
        Assert.That(TankGameplayTuning.OverdriveSmokeMinPuffs, Is.EqualTo(4));
        Assert.That(TankGameplayTuning.OverdriveSmokeMaxPuffsExclusive, Is.EqualTo(9));
        Assert.That(TankGameplayTuning.OverdriveSmokeMinStartSize, Is.EqualTo(3.2f));
        Assert.That(TankGameplayTuning.OverdriveSmokeMaxStartSize, Is.EqualTo(8.8f));
        Assert.That(TankGameplayTuning.OverdriveSmokeMinLifetime, Is.EqualTo(2.4f));
        Assert.That(TankGameplayTuning.OverdriveSmokeMaxLifetime, Is.EqualTo(4.8f));
        Assert.That(TankGameplayTuning.OverdriveSmokeMinParticlesPerPuff, Is.EqualTo(16));
        Assert.That(TankGameplayTuning.OverdriveSmokeMaxParticlesPerPuffExclusive, Is.EqualTo(45));
        Assert.That(TankGameplayTuning.OverdriveSmokeMaxParticles, Is.EqualTo(192));
    }

    private static GameObject CreateMinimalTank()
    {
        GameObject root = new GameObject("PolicyTank");
        root.AddComponent<Rigidbody>();
        root.AddComponent<AudioSource>();

        Transform turret = new GameObject("TurretYawPivot").transform;
        turret.SetParent(root.transform, false);
        turret.localPosition = new Vector3(0f, 1f, 0f);
        Transform barrel = new GameObject("BarrelPitchPivot").transform;
        barrel.SetParent(turret, false);
        barrel.localPosition = new Vector3(0f, 0.2f, 0.6f);
        Transform firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(barrel, false);
        firePoint.localPosition = new Vector3(0f, 0f, 1f);

        TankController tank = root.AddComponent<TankController>();
        tank.turretYawPivot = turret;
        tank.barrelPitchPivot = barrel;
        tank.cannonFirePoint = firePoint;
        tank.machineGunFirePoint = firePoint;
        root.AddComponent<TankMachineGun>();
        root.AddComponent<SniperRangeFinder>();

        GameObject cameraObject = new GameObject("PolicyCamera");
        cameraObject.transform.SetParent(root.transform, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        TankBarrelScopeCamera scopeCamera = cameraObject.AddComponent<TankBarrelScopeCamera>();
        scopeCamera.sight = root.transform;
        tank.gameplayCamera = camera;

        return root;
    }
}
#endif
