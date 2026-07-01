using NUnit.Framework;
using UnityEngine;

public class PracticeTargetTrackerTests
{
    [Test]
    public void ComputeCompletionRatio_ReturnsZeroWhenNoTargetsExist()
    {
        Assert.That(PracticeTargetTracker.ComputeCompletionRatio(), Is.Zero);
    }
}
