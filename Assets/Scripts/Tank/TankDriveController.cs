using UnityEngine;

public class TankDriveController : MonoBehaviour
{
    [Header("Locked Heavy Movement")]
    public float maxForwardSpeed = 8.5f;
    public float maxReverseSpeed = 4.2f;
    public float accelerationSeconds = 1.2f;
    public float brakingSeconds = 1.6f;
    [Range(0.1f, 1f)] public float highSpeedSteeringMultiplier = 0.45f;
    [Range(0.1f, 1f)] public float lowGearSpeedMultiplier = 0.55f;

    [Header("Slope Traction")]
    public float tractionLossSlopeDegrees = 40f;
    public float maxClimbSlopeDegrees = 55f;
    [Range(0f, 1f)] public float minimumUphillSpeedMultiplier = 0.2f;

    public float CurrentSlopeAngle { get; private set; }
    public bool IsGrounded { get; private set; }
    public float EngineStrain { get; private set; }

    private bool _testInputEnabled;
    private float _testThrottle;
    private float _testSteer;
    private bool _testLowGear;

    private void Reset()
    {
        maxForwardSpeed = 8.5f;
        maxReverseSpeed = 4.2f;
        accelerationSeconds = 1.2f;
        brakingSeconds = 1.6f;
        highSpeedSteeringMultiplier = 0.45f;
        lowGearSpeedMultiplier = 0.55f;
        tractionLossSlopeDegrees = 40f;
        maxClimbSlopeDegrees = 55f;
        minimumUphillSpeedMultiplier = 0.2f;
    }

    public void ReadInput(out float throttle, out float steer, out bool lowGear)
    {
        if (_testInputEnabled)
        {
            throttle = _testThrottle;
            steer = _testSteer;
            lowGear = _testLowGear;
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

    public float GetForwardSpeedLimit(float slopeAngle, bool lowGear = false)
    {
        return maxForwardSpeed * GetSlopeSpeedMultiplier(slopeAngle) * (lowGear ? lowGearSpeedMultiplier : 1f);
    }

    public float GetReverseSpeedLimit(float slopeAngle, bool lowGear = false)
    {
        return maxReverseSpeed * GetSlopeSpeedMultiplier(slopeAngle) * (lowGear ? lowGearSpeedMultiplier : 1f);
    }

    public float GetSlopeSpeedMultiplier(float slopeAngle)
    {
        if (slopeAngle >= maxClimbSlopeDegrees)
        {
            return 0f;
        }

        if (slopeAngle <= tractionLossSlopeDegrees)
        {
            return 1f;
        }

        float range = Mathf.Max(0.01f, maxClimbSlopeDegrees - tractionLossSlopeDegrees);
        float normalized = Mathf.Clamp01((slopeAngle - tractionLossSlopeDegrees) / range);
        return Mathf.Lerp(1f, minimumUphillSpeedMultiplier, normalized);
    }

    public float GetSteeringMultiplier(float absoluteForwardSpeed)
    {
        float normalizedSpeed = maxForwardSpeed > 0f ? Mathf.Clamp01(absoluteForwardSpeed / maxForwardSpeed) : 0f;
        return Mathf.Lerp(1f, highSpeedSteeringMultiplier, normalizedSpeed);
    }

    public float GetAccelerationLimit(bool reversing, float slopeAngle)
    {
        if (slopeAngle >= maxClimbSlopeDegrees)
        {
            return 0f;
        }

        float speed = reversing ? maxReverseSpeed : maxForwardSpeed;
        return speed / Mathf.Max(0.1f, accelerationSeconds);
    }

    public float GetBrakingResponse()
    {
        return maxForwardSpeed / Mathf.Max(0.1f, brakingSeconds);
    }

    public float GetEngineStrain(float throttle, float slopeAngle)
    {
        if (Mathf.Abs(throttle) < 0.01f)
        {
            return 0f;
        }

        float slopeStrain = Mathf.InverseLerp(0f, Mathf.Max(1f, maxClimbSlopeDegrees), slopeAngle);
        return Mathf.Clamp01(Mathf.Abs(throttle) * 0.45f + slopeStrain * 0.75f);
    }

    public void RecordGroundState(bool grounded, float slopeAngle, float throttle)
    {
        IsGrounded = grounded;
        CurrentSlopeAngle = grounded ? slopeAngle : 0f;
        EngineStrain = GetEngineStrain(throttle, CurrentSlopeAngle);
    }

    public void ApplyMotocrossPracticeProfile()
    {
        TankDrivingProfile.ApplyToDriveController(this);
    }

    public void SetTestInput(float throttle, float steer, bool lowGear = false)
    {
        _testInputEnabled = true;
        _testThrottle = Mathf.Clamp(throttle, -1f, 1f);
        _testSteer = Mathf.Clamp(steer, -1f, 1f);
        _testLowGear = lowGear;
    }

    public void ClearTestInput()
    {
        _testInputEnabled = false;
    }
}
