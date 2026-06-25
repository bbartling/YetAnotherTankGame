using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class DrivingPracticeAutopilot : MonoBehaviour
{
    public bool autoRunOnPlay = true;
    public bool muteAudioDuringTest = true;
    public float waypointReachDistance = 18f;
    public float stuckSpeedThreshold = 0.45f;
    public float stuckTimeSeconds = 2.5f;
    public float hardStuckTimeSeconds = 10f;
    public float unstuckReverseSeconds = 1.1f;
    public float statusLogInterval = 3f;

    public TankController playerTank;
    public TankDriveController driveController;

    private readonly List<Vector3> _waypoints = new List<Vector3>();
    private int _waypointIndex;
    private float _stuckTimer;
    private float _unstuckTimer;
    private int _unstuckPhase;
    private Vector3 _lastProgressPosition;
    private float _lastStatusLogTime;
    private float _savedListenerVolume = 1f;
    private float _nextUnstuckImpulseTime;
    private float _nextClimbAssistTime;
    private float _nextHardRecoveryTime;
    private int _hardRecoveryCount;
    private bool _audioMuted;
    private bool _running;
    private bool _completed;

    public bool IsRunning => _running;
    public bool IsCompleted => _completed;
    public int WaypointIndex => _waypointIndex;
    public int WaypointCount => _waypoints.Count;

    private void Start()
    {
        if (autoRunOnPlay)
        {
            BeginAutopilot();
        }
    }

    public void BeginAutopilot()
    {
        if (_running)
        {
            return;
        }

        ResolveReferences();
        if (playerTank == null || driveController == null)
        {
            Debug.LogError("[DrivingPracticeAutopilot] Missing tank or drive controller.");
            return;
        }

        PracticeFinishLine.ResetCourseCompletion();
        BuildWaypoints();
        _waypointIndex = 0;
        _stuckTimer = 0f;
        _unstuckTimer = 0f;
        _unstuckPhase = 0;
        _lastProgressPosition = playerTank.transform.position;
        _lastStatusLogTime = Time.time;
        _completed = false;
        _running = true;

        if (muteAudioDuringTest)
        {
            MuteAllAudio();
        }

        driveController.SetTestInput(1f, 0f, true);
        Debug.Log("[DrivingPracticeAutopilot] START waypoints=" + _waypoints.Count);
    }

    public void StopAutopilot()
    {
        if (!_running)
        {
            return;
        }

        _running = false;
        driveController?.ClearTestInput();
        RestoreAudio();
        Debug.Log("[DrivingPracticeAutopilot] STOP completed=" + _completed);
    }

    private void Update()
    {
        if (!_running || _completed)
        {
            return;
        }

        if (PracticeFinishLine.CourseCompleted)
        {
            CompleteRun();
            return;
        }

        ResolveReferences();
        if (playerTank == null || driveController == null)
        {
            return;
        }

        Vector3 tankPosition = playerTank.transform.position;
        AdvanceWaypoints(tankPosition);
        Vector3 target = GetCurrentTarget(tankPosition);
        float steer = ComputeSteer(target);
        float throttle = 1f;
        bool lowGear = true;

        float planarSpeed = GetPlanarSpeed();
        UpdateStuckState(tankPosition, planarSpeed, ref throttle, ref steer, ref lowGear);
        EnforceCourseDirection(tankPosition, target, ref throttle);
        ApplyNorthTurnSpeedControl(tankPosition, planarSpeed, ref throttle);
        ApplyClimbAssist(planarSpeed, throttle);
        ApplyUnstuckImpulse(throttle);
        driveController.SetTestInput(throttle, steer, lowGear);

        if (Time.time - _lastStatusLogTime >= statusLogInterval)
        {
            _lastStatusLogTime = Time.time;
            Debug.Log("[DrivingPracticeAutopilot] wp="
                + (_waypointIndex + 1) + "/" + _waypoints.Count
                + " pos=" + Format(tankPosition)
                + " speed=" + planarSpeed.ToString("0.00")
                + " steer=" + steer.ToString("0.00"));
        }
    }

    private void CompleteRun()
    {
        _completed = true;
        _running = false;
        driveController?.ClearTestInput();
        RestoreAudio();
        Debug.Log("DRIVING_PRACTICE_AUTOPILOT_SUCCESS");
    }

    private void ResolveReferences()
    {
        if (playerTank == null)
        {
            playerTank = Object.FindAnyObjectByType<TankController>();
        }

        if (playerTank != null && driveController == null)
        {
            driveController = playerTank.GetComponent<TankDriveController>();
        }
    }

    private void BuildWaypoints()
    {
        _waypoints.Clear();

        PracticeFinishLine finish = Object.FindAnyObjectByType<PracticeFinishLine>();
        float finishZ = finish != null ? finish.transform.position.z : -132f;
        bool expandedCourse = finishZ < -400f;

        if (expandedCourse)
        {
            AddWaypoint(0f, -640f);
            AddWaypoint(0f, -520f);
            AddWaypoint(0f, -360f);
            AddWaypoint(0f, -180f);
            AddWaypoint(0f, 0f);
            AddWaypoint(0f, 180f);
            AddWaypoint(0f, 360f);
            AddWaypoint(0f, 520f);
            AddWaypoint(0f, 650f);
            AddWaypoint(-25f, 700f);
            AddWaypoint(-70f, 760f);
            AddWaypoint(-160f, 790f);
            AddWaypoint(-210f, 760f);
            AddWaypoint(-210f, 600f);
            AddWaypoint(-210f, 420f);
            AddWaypoint(-210f, 240f);
            AddWaypoint(-210f, 60f);
            AddWaypoint(-210f, -120f);
            AddWaypoint(-210f, -300f);
            AddWaypoint(-210f, -440f);
            AddWaypoint(-150f, -560f);
            AddWaypoint(-70f, -630f);
            AddWaypoint(0f, finishZ);
        }
        else
        {
            AddWaypoint(0f, -130f);
            AddWaypoint(0f, -90f);
            AddWaypoint(0f, -40f);
            AddWaypoint(0f, 20f);
            AddWaypoint(0f, 80f);
            AddWaypoint(0f, 130f);
            AddWaypoint(-20f, 152f);
            AddWaypoint(-42f, 150f);
            AddWaypoint(-42f, 100f);
            AddWaypoint(-42f, 40f);
            AddWaypoint(-42f, -20f);
            AddWaypoint(-42f, -80f);
            AddWaypoint(-30f, -120f);
            AddWaypoint(0f, finishZ);
        }
    }

    private void AddWaypoint(float x, float z)
    {
        _waypoints.Add(new Vector3(x, 0f, z));
    }

    private void AdvanceWaypoints(Vector3 tankPosition)
    {
        while (_waypointIndex < _waypoints.Count - 1)
        {
            Vector3 waypoint = _waypoints[_waypointIndex];
            Vector3 nextWaypoint = _waypoints[_waypointIndex + 1];
            float distance = PlanarDistance(tankPosition, waypoint);
            if (distance <= waypointReachDistance || HasPassedWaypoint(tankPosition, waypoint, nextWaypoint))
            {
                _waypointIndex++;
                continue;
            }

            break;
        }
    }

    private bool HasPassedWaypoint(Vector3 position, Vector3 waypoint, Vector3 nextWaypoint)
    {
        Vector3 segment = nextWaypoint - waypoint;
        segment.y = 0f;
        if (segment.sqrMagnitude < 1f)
        {
            return PlanarDistance(position, waypoint) <= waypointReachDistance * 1.5f;
        }

        segment.Normalize();
        Vector3 toTank = position - waypoint;
        toTank.y = 0f;
        float progress = Vector3.Dot(toTank, segment);
        if (progress <= waypointReachDistance * 0.75f)
        {
            return false;
        }

        Vector3 lateral = toTank - segment * progress;
        return lateral.magnitude <= 22f;
    }

    private Vector3 SnapTargetToLane(Vector3 tankPosition, Vector3 target)
    {
        if (tankPosition.z > -520f && tankPosition.z < 680f)
        {
            target.x = 0f;
        }

        if (tankPosition.z > -460f && tankPosition.z < 780f && tankPosition.x < -60f)
        {
            target.x = -210f;
        }

        if (tankPosition.z < -520f && tankPosition.x < -40f)
        {
            target.x = 0f;
            target.z = Mathf.Max(target.z, -660f);
        }

        if (tankPosition.z >= 750f && tankPosition.z <= 860f && tankPosition.x > -170f)
        {
            target.x = Mathf.Min(target.x, -140f);
            target.z = Mathf.Min(target.z, 790f);
        }

        return target;
    }

    private Vector3 GetCurrentTarget(Vector3 tankPosition)
    {
        if (_waypointIndex >= _waypoints.Count)
        {
            return GetFinishTarget(tankPosition);
        }

        return SnapTargetToLane(tankPosition, _waypoints[_waypointIndex]);
    }

    private Vector3 GetFinishTarget(Vector3 tankPosition)
    {
        PracticeFinishLine finish = Object.FindAnyObjectByType<PracticeFinishLine>();
        Vector3 target = finish != null ? finish.transform.position : tankPosition + tankPosition.normalized;
        return SnapTargetToLane(tankPosition, target);
    }

    private float ComputeSteer(Vector3 target)
    {
        Vector3 toTarget = target - playerTank.transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f)
        {
            return 0f;
        }

        toTarget.Normalize();
        Vector3 forward = playerTank.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        float signedAngle = Vector3.SignedAngle(forward, toTarget, Vector3.up);
        float steer = Mathf.Clamp(signedAngle / 35f, -1f, 1f);
        return ApplyLaneAssist(steer, playerTank.transform.position);
    }

    private float ApplyLaneAssist(float steer, Vector3 position)
    {
        if (position.z > -520f && position.z < 680f)
        {
            float laneSteer = Mathf.Clamp(-position.x / 5f, -1f, 1f);
            float blend = Mathf.Abs(position.x) > 4f ? 0.75f : 0.45f;
            steer = Mathf.Clamp(steer * (1f - blend) + laneSteer * blend, -1f, 1f);
        }

        if (position.z >= 620f && position.z <= 780f && position.x > -120f)
        {
            float turnSteer = Mathf.Clamp((-160f - position.x) / 90f, -1f, 1f);
            steer = Mathf.Clamp(steer * 0.35f + turnSteer * 0.65f, -1f, 1f);
        }

        if (position.z > -460f && position.z < 780f && position.x < -80f)
        {
            float laneSteer = Mathf.Clamp((-210f - position.x) / 10f, -1f, 1f);
            steer = Mathf.Clamp(steer * 0.45f + laneSteer * 0.55f, -1f, 1f);
        }

        return steer;
    }

    private void ApplyNorthTurnSpeedControl(Vector3 tankPosition, float planarSpeed, ref float throttle)
    {
        if (tankPosition.z < 620f || tankPosition.z > 820f || _unstuckTimer > 0f)
        {
            return;
        }

        if (planarSpeed > 34f)
        {
            throttle = Mathf.Min(throttle, 0.35f);
        }
        else if (planarSpeed > 24f)
        {
            throttle = Mathf.Min(throttle, 0.65f);
        }
    }

    private void ApplyClimbAssist(float planarSpeed, float throttle)
    {
        if (throttle <= 0.2f || planarSpeed > 2.4f || Time.time < _nextClimbAssistTime)
        {
            return;
        }

        _nextClimbAssistTime = Time.time + 0.45f;
        Rigidbody body = playerTank.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        body.AddForce(playerTank.transform.forward * body.mass * 6f, ForceMode.Impulse);
    }

    private void EnforceCourseDirection(Vector3 tankPosition, Vector3 target, ref float throttle)
    {
        if (_unstuckTimer > 0f && throttle < 0f)
        {
            return;
        }

        float deltaZ = target.z - tankPosition.z;
        if (deltaZ > 8f && throttle < 0.25f)
        {
            throttle = 1f;
        }
        else if (deltaZ < -8f && throttle > -0.25f)
        {
            throttle = -0.5f;
        }
    }

    private void ApplyUnstuckImpulse(float throttle)
    {
        if (_unstuckTimer <= 0f || _unstuckPhase < 2 || throttle <= 0f)
        {
            return;
        }

        if (Time.time < _nextUnstuckImpulseTime)
        {
            return;
        }

        _nextUnstuckImpulseTime = Time.time + 0.35f;
        Rigidbody body = playerTank.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        body.AddForce(playerTank.transform.forward * body.mass * 12f, ForceMode.Impulse);
    }

    private void UpdateStuckState(Vector3 tankPosition, float planarSpeed, ref float throttle, ref float steer, ref bool lowGear)
    {
        float moved = PlanarDistance(tankPosition, _lastProgressPosition);
        if (moved > 1.2f || planarSpeed > stuckSpeedThreshold + 0.15f)
        {
            _lastProgressPosition = tankPosition;
            _stuckTimer = 0f;
            if (_unstuckTimer <= 0f)
            {
                _unstuckPhase = 0;
            }
        }
        else if (throttle > 0.2f)
        {
            _stuckTimer += Time.deltaTime;
        }

        if (_stuckTimer >= hardStuckTimeSeconds)
        {
            PerformHardStuckRecovery(tankPosition, ref throttle, ref steer);
            return;
        }

        if (_stuckTimer < stuckTimeSeconds && _unstuckTimer <= 0f)
        {
            return;
        }

        if (_unstuckTimer <= 0f)
        {
            _unstuckPhase = (_unstuckPhase + 1) % 4;
            _unstuckTimer = unstuckReverseSeconds;
            _stuckTimer = 0f;
            _lastProgressPosition = tankPosition;
            Debug.LogWarning("[DrivingPracticeAutopilot] UNSTUCK phase=" + _unstuckPhase);
        }

        _unstuckTimer -= Time.deltaTime;
        lowGear = true;
        switch (_unstuckPhase)
        {
            case 0:
                throttle = -0.55f;
                steer = ApplyLaneAssist(0.75f, playerTank.transform.position);
                break;
            case 1:
                throttle = -0.55f;
                steer = ApplyLaneAssist(-0.75f, playerTank.transform.position);
                break;
            case 2:
                throttle = 1f;
                steer = ApplyLaneAssist(0.35f, playerTank.transform.position);
                break;
            default:
                throttle = 1f;
                steer = ApplyLaneAssist(-0.35f, playerTank.transform.position);
                break;
        }
    }

    private void PerformHardStuckRecovery(Vector3 tankPosition, ref float throttle, ref float steer)
    {
        if (Time.time < _nextHardRecoveryTime || playerTank == null)
        {
            return;
        }

        _nextHardRecoveryTime = Time.time + 8f;
        _hardRecoveryCount++;
        _stuckTimer = 0f;
        _unstuckTimer = 0f;
        _unstuckPhase = 0;
        _lastProgressPosition = tankPosition;

        Rigidbody body = playerTank.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        Vector3 target = GetCurrentTarget(tankPosition);
        Vector3 forward = target - tankPosition;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.5f)
        {
            forward = playerTank.transform.forward;
        }

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 nudge = forward * 2.5f + right * ((_hardRecoveryCount % 2 == 0) ? 1f : -1f);
        body.position += new Vector3(nudge.x, 0.2f, nudge.z);
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = forward * 3.5f;
#else
        body.velocity = forward * 3.5f;
#endif
        body.angularVelocity *= 0.25f;
        body.WakeUp();

        throttle = 1f;
        steer = ComputeSteer(target);
        Debug.LogWarning("[DrivingPracticeAutopilot] HARD_STUCK_RECOVERY count="
            + _hardRecoveryCount
            + " pos=" + Format(body.position)
            + " wp=" + (_waypointIndex + 1) + "/" + _waypoints.Count);
    }

    private float GetPlanarSpeed()
    {
        return playerTank.CurrentGroundSpeed;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static string Format(Vector3 value)
    {
        return "(" + value.x.ToString("0") + "," + value.z.ToString("0") + ")";
    }

    private void MuteAllAudio()
    {
        if (_audioMuted)
        {
            return;
        }

        _savedListenerVolume = AudioListener.volume;
        AudioListener.volume = 0f;
        AudioSource[] sources = Object.FindObjectsByType<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].mute = true;
        }

        _audioMuted = true;
    }

    private void RestoreAudio()
    {
        if (!_audioMuted)
        {
            return;
        }

        AudioListener.volume = _savedListenerVolume;
        AudioSource[] sources = Object.FindObjectsByType<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].mute = false;
        }

        _audioMuted = false;
    }

    private void OnDestroy()
    {
        driveController?.ClearTestInput();
        RestoreAudio();
    }
}
