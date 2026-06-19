using UnityEngine;

public class RangeFinder : MonoBehaviour
{
    public Camera aimCamera;
    public LayerMask hitMask = ~0;
    public float maxRange = 1200f;

    public float Distance { get; private set; }
    public Vector3 AimPoint { get; private set; }
    public bool HasHit { get; private set; }

    public bool Sample()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (aimCamera == null)
        {
            return false;
        }

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        HasHit = Physics.Raycast(ray, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore);
        AimPoint = HasHit ? hit.point : ray.GetPoint(maxRange);
        Distance = Vector3.Distance(ray.origin, AimPoint);
        return HasHit;
    }
}
