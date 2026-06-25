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
        Assert.That(root.GetComponent<TankMachineGun>().allowInputFire, Is.False);
        Assert.That(root.GetComponent<SniperRangeFinder>().allowScope, Is.False);
        Assert.That(tank.gameplayCamera.GetComponent<TankBarrelScopeCamera>().allowScope, Is.False);
        Assert.That(tank.disableRolloverDefeat, Is.True);

        Object.DestroyImmediate(root);
    }

    [Test]
    public void TargetPractice_LocksDriveAndAllowsTurretScopeAndCannon()
    {
        GameObject root = CreateMinimalTank();
        TankController tank = root.GetComponent<TankController>();
        PracticeModeInputPolicy policy = root.AddComponent<PracticeModeInputPolicy>();
        policy.mode = PracticeModeInputPolicy.PracticeControlMode.TargetPractice;

        policy.ApplyTo(tank);

        Assert.That(tank.allowDrivingInput, Is.False);
        Assert.That(tank.allowTurretInput, Is.True);
        Assert.That(tank.allowCannonInput, Is.True);
        Assert.That(root.GetComponent<TankMachineGun>().allowInputFire, Is.False);
        Assert.That(root.GetComponent<SniperRangeFinder>().allowScope, Is.True);
        Assert.That(tank.gameplayCamera.GetComponent<TankBarrelScopeCamera>().allowScope, Is.True);

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
        TankDriveController drive = root.AddComponent<TankDriveController>();
        PracticeModeInputPolicy policy = root.AddComponent<PracticeModeInputPolicy>();
        policy.mode = PracticeModeInputPolicy.PracticeControlMode.War;

        policy.ApplyTo(tank);

        Assert.That(drive.maxForwardSpeed, Is.EqualTo(TankDrivingProfile.MaxForwardSpeed));
        Assert.That(drive.maxClimbSlopeDegrees, Is.EqualTo(TankDrivingProfile.MaxClimbSlopeDegrees));

        Object.DestroyImmediate(root);
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
