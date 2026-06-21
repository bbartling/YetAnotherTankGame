#if UNITY_EDITOR
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class TankCameraPlayModeTests
{
    [Test]
    public void ChaseCamera_DefaultsProvideRaisedTacticalView()
    {
        GameObject cameraObject = new GameObject("ChaseCamera");
        TankOrbitCamera orbit = cameraObject.AddComponent<TankOrbitCamera>();

        Assert.That(orbit.targetOffset.y, Is.GreaterThanOrEqualTo(7f));
        Assert.That(orbit.cameraHeight, Is.GreaterThanOrEqualTo(9f));
        Assert.That(orbit.followDistance, Is.GreaterThanOrEqualTo(24f));
        Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void ScopeCamera_ApplyScopePoseAlignsToBarrelSight()
    {
        GameObject cameraObject = new GameObject("ScopeCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        TankBarrelScopeCamera scope = cameraObject.AddComponent<TankBarrelScopeCamera>();
        Transform sight = new GameObject("FirePoint").transform;
        sight.SetPositionAndRotation(new Vector3(3f, 4f, 5f), Quaternion.Euler(-12f, 35f, 0f));
        scope.sight = sight;

        scope.ApplyScopePose();

        Assert.That(Vector3.Angle(camera.transform.forward, sight.forward), Is.LessThan(0.1f));
        Assert.That(camera.fieldOfView, Is.EqualTo(scope.scopeFieldOfView).Within(0.01f));
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(sight.gameObject);
    }

    [Test]
    public void ChaseCamera_CollisionSlidesIntoLowForwardViewInsteadOfTopDown()
    {
        GameObject target = new GameObject("CameraTarget");
        target.transform.position = Vector3.zero;
        GameObject sight = new GameObject("Sight");
        sight.transform.SetParent(target.transform, false);
        sight.transform.localRotation = Quaternion.identity;

        GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name = "BehindBlocker";
        blocker.transform.position = new Vector3(0f, 2f, -2f);
        blocker.transform.localScale = new Vector3(4f, 4f, 0.5f);
        Physics.SyncTransforms();

        GameObject cameraObject = new GameObject("ChaseCamera");
        TankOrbitCamera orbit = cameraObject.AddComponent<TankOrbitCamera>();
        orbit.target = target.transform;
        orbit.aimDirectionSource = sight.transform;
        orbit.targetOffset = new Vector3(0f, 2f, 0f);
        orbit.followDistance = 12f;
        orbit.cameraHeight = 8f;
        orbit.positionSmoothTime = 0f;
        orbit.collisionMask = ~0;

        orbit.SendMessage("LateUpdate");

        Assert.That(cameraObject.transform.position.y, Is.LessThan(5f));
        Assert.That(Vector3.Dot(cameraObject.transform.forward, sight.transform.forward), Is.GreaterThan(0.45f));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(blocker);
        Object.DestroyImmediate(target);
    }

    [Test]
    public void ProjectileCamera_DisablesLifetimeSelfDestructForFlyingShells()
    {
        GameObject projectile = new GameObject("TimerFreeProjectile");
        ProjectileCameraController controller = projectile.AddComponent<ProjectileCameraController>();
        controller.baseSelfDestructSeconds = 0.05f;

        MethodInfo lifetimeMethod = typeof(ProjectileCameraController).GetMethod("GetSelfDestructSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
        float lifetime = (float)lifetimeMethod.Invoke(controller, null);
        Assert.That(float.IsPositiveInfinity(lifetime), Is.True, "Projectile flight must end by impact/cancel, not by a lifetime timer.");

        Object.DestroyImmediate(projectile);
    }
}
#endif
