using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DrivingCoursePathTests
{
    [Test]
    public void ComputeCompletionRatio_StartsNearZeroAndCanReachOne()
    {
        List<Vector3> waypoints = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 100f),
            new Vector3(0f, 0f, 200f)
        };

        float start = DrivingCoursePath.ComputeCompletionRatio(new Vector3(0f, 0f, 0f), waypoints);
        float mid = DrivingCoursePath.ComputeCompletionRatio(new Vector3(0f, 0f, 100f), waypoints);
        float end = DrivingCoursePath.ComputeCompletionRatio(new Vector3(0f, 0f, 200f), waypoints);

        Assert.That(start, Is.LessThan(0.2f));
        Assert.That(mid, Is.InRange(0.35f, 0.75f));
        Assert.That(end, Is.GreaterThan(0.85f));
    }
}
