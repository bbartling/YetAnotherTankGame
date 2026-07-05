using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class BuiltInWheelTankDrive : MonoBehaviour
{
    public float motorTorque = TankGameplayTuning.BuiltInWheelMotorTorque;
    public float brakeTorque = TankGameplayTuning.BuiltInWheelBrakeTorque;
    public float maxSteerAngle = TankGameplayTuning.BuiltInWheelSteerAngle;
    public float climbAssistAcceleration = TankGameplayTuning.BuiltInWheelClimbAssist;
    public float wheelRadius = 0.48f;
    public float suspensionDistance = TankGameplayTuning.BuiltInWheelSuspensionDistance;
    public float wheelHalfWidth = 1.25f;
    public float frontAxleZ = 1.75f;
    public float rearAxleZ = -1.75f;
    public float wheelLocalY = -0.55f;
    public bool allowDrivingInput = true;

    public int WheelColliderCount => _wheels != null ? _wheels.Length : 0;
    public int GroundedWheelCount { get; private set; }
    public bool IsGrounded => GroundedWheelCount > 0;
    public float CurrentSlopeAngle { get; private set; }
    public float CurrentThrottle { get; private set; }

    private Rigidbody _body;
    private TankController _tank;
    private TankOverdriveController _overdrive;
    private WheelCollider[] _wheels;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _tank = GetComponent<TankController>();
        _overdrive = GetComponent<TankOverdriveController>();
        EnsureWheelColliders();
        ApplyTuning();
    }

    private void OnEnable()
    {
        EnsureWheelColliders();
        ApplyTuning();
    }

    public void ApplyTuning()
    {
        motorTorque = TankGameplayTuning.BuiltInWheelMotorTorque;
        brakeTorque = TankGameplayTuning.BuiltInWheelBrakeTorque;
        maxSteerAngle = TankGameplayTuning.BuiltInWheelSteerAngle;
        climbAssistAcceleration = TankGameplayTuning.BuiltInWheelClimbAssist;
        suspensionDistance = TankGameplayTuning.BuiltInWheelSuspensionDistance;
        ConfigureWheelColliders();
    }

    private void FixedUpdate()
    {
        TickDrive(Time.fixedDeltaTime);
    }

    public void TickDrive(float deltaTime)
    {
        if (_body == null)
        {
            _body = GetComponent<Rigidbody>();
        }

        if (_overdrive == null)
        {
            _overdrive = GetComponent<TankOverdriveController>();
        }

        EnsureWheelColliders();
        ReadDriveInput(out float throttle, out float steer, out bool lowGear);
        CurrentThrottle = throttle;
        UpdateGroundState();

        float speedMultiplier = _overdrive != null ? _overdrive.SpeedMultiplier : 1f;
        float accelerationMultiplier = _overdrive != null ? _overdrive.AccelerationMultiplier : 1f;
        float climbMultiplier = _overdrive != null ? _overdrive.ClimbSlopeMultiplier : 1f;
        float targetMaxSpeed = (throttle >= 0f ? TankDrivingProfile.MaxForwardSpeed : TankDrivingProfile.MaxReverseSpeed)
            * speedMultiplier
            * (lowGear ? 0.62f : 1f);

        Vector3 planarVelocity = Vector3.ProjectOnPlane(ReadVelocity(), Vector3.up);
        float forwardSpeed = Vector3.Dot(planarVelocity, transform.forward);
        bool overSpeed = Mathf.Abs(forwardSpeed) > targetMaxSpeed && Mathf.Sign(forwardSpeed) == Mathf.Sign(throttle);
        float driveTorque = overSpeed ? 0f : throttle * motorTorque * accelerationMultiplier;
        float steerAngle = steer * maxSteerAngle * Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(1f, targetMaxSpeed)));
        float braking = Mathf.Abs(throttle) < 0.05f ? brakeTorque : 0f;

        for (int i = 0; i < _wheels.Length; i++)
        {
            WheelCollider wheel = _wheels[i];
            if (wheel == null)
            {
                continue;
            }

            bool frontWheel = i < 2;
            wheel.steerAngle = frontWheel ? steerAngle : 0f;
            wheel.motorTorque = driveTorque / Mathf.Max(1, _wheels.Length);
            wheel.brakeTorque = braking;
        }

        if (IsGrounded && throttle > 0.05f && CurrentSlopeAngle > 5f)
        {
            Vector3 climbForward = Vector3.ProjectOnPlane(transform.forward, GetAverageGroundNormal());
            if (climbForward.sqrMagnitude > 0.0001f)
            {
                climbForward.Normalize();
                _body.AddForce(climbForward * climbAssistAcceleration * climbMultiplier * throttle, ForceMode.Acceleration);
            }
        }

        Stabilize(deltaTime);
    }

    private void ReadDriveInput(out float throttle, out float steer, out bool lowGear)
    {
        if (!allowDrivingInput || (_tank != null && !_tank.allowDrivingInput))
        {
            throttle = 0f;
            steer = 0f;
            lowGear = false;
            return;
        }

        throttle = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) throttle += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) throttle -= 1f;

        steer = 0f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steer += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steer -= 1f;
        lowGear = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private void UpdateGroundState()
    {
        GroundedWheelCount = 0;
        Vector3 normalSum = Vector3.zero;
        for (int i = 0; i < _wheels.Length; i++)
        {
            if (_wheels[i] != null && _wheels[i].GetGroundHit(out WheelHit hit))
            {
                GroundedWheelCount++;
                normalSum += hit.normal;
            }
        }

        Vector3 normal = normalSum.sqrMagnitude > 0.0001f ? normalSum.normalized : Vector3.up;
        CurrentSlopeAngle = GroundedWheelCount > 0 ? Vector3.Angle(normal, Vector3.up) : 0f;
    }

    private Vector3 GetAverageGroundNormal()
    {
        Vector3 normalSum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < _wheels.Length; i++)
        {
            if (_wheels[i] != null && _wheels[i].GetGroundHit(out WheelHit hit))
            {
                normalSum += hit.normal;
                count++;
            }
        }

        return count > 0 && normalSum.sqrMagnitude > 0.0001f ? normalSum.normalized : Vector3.up;
    }

    private void Stabilize(float deltaTime)
    {
        if (_body == null || !IsGrounded)
        {
            return;
        }

        Vector3 velocity = ReadVelocity();
        Vector3 lateral = Vector3.Project(velocity, transform.right);
        _body.AddForce(-lateral * 5.5f, ForceMode.Acceleration);
        _body.AddForce(-transform.up * 18f, ForceMode.Acceleration);

        Vector3 angularVelocity = _body.angularVelocity;
        angularVelocity.x = Mathf.Lerp(angularVelocity.x, 0f, Mathf.Clamp01(deltaTime * 5f));
        angularVelocity.z = Mathf.Lerp(angularVelocity.z, 0f, Mathf.Clamp01(deltaTime * 5f));
        _body.angularVelocity = angularVelocity;
    }

    private void EnsureWheelColliders()
    {
        if (_wheels != null && _wheels.Length >= 4)
        {
            return;
        }

        _wheels = new[]
        {
            EnsureWheel("BuiltInWheel_FL", new Vector3(-wheelHalfWidth, wheelLocalY, frontAxleZ)),
            EnsureWheel("BuiltInWheel_FR", new Vector3(wheelHalfWidth, wheelLocalY, frontAxleZ)),
            EnsureWheel("BuiltInWheel_RL", new Vector3(-wheelHalfWidth, wheelLocalY, rearAxleZ)),
            EnsureWheel("BuiltInWheel_RR", new Vector3(wheelHalfWidth, wheelLocalY, rearAxleZ))
        };
    }

    private WheelCollider EnsureWheel(string wheelName, Vector3 localPosition)
    {
        Transform existing = transform.Find(wheelName);
        GameObject wheelObject = existing != null ? existing.gameObject : new GameObject(wheelName);
        wheelObject.transform.SetParent(transform, false);
        wheelObject.transform.localPosition = localPosition;
        wheelObject.transform.localRotation = Quaternion.identity;

        WheelCollider wheel = wheelObject.GetComponent<WheelCollider>();
        if (wheel == null)
        {
            wheel = wheelObject.AddComponent<WheelCollider>();
        }

        return wheel;
    }

    private void ConfigureWheelColliders()
    {
        if (_wheels == null)
        {
            return;
        }

        for (int i = 0; i < _wheels.Length; i++)
        {
            WheelCollider wheel = _wheels[i];
            if (wheel == null)
            {
                continue;
            }

            wheel.radius = wheelRadius;
            wheel.suspensionDistance = suspensionDistance;
            wheel.forceAppPointDistance = 0.15f;
            JointSpring spring = wheel.suspensionSpring;
            spring.spring = TankGameplayTuning.BuiltInWheelSpring;
            spring.damper = TankGameplayTuning.BuiltInWheelDamper;
            spring.targetPosition = 0.55f;
            wheel.suspensionSpring = spring;

            WheelFrictionCurve forward = wheel.forwardFriction;
            forward.extremumSlip = 0.45f;
            forward.extremumValue = 1.6f;
            forward.asymptoteSlip = 0.85f;
            forward.asymptoteValue = 1.2f;
            forward.stiffness = 2.2f;
            wheel.forwardFriction = forward;

            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.extremumSlip = 0.35f;
            sideways.extremumValue = 1.5f;
            sideways.asymptoteSlip = 0.75f;
            sideways.asymptoteValue = 1.1f;
            sideways.stiffness = 2.4f;
            wheel.sidewaysFriction = sideways;
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
