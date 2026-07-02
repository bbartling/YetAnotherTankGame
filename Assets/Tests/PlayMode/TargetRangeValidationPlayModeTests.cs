#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TargetRangeValidationPlayModeTests
{
    [Test]
    public void TargetRange_PlacesTankOnGroundAndAllowsDriving()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "RangeGround";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(12f, 1f, 12f);
        Physics.SyncTransforms();

        GameObject tankRoot = CreateMinimalTank();
        tankRoot.transform.position = new Vector3(2f, 40f, -18f);

        PracticeModeInputPolicy policy = tankRoot.AddComponent<PracticeModeInputPolicy>();
        policy.mode = PracticeModeInputPolicy.PracticeControlMode.TargetPractice;
        policy.ApplyTo(tankRoot.GetComponent<TankController>());

        TankController tank = tankRoot.GetComponent<TankController>();
        TargetRangeTankAnchor anchor = tankRoot.GetComponent<TargetRangeTankAnchor>();

        Assert.That(tank.allowDrivingInput, Is.True);
        Assert.That(anchor, Is.Not.Null);
        Assert.That(anchor.IsPlaced, Is.True);

        float groundY = anchor.SampleSupportSurfaceY();
        Assert.That(groundY, Is.GreaterThan(-0.5f));
        Assert.That(groundY, Is.LessThan(1.5f));

        float bottomY = float.MaxValue;
        Collider[] colliders = tankRoot.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || !colliders[i].enabled || colliders[i].isTrigger)
            {
                continue;
            }

            bottomY = Mathf.Min(bottomY, colliders[i].bounds.min.y);
        }

        Assert.That(bottomY, Is.EqualTo(groundY + anchor.hullClearance).Within(0.35f));
        Assert.That(tankRoot.GetComponent<TankVoidFallController>(), Is.Not.Null);

        Object.DestroyImmediate(tankRoot);
        Object.DestroyImmediate(ground);
    }

    [Test]
    public void FireCannon_SpawnsShellForwardOfBarrel()
    {
        GameObject tankRoot = CreateMinimalTank();
        TankController tank = tankRoot.GetComponent<TankController>();
        GameObject shellPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shellPrefab.AddComponent<Rigidbody>();
        shellPrefab.AddComponent<ProjectileCameraController>();

        ProjectileCameraController[] existing = Object.FindObjectsByType<ProjectileCameraController>(FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null)
            {
                Object.DestroyImmediate(existing[i].gameObject);
            }
        }

        tank.shellPrefab = shellPrefab;
        tank.allowCannonInput = true;
        tank.leftClickFiresCannon = true;
        tank.cannonFirePoint = tank.barrelPitchPivot != null
            ? tank.barrelPitchPivot.Find("FirePoint") ?? tank.cannonFirePoint
            : tank.cannonFirePoint;
        tank.PrepareForGameplay();
        tank.shellPrefab = shellPrefab;
        tank.FireCannon();

        ProjectileCameraController[] shells = Object.FindObjectsByType<ProjectileCameraController>(FindObjectsSortMode.None);
        shells = System.Array.FindAll(shells, s => s != null && s.gameObject != shellPrefab);
        Assert.That(shells.Length, Is.EqualTo(1));
        float separation = Vector3.Distance(shells[0].transform.position, tank.cannonFirePoint.position);
        Assert.That(separation, Is.GreaterThan(0.8f));

        Object.DestroyImmediate(shells[0].gameObject);
        Object.DestroyImmediate(shellPrefab);
        Object.DestroyImmediate(tankRoot);
    }

    private static GameObject CreateMinimalTank()
    {
        GameObject root = new GameObject("RangeTank");
        root.AddComponent<Rigidbody>();
        root.AddComponent<BoxCollider>();
        root.AddComponent<AudioSource>();
        root.AddComponent<TankAudioController>();

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
        Camera camera = cameraObject.AddComponent<Camera>();
        TankBarrelScopeCamera scopeCamera = cameraObject.AddComponent<TankBarrelScopeCamera>();
        scopeCamera.sight = root.transform;
        tank.gameplayCamera = camera;

        return root;
    }
}
#endif
