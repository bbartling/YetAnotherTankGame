using NUnit.Framework;

public class TankCombatBrainPlayModeTests
{
    [Test]
    public void VisibleTarget_StopsToAimBeforeFiring()
    {
        TankCombatBrain brain = new TankCombatBrain();

        Assert.That(brain.Decide(true, 220f, 3f, false, false), Is.EqualTo(TankCombatBrain.State.HaltToAim));
        Assert.That(brain.Decide(true, 220f, 0.1f, true, false), Is.EqualTo(TankCombatBrain.State.Firing));
    }

    [Test]
    public void ReloadingTank_RemainsStopped()
    {
        TankCombatBrain brain = new TankCombatBrain();

        Assert.That(brain.Decide(true, 220f, 0f, true, true), Is.EqualTo(TankCombatBrain.State.Reloading));
    }

    [Test]
    public void DistanceControlsRetreatAndReposition()
    {
        TankCombatBrain brain = new TankCombatBrain();

        Assert.That(brain.Decide(true, 70f, 0f, true, false), Is.EqualTo(TankCombatBrain.State.Retreating));
        Assert.That(brain.Decide(true, 350f, 0f, true, false), Is.EqualTo(TankCombatBrain.State.Repositioning));
    }

    [Test]
    public void LostSightSearchesBeforeReturningToPatrol()
    {
        TankCombatBrain brain = new TankCombatBrain();

        Assert.That(brain.Decide(false, 220f, 0f, false, false, true), Is.EqualTo(TankCombatBrain.State.Suspicious));
        Assert.That(brain.Decide(false, 220f, 0f, false, false, false), Is.EqualTo(TankCombatBrain.State.Patrol));
    }
}
