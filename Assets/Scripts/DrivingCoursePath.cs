using System.Collections.Generic;
using UnityEngine;

public static class DrivingCoursePath
{
    public static IReadOnlyList<Vector3> BuildWaypoints()
    {
        List<Vector3> waypoints = new List<Vector3>();
        PracticeFinishLine finish = Object.FindAnyObjectByType<PracticeFinishLine>();
        float finishZ = finish != null ? finish.transform.position.z : -132f;
        bool expandedCourse = finishZ < -400f;

        if (expandedCourse)
        {
            AddWaypoint(waypoints, 0f, -640f);
            AddWaypoint(waypoints, 0f, -520f);
            AddWaypoint(waypoints, 0f, -360f);
            AddWaypoint(waypoints, 0f, -180f);
            AddWaypoint(waypoints, 0f, 0f);
            AddWaypoint(waypoints, 0f, 180f);
            AddWaypoint(waypoints, 0f, 360f);
            AddWaypoint(waypoints, 0f, 520f);
            AddWaypoint(waypoints, 0f, 650f);
            AddWaypoint(waypoints, -25f, 700f);
            AddWaypoint(waypoints, -70f, 760f);
            AddWaypoint(waypoints, -160f, 790f);
            AddWaypoint(waypoints, -210f, 760f);
            AddWaypoint(waypoints, -210f, 600f);
            AddWaypoint(waypoints, -210f, 420f);
            AddWaypoint(waypoints, -210f, 240f);
            AddWaypoint(waypoints, -210f, 60f);
            AddWaypoint(waypoints, -210f, -120f);
            AddWaypoint(waypoints, -210f, -300f);
            AddWaypoint(waypoints, -210f, -440f);
            AddWaypoint(waypoints, -150f, -560f);
            AddWaypoint(waypoints, -70f, -630f);
            AddWaypoint(waypoints, 0f, finishZ);
        }
        else
        {
            AddWaypoint(waypoints, 0f, -130f);
            AddWaypoint(waypoints, 0f, -90f);
            AddWaypoint(waypoints, 0f, -40f);
            AddWaypoint(waypoints, 0f, 20f);
            AddWaypoint(waypoints, 0f, 80f);
            AddWaypoint(waypoints, 0f, 130f);
            AddWaypoint(waypoints, -20f, 152f);
            AddWaypoint(waypoints, -42f, 150f);
            AddWaypoint(waypoints, -42f, 100f);
            AddWaypoint(waypoints, -42f, 40f);
            AddWaypoint(waypoints, -42f, -20f);
            AddWaypoint(waypoints, -42f, -80f);
            AddWaypoint(waypoints, -30f, -120f);
            AddWaypoint(waypoints, 0f, finishZ);
        }

        return waypoints;
    }

    public static float ComputeCompletionRatio(Vector3 tankPosition, IReadOnlyList<Vector3> waypoints)
    {
        if (waypoints == null || waypoints.Count < 2)
        {
            return PracticeFinishLine.CourseCompleted ? 1f : 0f;
        }

        if (PracticeFinishLine.CourseCompleted)
        {
            return 1f;
        }

        float totalLength = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            totalLength += PlanarDistance(waypoints[i], waypoints[i + 1]);
        }

        if (totalLength <= 0.01f)
        {
            return 0f;
        }

        float bestDistance = float.MaxValue;
        float bestProgress = 0f;
        float walked = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 start = waypoints[i];
            Vector3 end = waypoints[i + 1];
            float segmentLength = PlanarDistance(start, end);
            if (segmentLength <= 0.01f)
            {
                continue;
            }

            Vector3 segment = end - start;
            segment.y = 0f;
            segment /= segmentLength;
            Vector3 toTank = tankPosition - start;
            toTank.y = 0f;
            float along = Mathf.Clamp(Vector3.Dot(toTank, segment), 0f, segmentLength);
            Vector3 closest = start + segment * along;
            float distance = PlanarDistance(tankPosition, closest);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestProgress = walked + along;
            }

            walked += segmentLength;
        }

        return Mathf.Clamp01(bestProgress / totalLength);
    }

    private static void AddWaypoint(List<Vector3> waypoints, float x, float z)
    {
        waypoints.Add(new Vector3(x, 0f, z));
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
