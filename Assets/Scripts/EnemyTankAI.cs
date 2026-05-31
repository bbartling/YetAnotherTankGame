using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class EnemyTankAI : MonoBehaviour
{
    [Header("References")]
    public Transform turret;
    public Transform barrelPivot;
    public Transform firePoint;
    public GameObject shellPrefab;
    public AudioClip enemyFireSound;

    [Header("Engagement")]
    public float detectionRange = 160f;
    public float preferredDistance = 78f;
    public float retreatDistance = 44f;
    public float moveForce = 85f;
    public float turnTorque = 75f;
    public float strafeForce = 20f;
    public float shellPower = 55f;
    public float fireCooldown = 2.4f;
    public float aimSpeed = 3.25f;
    public float accuracy = 0.72f;

    [Header("Patrol")]
    public Transform patrolCenter;
    public float patrolRadius = 44f;
    public float patrolOrbitSpeed = 0.35f;
    public float patrolMoveForce = 55f;
    public float patrolTurnTorque = 55f;
    public float patrolStartAngle = 0f;
    public bool patrolClockwise = true;

    [Header("Barrel")]
    public float minElevation = -8f;
    public float maxElevation = 48f;
    public float barrelElevationSpeed = 25f;
    public float visibilityHeight = 0.6f;

    [Header("Audio")]
    public AudioClip engineRunningClip;
    public AudioClip engineStartClip;
    public AudioClip engineStopClip;
    public float engineStopDelay = 0.8f;

    private Rigidbody _rb;
    private AudioSource _audio;
    private Transform _player;
    private float _nextFireTime;
    private float _currentElevation;
    private float _stopTimer;
    private bool _engineRunning;
    private bool _engineStarting;
    private float _patrolAngle;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();
        _rb.linearDamping = 0.8f;
        _rb.angularDamping = 2.3f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        AutoWireReferences();
        _patrolAngle = patrolStartAngle;
    }

    private void Start()
    {
        FindPlayer();
        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "EnemyTank";
        }
    }

    private void Update()
    {
        if (_player == null)
        {
            FindPlayer();
            return;
        }

        float distance = Vector3.Distance(transform.position, _player.position);
        bool shouldEngage = distance <= detectionRange;
        HandleAudio(shouldEngage, distance);

        if (!shouldEngage)
        {
            Patrol();
            return;
        }

        AimTurret();
        TryFire(distance);
    }

    private void FixedUpdate()
    {
        if (_player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, _player.position);
        if (distance > detectionRange)
        {
            Patrol();
            return;
        }

        Drive(distance);
    }

    private void AutoWireReferences()
    {
        if (turret == null)
        {
            Transform foundTurret = transform.Find("Turret");
            if (foundTurret != null)
            {
                turret = foundTurret;
            }
        }

        if (barrelPivot == null && turret != null)
        {
            barrelPivot = turret.Find("BarrelPivot");
        }

        if (firePoint == null && barrelPivot != null)
        {
            firePoint = barrelPivot.Find("FirePoint");
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.Find("PlayerTank");
        if (player != null)
        {
            _player = player.transform;
        }

        if (patrolCenter == null)
        {
            GameObject castle = GameObject.Find("Castle");
            if (castle == null)
            {
                castle = GameObject.Find("Castle(Clone)");
            }

            if (castle != null)
            {
                patrolCenter = castle.transform;
            }
        }
    }

    private void Patrol()
    {
        if (patrolCenter == null)
        {
            return;
        }

        float direction = patrolClockwise ? -1f : 1f;
        _patrolAngle += patrolOrbitSpeed * direction * Time.deltaTime;

        Vector3 center = patrolCenter.position;
        Vector3 orbitOffset = new Vector3(Mathf.Cos(_patrolAngle), 0f, Mathf.Sin(_patrolAngle)) * patrolRadius;
        Vector3 targetPoint = center + orbitOffset;
        targetPoint.y = transform.position.y;

        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 desiredDir = toTarget.normalized;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        float signedAngle = Vector3.SignedAngle(forward, desiredDir, Vector3.up);
        float turnInput = Mathf.Clamp(signedAngle / 45f, -1f, 1f);
        _rb.AddTorque(Vector3.up * (turnInput * patrolTurnTorque), ForceMode.Force);

        _rb.AddForce(transform.forward * patrolMoveForce, ForceMode.Force);
    }

    private void HandleAudio(bool shouldEngage, float distance)
    {
        bool driving = shouldEngage && distance > retreatDistance * 0.8f;
        if (driving)
        {
            _stopTimer = engineStopDelay;
            if (!_engineStarting && !_engineRunning)
            {
                StartEngine();
            }
        }
        else if (_engineStarting || _engineRunning)
        {
            _stopTimer -= Time.deltaTime;
            if (_stopTimer <= 0f)
            {
                StopEngine();
            }
        }

    }

    private void StartEngine()
    {
        _engineStarting = true;
        _engineRunning = false;

        if (_audio == null || engineStartClip == null)
        {
            _engineStarting = false;
            _engineRunning = true;
            return;
        }

        _audio.clip = engineStartClip;
        _audio.loop = false;
        _audio.Play();
        Invoke(nameof(TransitionToRunning), engineStartClip.length);
    }

    private void TransitionToRunning()
    {
        if (!_engineStarting)
        {
            return;
        }

        _engineStarting = false;
        _engineRunning = true;

        if (_audio != null && engineRunningClip != null)
        {
            _audio.clip = engineRunningClip;
            _audio.loop = true;
            _audio.Play();
        }
    }

    private void StopEngine()
    {
        CancelInvoke(nameof(TransitionToRunning));
        _engineStarting = false;
        _engineRunning = false;

        if (_audio != null)
        {
            _audio.Stop();
            if (engineStopClip != null)
            {
                _audio.PlayOneShot(engineStopClip);
            }
        }
    }

    private void Drive(float distance)
    {
        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 desiredDir = toPlayer.normalized;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        float signedAngle = Vector3.SignedAngle(forward, desiredDir, Vector3.up);
        float turnInput = Mathf.Clamp(signedAngle / 45f, -1f, 1f);
        _rb.AddTorque(Vector3.up * (turnInput * turnTorque), ForceMode.Force);

        float moveInput = 0f;
        if (distance > preferredDistance)
        {
            moveInput = 1f;
        }
        else if (distance < retreatDistance)
        {
            moveInput = -1f;
        }

        Vector3 lateral = Vector3.Cross(Vector3.up, desiredDir).normalized;
        float strafeDirection = Mathf.Sin(Time.time * 0.9f) * 0.5f;

        if (moveInput != 0f)
        {
            _rb.AddForce(transform.forward * (moveInput * moveForce), ForceMode.Force);
        }

        if (Mathf.Abs(strafeForce) > 0.01f)
        {
            _rb.AddForce(lateral * (strafeDirection * strafeForce), ForceMode.Force);
        }
    }

    private void AimTurret()
    {
        if (turret == null || barrelPivot == null || firePoint == null)
        {
            return;
        }

        Vector3 toPlayer = _player.position - turret.position;
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (flatToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(flatToPlayer.normalized, Vector3.up);
            turret.rotation = Quaternion.Slerp(turret.rotation, targetYaw, Time.deltaTime * aimSpeed);
        }

        Vector3 barrelToPlayer = _player.position - barrelPivot.position;
        float horizontalDistance = new Vector2(barrelToPlayer.x, barrelToPlayer.z).magnitude;
        float targetElevation = Mathf.Atan2(barrelToPlayer.y, Mathf.Max(0.01f, horizontalDistance)) * Mathf.Rad2Deg;
        targetElevation = Mathf.Clamp(targetElevation, minElevation, maxElevation);
        _currentElevation = Mathf.MoveTowards(_currentElevation, targetElevation, barrelElevationSpeed * Time.deltaTime);
        barrelPivot.localRotation = Quaternion.Euler(-_currentElevation, 0f, 0f);
    }

    private void TryFire(float distance)
    {
        if (Time.time < _nextFireTime || shellPrefab == null || firePoint == null)
        {
            return;
        }

        Vector3 toPlayer = (_player.position + Vector3.up * visibilityHeight) - firePoint.position;
        float facingDot = Vector3.Dot(firePoint.forward, toPlayer.normalized);
        if (distance > detectionRange || facingDot < 0.82f)
        {
            return;
        }

        if (!HasLineOfSight())
        {
            return;
        }

        Fire();
        _nextFireTime = Time.time + fireCooldown;
    }

    private bool HasLineOfSight()
    {
        if (_player == null || firePoint == null)
        {
            return false;
        }

        Vector3 origin = firePoint.position;
        Vector3 target = _player.position + Vector3.up * visibilityHeight;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
        {
            return true;
        }

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance))
        {
            return hit.transform == _player || hit.transform.IsChildOf(_player);
        }

        return true;
    }

    private void Fire()
    {
        if (_audio != null && enemyFireSound != null)
        {
            _audio.PlayOneShot(enemyFireSound);
        }

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        ProjectileCameraController cameraController = shell.GetComponent<ProjectileCameraController>();
        if (cameraController != null)
        {
            cameraController.enableCameraSwitching = false;
        }

        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb != null)
        {
            float spread = Mathf.Clamp01(1f - accuracy) * 0.02f;
            Vector3 target = _player.position + Vector3.up * 1.1f;
            Vector3 aimOrigin = firePoint.position;
            Vector3 aimVelocity;

            if (TryGetBallisticVelocity(aimOrigin, target, shellPower, Physics.gravity, true, out aimVelocity))
            {
                Vector3 scatter = Random.insideUnitSphere * spread;
                aimVelocity += scatter;
                SetVelocity(rb, aimVelocity);
            }
            else
            {
                Vector3 fallback = firePoint.forward * shellPower;
                SetVelocity(rb, fallback);
            }
        }
    }

    private bool TryGetBallisticVelocity(Vector3 origin, Vector3 target, float speed, Vector3 gravity, bool highArc, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float x = toTargetXZ.magnitude;
        float y = toTarget.y;
        float g = Mathf.Abs(gravity.y);

        if (x < 0.01f || speed <= 0.01f)
        {
            return false;
        }

        float speedSqr = speed * speed;
        float discriminant = speedSqr * speedSqr - g * (g * x * x + 2f * y * speedSqr);
        if (discriminant < 0f)
        {
            return false;
        }

        float sqrt = Mathf.Sqrt(discriminant);
        float tanTheta = highArc
            ? (speedSqr + sqrt) / (g * x)
            : (speedSqr - sqrt) / (g * x);

        float cos = 1f / Mathf.Sqrt(1f + tanTheta * tanTheta);
        float sin = tanTheta * cos;
        Vector3 xzDir = toTargetXZ.normalized;
        velocity = xzDir * (speed * cos) + Vector3.up * (speed * sin);
        return true;
    }

    private void SetVelocity(Rigidbody body, Vector3 value)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = value;
#else
        body.velocity = value;
#endif
    }
}
