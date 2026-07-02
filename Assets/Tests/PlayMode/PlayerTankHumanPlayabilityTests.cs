#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PlayerTankHumanPlayabilityTests
{
    [Test]
    public void OrbitCamera_FramesBehindCannonDirectionInsteadOfHullOnly()
    {
        GameObject target = new GameObject("TankRoot");
        target.transform.rotation = Quaternion.identity;

        GameObject sightObject = new GameObject("CannonFirePoint");
        sightObject.transform.SetParent(target.transform, false);
        sightObject.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        GameObject cameraObject = new GameObject("TankCamera");
        cameraObject.transform.position = new Vector3(0f, 4f, -8f);
        TankOrbitCamera orbit = cameraObject.AddComponent<TankOrbitCamera>();
        orbit.target = target.transform;
        orbit.targetOffset = Vector3.up * 2f;
        orbit.followDistance = 8f;
        orbit.cameraHeight = 2f;
        orbit.positionSmoothTime = 0f;

        typeof(TankOrbitCamera)
            .GetField("aimDirectionSource", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(orbit, sightObject.transform);

        orbit.SendMessage("LateUpdate");

        Vector3 focus = target.transform.position + orbit.targetOffset;
        Vector3 expectedBehindGun = -sightObject.transform.forward;
        Vector3 actualFromFocus = (cameraObject.transform.position - focus).normalized;
        Assert.That(Vector3.Angle(actualFromFocus, expectedBehindGun), Is.LessThan(35f));

        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(target);
    }

    [Test]
    public void SniperRangeFinder_UsesAuthoritativeCannonFirePointWhenLegacyFirePointIsMissing()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = new Vector3(0f, 0f, 25f);

        GameObject tankObject = new GameObject("Tank");
        tankObject.SetActive(false);
        TankController tank = tankObject.AddComponent<TankController>();
        GameObject turret = new GameObject("TurretYawPivot");
        turret.transform.SetParent(tankObject.transform, false);
        GameObject barrel = new GameObject("BarrelPitchPivot");
        barrel.transform.SetParent(turret.transform, false);
        GameObject cannon = new GameObject("CannonFirePoint");
        cannon.transform.SetParent(barrel.transform, false);
        cannon.transform.localPosition = Vector3.up;
        cannon.transform.localRotation = Quaternion.identity;
        tank.turretYawPivot = turret.transform;
        tank.barrelPitchPivot = barrel.transform;
        tank.cannonFirePoint = cannon.transform;
        tank.firePoint = null;
        tank.maxPower = 50f;
        tank.powerPercentage = 100f;
        tankObject.SetActive(true);

        GameObject rangeFinderObject = new GameObject("RangeFinder");
        SniperRangeFinder rangeFinder = rangeFinderObject.AddComponent<SniperRangeFinder>();
        rangeFinder.tank = tank;
        rangeFinder.maxRange = 100f;

        MethodInfo estimate = typeof(SniperRangeFinder).GetMethod("EstimateBallisticRange", BindingFlags.Instance | BindingFlags.NonPublic);
        float range = (float)estimate.Invoke(rangeFinder, null);

        Assert.That(range, Is.GreaterThan(1f));

        Object.DestroyImmediate(rangeFinderObject);
        Object.DestroyImmediate(tankObject);
        Object.DestroyImmediate(ground);
    }

    [Test]
    public void TreeFieldSpawner_ProjectsTreesOnlyOntoTerrainWhenOtherCollidersAreAboveGround()
    {
        GameObject terrainObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        terrainObject.name = "CraterTerrainHost";
        terrainObject.transform.position = Vector3.zero;
        terrainObject.transform.localScale = new Vector3(100f, 1f, 100f);
        CraterTerrain terrain = terrainObject.AddComponent<CraterTerrain>();

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "FloatingCanopyDecoy";
        canopy.transform.position = new Vector3(0f, 18f, 0f);
        canopy.transform.localScale = Vector3.one * 12f;
        Physics.SyncTransforms();

        GameObject spawnerObject = new GameObject("TreeSpawner");
        TreeFieldSpawner spawner = spawnerObject.AddComponent<TreeFieldSpawner>();
        spawner.generateOnAwake = false;
        spawner.terrainSource = terrain;

        MethodInfo projectMethod = typeof(TreeFieldSpawner).GetMethod("TryProjectToGround", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] args = { new Vector3(0f, 60f, 0f), null };
        bool projected = (bool)projectMethod.Invoke(spawner, args);
        Vector3 hitPoint = (Vector3)args[1];

        Assert.That(projected, Is.True);
        Assert.That(hitPoint.y, Is.LessThan(1f), "Tree projection must ignore tree/canopy/legacy colliders and use the terrain surface.");

        Object.DestroyImmediate(spawnerObject);
        Object.DestroyImmediate(canopy);
        Object.DestroyImmediate(terrainObject);
    }

}
#endif
