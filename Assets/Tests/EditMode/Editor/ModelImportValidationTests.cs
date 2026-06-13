using System.IO;
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ModelImportValidationTests
{
    private static readonly string[] RequiredSources =
    {
        "BlenderSource/Tanks/SillyPlayerTank.blend",
        "BlenderSource/Tanks/SillyEnemyScout.blend",
        "BlenderSource/Tanks/SillyEnemyStandard.blend",
        "BlenderSource/Tanks/SillyEnemyCommander.blend",
        "BlenderSource/Castle/SillyCastleKit.blend",
        "BlenderSource/Turrets/SillyCastleTurrets.blend",
        "BlenderSource/Trees/SillyTreeKit.blend"
    };

    private static readonly string[] RequiredRuntimeModels =
    {
        "Assets/Resources/Models/Tanks/SillyPlayerTank.fbx",
        "Assets/Resources/Models/Tanks/SillyEnemyScout.fbx",
        "Assets/Resources/Models/Tanks/SillyEnemyStandard.fbx",
        "Assets/Resources/Models/Tanks/SillyEnemyCommander.fbx",
        "Assets/Resources/Models/Castle/SillyCastleKit.fbx",
        "Assets/Resources/Models/Turrets/SillyCastleTurrets.fbx",
        "Assets/Resources/Models/Trees/SillyTreeKit.fbx"
    };

    [Test]
    public void RequiredBlenderSourcesExist()
    {
        foreach (string path in RequiredSources)
        {
            Assert.That(File.Exists(path), Is.True, path);
        }
    }

    [Test]
    public void RequiredRuntimeModelsImportWithVisibleBounds()
    {
        foreach (string path in RequiredRuntimeModels)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(model, Is.Not.Null, path);
            Assert.That(ModelImportValidation.HasVisibleRendererBounds(model), Is.True, path);
        }
    }

    [Test]
    public void TankModelsContainGameplayPivots()
    {
        string[] tankPaths =
        {
            "Assets/Resources/Models/Tanks/SillyPlayerTank.fbx",
            "Assets/Resources/Models/Tanks/SillyEnemyScout.fbx",
            "Assets/Resources/Models/Tanks/SillyEnemyStandard.fbx",
            "Assets/Resources/Models/Tanks/SillyEnemyCommander.fbx"
        };

        foreach (string path in tankPaths)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(ModelImportValidation.HasRequiredTankParts(model), Is.True, path);
        }
    }
}
#endif
