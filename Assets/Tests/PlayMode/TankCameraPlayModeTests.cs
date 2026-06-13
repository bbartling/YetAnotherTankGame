#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TankCameraPlayModeTests
{
    [Test]
    public void ChaseCamera_DefaultsProvideRaisedTacticalView()
    {
        GameObject cameraObject = new GameObject("ChaseCamera");
        TankOrbitCamera orbit = cameraObject.AddComponent<TankOrbitCamera>();

        Assert.That(orbit.targetOffset.y, Is.GreaterThanOrEqualTo(4.2f));
        Assert.That(orbit.followDistance, Is.GreaterThanOrEqualTo(10f));
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
}
#endif
