using UnityEngine;

public class TankScopeController : MonoBehaviour
{
    public TankController tank;
    public RangeFinder rangeFinder;
    public bool useHighArc;

    public TankBallistics.Solution LastSolution { get; private set; }
    public bool HasSolution { get; private set; }

    private void Update()
    {
        if (!Input.GetMouseButton(1) || tank == null || tank.cannonFirePoint == null || rangeFinder == null)
        {
            return;
        }

        rangeFinder.Sample();
        HasSolution = TankBallistics.TrySolve(
            tank.cannonFirePoint.position,
            rangeFinder.AimPoint,
            TankBallistics.GetMuzzleSpeed(tank.maxPower, tank.powerPercentage),
            Physics.gravity,
            useHighArc,
            out TankBallistics.Solution solution);
        LastSolution = solution;
    }
}
