using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

public class TankBuildPipelineEditModeTests
{
    [Test]
    public void RequiredBuildAndDeploymentFilesExist()
    {
        Assert.That(File.Exists("Assets/Editor/TankWebGLBuildPipeline.cs"), Is.True);
        Assert.That(File.Exists("pythonanywhere_flask/flask_app.py"), Is.True);
        Assert.That(File.Exists("pythonanywhere_flask/requirements.txt"), Is.True);
        Assert.That(File.Exists("scripts/build_webgl_pythonanywhere.ps1"), Is.True);
    }

    [Test]
    public void WebGLCompressionIsDisabledForPythonAnywhere()
    {
        Assert.That(PlayerSettings.WebGL.compressionFormat, Is.EqualTo(WebGLCompressionFormat.Disabled));
        Assert.That(PlayerSettings.WebGL.threadsSupport, Is.False);
    }

    [Test]
    public void BuildSceneAndDeploymentContractAreConfigured()
    {
        Assert.That(
            EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == "Assets/Scenes/Practice.unity"),
            Is.True);
        Assert.That(TankWebGLBuildPipeline.BuildOutputDirectory, Is.EqualTo("Builds/WebGL"));
        Assert.That(TankWebGLBuildPipeline.BuildManifestPath, Is.EqualTo("Builds/WebGL_BUILD_MANIFEST.json"));
        Assert.That(TankWebGLBuildPipeline.DeploymentWebGLDirectory, Is.EqualTo("pythonanywhere_flask/webgl"));
        Assert.That(TankWebGLBuildPipeline.DeploymentManifestPath, Is.EqualTo("pythonanywhere_flask/WEBGL_BUILD_MANIFEST.json"));
        Assert.That(TankWebGLBuildPipeline.ZipOutputPath, Is.EqualTo("tank_game_pythonanywhere.zip"));
    }
}
