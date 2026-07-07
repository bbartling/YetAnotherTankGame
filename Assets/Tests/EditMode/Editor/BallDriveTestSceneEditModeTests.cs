#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class BallDriveTestSceneEditModeTests
{
    [Test]
    public void BallDriveTestScene_UsesSmallerHeavyPowerBallTuning()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BallDriveTest.unity");

        BallDriveTestController controller = Object.FindAnyObjectByType<BallDriveTestController>();
        Assert.That(controller, Is.Not.Null);

        Rigidbody body = controller.GetComponent<Rigidbody>();
        Assert.That(body, Is.Not.Null);
        Assert.That(controller.transform.localScale.x, Is.EqualTo(12f).Within(0.01f));
        Assert.That(body.mass, Is.GreaterThanOrEqualTo(28800f));
        Assert.That(controller.gravityMultiplier, Is.EqualTo(10f).Within(0.01f));
        Assert.That(controller.maxAngularVelocity, Is.GreaterThanOrEqualTo(30f));
        Assert.That(controller.rollForce, Is.GreaterThanOrEqualTo(85f));
        Assert.That(controller.rollTorque, Is.GreaterThanOrEqualTo(120f));
        Assert.That(controller.maxSpeed, Is.GreaterThanOrEqualTo(24f));
        Assert.That(controller.groundCheckDistance, Is.InRange(6.25f, 7.25f));

        Renderer ballRenderer = controller.GetComponent<Renderer>();
        Assert.That(ballRenderer.sharedMaterial, Is.Not.Null);
        Assert.That(ballRenderer.enabled, Is.False, "The rolling ball should keep physics but be visually hidden.");

        BallDriveTankVisualFollower visualFollower = controller.GetComponent<BallDriveTankVisualFollower>();
        Assert.That(visualFollower, Is.Not.Null, "BallDriveTest should show a tank visual riding on the hidden ball.");
        Assert.That(visualFollower.visualRoot, Is.Not.Null);
        Transform authoredTank = visualFollower.visualRoot.Find("SillyPlayerTank");
        Assert.That(authoredTank, Is.Not.Null, "BallDriveTest must use the existing authored SillyPlayerTank visual, not primitive proxy geometry.");
        Assert.That(authoredTank.lossyScale.x, Is.EqualTo(3f).Within(0.05f), "Authored tank visual should match TankDrivingPractice scale.");
        Assert.That(authoredTank.lossyScale.y, Is.EqualTo(3f).Within(0.05f), "Authored tank visual should match TankDrivingPractice scale.");
        Assert.That(authoredTank.lossyScale.z, Is.EqualTo(3f).Within(0.05f), "Authored tank visual should match TankDrivingPractice scale.");
        Assert.That(visualFollower.visualRoot.parent, Is.Null, "Tank visual must not inherit the hidden ball scale or rolling transform.");
        Assert.That(visualFollower.snapVisualToGround, Is.True);
        Assert.That(visualFollower.groundSnapDistance, Is.GreaterThanOrEqualTo(20f));
        Assert.That(visualFollower.visualRoot.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0));
        Assert.That(visualFollower.visualRoot.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(0));
        Assert.That(visualFollower.visualRoot.GetComponentsInChildren<Rigidbody>(true).Length, Is.EqualTo(0));

        foreach (Transform part in authoredTank.GetComponentsInChildren<Transform>(true))
        {
            if (part.name.Contains("Damaged"))
            {
                Assert.That(part.gameObject.activeSelf, Is.False, $"{part.name} should be hidden until damage state is active.");
            }
        }

        BallDriveOverdriveSmokeController smoke = controller.GetComponent<BallDriveOverdriveSmokeController>();
        Assert.That(smoke, Is.Not.Null, "BallDriveTest should preserve the black overdrive exhaust puff on the hidden-ball tank.");
        Assert.That(smoke.exhaustPoint, Is.Not.Null);
        Assert.That(smoke.exhaustPoint.name, Is.EqualTo("ExhaustPoint"));
        Assert.That(smoke.exhaustPoint.parent, Is.EqualTo(visualFollower.visualRoot));

        GameObject indicator = GameObject.Find("BallDriveSlopeIndicator");
        Assert.That(indicator, Is.Null, "BallDriveTest should not show the old blue slope indicator bar.");
    }
}
#endif
