using UnityEngine;

[DisallowMultipleComponent]
public class VisualPivotFollower : MonoBehaviour
{
    public Transform target;
    private Quaternion _targetRest;
    private Quaternion _visualRest;

    public void Bind(Transform targetTransform)
    {
        target = targetTransform;
        _targetRest = target != null ? target.rotation : Quaternion.identity;
        _visualRest = transform.rotation;
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.rotation = target.rotation * Quaternion.Inverse(_targetRest) * _visualRest;
        }
    }
}
