using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallDriveTankVisualFollower : MonoBehaviour
{
    public Transform visualRoot;
    public float visualHeightOffset = 6.7f;
    public float yawFollowSpeed = 18f;
    public bool hidePhysicsBallRenderer = true;
    public bool snapVisualToGround = true;
    public float groundSnapDistance = 30f;
    public float visualGroundClearance = 0.05f;
    public LayerMask groundMask = ~0;

    private BallDriveTestController _controller;

    private void Awake()
    {
        _controller = GetComponent<BallDriveTestController>();
        if (hidePhysicsBallRenderer)
        {
            HidePhysicsRootRenderers();
        }

        HideDamageVariantRenderers();
    }

    private void LateUpdate()
    {
        if (visualRoot == null)
        {
            return;
        }

        float targetYaw = _controller != null ? _controller.CurrentTankYaw : transform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
        float lerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, yawFollowSpeed) * Time.deltaTime);

        visualRoot.position = CalculateVisualPosition();
        visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRotation, lerp);
    }

    public void HidePhysicsRootRenderers()
    {
        Renderer[] renderers = GetComponents<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }

    public void HideDamageVariantRenderers()
    {
        if (visualRoot == null)
        {
            return;
        }

        Transform[] parts = visualRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            Transform part = parts[i];
            if (part != null && part.name.Contains("Damaged"))
            {
                part.gameObject.SetActive(false);
            }
        }
    }

    private Vector3 CalculateVisualPosition()
    {
        if (!snapVisualToGround)
        {
            return transform.position + Vector3.up * visualHeightOffset;
        }

        Vector3 rayStart = transform.position + Vector3.up * Mathf.Max(2f, visualHeightOffset);
        float rayDistance = Mathf.Max(1f, visualHeightOffset + groundSnapDistance);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, rayDistance, groundMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || ShouldIgnoreGroundHit(hitCollider.transform))
            {
                continue;
            }

            return hits[i].point + Vector3.up * visualGroundClearance;
        }

        return transform.position + Vector3.up * visualHeightOffset;
    }

    private bool ShouldIgnoreGroundHit(Transform hitTransform)
    {
        if (hitTransform == null)
        {
            return true;
        }

        if (hitTransform == transform || hitTransform.IsChildOf(transform))
        {
            return true;
        }

        return visualRoot != null && (hitTransform == visualRoot || hitTransform.IsChildOf(visualRoot));
    }
}
