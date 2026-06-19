#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TankBallisticsEditModeTests
{
    [TestCase(25f)]
    [TestCase(50f)]
    [TestCase(100f)]
    public void BallisticSolution_LandsNearKnownDistance(float distance)
    {
        Vector3 origin = Vector3.zero;
        Vector3 target = Vector3.forward * distance;

        Assert.That(TankBallistics.TrySolve(origin, target, 45f, Physics.gravity, false, out TankBallistics.Solution solution), Is.True);

        Vector3 landing = TankBallistics.PositionAtTime(origin, solution.LaunchVelocity, Physics.gravity, solution.TimeOfFlight);
        Assert.That(Vector3.Distance(landing, target), Is.LessThan(0.05f));
        Assert.That(solution.ElevationDegrees, Is.GreaterThan(0f));
        Assert.That(solution.TimeOfFlight, Is.GreaterThan(0f));
    }

    [Test]
    public void MuzzleSpeed_MatchesPhysicalCannonContract()
    {
        Assert.That(TankBallistics.GetMuzzleSpeed(75f, 50f), Is.EqualTo(37.5f).Within(0.001f));
    }
}
#endif
