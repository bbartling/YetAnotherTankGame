using UnityEngine;

[DisallowMultipleComponent]
public class DestructionAnimator : MonoBehaviour
{
    public float collapseDistance = 1.4f;
    public float tumbleDegrees = 65f;
    public float collapseSpeed = 1f;

    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private float _collapse;
    private bool _collapsing;

    private void Awake()
    {
        CaptureRestPose();
    }

    private void Update()
    {
        if (_collapsing) TickCollapse(Time.deltaTime);
    }

    public void CaptureRestPose()
    {
        _restPosition = transform.localPosition;
        _restRotation = transform.localRotation;
    }

    public void BeginCollapse()
    {
        _collapsing = true;
    }

    public void TickCollapse(float deltaTime)
    {
        _collapse = Mathf.MoveTowards(_collapse, 1f, deltaTime * collapseSpeed);
        transform.localPosition = _restPosition + Vector3.down * collapseDistance * _collapse;
        transform.localRotation = _restRotation * Quaternion.Euler(
            tumbleDegrees * _collapse,
            tumbleDegrees * 0.45f * _collapse,
            tumbleDegrees * 0.25f * _collapse);
    }
}
