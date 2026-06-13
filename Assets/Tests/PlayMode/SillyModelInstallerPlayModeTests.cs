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

        installer.ReplaceResource("Models/Tanks/SillyEnemyScout", 1f);
        Assert.That(installer.resourcePath, Is.EqualTo("Models/Tanks/SillyEnemyScout"));
        Assert.That(installer.InstalledVisual.transform.Find("Hull"), Is.Not.Null);

        Object.DestroyImmediate(root);
    }
}
#endif
