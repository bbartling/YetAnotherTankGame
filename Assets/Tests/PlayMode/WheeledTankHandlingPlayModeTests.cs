#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class WheeledTankHandlingPlayModeTests
{
    [Test]
    public void GameplayTestApi_ReportsGroundedWheelCount()
    {
        GameObject tankObject = new GameObject("TestApiWheelTank");
        tankObject.SetActive(false);
        TankController tank = tankObject.AddComponent<TankController>();
        WheeledSuspensionController suspension = tankObject.AddComponent<WheeledSuspensionController>();
        typeof(WheeledSuspensionController)
            .GetProperty(nameof(WheeledSuspensionController.GroundedWheelCount))
            .GetSetMethod(true)
            .Invoke(suspension, new object[] { 6 });
        typeof(TankController)
            .GetField("_wheeledSuspension", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(tank, suspension);
        GameObject apiObject = new GameObject("GameplayTestApi");
        GameplayTestApi api = apiObject.AddComponent<GameplayTestApi>();
        api.playerTank = tank;

        Assert.That(api.PlayerGroundedWheelCount, Is.EqualTo(6));
        Object.DestroyImmediate(apiObject);
        Object.DestroyImmediate(tankObject);
    }

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

    [Test]
    public void WheeledSuspension_IgnoresSelfColliderAndFindsGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "SelfColliderTank";
        root.transform.position = Vector3.up;
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        WheeledSuspensionController suspension = root.AddComponent<WheeledSuspensionController>();
        Physics.SyncTransforms();

        int grounded = suspension.ApplySuspension(body);

        Assert.That(grounded, Is.GreaterThan(0));
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(ground);
    }
}
#endif
