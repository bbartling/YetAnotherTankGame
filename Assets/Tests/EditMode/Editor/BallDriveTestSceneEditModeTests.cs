#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

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
        Assert.That(ballRenderer.sharedMaterial.color.b, Is.GreaterThan(ballRenderer.sharedMaterial.color.r));

        System.Type indicatorType = System.Type.GetType("BallDriveSlopeIndicator, Assembly-CSharp");
        Assert.That(indicatorType, Is.Not.Null);

        GameObject indicator = GameObject.Find("BallDriveSlopeIndicator");
        Assert.That(indicator, Is.Not.Null);
        Assert.That(indicator.transform.parent, Is.Null, "Indicator must not inherit the rolling ball transform.");
        Assert.That(indicator.transform.localScale.x, Is.GreaterThanOrEqualTo(24f));
        Assert.That(indicator.GetComponent<Collider>(), Is.Null, "Indicator must be visual-only.");

        Renderer indicatorRenderer = indicator.GetComponentInChildren<Renderer>();
        Assert.That(indicatorRenderer, Is.Not.Null);
        Assert.That(indicatorRenderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
    }
}
#endif
