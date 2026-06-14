using UnityEngine;

public class TankOrbitCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 2.6f, 1f);
    public float cameraHeight = 4.2f;
    public float followDistance = 11f;
    public float collisionRadius = 0.35f;
    public float positionSmoothTime = 0.12f;
    public LayerMask collisionMask = ~0;

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (target == null || ProjectileCameraController.ActivePlayerProjectile != null)
        {
            return;
        }

        Vector3 focus = target.position + targetOffset;
        Vector3 desiredDirection = -target.forward;
        float distance = followDistance;
        if (Physics.SphereCast(focus, collisionRadius, desiredDirection, out RaycastHit hit, followDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(target))
            {
                distance = Mathf.Max(1.5f, hit.distance - collisionRadius);
            }
        }

        Vector3 desiredPosition = focus + desiredDirection * distance + Vector3.up * cameraHeight;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, positionSmoothTime);
        transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
    }
}
