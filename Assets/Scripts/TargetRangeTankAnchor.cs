using UnityEngine;

[DisallowMultipleComponent]
public class TargetRangeTankAnchor : MonoBehaviour
{
    public TankController tank;
    public float groundRayHeight = 8f;
    public float hullClearance = 1.05f;

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

    private void Start()
    {
        if (tank == null)
        {
            tank = GetComponent<TankController>();
        }

        ApplyNow();
    }

    public void ApplyNow()
    {
        if (tank == null)
        {
            return;
        }

        tank.ApplyTargetRangeAnchor();
        SnapToGround();
    }

    private void SnapToGround()
    {
        Vector3 origin = tank.transform.position + Vector3.up * groundRayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayHeight + 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 position = tank.transform.position;
            position.y = hit.point.y + hullClearance;
            tank.transform.position = position;
        }

        Rigidbody body = tank.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = tank.transform.position;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
