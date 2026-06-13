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
}
#endif
