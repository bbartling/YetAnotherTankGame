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

    [Test]
    public void BuildTankDriveDirection_UsesTankYawForForwardAndReverse()
    {
        Vector3 forward = BallDriveTestController.BuildTankDriveDirection(1f, 90f);
        Vector3 reverse = BallDriveTestController.BuildTankDriveDirection(-1f, 90f);

        Assert.That(forward.x, Is.EqualTo(1f).Within(0.001f));
        Assert.That(forward.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(forward.z, Is.EqualTo(0f).Within(0.001f));
        Assert.That(reverse.x, Is.EqualTo(-1f).Within(0.001f));
        Assert.That(reverse.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(reverse.z, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void CalculateTankYaw_TurnsLeftAndRightFromCurrentHeading()
    {
        float right = BallDriveTestController.CalculateTankYaw(10f, 1f, 90f, 0.5f);
        float left = BallDriveTestController.CalculateTankYaw(10f, -1f, 90f, 0.5f);

        Assert.That(right, Is.EqualTo(55f).Within(0.001f));
        Assert.That(left, Is.EqualTo(325f).Within(0.001f));
    }
}
