using System.Collections.Generic;
using UnityEngine;

public static class PracticeTargetTracker
{
    public static int CountPracticeTargets()
    {
        CastleDamageReceiver[] targets = Object.FindObjectsByType<CastleDamageReceiver>();
        int count = 0;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null && targets[i].practiceTargetExplosion)
            {
                count++;
            }
        }

        return count;
    }

    public static int CountClearedPracticeTargets()
    {
        CastleDamageReceiver[] targets = Object.FindObjectsByType<CastleDamageReceiver>();
        int count = 0;
        for (int i = 0; i < targets.Length; i++)
        {
            CastleDamageReceiver target = targets[i];
            if (target != null && target.practiceTargetExplosion && target.IsCollapsed)
            {
                count++;
            }
        }

        return count;
    }

    public static float ComputeCompletionRatio()
    {
        int total = CountPracticeTargets();
        if (total <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01(CountClearedPracticeTargets() / (float)total);
    }
}
