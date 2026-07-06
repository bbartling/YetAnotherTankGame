using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallDriveSlopeIndicator : MonoBehaviour
{
    public Transform target;
    public Vector3 targetOffset = Vector3.zero;
    public float groundProbeHeight = 8f;
    public float groundProbeDistance = 20f;
    public LayerMask groundMask = ~0;
    public float rotationSharpness = 14f;
    public float positionSharpness = 20f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + targetOffset;
        Vector3 groundNormal = Vector3.up;
        Vector3 rayStart = target.position + Vector3.up * groundProbeHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;
        }

        Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, groundNormal);
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.right, groundNormal);
        }

        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, groundNormal);
        float positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
        float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionT);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationT);
    }
}
