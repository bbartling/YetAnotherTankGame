using UnityEngine;

[DisallowMultipleComponent]
public class VisualPivotFollower : MonoBehaviour
{
    public Transform target;
    private Quaternion _targetRest;
    private Quaternion _visualRest;
    private Vector3 _targetLocalPositionOffset;

    public void Bind(Transform targetTransform)
    {
        target = targetTransform;
        _targetRest = target != null ? target.rotation : Quaternion.identity;
        _visualRest = transform.rotation;
        _targetLocalPositionOffset = target != null
            ? Quaternion.Inverse(target.rotation) * (transform.position - target.position)
            : Vector3.zero;
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + target.rotation * _targetLocalPositionOffset;
            transform.rotation = target.rotation * Quaternion.Inverse(_targetRest) * _visualRest;
        }
    }
}
