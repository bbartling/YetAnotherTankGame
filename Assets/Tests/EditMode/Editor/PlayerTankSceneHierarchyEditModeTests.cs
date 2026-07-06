#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PlayerTankSceneHierarchyEditModeTests
{
    [Test]
    public void PracticeScene_PlayerTankHasCleanPlayablePivotHierarchy()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Practice.unity");

        GameObject player = GameObject.Find("PlayerTank");
        Assert.That(player, Is.Not.Null);
        Assert.That(player.transform.localScale.x, Is.EqualTo(1f).Within(0.01f));
        Assert.That(player.transform.localScale.y, Is.EqualTo(1f).Within(0.01f));
        Assert.That(player.transform.localScale.z, Is.EqualTo(1f).Within(0.01f));

        TankController tank = player.GetComponent<TankController>();
        Assert.That(tank, Is.Not.Null);
        Assert.That(tank.turretYawPivot, Is.Not.Null);
        Assert.That(tank.barrelPitchPivot, Is.Not.Null);
        Assert.That(tank.cannonFirePoint, Is.Not.Null);
        Assert.That(tank.turretYawPivot.localScale, Is.EqualTo(Vector3.one));
        Assert.That(tank.barrelPitchPivot.localScale, Is.EqualTo(Vector3.one));
        Assert.That(Vector3.Angle(tank.cannonFirePoint.forward, tank.barrelPitchPivot.forward), Is.LessThan(0.1f));

        Camera camera = tank.gameplayCamera;
        Assert.That(camera, Is.Not.Null);
        Assert.That(camera.GetComponent<TankOrbitCamera>().aimDirectionSource, Is.SameAs(tank.cannonFirePoint));
        Assert.That(camera.GetComponent<TankBarrelScopeCamera>().sight, Is.SameAs(tank.cannonFirePoint));

        AssertNoLegacyPrimitiveVisualIfPresent(player.transform, "BodyVisual");
        AssertNoLegacyPrimitiveVisual(player.transform, "TurretYawPivot/TurretVisual/TurretMesh");
        AssertNoLegacyPrimitiveVisual(player.transform, "TurretYawPivot/BarrelPitchPivot/BarrelVisual");
    }

    private static void AssertNoLegacyPrimitiveVisualIfPresent(Transform player, string path)
    {
        Transform target = player.Find(path);
        if (target == null)
        {
            return;
        }

        Assert.That(target.GetComponent<MeshRenderer>(), Is.Null, path);
        Assert.That(target.GetComponent<MeshFilter>(), Is.Null, path);
    }

    private static void AssertNoLegacyPrimitiveVisual(Transform player, string path)
    {
        Transform target = player.Find(path);
        Assert.That(target, Is.Not.Null, path);
        Assert.That(target.GetComponent<MeshRenderer>(), Is.Null, path);
        Assert.That(target.GetComponent<MeshFilter>(), Is.Null, path);
    }
}
#endif
