#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TankPerceptionPlayModeTests
{
    [Test]
    public void VisibleTarget_UpdatesLastKnownPositionAndMemory()
    {
        TankPerception perception = new TankPerception(6f);
        Vector3 target = new Vector3(12f, 0f, 40f);

        perception.Observe(true, target, 10f);

        Assert.That(perception.HasLineOfSight, Is.True);
        Assert.That(perception.LastKnownPosition, Is.EqualTo(target));
        Assert.That(perception.HasRecentMemory(15f), Is.True);
        Assert.That(perception.HasRecentMemory(17f), Is.False);
    }

    [Test]
    public void NoiseCreatesTemporaryAwarenessWithoutLineOfSight()
    {
        TankPerception perception = new TankPerception(6f);
        Vector3 noise = new Vector3(4f, 0f, 8f);

        perception.HearNoise(noise, 20f, 3f);

        Assert.That(perception.HasLineOfSight, Is.False);
        Assert.That(perception.LastKnownPosition, Is.EqualTo(noise));
        Assert.That(perception.HasRecentMemory(22f), Is.True);
        Assert.That(perception.HasRecentMemory(24f), Is.False);
    }
}
#endif
