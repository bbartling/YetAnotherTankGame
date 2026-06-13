#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class WheeledTankHandlingPlayModeTests
{
    [Test]
    public void WheeledSuspension_DefinesEightIndependentWheelMounts()
    {
        GameObject root = new GameObject("WheeledTank");
        WheeledSuspensionController suspension = root.AddComponent<WheeledSuspensionController>();

        Assert.That(suspension.WheelCount, Is.EqualTo(8));
        Assert.That(suspension.GetLocalWheelMount(0).x, Is.LessThan(0f));
        Assert.That(suspension.GetLocalWheelMount(4).x, Is.GreaterThan(0f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void WheeledSuspension_ComputesSpringForceFromCompressionAndDamping()
    {
        float force = WheeledSuspensionController.CalculateSpringForce(
            compression: 0.75f,
            springStrength: 30f,
            contactVelocity: -1.5f,
            damperStrength: 8f);

        Assert.That(force, Is.EqualTo(34.5f).Within(0.01f));
    }

    [Test]
    public void TankController_NormalDrivingDoesNotUseHighestCornerTerrainSnap()
    {
        GameObject root = new GameObject("CraterFollower");
        root.SetActive(false);
        root.AddComponent<Rigidbody>();
        TankController tank = root.AddComponent<TankController>();
        GameObject turret = new GameObject("TurretYawPivot");
        turret.transform.SetParent(root.transform, false);
        GameObject barrel = new GameObject("BarrelPivot");
        barrel.transform.SetParent(turret.transform, false);
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(barrel.transform, false);
        tank.turretYawPivot = turret.transform;
        tank.barrelPitchPivot = barrel.transform;
        tank.cannonFirePoint = firePoint.transform;
        root.SetActive(true);

        Assert.That(tank.continuousTerrainSnapEnabled, Is.False);

        Object.DestroyImmediate(root);
    }
}
#endif
