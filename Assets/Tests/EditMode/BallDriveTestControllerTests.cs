using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BallDriveTestControllerTests
{
    [Test]
    public void BuildCameraRelativeInput_UsesCameraYawForForward()
    {
        MethodInfo method = typeof(BallDriveTestController).GetMethod(
            "BuildCameraRelativeInput",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(method, Is.Not.Null, "Ball drive needs a public testable camera-relative input helper.");

        Vector3 direction = (Vector3)method.Invoke(null, new object[] { new Vector2(0f, 1f), 90f });

        Assert.That(direction.x, Is.EqualTo(1f).Within(0.001f));
        Assert.That(direction.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(direction.z, Is.EqualTo(0f).Within(0.001f));
    }

}
