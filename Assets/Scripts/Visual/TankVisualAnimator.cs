using UnityEngine;

[DisallowMultipleComponent]
public class TankVisualAnimator : MonoBehaviour
{
    public Transform barrel;
    public Transform antenna;
    public Transform leftTrack;
    public Transform rightTrack;
    public Transform[] wheels;
    public float recoilDistance = 0.45f;
    public float recoilReturnSpeed = 5f;
    public float antennaWobbleDegrees = 7f;

    private Vector3 _barrelRestPosition;
    private Quaternion _antennaRestRotation = Quaternion.identity;
    private float _recoil;
    private Rigidbody _body;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float speed = _body != null ? Vector3.ProjectOnPlane(_body.linearVelocity, Vector3.up).magnitude : 0f;
        TickVisuals(Time.deltaTime, speed, 0f);
    }

    public void Bind(Transform barrelTransform, Transform antennaTransform)
    {
        barrel = barrelTransform;
        antenna = antennaTransform;
        CaptureRestPose();
    }

    public void CaptureRestPose()
    {
        if (barrel != null) _barrelRestPosition = barrel.localPosition;
        if (antenna != null) _antennaRestRotation = antenna.localRotation;
    }

    public void TriggerRecoil()
    {
        _recoil = 1f;
    }

    public void TickVisuals(float deltaTime, float speed, float strain)
    {
        if (barrel != null)
        {
            float offset = recoilDistance * _recoil;
            barrel.localPosition = Vector3.Lerp(barrel.localPosition, _barrelRestPosition + Vector3.back * offset, Mathf.Clamp01(deltaTime * 18f));
        }

        _recoil = Mathf.MoveTowards(_recoil, 0f, deltaTime * recoilReturnSpeed);

        if (antenna != null)
        {
            float intensity = Mathf.Clamp01(speed * 0.25f + strain * 0.7f);
            float wobble = Mathf.Sin(Time.time * 6f) * antennaWobbleDegrees * intensity;
            antenna.localRotation = _antennaRestRotation * Quaternion.Euler(wobble, 0f, wobble * 0.55f);
        }

        float trackDegrees = speed * deltaTime * 95f;
        if (leftTrack != null) leftTrack.Rotate(trackDegrees, 0f, 0f, Space.Self);
        if (rightTrack != null) rightTrack.Rotate(trackDegrees, 0f, 0f, Space.Self);
        if (wheels != null)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null) wheels[i].Rotate(0f, trackDegrees, 0f, Space.Self);
            }
        }
    }
}
