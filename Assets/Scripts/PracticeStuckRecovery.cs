using UnityEngine;

[DisallowMultipleComponent]
public class PracticeStuckRecovery : MonoBehaviour
{
    public TankController tank;
    public float stuckSpeedThreshold = 0.35f;
    public float stuckMoveThreshold = 0.75f;
    public float softStuckSeconds = 3f;
    public float hardStuckSeconds = 10f;
    public float recoveryCooldownSeconds = 6f;

    private float _stuckTimer;
    private float _cooldownTimer;
    private Vector3 _lastPosition;
    private int _hardRecoveryCount;

    private void Start()
    {
        if (tank == null)
        {
            tank = GetComponent<TankController>();
        }

        _lastPosition = tank != null ? tank.transform.position : transform.position;
    }

    private void FixedUpdate()
    {
        if (tank == null || tank.IsDestroyed)
        {
            return;
        }

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.fixedDeltaTime;
        }

        Vector3 position = tank.transform.position;
        float moved = Vector3.Distance(
            new Vector3(position.x, 0f, position.z),
            new Vector3(_lastPosition.x, 0f, _lastPosition.z));
        float speed = tank.CurrentGroundSpeed;

        if (moved > stuckMoveThreshold || speed > stuckSpeedThreshold + 0.1f)
        {
            _stuckTimer = 0f;
            _lastPosition = position;
            return;
        }

        _stuckTimer += Time.fixedDeltaTime;
        if (_stuckTimer < softStuckSeconds || _cooldownTimer > 0f)
        {
            return;
        }

        if (_stuckTimer >= hardStuckSeconds)
        {
            PerformHardRecovery(position);
            return;
        }

        PerformSoftRecovery();
    }

    private void PerformSoftRecovery()
    {
        Rigidbody body = tank.RigidbodyComponent;
        if (body == null)
        {
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(tank.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        body.AddForce(forward * body.mass * 4f, ForceMode.Impulse);
    }

    private void PerformHardRecovery(Vector3 position)
    {
        Rigidbody body = tank.RigidbodyComponent;
        if (body == null)
        {
            return;
        }

        _hardRecoveryCount++;
        _stuckTimer = 0f;
        _cooldownTimer = recoveryCooldownSeconds;
        _lastPosition = position;

        Vector3 forward = Vector3.ProjectOnPlane(tank.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 nudge = forward * 2.2f + right * ((_hardRecoveryCount % 2 == 0) ? 0.8f : -0.8f);
        body.position += new Vector3(nudge.x, 0.15f, nudge.z);
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = forward * Mathf.Min(4f, tank.CurrentGroundSpeed + 1.5f);
#else
        body.velocity = forward * Mathf.Min(4f, tank.CurrentGroundSpeed + 1.5f);
#endif
        body.angularVelocity *= 0.35f;
        body.WakeUp();

        Debug.LogWarning("[PracticeStuckRecovery] HARD_RECOVERY count="
            + _hardRecoveryCount
            + " pos=" + Format(body.position)
            + " speed=" + tank.CurrentGroundSpeed.ToString("0.00"));
    }

    public int HardRecoveryCount => _hardRecoveryCount;

    private static string Format(Vector3 value)
    {
        return "(" + value.x.ToString("0") + "," + value.y.ToString("0.0") + "," + value.z.ToString("0") + ")";
    }
}
