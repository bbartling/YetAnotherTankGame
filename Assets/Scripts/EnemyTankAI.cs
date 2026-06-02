using System.Collections;
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
    public float detectionRange = 190f;
    public float preferredDistance = 84f;
    public float retreatDistance = 42f;
    public float moveForce = 92f;
    public float turnTorque = 82f;
    public float strafeForce = 28f;
    public float shellPower = 92f;
    public float fireCooldown = 2.1f;
    public float aimSpeed = 3.75f;
    public float accuracy = 0.72f;
    public float visibilityHeight = 0.55f;
    public float lastKnownMemorySeconds = 5.5f;
    public float searchOrbitSpeed = 0.8f;
    public float searchMoveForce = 72f;
    public float searchTurnTorque = 68f;
    public float ambushRadius = 18f;

    [Header("Patrol")]
    public Transform patrolCenter;
    public float patrolRadius = 56f;
    public float patrolOrbitSpeed = 0.3f;
    public float patrolMoveForce = 58f;
    public float patrolTurnTorque = 58f;
    public float patrolStartAngle = 0f;
    public bool patrolClockwise = true;
    public float patrolJitter = 0.25f;

    [Header("Barrel")]
    public float minElevation = -8f;
    public float maxElevation = 48f;
    public float barrelElevationSpeed = 25f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float projectileDirectDamage = 120f;
    public float projectileBlastDamage = 100f;
    public float collisionDamage = 60f;
    public float fatalImpactThreshold = 0.35f;
    public float deathDelay = 0.35f;
    public int crumblePieceCount = 12;
    public float crumbleForce = 20f;

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
    private float _health;
    private bool _isDead;
    private Vector3 _spawnPosition;
    private Vector3 _lastKnownPlayerPosition;
    private float _lastSeenTime;
    private float _searchOrbitAngle;

    private enum EnemyState
    {
        Patrol,
        Hunt,
        Search,
        Dead
    }

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
        _spawnPosition = transform.position;
        _health = maxHealth;
    }

    private void Start()
    {
        FindPlayerAndPatrolCenter();

        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "EnemyTank";
        }
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        if (_player == null)
        {
            FindPlayerAndPatrolCenter();
        }

        EnemyState state = GetState();
        float distance = GetCurrentDistance();

        HandleAudio(state, distance);

        if (state == EnemyState.Patrol)
        {
            return;
        }

        Vector3 targetPoint = GetCurrentTargetPoint(state);
        AimTurret(targetPoint);
        TryFire(targetPoint, distance, state);
    }

    private void FixedUpdate()
    {
        if (_isDead)
        {
            return;
        }

        EnemyState state = GetState();
        Vector3 targetPoint = GetCurrentTargetPoint(state);

        switch (state)
        {
            case EnemyState.Hunt:
                DriveToward(targetPoint, moveForce, turnTorque, preferredDistance, retreatDistance, true, false);
                break;
            case EnemyState.Search:
                DriveToward(targetPoint, searchMoveForce, searchTurnTorque, Mathf.Max(10f, ambushRadius), retreatDistance, true, true);
                break;
            default:
                DrivePatrol();
                break;
        }
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

    private void FindPlayerAndPatrolCenter()
    {
        if (_player == null)
        {
            GameObject player = GameObject.Find("PlayerTank");
            if (player != null)
            {
                _player = player.transform;
            }
        }

        if (patrolCenter == null)
        {
            GameObject mapRoot = GameObject.Find("MapRoot");
            if (mapRoot != null)
            {
                patrolCenter = mapRoot.transform;
                return;
            }

            GameObject castle = GameObject.Find("Castle");
            if (castle == null)
            {
                castle = GameObject.Find("Castle(Clone)");
            }

            if (castle != null)
            {
                patrolCenter = castle.transform;
                return;
            }

            patrolCenter = transform;
        }
    }

private EnemyState GetState()
    {
        if (_player == null)
        {
            return EnemyState.Patrol;
        }

        float distance = Vector3.Distance(transform.position, _player.position);
        bool hasSight = HasLineOfSight();
        if (distance <= detectionRange && hasSight)
        {
            _lastKnownPlayerPosition = _player.position;
            _lastSeenTime = Time.time;
            return EnemyState.Hunt;
        }

        if (distance <= detectionRange && Time.time - _lastSeenTime <= lastKnownMemorySeconds)
        {
            return EnemyState.Search;
        }

        return EnemyState.Patrol;
    }

    private float GetCurrentDistance()
    {
        if (_player == null)
        {
            return float.PositiveInfinity;
        }

        return Vector3.Distance(transform.position, _player.position);
    }

    private Vector3 GetCurrentTargetPoint(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Hunt:
                if (_player != null)
                {
                    return _player.position;
                }
                break;
            case EnemyState.Search:
                return BuildSearchPoint();
        }

        return BuildPatrolPoint();
    }

    private Vector3 BuildPatrolPoint()
    {
        if (patrolCenter == null)
        {
            return _spawnPosition;
        }

        float direction = patrolClockwise ? -1f : 1f;
        _patrolAngle += patrolOrbitSpeed * direction * Time.deltaTime;

        Vector3 center = patrolCenter.position;
        Vector3 orbitOffset = new Vector3(Mathf.Cos(_patrolAngle), 0f, Mathf.Sin(_patrolAngle)) * patrolRadius;
        Vector3 jitter = new Vector3(Mathf.Sin(Time.time * 0.7f + transform.position.x * 0.05f), 0f, Mathf.Cos(Time.time * 0.45f + transform.position.z * 0.05f)) * patrolJitter;
        return center + orbitOffset + jitter;
    }

    private Vector3 BuildSearchPoint()
    {
        _searchOrbitAngle += searchOrbitSpeed * Time.deltaTime;
        Vector3 orbit = new Vector3(Mathf.Cos(_searchOrbitAngle), 0f, Mathf.Sin(_searchOrbitAngle)) * ambushRadius;
        return _lastKnownPlayerPosition + orbit;
    }

    private void Patrol()
    {
        DriveToward(BuildPatrolPoint(), patrolMoveForce, patrolTurnTorque, patrolRadius, 8f, false, false);
    }

    private void DrivePatrol()
    {
        Patrol();
    }

    private void DriveToward(Vector3 targetPoint, float driveForce, float torque, float preferredRadius, float reverseRadius, bool allowReverse, bool useOrbitPressure)
    {
        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 desiredDir = toTarget.normalized;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        float signedAngle = Vector3.SignedAngle(forward, desiredDir, Vector3.up);
        float turnInput = Mathf.Clamp(signedAngle / 45f, -1f, 1f);
        _rb.AddTorque(Vector3.up * (turnInput * torque), ForceMode.Force);

        float distance = toTarget.magnitude;
        float moveInput = 0f;
        if (distance > preferredRadius)
        {
            moveInput = 1f;
        }
        else if (allowReverse && distance < reverseRadius)
        {
            moveInput = -1f;
        }

        if (moveInput != 0f)
        {
            _rb.AddForce(transform.forward * (moveInput * driveForce), ForceMode.Force);
        }

        if (useOrbitPressure)
        {
            Vector3 lateral = Vector3.Cross(Vector3.up, desiredDir).normalized;
            float pressure = Mathf.Sin(Time.time * 1.05f + transform.position.x * 0.03f + transform.position.z * 0.02f) * 0.65f;
            _rb.AddForce(lateral * (pressure * strafeForce), ForceMode.Force);
        }
    }

    private void HandleAudio(EnemyState state, float distance)
    {
        bool driving = state != EnemyState.Patrol && distance > retreatDistance * 0.8f;
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

    private void AimTurret(Vector3 targetPoint)
    {
        if (turret == null || barrelPivot == null || firePoint == null)
        {
            return;
        }

        Vector3 toTarget = targetPoint - turret.position;
        Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flatToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(flatToTarget.normalized, Vector3.up);
            turret.rotation = Quaternion.Slerp(turret.rotation, targetYaw, Time.deltaTime * aimSpeed);
        }

        Vector3 barrelToTarget = targetPoint - barrelPivot.position;
        float horizontalDistance = new Vector2(barrelToTarget.x, barrelToTarget.z).magnitude;
        float targetElevation = Mathf.Atan2(barrelToTarget.y, Mathf.Max(0.01f, horizontalDistance)) * Mathf.Rad2Deg;
        targetElevation = Mathf.Clamp(targetElevation, minElevation, maxElevation);
        _currentElevation = Mathf.MoveTowards(_currentElevation, targetElevation, barrelElevationSpeed * Time.deltaTime);
        barrelPivot.localRotation = Quaternion.Euler(-_currentElevation, 0f, 0f);
    }

    private void TryFire(Vector3 targetPoint, float distance, EnemyState state)
    {
        if (Time.time < _nextFireTime || shellPrefab == null || firePoint == null)
        {
            return;
        }

        if (state == EnemyState.Patrol)
        {
            return;
        }

        Vector3 target = targetPoint + Vector3.up * visibilityHeight;
        Vector3 toTarget = target - firePoint.position;
        float facingDot = Vector3.Dot(firePoint.forward, toTarget.normalized);
        if (distance > detectionRange || facingDot < 0.72f)
        {
            return;
        }

        if (!HasLineOfSight(target))
        {
            return;
        }

        Fire(target);
        _nextFireTime = Time.time + fireCooldown;
    }

    private bool HasLineOfSight()
    {
        if (_player == null)
        {
            return false;
        }

        return HasLineOfSight(_player.position + Vector3.up * visibilityHeight);
    }

    private bool HasLineOfSight(Vector3 target)
    {
        if (firePoint == null)
        {
            return false;
        }

        Vector3 origin = firePoint.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
        {
            return true;
        }

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance))
        {
            return hit.transform == _player || (_player != null && hit.transform.IsChildOf(_player));
        }

        return true;
    }

    private void Fire(Vector3 target)
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
            cameraController.applyWindDrift = false;
            cameraController.launchPowerPercentage = 100f;
        }

        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb == null)
        {
            return;
        }

        float spread = Mathf.Clamp01(1f - accuracy) * 0.02f;
        Vector3 aimVelocity;
        if (TryGetBallisticVelocity(firePoint.position, target, shellPower, Physics.gravity, true, out aimVelocity))
        {
            aimVelocity += Random.insideUnitSphere * spread;
            SetVelocity(rb, aimVelocity);
        }
        else
        {
            SetVelocity(rb, firePoint.forward * shellPower);
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

    public void ApplyProjectileDamage(float impactForce, Vector3 worldPoint, Vector3 worldNormal)
    {
        if (_isDead)
        {
            return;
        }

        float damage = Mathf.Max(projectileDirectDamage, impactForce * 1.25f);
        ApplyDamage(damage, worldPoint, worldNormal, true);
    }

    public void ApplyExplosionDamage(float explosionForce, Vector3 explosionPoint, Vector3 worldNormal, float distanceFactor = 1f)
    {
        if (_isDead)
        {
            return;
        }

        float damage = Mathf.Max(projectileBlastDamage, explosionForce * Mathf.Clamp01(distanceFactor) * 1.15f);
        ApplyDamage(damage, explosionPoint, worldNormal, true);
    }

    public void ApplyCollisionDamage(float impactForce, Vector3 worldPoint, Vector3 worldNormal)
    {
        if (_isDead)
        {
            return;
        }

        float damage = Mathf.Max(collisionDamage, impactForce);
        ApplyDamage(damage, worldPoint, worldNormal, false);
    }

    private void ApplyDamage(float amount, Vector3 worldPoint, Vector3 worldNormal, bool canTriggerFatalImpact)
    {
        if (_isDead)
        {
            return;
        }

        _health -= Mathf.Max(0.1f, amount);
        if (canTriggerFatalImpact && amount >= maxHealth * fatalImpactThreshold)
        {
            _health = 0f;
        }

        if (_health <= 0f)
        {
            Die(worldPoint, worldNormal, amount);
        }
    }

    private void Die(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        CancelInvoke();
        StopAllCoroutines();
        StartCoroutine(DeathSequence(worldPoint, worldNormal, force));
    }

    private IEnumerator DeathSequence(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        if (_audio != null)
        {
            _audio.Stop();
        }

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.AddExplosionForce(Mathf.Max(400f, force * 10f), worldPoint, 12f, 1.5f, ForceMode.Impulse);
            _rb.isKinematic = true;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }

        SpawnCrumblePieces(worldPoint, worldNormal, force);

        if (worldNormal.sqrMagnitude > 0.001f && _rb != null)
        {
            _rb.AddForce(worldNormal.normalized * Mathf.Max(20f, force * 0.5f), ForceMode.Impulse);
        }

        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }

    private void SpawnCrumblePieces(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        Color baseColor = new Color(0.42f, 0.41f, 0.4f, 1f);
        Renderer renderer = GetComponentInChildren<Renderer>(true);
        if (renderer != null && renderer.material != null)
        {
            baseColor = renderer.material.color;
        }

        for (int i = 0; i < crumblePieceCount; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere);
            piece.name = name + "_Piece";
            piece.transform.position = transform.position + Random.insideUnitSphere * 0.8f;
            piece.transform.localScale = Vector3.one * Random.Range(0.16f, 0.45f);

            Collider collider = piece.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer pieceRenderer = piece.GetComponent<Renderer>();
            if (pieceRenderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = Color.Lerp(baseColor, new Color(0.18f, 0.18f, 0.18f, 1f), Random.Range(0.1f, 0.55f));
                pieceRenderer.material = material;
            }

            Rigidbody rb = piece.AddComponent<Rigidbody>();
            rb.mass = Random.Range(0.03f, 0.12f);
            rb.linearDamping = 0.12f;
            rb.angularDamping = 0.08f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            Vector3 toss = (piece.transform.position - worldPoint).normalized + worldNormal * 0.8f + Random.insideUnitSphere * 0.45f;
            rb.AddForce(toss.normalized * Random.Range(crumbleForce * 0.55f, crumbleForce), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * force * 0.12f, ForceMode.Impulse);

            Destroy(piece, Random.Range(1.5f, 4.5f));
        }
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
