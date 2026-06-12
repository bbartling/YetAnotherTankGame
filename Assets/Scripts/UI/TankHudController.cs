using TMPro;
using UnityEngine;

public class TankHudController : MonoBehaviour
{
    public TankScopeController scope;
    public TextMeshProUGUI scopeReadout;

    private void LateUpdate()
    {
        if (scopeReadout == null || scope == null)
        {
            return;
        }

        if (!scope.HasSolution)
        {
            scopeReadout.text = "RANGE --  NO SOLUTION";
            return;
        }

        TankBallistics.Solution solution = scope.LastSolution;
        scopeReadout.text = $"ELEV {solution.ElevationDegrees:0.0}°  TOF {solution.TimeOfFlight:0.0}s";
    }
}
