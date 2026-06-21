#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class SillyModelInstallerPlayModeTests
{
    [Test]
    public void Installer_LoadsRequiredPlayerTankModel()
    {
        GameObject root = new GameObject("ModelInstallRoot");

        SillyModelInstaller installer = SillyModelInstaller.Ensure(
            root,
            "Models/Tanks/SillyPlayerTank",
            1f,
            false);

        Assert.That(installer, Is.Not.Null);
        Assert.That(installer.InstalledVisual, Is.Not.Null);
        Assert.That(installer.InstalledVisual.transform.Find("Hull"), Is.Not.Null);
        Assert.That(installer.InstalledVisual.transform.Find("Hull_Damaged").gameObject.activeSelf, Is.False);
        for (int i = 0; i < 4; i++)
        {
            Assert.That(installer.InstalledVisual.transform.Find($"LeftWheel_{i}"), Is.Not.Null);
            Assert.That(installer.InstalledVisual.transform.Find($"RightWheel_{i}"), Is.Not.Null);
        }
        Assert.That(installer.InstalledVisual.transform.Find("LeftTrack"), Is.Null);
        Assert.That(installer.InstalledVisual.transform.Find("RightTrack"), Is.Null);

        installer.ReplaceResource("Models/Tanks/SillyEnemyScout", 1f);
        Assert.That(installer.resourcePath, Is.EqualTo("Models/Tanks/SillyEnemyScout"));
        Assert.That(installer.InstalledVisual.transform.Find("Hull"), Is.Not.Null);

        Object.DestroyImmediate(root);
    }

    [Test]
    public void Installer_HidesLegacyPrimitiveRenderersWhenUsingBlenderModel()
    {
        GameObject root = new GameObject("LegacyVisualRoot");
        GameObject legacyBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legacyBlock.name = "LegacyBlockTankVisual";
        legacyBlock.transform.SetParent(root.transform, false);
        MeshRenderer legacyRenderer = legacyBlock.GetComponent<MeshRenderer>();

        SillyModelInstaller installer = SillyModelInstaller.Ensure(
            root,
            "Models/Tanks/SillyPlayerTank",
            1f,
            true);

        Assert.That(installer.InstalledVisual, Is.Not.Null);
        Assert.That(installer.InstalledVisual.transform.Find("Hull"), Is.Not.Null);
        Assert.That(legacyRenderer.enabled, Is.False);

        Object.DestroyImmediate(root);
    }

    [Test]
    public void Installer_BindsVisibleTurretAndBarrelToGameplayPivots()
    {
        GameObject root = new GameObject("PivotInstallRoot");
        root.SetActive(false);
        TankController tank = root.AddComponent<TankController>();
        tank.turretYawPivot = new GameObject("TurretYawPivot").transform;
        tank.turretYawPivot.SetParent(root.transform, false);
        tank.barrelPitchPivot = new GameObject("BarrelPitchPivot").transform;
        tank.barrelPitchPivot.SetParent(tank.turretYawPivot, false);

        SillyModelInstaller installer = SillyModelInstaller.Ensure(root, "Models/Tanks/SillyPlayerTank", 1f, false);
        VisualPivotFollower turretFollower = installer.InstalledVisual.transform.Find("Turret").GetComponent<VisualPivotFollower>();
        VisualPivotFollower barrelFollower = installer.InstalledVisual.transform.Find("Barrel").GetComponent<VisualPivotFollower>();

        Assert.That(turretFollower.target, Is.SameAs(tank.turretYawPivot));
        Assert.That(barrelFollower.target, Is.SameAs(tank.barrelPitchPivot));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void VisualPivotFollower_KeepsBarrelVisualAttachedToMovingPitchPivot()
    {
        GameObject root = new GameObject("VisualFollowerRoot");
        Transform pitchPivot = new GameObject("BarrelPitchPivot").transform;
        pitchPivot.SetParent(root.transform, false);
        pitchPivot.localPosition = new Vector3(0f, 1.25f, 0.6f);
        pitchPivot.localRotation = Quaternion.identity;

        GameObject visualObject = new GameObject("BarrelVisual");
        visualObject.transform.SetParent(root.transform, false);
        visualObject.transform.position = pitchPivot.position;
        visualObject.transform.rotation = pitchPivot.rotation;

        VisualPivotFollower follower = visualObject.AddComponent<VisualPivotFollower>();
        follower.Bind(pitchPivot);

        pitchPivot.localPosition = new Vector3(0.45f, 1.25f, 0.85f);
        pitchPivot.localRotation = Quaternion.Euler(-8f, 35f, 0f);
        follower.SendMessage("LateUpdate");

        Assert.That(Vector3.Distance(visualObject.transform.position, pitchPivot.position), Is.LessThan(0.01f));
        Assert.That(Quaternion.Angle(visualObject.transform.rotation, pitchPivot.rotation), Is.LessThan(0.01f));

        Object.DestroyImmediate(root);
    }
}
#endif
