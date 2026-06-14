#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TankCorePolicyPlayModeTests
{
    [Test]
    public void TankMovement_WheeledVehicleHasDeliberateRoadSpeed()
    {
        GameObject tankObject = new GameObject("PolicyTank");
        TankDriveController drive = tankObject.AddComponent<TankDriveController>();

        Assert.That(drive.maxForwardSpeed, Is.EqualTo(7.5f).Within(0.1f));
        Assert.That(drive.maxReverseSpeed, Is.EqualTo(3.5f).Within(0.1f));
        Assert.That(drive.accelerationSeconds, Is.EqualTo(4f).Within(0.25f));
        Assert.That(drive.brakingSeconds, Is.EqualTo(2f).Within(0.25f));
        Assert.That(drive.GetSteeringMultiplier(drive.maxForwardSpeed), Is.LessThan(0.55f));

        Object.Destroy(tankObject);
    }

    [Test]
    public void TankMovement_UpgradesLegacySerializedValuesToWheeledPolicy()
    {
        GameObject tankObject = new GameObject("LegacyFastTank");
        tankObject.SetActive(false);
        TankDriveController drive = tankObject.AddComponent<TankDriveController>();
        drive.maxForwardSpeed = 13f;
        drive.maxReverseSpeed = 7.5f;
        drive.accelerationSeconds = 2.4f;

        tankObject.SetActive(true);

        Assert.That(drive.maxForwardSpeed, Is.EqualTo(7.5f).Within(0.1f));
        Assert.That(drive.maxReverseSpeed, Is.EqualTo(3.5f).Within(0.1f));
        Assert.That(drive.accelerationSeconds, Is.EqualTo(4f).Within(0.25f));

        Object.Destroy(tankObject);
    }

    [Test]
    public void TankSlope_StrugglesOnSteepSlope()
    {
        GameObject tankObject = new GameObject("SlopePolicyTank");
        TankDriveController drive = tankObject.AddComponent<TankDriveController>();

        float levelLimit = drive.GetForwardSpeedLimit(0f);
        float uphillLimit = drive.GetForwardSpeedLimit(drive.tractionLossSlopeDegrees + 5f);
        float blockedLimit = drive.GetForwardSpeedLimit(drive.maxClimbSlopeDegrees + 1f);

        Assert.That(uphillLimit, Is.LessThan(levelLimit));
        Assert.That(blockedLimit, Is.GreaterThan(0f));
        Assert.That(drive.GetEngineStrain(1f, drive.tractionLossSlopeDegrees + 5f), Is.GreaterThan(0.5f));

        Object.Destroy(tankObject);
    }

    [Test]
    public void SuspensionVisual_AlignsUpAxisToSlope()
    {
        Quaternion aligned = TankSuspensionVisual.CalculateTargetRotation(
            Quaternion.identity,
            Vector3.forward,
            Quaternion.AngleAxis(20f, Vector3.right) * Vector3.up);

        Assert.That(Vector3.Angle(aligned * Vector3.up, Quaternion.AngleAxis(20f, Vector3.right) * Vector3.up), Is.LessThan(0.1f));
    }

    [Test]
    public void Turret_RotatesIndependentOfHull()
    {
        GameObject hull = new GameObject("Hull");
        GameObject turret = new GameObject("Turret");
        turret.transform.SetParent(hull.transform, false);
        TankTurretController controller = hull.AddComponent<TankTurretController>();
        controller.turretYawPivot = turret.transform;

        Quaternion hullBefore = hull.transform.rotation;
        controller.SetDesiredYawForTest(45f);
        controller.TickRotation(1f);

        Assert.That(Quaternion.Angle(hullBefore, hull.transform.rotation), Is.LessThan(0.01f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, turret.transform.localEulerAngles.y)), Is.GreaterThan(10f));

        Object.Destroy(hull);
    }

    [Test]
    public void TankStability_UsesHeavyLowCenterOfMassWithoutRotationConstraints()
    {
        GameObject tankObject = new GameObject("HeavyTank");
        tankObject.SetActive(false);
        Rigidbody body = tankObject.AddComponent<Rigidbody>();
        TankController tank = tankObject.AddComponent<TankController>();
        typeof(TankController)
            .GetField("_rb", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(tank, body);
        typeof(TankController)
            .GetMethod("ConfigureRigidbody", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(tank, null);

        Assert.That(body.mass, Is.GreaterThanOrEqualTo(15000f));
        Assert.That(body.centerOfMass.y, Is.LessThanOrEqualTo(-1f));
        Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.None));
        Assert.That(body.angularDamping, Is.GreaterThanOrEqualTo(4f));
        Object.DestroyImmediate(tankObject);
    }

    [Test]
    public void TankPresentation_UsesRealVehicleScaleForPlayableCameraFraming()
    {
        GameObject tankObject = new GameObject("ScalePolicyTank");
        tankObject.SetActive(false);
        tankObject.AddComponent<Rigidbody>();
        TankController tank = tankObject.AddComponent<TankController>();

        Assert.That(tank.playerVisualScale, Is.InRange(2.5f, 3.5f));
        Object.DestroyImmediate(tankObject);
    }

    [Test]
    public void RolloverDefeat_RequiresSustainedOverturn()
    {
        TankRolloverController rollover = new TankRolloverController();

        Assert.That(rollover.Tick(80f, 1.9f), Is.False);
        Assert.That(rollover.Tick(20f, 0.1f), Is.False);
        Assert.That(rollover.OverturnedSeconds, Is.Zero);
        Assert.That(rollover.Tick(80f, 2.1f), Is.True);
    }

    [Test]
    public void RolloverDefeat_MarksPlayerTankDestroyed()
    {
        GameObject tankObject = new GameObject("OverturnedPlayer");
        tankObject.SetActive(false);
        Rigidbody body = tankObject.AddComponent<Rigidbody>();
        TankController tank = tankObject.AddComponent<TankController>();
        tank.rolloverDefeatDelay = 0f;
        tankObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        typeof(TankController)
            .GetField("_rb", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(tank, body);

        bool defeated = (bool)typeof(TankController)
            .GetMethod("CheckRolloverDefeat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(tank, null);

        Assert.That(defeated, Is.True);
        Assert.That(tank.IsDestroyed, Is.True);
        Object.DestroyImmediate(tankObject);
    }
}
#endif
