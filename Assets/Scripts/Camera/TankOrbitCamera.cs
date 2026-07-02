using UnityEngine;

public class TankOrbitCamera : MonoBehaviour
{
    public Transform target;
    public Transform aimDirectionSource;
    public Vector3 targetOffset = new Vector3(0f, 7.5f, 2.5f);
    public float cameraHeight = 10f;
    public float followDistance = 26f;
    public float closeCollisionDistance = 4f;
    public float closeViewHeight = 1.35f;
    public float closeViewForwardOffset = 3.25f;
    public float lookAheadDistance = 8f;
    public float collisionRadius = 0.35f;
    public float positionSmoothTime = 0.28f;
    public float focusSmoothTime = 0.24f;
    public float focusVerticalSmoothTime = 0.34f;
    public LayerMask collisionMask = ~0;

    private Vector3 _velocity;
    private Vector3 _focusVelocity;
    private float _focusVerticalVelocity;
    private Vector3 _smoothedFocus;
    private bool _focusInitialized;
    private bool _detachedFromParent;

    private void LateUpdate()
    {
        if (target == null || ProjectileCameraController.ActivePlayerProjectile != null)
        {
            return;
        }

        TankBarrelScopeCamera scopeCamera = GetComponent<TankBarrelScopeCamera>();
        if (scopeCamera != null && scopeCamera.allowScope && Input.GetMouseButton(1))
        {
            return;
        }

        EnsureDetachedFromTank();

        Vector3 aimForward = aimDirectionSource != null ? aimDirectionSource.forward : target.forward;
        Vector3 flatAimForward = Vector3.ProjectOnPlane(aimForward, Vector3.up);
        if (flatAimForward.sqrMagnitude < 0.001f)
        {
            flatAimForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        }
        flatAimForward.Normalize();

        Vector3 rawFocus = target.position + targetOffset;
        if (!_focusInitialized)
        {
            _smoothedFocus = rawFocus;
            _focusInitialized = true;
        }
        else
        {
            _smoothedFocus = Vector3.SmoothDamp(_smoothedFocus, rawFocus, ref _focusVelocity, focusSmoothTime);
            _smoothedFocus.y = Mathf.SmoothDamp(
                _smoothedFocus.y,
                rawFocus.y,
                ref _focusVerticalVelocity,
                focusVerticalSmoothTime);
        }

        Vector3 focus = _smoothedFocus;
        Vector3 desiredDirection = -flatAimForward;
        float distance = followDistance;
        bool blockedClose = false;
        if (Physics.SphereCast(focus, collisionRadius, desiredDirection, out RaycastHit hit, followDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(target))
            {
                distance = Mathf.Max(1.5f, hit.distance - collisionRadius);
                blockedClose = distance <= closeCollisionDistance;
            }
        }

        Vector3 lookTarget = focus;
        Vector3 desiredPosition;
        if (blockedClose)
        {
            desiredPosition = focus + flatAimForward * closeViewForwardOffset + Vector3.up * closeViewHeight;
            lookTarget = focus + flatAimForward * lookAheadDistance;
        }
        else
        {
            float height = Mathf.Lerp(closeViewHeight, cameraHeight, Mathf.Clamp01(distance / followDistance));
            desiredPosition = focus + desiredDirection * distance + Vector3.up * height;
        }

        if (positionSmoothTime <= 0f)
        {
            transform.position = desiredPosition;
            _velocity = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, positionSmoothTime);
        }

        transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
    }

    private void EnsureDetachedFromTank()
    {
        if (_detachedFromParent || target == null)
        {
            return;
        }

        if (transform.parent == null)
        {
            _detachedFromParent = true;
            return;
        }

        if (transform.IsChildOf(target) || transform.parent == target.parent)
        {
            transform.SetParent(null, true);
        }

        _detachedFromParent = true;
    }
}
