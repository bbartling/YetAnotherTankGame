using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public class TargetRangeTankAnchor : MonoBehaviour
{
    public TankController tank;
    public float hullClearance = 0.08f;
    public float maxGroundSearchHeight = 12f;
    public float footprintSampleRadius = 1.35f;
    public float minGroundNormalY = 0.55f;

    public float AnchoredGroundY { get; private set; }
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

        if (!IsPlaced)
        {
            ApplyNow();
        }
    }

    public void ApplyNow()
    {
        if (tank == null)
        {
            return;
        }

        SnapToGround();
        EnforcePlacementPose();
        IsPlaced = true;
    }

    private void EnforcePlacementPose()
    {
        Vector3 position = tank.transform.position;
        position.y = AnchoredGroundY;
        tank.transform.position = position;
        tank.transform.rotation = Quaternion.Euler(0f, tank.transform.eulerAngles.y, 0f);

        Rigidbody body = tank.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = position;
            body.rotation = tank.transform.rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }
    }

    private void SnapToGround()
    {
        float groundY = SampleSupportSurfaceY();
        float pivotToBottom = GetPivotToBottomOffset();
        AnchoredGroundY = groundY + hullClearance + pivotToBottom;
    }

    public float SampleSupportSurfaceY()
    {
        if (tank == null)
        {
            return 0f;
        }

        Collider[] ownColliders = tank.GetComponentsInChildren<Collider>(true);
        Vector3 center = tank.transform.position;
        Vector3 forward = tank.transform.forward;
        Vector3 right = tank.transform.right;
        Vector3[] samplePoints =
        {
            center,
            center + forward * footprintSampleRadius,
            center - forward * footprintSampleRadius,
            center + right * footprintSampleRadius,
            center - right * footprintSampleRadius
        };

        float lowestGround = float.MaxValue;
        for (int i = 0; i < samplePoints.Length; i++)
        {
            Vector3 sample = samplePoints[i];
            float rayStartY = Mathf.Max(center.y + maxGroundSearchHeight, maxGroundSearchHeight + 2f);
            Vector3 origin = new Vector3(sample.x, rayStartY, sample.z);
            float rayLength = rayStartY + maxGroundSearchHeight * 2f;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                rayLength,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int h = 0; h < hits.Length; h++)
            {
                RaycastHit hit = hits[h];
                if (hit.collider == null || IsOwnCollider(hit.collider, ownColliders))
                {
                    continue;
                }

                if (hit.normal.y < minGroundNormalY)
                {
                    continue;
                }

                if (hit.point.y < lowestGround)
                {
                    lowestGround = hit.point.y;
                }
            }
        }

        if (lowestGround >= float.MaxValue - 1f)
        {
            return tank.transform.position.y - GetPivotToBottomOffset();
        }

        return lowestGround;
    }

    public float GetPivotToBottomOffset()
    {
        if (tank == null)
        {
            return 0f;
        }

        float lowestBottom = float.MaxValue;
        Collider[] colliders = tank.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                continue;
            }

            lowestBottom = Mathf.Min(lowestBottom, collider.bounds.min.y);
        }

        if (lowestBottom >= float.MaxValue - 1f)
        {
            return 0f;
        }

        return tank.transform.position.y - lowestBottom;
    }

    private static bool IsOwnCollider(Collider candidate, Collider[] ownColliders)
    {
        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == candidate)
            {
                return true;
            }
        }

        return false;
    }
}
