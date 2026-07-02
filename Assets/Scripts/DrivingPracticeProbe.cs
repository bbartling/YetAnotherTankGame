using UnityEngine;

[DisallowMultipleComponent]
public class DrivingPracticeProbe : MonoBehaviour
{
    public TankController playerTank;
    public float fallThroughY = -8f;
    public bool logStatus = true;

    private float _lastLogTime;
    private bool _reportedFall;
    private float _maxY;
    private float _maxUpwardVelocity;
    private float _maxPlanarSpeed;
    private int _airborneFrames;
    private int _maxAirborneFrames;

    private void OnEnable()
    {
        ResetTelemetry();
    }

    public void ResetTelemetry()
    {
        _maxY = float.MinValue;
        _maxUpwardVelocity = 0f;
        _maxPlanarSpeed = 0f;
        _airborneFrames = 0;
        _maxAirborneFrames = 0;
        _reportedFall = false;
    }

    private void Update()
    {
        if (playerTank == null)
        {
            playerTank = Object.FindAnyObjectByType<TankController>();
        }

        if (playerTank == null)
        {
            return;
        }

        Vector3 position = playerTank.transform.position;
        Vector3 velocity = playerTank.CurrentVelocity;
        float y = position.y;
        float upwardVelocity = velocity.y;
        float planarSpeed = Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;

        if (y > _maxY)
        {
            _maxY = y;
        }

        if (upwardVelocity > _maxUpwardVelocity)
        {
            _maxUpwardVelocity = upwardVelocity;
        }

        if (planarSpeed > _maxPlanarSpeed)
        {
            _maxPlanarSpeed = planarSpeed;
        }

        if (!playerTank.IsGrounded)
        {
            _airborneFrames++;
            if (_airborneFrames > _maxAirborneFrames)
            {
                _maxAirborneFrames = _airborneFrames;
            }
        }
        else
        {
            _airborneFrames = 0;
        }

        if (!_reportedFall && y < fallThroughY)
        {
            _reportedFall = true;
            Debug.LogError("[DrivingPracticeProbe] FALL_THROUGH_MAP playerY=" + y.ToString("0.00"));
        }

        if (!logStatus || Time.time - _lastLogTime < 3f)
        {
            return;
        }

        _lastLogTime = Time.time;
        Debug.Log("[DrivingPracticeProbe] y=" + y.ToString("0.00")
            + " maxY=" + _maxY.ToString("0.00")
            + " vy=" + upwardVelocity.ToString("0.00")
            + " maxVy=" + _maxUpwardVelocity.ToString("0.00")
            + " speed=" + planarSpeed.ToString("0.00")
            + " maxSpeed=" + _maxPlanarSpeed.ToString("0.00")
            + " grounded=" + playerTank.IsGrounded
            + " airFrames=" + _maxAirborneFrames);
    }

    public string GetTelemetrySummary()
    {
        if (_maxY <= float.MinValue)
        {
            return "telemetry=empty";
        }

        return "maxY=" + _maxY.ToString("0.00")
            + " maxVy=" + _maxUpwardVelocity.ToString("0.00")
            + " maxSpeed=" + _maxPlanarSpeed.ToString("0.00")
            + " airFrames=" + _maxAirborneFrames;
    }

    private void OnDisable()
    {
        if (_maxY <= float.MinValue)
        {
            return;
        }

        Debug.Log("[DrivingPracticeProbe] TELEMETRY_SUMMARY maxY=" + _maxY.ToString("0.00")
            + " maxUpwardVel=" + _maxUpwardVelocity.ToString("0.00")
            + " maxPlanarSpeed=" + _maxPlanarSpeed.ToString("0.00")
            + " maxAirborneFrames=" + _maxAirborneFrames);
    }
}
