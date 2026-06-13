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
    public float detectionRange = 1300f;
    public float preferredDistance = 220f;
    public float retreatDistance = 100f;
    public float moveForce = 24f;
    public float turnTorque = 32f;
    public float strafeForce = 0f;
    public float shellPower = 145f;
    public float fireCooldown = 8f;
    public float aimSpeed = 1.25f;
    public float accuracy = 0.72f;
    public float visibilityHeight = 0.55f;
    public float lastKnownMemorySeconds = 5.5f;
    public float searchOrbitSpeed = 0.8f;
    public float searchMoveForce = 20f;
    public float searchTurnTorque = 28f;
    public float ambushRadius = 18f;

    [Header("Patrol")]
    public Transform patrolCenter;
    public float patrolRadius = 56f;
    public float patrolOrbitSpeed = 0.3f;
    public float patrolMoveForce = 18f;
    public float patrolTurnTorque = 24f;
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
    public float deathDelay = 2.8f;
    public int crumblePieceCount = 48;
    public float crumbleForce = 38f;
    public float machineGunDamageScale = 1f;
    public float tankKillCraterForce = 82f;
    public float tankKillBlastRadius = 12f;
    public float tankKillBlastForce = 520f;

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
    private TankPerception _perception;
    private TankCombatBrain _combatBrain;
    private TankPathingBrain _pathingBrain;
    private DamageStateController _damageStateController;

    public string CurrentOperationalState { get; private set; } = "Patrol";
    public bool CurrentHasLineOfSight { get; private set; }
    public float CurrentDistanceToPlayer { get; private set; } = float.PositiveInfinity;

    private enum EnemyState
    {
        Patrol,
        Hunt,
        Search,
        Dead
    }

    private void OnEnable()
    {
        preferredDistance = Mathf.Max(preferredDistance, 220f);
        retreatDistance = Mathf.Clamp(retreatDistance, 100f, preferredDistance - 60f);
        moveForce = Mathf.Min(moveForce, 24f);
        turnTorque = Mathf.Min(turnTorque, 32f);
        strafeForce = 0f;
        fireCooldown = Mathf.Max(fireCooldown, 8f);
        aimSpeed = Mathf.Min(aimSpeed, 1.25f);
        searchMoveForce = Mathf.Min(searchMoveForce, 20f);
        searchTurnTorque = Mathf.Min(searchTurnTorque, 28f);
        patrolMoveForce = Mathf.Min(patrolMoveForce, 18f);
        patrolTurnTorque = Mathf.Min(patrolTurnTorque, 24f);
        InitializePolicies();
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();
        EnemyAudioController enemyAudio = GetComponent<EnemyAudioController>();
        if (enemyAudio == null) enemyAudio = gameObject.AddComponent<EnemyAudioController>();
        enemyAudio.ConfigureDistanceRolloff();
        _rb.linearDamping = 0.8f;
        _rb.angularDamping = 2.3f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        AutoWireReferences();
        SillyModelInstaller.Ensure(gameObject, "Models/Tanks/SillyEnemyStandard", 0.7f, true);
        _patrolAngle = patrolStartAngle;
        DisableNonPlayerCameras();

        _spawnPosition = transform.position;
        _health = maxHealth;
        _damageStateController = GetComponent<DamageStateController>();
        if (_damageStateController == null)
        {
            _damageStateController = gameObject.AddComponent<DamageStateController>();
        }
        _damageStateController?.ApplyHealthRatio(1f);
    }

private void Start()
    {
        FindPlayerAndPatrolCenter();

        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "EnemyTank";
        }

        if (GetComponent<EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<EnemyHealthBar>();
        }
    }

private void Update()
    {
        if (_isDead || (BattlefieldDirector.Instance != null && BattlefieldDirector.Instance.IsResolved))
        {
            return;
        }

        if (_player == null)
        {
            FindPlayerAndPatrolCenter();
        }

        EnemyState state = GetState();
        float distance = GetCurrentDistance();
        TankCombatBrain.State operationalState = GetOperationalState(distance);

        HandleAudio(operationalState);

        Vector3 targetPoint = GetCurrentTargetPoint(state);
        if (state == EnemyState.Patrol && _player != null && distance <= detectionRange)
        {
            targetPoint = _player.position;
        }

        AimTurret(targetPoint);
        TryFire(targetPoint, distance, operationalState);
    }

private void FixedUpdate()
    {
        if (_isDead || (BattlefieldDirector.Instance != null && BattlefieldDirector.Instance.IsResolved))
        {
            return;
        }

        EnemyState state = GetState();
        float distance = GetCurrentDistance();
        TankCombatBrain.State operationalState = GetOperationalState(distance);
        Vector3 targetPoint = GetCurrentTargetPoint(state);

        switch (operationalState)
        {
            case TankCombatBrain.State.Repositioning:
                DriveToward(targetPoint, moveForce, turnTorque, preferredDistance, retreatDistance, true, false);
                break;
            case TankCombatBrain.State.Retreating:
                DriveToward(targetPoint, moveForce, turnTorque, preferredDistance, retreatDistance, true, false);
                break;
            case TankCombatBrain.State.Suspicious:
                DriveToward(targetPoint, searchMoveForce, searchTurnTorque, Mathf.Max(10f, ambushRadius), retreatDistance, true, true);
                break;
            case TankCombatBrain.State.Patrol:
                DrivePatrol();
                break;
            default:
                SlowToAimingHalt();
                break;
        }

        _rb.linearVelocity = _pathingBrain.ClampPlanarVelocity(_rb.linearVelocity);
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

    private void DisableNonPlayerCameras()
    {
        Camera[] cameras = GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = false;
        }

        AudioListener[] listeners = GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
        {
            listeners[i].enabled = false;
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
        _perception.Observe(hasSight, _player.position, Time.time);
        CurrentHasLineOfSight = hasSight;
        if (distance <= detectionRange && hasSight)
        {
            _lastKnownPlayerPosition = _player.position;
            _lastSeenTime = Time.time;
            return EnemyState.Hunt;
        }

        if (distance <= detectionRange && _perception.HasRecentMemory(Time.time))
        {
            return EnemyState.Search;
        }

        return EnemyState.Patrol;
    }

    private void InitializePolicies()
    {
        _perception = new TankPerception(lastKnownMemorySeconds);
        _combatBrain = new TankCombatBrain
        {
            PreferredDistance = preferredDistance,
            RetreatDistance = retreatDistance,
            RepositionDistance = preferredDistance + 80f
        };
        _pathingBrain = new TankPathingBrain
        {
            MaxTravelSpeed = 2.25f,
            MaxSlopeDegrees = 35f
        };
    }

    private TankCombatBrain.State GetOperationalState(float distance)
    {
        InitializePoliciesIfNeeded();
        CurrentDistanceToPlayer = distance;
        bool aimAligned = IsAimAligned();
        bool reloading = Time.time < _nextFireTime;
        bool remembersTarget = _perception.HasRecentMemory(Time.time);
        TankCombatBrain.State state = _combatBrain.Decide(
            CurrentHasLineOfSight,
            distance,
            _rb != null ? _rb.linearVelocity.magnitude : 0f,
            aimAligned,
            reloading,
            remembersTarget,
            false,
            _isDead);
        CurrentOperationalState = state.ToString();
        return state;
    }

    public string EvaluateOperationalState(bool hasLineOfSight, float distance, float speed, bool aimAligned, bool reloading, bool remembersTarget)
    {
        InitializePoliciesIfNeeded();
        return _combatBrain.Decide(hasLineOfSight, distance, speed, aimAligned, reloading, remembersTarget, false, _isDead).ToString();
    }

    private void InitializePoliciesIfNeeded()
    {
        if (_perception == null || _combatBrain == null || _pathingBrain == null)
        {
            InitializePolicies();
        }
    }

    private bool IsAimAligned()
    {
        if (_player == null || firePoint == null)
        {
            return false;
        }

        Vector3 toTarget = (_player.position + Vector3.up * visibilityHeight) - firePoint.position;
        return toTarget.sqrMagnitude > 0.01f && Vector3.Dot(firePoint.forward, toTarget.normalized) >= 0.96f;
    }

    private void SlowToAimingHalt()
    {
        Vector3 velocity = _rb.linearVelocity;
        velocity.x *= 0.82f;
        velocity.z *= 0.82f;
        _rb.linearVelocity = velocity;
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
        DriveToward(BuildPatrolPoint(), patrolMoveForce, patrolTurnTorque, 5f, 2f, false, false);
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
        _rb.AddTorque(Vector3.up * (turnInput * torque), ForceMode.Acceleration);

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
            _rb.AddForce(transform.forward * (moveInput * driveForce), ForceMode.Acceleration);
        }

        if (useOrbitPressure)
        {
            Vector3 lateral = Vector3.Cross(Vector3.up, desiredDir).normalized;
            float pressure = Mathf.Sin(Time.time * 1.05f + transform.position.x * 0.03f + transform.position.z * 0.02f) * 0.65f;
            _rb.AddForce(lateral * (pressure * strafeForce), ForceMode.Acceleration);
        }
    }

    private void HandleAudio(TankCombatBrain.State state)
    {
        bool driving = state == TankCombatBrain.State.Patrol ||
                       state == TankCombatBrain.State.Suspicious ||
                       state == TankCombatBrain.State.Repositioning ||
                       state == TankCombatBrain.State.Retreating;
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

    private void TryFire(Vector3 targetPoint, float distance, TankCombatBrain.State state)
    {
        if (Time.time < _nextFireTime || shellPrefab == null || firePoint == null)
        {
            return;
        }

        if (state != TankCombatBrain.State.Firing)
        {
            return;
        }

        Vector3 target = targetPoint + Vector3.up * visibilityHeight;
        Vector3 toTarget = target - firePoint.position;
        float facingDot = Vector3.Dot(firePoint.forward, toTarget.normalized);
        if (distance > detectionRange || facingDot < 0.45f)
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

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, ~0, QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        RaycastHit nearestHit = default;
        bool foundHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearestHit = hits[i];
                foundHit = true;
            }
        }

        if (foundHit)
        {
            return nearestHit.transform == _player || (_player != null && nearestHit.transform.IsChildOf(_player));
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
        if (shell.GetComponent<ProjectileAudioController>() == null)
        {
            shell.AddComponent<ProjectileAudioController>();
        }
        GetComponent<TankVisualAnimator>()?.TriggerRecoil();
        IgnoreShellOwnerCollision(shell);
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

    private void IgnoreShellOwnerCollision(GameObject shell)
    {
        if (shell == null)
        {
            return;
        }

        Collider[] shellColliders = shell.GetComponentsInChildren<Collider>(true);
        Collider[] ownerColliders = GetComponentsInChildren<Collider>(true);
        for (int s = 0; s < shellColliders.Length; s++)
        {
            if (shellColliders[s] == null)
            {
                continue;
            }

            for (int o = 0; o < ownerColliders.Length; o++)
            {
                if (ownerColliders[o] != null)
                {
                    Physics.IgnoreCollision(shellColliders[s], ownerColliders[o], true);
                }
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

public void ApplyProjectileDamage(float impactForce, Vector3 worldPoint, Vector3 worldNormal)
    {
        if (_isDead)
        {
            return;
        }

        float damage = Mathf.Max(projectileDirectDamage, impactForce * 1.25f);
        if (BattlefieldDirector.Instance != null)
        {
            BattlefieldDirector.Instance.RegisterDamageDealt(damage);
        }

        ApplyDamage(damage, worldPoint, worldNormal, true);
    }

public void ApplyExplosionDamage(float explosionForce, Vector3 explosionPoint, Vector3 worldNormal, float distanceFactor = 1f)
    {
        if (_isDead)
        {
            return;
        }

        float damage = Mathf.Max(projectileBlastDamage, explosionForce * Mathf.Clamp01(distanceFactor) * 1.15f);
        if (BattlefieldDirector.Instance != null)
        {
            BattlefieldDirector.Instance.RegisterDamageDealt(damage);
        }

        ApplyDamage(damage, explosionPoint, worldNormal, true);
    }

    public void ApplyMachineGunDamage(float damage, Vector3 worldPoint, Vector3 worldNormal)
    {
        if (_isDead)
        {
            return;
        }

        float scaledDamage = Mathf.Max(0.05f, damage * machineGunDamageScale);
        BattlefieldDirector director = BattlefieldDirector.Instance;
        if (director == null)
        {
            director = Object.FindFirstObjectByType<BattlefieldDirector>();
        }

        if (director != null)
        {
            director.RegisterDamageDealt(scaledDamage);
        }

        ApplyDamage(scaledDamage, worldPoint, worldNormal, false);
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

        _damageStateController?.ApplyHealthRatio(maxHealth > 0f ? _health / maxHealth : 0f);

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

        
        if (BattlefieldDirector.Instance != null)
        {
            BattlefieldDirector.Instance.RegisterEnemyDestroyed(this);
        }
        _isDead = true;
        _damageStateController?.ApplyHealthRatio(0f);
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

        SpawnKillCrater(worldPoint, worldNormal, force);
        SpawnKillBlast(worldPoint, worldNormal, force);
        PlayKillSound(worldPoint);
        SpawnCrumblePieces(worldPoint, worldNormal, force * 0.65f, crumblePieceCount);
        yield return HideRenderersGradually(worldPoint, worldNormal, force);

        if (worldNormal.sqrMagnitude > 0.001f && _rb != null)
        {
            _rb.AddForce(worldNormal.normalized * Mathf.Max(20f, force * 0.5f), ForceMode.Impulse);
        }

        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }

    private void SpawnKillCrater(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        float craterForce = Mathf.Clamp(Mathf.Max(tankKillCraterForce, force * 0.55f), 0f, 130f);
        CraterTerrain craterTerrain = Object.FindFirstObjectByType<CraterTerrain>();
        if (craterTerrain != null)
        {
            craterTerrain.ApplyImpact(worldPoint, worldNormal.sqrMagnitude > 0.001f ? worldNormal : Vector3.up, craterForce);
            return;
        }

        DestructibleGround ground = Object.FindFirstObjectByType<DestructibleGround>();
        if (ground != null)
        {
            ground.ApplyImpact(worldPoint, worldNormal.sqrMagnitude > 0.001f ? worldNormal : Vector3.up, craterForce);
        }
    }

    private void SpawnKillBlast(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = name + "_KillFlash";
        BattlefieldEffectController.RegisterTemporary(flash, "Explosions", 16);
        flash.transform.position = transform.position + Vector3.up * 1.2f;
        flash.transform.localScale = Vector3.one * 4.5f;

        Collider flashCollider = flash.GetComponent<Collider>();
        if (flashCollider != null)
        {
            Destroy(flashCollider);
        }

        Renderer flashRenderer = flash.GetComponent<Renderer>();
        if (flashRenderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(1f, 0.38f, 0.06f, 0.92f);
            flashRenderer.material = material;
        }

        Collider[] nearby = Physics.OverlapSphere(transform.position, tankKillBlastRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < nearby.Length; i++)
        {
            Rigidbody body = nearby[i].attachedRigidbody;
            if (body != null && body != _rb)
            {
                body.AddExplosionForce(Mathf.Max(tankKillBlastForce, force * 7.5f), transform.position, tankKillBlastRadius, 1.2f, ForceMode.Impulse);
            }
        }

        Destroy(flash, 0.28f);
    }

    private void PlayKillSound(Vector3 worldPoint)
    {
        CombatSoundSlots slots = Object.FindFirstObjectByType<CombatSoundSlots>();
        if (slots != null && slots.enemyTankKill != null)
        {
            AudioSource.PlayClipAtPoint(slots.enemyTankKill, worldPoint);
        }
    }

    private void SpawnCrumblePieces(Vector3 worldPoint, Vector3 worldNormal, float force, int pieceCount)
    {
        Color baseColor = new Color(0.42f, 0.41f, 0.4f, 1f);
        Renderer renderer = GetComponentInChildren<Renderer>(true);
        if (renderer != null && renderer.material != null)
        {
            baseColor = renderer.material.color;
        }

        for (int i = 0; i < pieceCount; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere);
            piece.name = name + "_Piece";
            BattlefieldEffectController.RegisterTemporary(piece, "Debris", 64);
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

            Destroy(piece, Random.Range(4f, 8f));
        }
    }

    private IEnumerator HideRenderersGradually(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        float stepDelay = renderers.Length > 0 ? deathDelay / Mathf.Max(1, renderers.Length) : deathDelay;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            SpawnCrumblePieces(bounds.center, worldNormal.sqrMagnitude > 0.001f ? worldNormal : Vector3.up, Mathf.Max(8f, force * 0.25f), 4);
            renderer.enabled = false;
            yield return new WaitForSeconds(Mathf.Clamp(stepDelay, 0.05f, 0.22f));
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


public bool IsDestroyed
    {
        get { return _isDead; }
    }

    public string CurrentDamageState
    {
        get { return _damageStateController != null ? _damageStateController.CurrentState.ToString() : (_isDead ? "Wrecked" : "Intact"); }
    }

    public float CurrentHealth
    {
        get { return Mathf.Max(0f, _health); }
    }

    public float HealthPercent
    {
        get { return maxHealth > 0f ? Mathf.Clamp01(_health / maxHealth) * 100f : 0f; }
    }
}
