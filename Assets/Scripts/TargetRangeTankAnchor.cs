using UnityEngine;

/// <summary>
/// Places the player tank on the shooting-range lane at scene start.
/// Baseline placement from commit 78210fb — snap only, no physics freeze.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(500)]
public class TargetRangeTankAnchor : MonoBehaviour
{
    public TankController tank;
    public float groundRayHeight = 8f;
    public float hullClearance = 1.05f;

    public bool IsPlaced { get; private set; }

    public static void Apply(TankController targetTank)
    {
        if (targetTank == null)
        {
            return;
        }

        TargetRangeTankAnchor anchor = targetTank.GetComponent<TargetRangeTankAnchor>();
        if (anchor == null)
        {
            anchor = targetTank.gameObject.AddComponent<TargetRangeTankAnchor>();
        }

        anchor.tank = targetTank;
        anchor.ApplyNow();
    }

    private void Awake()
    {
        if (tank == null)
        {
            tank = GetComponent<TankController>();
        }
    }

    private void Start()
    {
        ApplyNow();
    }

    public void ApplyNow()
    {
        if (tank == null)
        {
            return;
        }

        tank.continuousTerrainSnapEnabled = false;
        SnapToGround();
        IsPlaced = true;
    }

    public float SampleSupportSurfaceY()
    {
        if (tank == null)
        {
            return 0f;
        }

        Vector3 origin = tank.transform.position + Vector3.up * groundRayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayHeight + 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && !hit.collider.transform.IsChildOf(tank.transform))
            {
                return hit.point.y;
            }
        }

        return tank.transform.position.y - hullClearance;
    }

    private void SnapToGround()
    {
        Vector3 origin = tank.transform.position + Vector3.up * groundRayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayHeight + 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.transform.IsChildOf(tank.transform))
            {
                return;
            }

            Vector3 position = tank.transform.position;
            position.y = hit.point.y + hullClearance;
            tank.transform.position = position;

            Rigidbody body = tank.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
        }
    }
}
