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
    private TankController _tank;
    private TankDriveController _drive;
    private TankOverdriveController _overdrive;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _tank = GetComponent<TankController>();
        _drive = GetComponent<TankDriveController>();
        _overdrive = GetComponent<TankOverdriveController>();
    }

    private void Update()
    {
        float speed = _body != null ? Vector3.Dot(ReadVelocity(), transform.forward) : 0f;
        float throttle = 0f;
        _drive?.ReadInput(out throttle, out _, out _);

        if (_overdrive != null && _overdrive.IsOverdriveActive && throttle > 0.25f)
        {
            float rpmRatio = Mathf.InverseLerp(TankGameplayTuning.RpmIdle, TankGameplayTuning.RpmRedline, _overdrive.DisplayRpm);
            float spinSpeed = (_drive != null ? _drive.maxForwardSpeed : 8.5f) * Mathf.Lerp(1.2f, 2.8f, rpmRatio);
            if (speed < spinSpeed * 0.65f)
            {
                speed = Mathf.Lerp(speed, spinSpeed, _overdrive.IsWheelSpinOut ? 0.85f : 0.55f);
            }
        }
        else if (_tank != null && _tank.IsOverturned && _drive != null)
        {
            if (Mathf.Abs(throttle) > 0.01f)
            {
                speed = throttle * _drive.maxForwardSpeed;
            }
        }

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

        float wheelMultiplier = _overdrive != null ? _overdrive.WheelVisualMultiplier : 1f;
        if (_overdrive != null && _overdrive.IsWheelSpinOut)
        {
            wheelMultiplier *= 1.6f;
        }

        float trackDegrees = speed * deltaTime * 95f * wheelMultiplier;
        if (leftTrack != null) leftTrack.Rotate(trackDegrees, 0f, 0f, Space.Self);
        if (rightTrack != null) rightTrack.Rotate(trackDegrees, 0f, 0f, Space.Self);
        if (wheels != null)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null) wheels[i].Rotate(trackDegrees, 0f, 0f, Space.Self);
            }
        }
    }

    private Vector3 ReadVelocity()
    {
        if (_body == null)
        {
            return Vector3.zero;
        }

#if UNITY_6000_0_OR_NEWER
        return _body.linearVelocity;
#else
        return _body.velocity;
#endif
    }
}
