#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TankPathingBrainPlayModeTests
{
    [Test]
    public void StandoffBandStopsInsteadOfRushing()
    {
        TankPathingBrain brain = new TankPathingBrain();

        Assert.That(brain.DecideMotion(220f, 220f, 100f), Is.EqualTo(TankPathingBrain.Motion.Stop));
        Assert.That(brain.DecideMotion(350f, 220f, 100f), Is.EqualTo(TankPathingBrain.Motion.Advance));
        Assert.That(brain.DecideMotion(70f, 220f, 100f), Is.EqualTo(TankPathingBrain.Motion.Retreat));
    }

    [Test]
    public void VelocityIsCappedAtSlowTankSpeed()
    {
        TankPathingBrain brain = new TankPathingBrain { MaxTravelSpeed = 2.25f };

        Vector3 capped = brain.ClampPlanarVelocity(new Vector3(12f, 3f, 0f));

        Assert.That(new Vector2(capped.x, capped.z).magnitude, Is.EqualTo(2.25f).Within(0.001f));
        Assert.That(capped.y, Is.EqualTo(3f));
    }

    [Test]
    public void ExcessiveSlopeStopsAdvance()
    {
        TankPathingBrain brain = new TankPathingBrain { MaxSlopeDegrees = 35f };

        Assert.That(brain.CanAdvanceOnSlope(30f), Is.True);
        Assert.That(brain.CanAdvanceOnSlope(40f), Is.False);
    }
}
#endif
