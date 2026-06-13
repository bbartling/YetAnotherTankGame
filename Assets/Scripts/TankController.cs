using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    [Header("Required Tank References")]
    [Tooltip("Best setup: an EMPTY child object located at the exact center of the turret ring on top of the tank.")]
    public Transform turretYawPivot;

    [Tooltip("Best setup: an EMPTY child object located at the barrel hinge/trunnion.")]
    public Transform barrelPitchPivot;

    [Tooltip("Muzzle point for the cannon. Its blue Z/forward arrow should point out of the barrel.")]
    public Transform cannonFirePoint;

    [Tooltip("Optional separate muzzle point for the machine gun. If empty, the cannon fire point is used.")]
    public Transform machineGunFirePoint;

    [Tooltip("Optional camera used by projectile camera switching.")]
    public Camera gameplayCamera;

    [Header("Legacy References Kept So Existing Scene Assignments Still Work")]
    public Transform turret;
    public Transform barrel;
    public Transform firePoint;
    public LineRenderer lineRenderer;

    [Header("Track Drive / WASD")]
    public float forwardAcceleration = 55f;
    public float reverseAcceleration = 38f;
    public float maxForwardSpeed = 13f;
    public float maxReverseSpeed = 7.5f;
    public float trackDriveResponse = 5.5f;
    public float turnAcceleration = 21f;
    public float pivotTurnAcceleration = 24f;
    public float lateralGrip = 36f;
    public float headingGrip = 14f;
    public float groundCheckDistance = 1.4f;
    public LayerMask groundMask = ~0;
    public float terrainSurfaceSkin = 0.18f;
    public float terrainProbeHeight = 35f;
    public float terrainProbeDistance = 90f;
    public float startTerrainClearance = 1.5f;

    [Header("Tracked Vehicle Grounding")]
    public float trackHalfWidth = 0.42f;
    public float trackHalfLength = 0.72f;
    public float suspensionProbeHeight = 1.1f;
    public float suspensionProbeDistance = 2.8f;
    public float suspensionRideHeight = 0.38f;
    public float suspensionSpring = 34f;
    public float suspensionDamper = 7f;
    public float trackDownforce = 26f;
    public float slopeAlignmentTorque = 52f;
    public float slopeAlignmentDamping = 7f;
    public float maxAssistedSlopeAngle = 62f;
    public float maxDriveSlopeAngle = 74f;
    public float brakeDrag = 7f;

    [Header("Tank Stability")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.55f, 0f);
    public float uprightAssist = 12f;
    public float angularDamping = 3f;
    public float linearDamping = 0.65f;

    [Header("Mouse Turret")]
    [Tooltip("Mouse X yaws the turret left/right. This is intentionally local to turretYawPivot so the turret does not orbit the map.")]
    public float mouseYawDegreesPerSecond = 42.5f;
    public float turretYawSpeed = 110f;

    [Tooltip("Mouse wheel plus PageUp/PageDown pitch the barrel.")]
    public float mouseWheelPitchSensitivity = 18f;
    public float keyboardPitchSpeed = 35f;
    public float minBarrelElevation = -8f;
    public float maxBarrelElevation = 35f;
    public float defaultBattleElevation = 5f;

    [Header("Cannon")]
    public GameObject shellPrefab;
    public float maxPower = 75f;
    [Range(0f, 100f)] public float powerPercentage = 65f;
    public float cannonCooldown = 0.45f;
    public KeyCode cannonKey = KeyCode.Space;
    public bool leftClickFiresCannon = true;
    public AudioClip playerFireSound;

    [Header("Optional UI")]
    public TextMeshProUGUI elevationText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI powerText;

    [Header("Health / Damage API")]
    public float maxHealth = 100f;
    public float projectileDirectDamage = 55f;
    public float projectileBlastDamage = 42f;
    public float tankCollisionDamage = 24f;
    public float fatalImpactThreshold = 0.55f;

    private Rigidbody _rb;
    private TankDriveController _driveController;
    private TankSuspensionVisual _suspensionVisual;
    private TankTurretController _turretController;
    private TankAimController _aimController;
    private TankAudioController _tankAudioController;
    private AudioSource _audio;
    private float _turretYaw;
    private float _barrelElevation;
    private float _nextCannonTime;
    private float _health;
    private bool _dead;
    private CraterTerrain _terrainSource;
    private Collider _terrainCollider;
    private Renderer _terrainRenderer;
    private DamageStateController _damageStateController;

    private struct TrackGroundHit
    {
        public Vector3 point;
        public Vector3 normal;
        public float distanceFromRideHeight;
    }

    private struct TrackGroundInfo
    {
        public bool grounded;
        public int hitCount;
        public Vector3 normal;
        public TrackGroundHit[] hits;
    }

    public Rigidbody RigidbodyComponent => _rb;
    public Vector3 CurrentVelocity => ReadVelocity();
    public float TurretYawDegrees => Mathf.Repeat(_turretYaw, 360f);
    public bool IsDestroyed => _dead;
    public float CurrentHealth => Mathf.Max(0f, _health);
    public float HealthPercent => maxHealth > 0f ? Mathf.Clamp01(_health / maxHealth) * 100f : 0f;
    public float CurrentGroundSpeed => Vector3.ProjectOnPlane(ReadVelocity(), Vector3.up).magnitude;
    public float CurrentSlopeAngle => _driveController != null ? _driveController.CurrentSlopeAngle : 0f;
    public bool IsGrounded => _driveController != null && _driveController.IsGrounded;
    public float EngineStrain => _driveController != null ? _driveController.EngineStrain : 0f;
    public string CurrentDamageState => _damageStateController != null ? _damageStateController.CurrentState.ToString() : (_dead ? "Wrecked" : "Intact");

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();
        _health = maxHealth;
        _damageStateController = GetComponent<DamageStateController>();
        if (_damageStateController == null)
        {
            _damageStateController = gameObject.AddComponent<DamageStateController>();
        }
        _damageStateController?.ApplyHealthRatio(1f);

        ConfigureRigidbody();
        AutoWireReferences();
        CacheStartingAngles();
        EnsureFocusedControllers();
        ValidatePivotSetup();
        SillyModelInstaller.Ensure(gameObject, "Models/Tanks/SillyPlayerTank", 6f, true);
    }

    private void Start()
    {
        SnapAboveTerrain(startTerrainClearance);
    }

    private void ConfigureRigidbody()
    {
        if (_rb == null) return;

        _rb.centerOfMass = centerOfMassOffset;
        _rb.constraints = RigidbodyConstraints.None;

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.angularDamping = angularDamping;
        _rb.linearDamping = linearDamping;
        _rb.maxAngularVelocity = 12f;
    }

    private void AutoWireReferences()
    {
        if (turretYawPivot == null)
        {
            Transform found = transform.Find("TurretYawPivot");
            if (found != null) turretYawPivot = found;
        }

        if (barrelPitchPivot == null && turretYawPivot != null)
        {
            Transform found = turretYawPivot.Find("BarrelPitchPivot");
            if (found == null) found = turretYawPivot.Find("BarrelPivot");
            if (found != null) barrelPitchPivot = found;
        }

        if (cannonFirePoint == null && barrelPitchPivot != null)
        {
            Transform found = barrelPitchPivot.Find("FirePoint");
            if (found == null) found = barrelPitchPivot.Find("CannonFirePoint");
            if (found != null) cannonFirePoint = found;
        }

        // Legacy fallback from your older script fields.
        if (turretYawPivot == null && turret != null) turretYawPivot = turret;
        if (barrelPitchPivot == null && barrel != null) barrelPitchPivot = barrel;
        if (cannonFirePoint == null && firePoint != null) cannonFirePoint = firePoint;
        if (machineGunFirePoint == null) machineGunFirePoint = cannonFirePoint;

        if (gameplayCamera == null)
        {
            gameplayCamera = GetComponentInChildren<Camera>(true);
            if (gameplayCamera == null) gameplayCamera = Camera.main;
        }

        if (firePoint == null) firePoint = cannonFirePoint;
        if (turret == null) turret = turretYawPivot;
        if (barrel == null) barrel = barrelPitchPivot;
    }

    private void CacheStartingAngles()
    {
        if (turretYawPivot != null)
        {
            _turretYaw = NormalizeSignedAngle(turretYawPivot.localEulerAngles.y);
        }

        if (barrelPitchPivot != null)
        {
            _barrelElevation = -NormalizeSignedAngle(barrelPitchPivot.localEulerAngles.x);
            _barrelElevation = Mathf.Clamp(_barrelElevation, minBarrelElevation, maxBarrelElevation);
        }
    }

    private void ValidatePivotSetup()
    {
        if (turretYawPivot == null)
        {
            Debug.LogError("[TankController] Missing turretYawPivot. Create an EMPTY child named TurretYawPivot on the tank roof and assign it.");
            return;
        }

        if (turret != null && turretYawPivot == turret)
        {
            Debug.LogWarning("[TankController] turretYawPivot is using the visible turret transform. If the turret orbits, create an EMPTY TurretYawPivot at the turret ring, parent the visible turret mesh under it, and assign that empty object instead.");
        }

        if (cannonFirePoint == null)
        {
            Debug.LogWarning("[TankController] Missing cannonFirePoint. Cannon will not fire until assigned.");
        }
    }

    private void Update()
    {
        if (_dead) return;

        HandleTurretAndBarrelInput();
        HandleCannonInput();
        UpdateOptionalUi();
    }

    private void FixedUpdate()
    {
        if (_dead || _rb == null) return;

        PreventTerrainPenetration();
        TrackGroundInfo groundInfo = ProbeTrackGround();
        float slopeAngle = groundInfo.grounded ? Vector3.Angle(groundInfo.normal, Vector3.up) : 0f;
        ReadDriveInput(out float telemetryThrottle, out _, out _);
        _driveController?.RecordGroundState(groundInfo.grounded, slopeAngle, telemetryThrottle);
        _suspensionVisual?.RecordGroundNormal(groundInfo.normal, Time.fixedDeltaTime);
        _tankAudioController?.SetEngineStrain(EngineStrain);
        ApplyTrackSuspension(groundInfo);
        HandleTrackDrive(groundInfo);
        ApplyTrackedGrip(groundInfo);
        AlignHullToTrackGrade(groundInfo);
        ClampGroundSpeed(groundInfo);
        PreventTerrainPenetration();
    }

    private void EnsureFocusedControllers()
    {
        _driveController = GetComponent<TankDriveController>();
        if (_driveController == null) _driveController = gameObject.AddComponent<TankDriveController>();

        _suspensionVisual = GetComponent<TankSuspensionVisual>();
        if (_suspensionVisual == null) _suspensionVisual = gameObject.AddComponent<TankSuspensionVisual>();

        _turretController = GetComponent<TankTurretController>();
        if (_turretController == null) _turretController = gameObject.AddComponent<TankTurretController>();
        _turretController.Bind(turretYawPivot, 55f, 32f);

        _aimController = GetComponent<TankAimController>();
        if (_aimController == null) _aimController = gameObject.AddComponent<TankAimController>();
        _aimController.Bind(barrelPitchPivot, _barrelElevation, minBarrelElevation, maxBarrelElevation);

        _tankAudioController = GetComponent<TankAudioController>();
        if (_tankAudioController == null) _tankAudioController = gameObject.AddComponent<TankAudioController>();

        if (gameplayCamera != null)
        {
            TankOrbitCamera orbitCamera = gameplayCamera.GetComponent<TankOrbitCamera>();
            if (orbitCamera == null) orbitCamera = gameplayCamera.gameObject.AddComponent<TankOrbitCamera>();
            orbitCamera.target = transform;
        }

        maxForwardSpeed = _driveController.maxForwardSpeed;
        maxReverseSpeed = _driveController.maxReverseSpeed;
        maxDriveSlopeAngle = _driveController.maxClimbSlopeDegrees;
        forwardAcceleration = _driveController.GetAccelerationLimit(false, 0f);
        reverseAcceleration = _driveController.GetAccelerationLimit(true, 0f);
        turnAcceleration = Mathf.Min(turnAcceleration, 10f);
        pivotTurnAcceleration = Mathf.Min(pivotTurnAcceleration, 12f);
        trackDriveResponse = Mathf.Min(trackDriveResponse, 2.4f);
        brakeDrag = Mathf.Max(brakeDrag, _driveController.GetBrakingResponse());
    }

    private void ReadDriveInput(out float throttle, out float steer, out bool lowGear)
    {
        if (_driveController != null)
        {
            _driveController.ReadInput(out throttle, out steer, out lowGear);
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

    private float GetForwardSpeedLimit(float slopeAngle, bool lowGear)
    {
        return _driveController != null ? _driveController.GetForwardSpeedLimit(slopeAngle, lowGear) : maxForwardSpeed;
    }

    private float GetReverseSpeedLimit(float slopeAngle, bool lowGear)
    {
        return _driveController != null ? _driveController.GetReverseSpeedLimit(slopeAngle, lowGear) : maxReverseSpeed;
    }

    private float GetMaximumDriveSlope()
    {
        return _driveController != null ? _driveController.maxClimbSlopeDegrees : maxDriveSlopeAngle;
    }

    private void HandleTrackDrive(TrackGroundInfo groundInfo)
    {
        ReadDriveInput(out float throttle, out float steer, out bool lowGear);

        if (!groundInfo.grounded)
        {
            return;
        }

        float slopeAngle = Vector3.Angle(groundInfo.normal, Vector3.up);
        if (slopeAngle > GetMaximumDriveSlope())
        {
            return;
        }

        Vector3 groundNormal = groundInfo.normal;
        Vector3 driveForward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        if (driveForward.sqrMagnitude < 0.0001f)
        {
            driveForward = Vector3.ProjectOnPlane(Vector3.forward, groundNormal);
        }
        driveForward.Normalize();

        if (Mathf.Abs(throttle) > 0.01f)
        {
            Vector3 groundVelocity = Vector3.ProjectOnPlane(ReadVelocity(), groundNormal);
            float currentForwardSpeed = Vector3.Dot(groundVelocity, driveForward);
            float targetSpeed = throttle > 0f
                ? GetForwardSpeedLimit(slopeAngle, lowGear)
                : -GetReverseSpeedLimit(slopeAngle, lowGear);
            float maxAccel = _driveController != null
                ? _driveController.GetAccelerationLimit(throttle < 0f, slopeAngle)
                : (throttle > 0f ? forwardAcceleration : reverseAcceleration);
            float requestedAccel = Mathf.Clamp((targetSpeed - currentForwardSpeed) * trackDriveResponse, -maxAccel, maxAccel);
            _rb.AddForce(driveForward * requestedAccel, ForceMode.Acceleration);
        }

        if (Mathf.Abs(steer) > 0.01f)
        {
            float steeringMultiplier = _driveController != null
                ? _driveController.GetSteeringMultiplier(Mathf.Abs(Vector3.Dot(ReadVelocity(), driveForward)))
                : 1f;
            float torque = (Mathf.Abs(throttle) > 0.01f ? turnAcceleration : pivotTurnAcceleration) * steeringMultiplier;
            _rb.AddTorque(groundNormal * (steer * torque), ForceMode.Acceleration);
        }
    }

    private bool TryGetGroundNormal(out Vector3 groundNormal)
    {
        groundNormal = Vector3.up;
        Vector3 origin = transform.position + Vector3.up * 0.35f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        bool foundGround = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                groundNormal = hits[i].normal;
                foundGround = true;
            }
        }

        return foundGround;
    }

    private TrackGroundInfo ProbeTrackGround()
    {
        TrackGroundInfo info = new TrackGroundInfo
        {
            normal = Vector3.up,
            hits = new TrackGroundHit[5],
            grounded = false,
            hitCount = 0
        };

        Vector3[] localProbePoints =
        {
            new Vector3(-trackHalfWidth, 0f, trackHalfLength),
            new Vector3(trackHalfWidth, 0f, trackHalfLength),
            new Vector3(-trackHalfWidth, 0f, -trackHalfLength),
            new Vector3(trackHalfWidth, 0f, -trackHalfLength),
            Vector3.zero
        };

        Vector3 normalSum = Vector3.zero;
        for (int i = 0; i < localProbePoints.Length; i++)
        {
            Vector3 probeBase = transform.TransformPoint(localProbePoints[i]);
            Vector3 origin = probeBase + transform.up * suspensionProbeHeight;
            if (!TryGetNearestNonSelfHit(origin, -transform.up, suspensionProbeHeight + suspensionProbeDistance, out RaycastHit hit))
            {
                origin = probeBase + Vector3.up * suspensionProbeHeight;
                TryGetNearestNonSelfHit(origin, Vector3.down, suspensionProbeHeight + suspensionProbeDistance, out hit);
            }

            if (hit.collider == null)
            {
                continue;
            }

            float rideDistance = Mathf.Max(0f, hit.distance - suspensionProbeHeight);
            info.hits[info.hitCount] = new TrackGroundHit
            {
                point = hit.point,
                normal = hit.normal,
                distanceFromRideHeight = rideDistance
            };
            info.hitCount++;
            normalSum += hit.normal;
        }

        if (info.hitCount > 0)
        {
            info.grounded = true;
            info.normal = normalSum.normalized;
            if (info.normal.sqrMagnitude < 0.0001f)
            {
                info.normal = Vector3.up;
            }
        }

        return info;
    }

    private bool TryGetNearestNonSelfHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearestHit)
    {
        nearestHit = default;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, groundMask, QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearestHit = hits[i];
                found = true;
            }
        }

        return found;
    }

    private void ApplyTrackedGrip(TrackGroundInfo groundInfo)
    {
        if (!groundInfo.grounded)
        {
            return;
        }

        Vector3 groundNormal = groundInfo.normal;
        Vector3 velocity = ReadVelocity();
        Vector3 groundVelocity = Vector3.ProjectOnPlane(velocity, groundNormal);
        Vector3 groundRight = Vector3.ProjectOnPlane(transform.right, groundNormal);
        Vector3 groundForward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        if (groundRight.sqrMagnitude < 0.0001f)
        {
            return;
        }

        groundRight.Normalize();
        if (groundForward.sqrMagnitude > 0.0001f)
        {
            groundForward.Normalize();
        }

        Vector3 lateralVelocity = groundRight * Vector3.Dot(groundVelocity, groundRight);
        _rb.AddForce(-lateralVelocity * lateralGrip, ForceMode.Acceleration);

        if (groundForward.sqrMagnitude > 0.0001f)
        {
            Vector3 desiredHeadingVelocity = groundForward * Vector3.Dot(groundVelocity, groundForward);
            Vector3 headingError = groundVelocity - desiredHeadingVelocity - lateralVelocity;
            _rb.AddForce(-headingError * headingGrip, ForceMode.Acceleration);
        }

        ReadDriveInput(out float throttle, out _, out _);
        if (Mathf.Abs(throttle) < 0.01f)
        {
            Vector3 forwardVelocity = groundForward * Vector3.Dot(groundVelocity, groundForward);
            _rb.AddForce(-forwardVelocity * brakeDrag, ForceMode.Acceleration);
        }
    }

    private void ApplyTrackSuspension(TrackGroundInfo groundInfo)
    {
        if (!groundInfo.grounded)
        {
            return;
        }

        for (int i = 0; i < groundInfo.hitCount; i++)
        {
            TrackGroundHit hit = groundInfo.hits[i];
            float compression = Mathf.Clamp01((suspensionRideHeight - hit.distanceFromRideHeight) / Mathf.Max(0.01f, suspensionRideHeight));
            float verticalVelocity = Vector3.Dot(_rb.GetPointVelocity(hit.point), hit.normal);
            float force = compression * suspensionSpring - verticalVelocity * suspensionDamper;
            if (force > 0f)
            {
                _rb.AddForceAtPosition(hit.normal * force, hit.point, ForceMode.Acceleration);
            }
        }

        _rb.AddForce(-groundInfo.normal * trackDownforce, ForceMode.Acceleration);
    }

    private void AlignHullToTrackGrade(TrackGroundInfo groundInfo)
    {
        if (!groundInfo.grounded || groundInfo.hitCount < 3)
        {
            return;
        }

        float slopeAngle = Vector3.Angle(groundInfo.normal, Vector3.up);
        if (slopeAngle > maxAssistedSlopeAngle)
        {
            return;
        }

        Vector3 tiltAxis = Vector3.Cross(transform.up, groundInfo.normal);
        if (tiltAxis.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 angularDampingTorque = -Vector3.Project(_rb.angularVelocity, tiltAxis.normalized) * slopeAlignmentDamping;
        _rb.AddTorque(tiltAxis * slopeAlignmentTorque + angularDampingTorque, ForceMode.Acceleration);
    }

    private void ClampGroundSpeed(TrackGroundInfo groundInfo)
    {
        Vector3 velocity = ReadVelocity();
        if (!groundInfo.grounded)
        {
            return;
        }

        Vector3 groundNormal = groundInfo.normal;
        Vector3 groundForward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        if (groundForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        groundForward.Normalize();
        Vector3 groundVelocity = Vector3.ProjectOnPlane(velocity, groundNormal);
        Vector3 verticalVelocity = velocity - groundVelocity;
        float forwardSpeed = Vector3.Dot(groundVelocity, groundForward);
        ReadDriveInput(out _, out _, out bool lowGear);
        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
        float clampedForwardSpeed = Mathf.Clamp(
            forwardSpeed,
            -GetReverseSpeedLimit(slopeAngle, lowGear),
            GetForwardSpeedLimit(slopeAngle, lowGear));
        Vector3 lateralVelocity = groundVelocity - groundForward * forwardSpeed;

        SetVelocity(groundForward * clampedForwardSpeed + lateralVelocity + verticalVelocity);
    }

    private void PreventTerrainPenetration()
    {
        SnapAboveTerrain(terrainSurfaceSkin);
    }

    private void SnapAboveTerrain(float clearance)
    {
        if (TryGetTerrainCorrection(clearance, out Vector3 correctedPosition))
        {
            if (_rb != null)
            {
                _rb.position = correctedPosition;
            }

            transform.position = correctedPosition;

            Vector3 velocity = ReadVelocity();
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
                SetVelocity(velocity);
            }

            Physics.SyncTransforms();
        }
    }

    private bool TryGetTerrainCorrection(float clearance, out Vector3 correctedPosition)
    {
        correctedPosition = transform.position;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        float highestGround = float.MinValue;
        float lowestBottom = float.MaxValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider tankCollider = colliders[i];
            if (tankCollider == null || !tankCollider.enabled || tankCollider.isTrigger)
            {
                continue;
            }

            Bounds bounds = tankCollider.bounds;
            lowestBottom = Mathf.Min(lowestBottom, bounds.min.y);
            SampleTerrainAtXZ(bounds.center.x, bounds.center.z, ref highestGround);
            SampleTerrainAtXZ(bounds.min.x, bounds.min.z, ref highestGround);
            SampleTerrainAtXZ(bounds.min.x, bounds.max.z, ref highestGround);
            SampleTerrainAtXZ(bounds.max.x, bounds.min.z, ref highestGround);
            SampleTerrainAtXZ(bounds.max.x, bounds.max.z, ref highestGround);
        }

        if (highestGround == float.MinValue || lowestBottom == float.MaxValue)
        {
            return false;
        }

        float targetBottom = highestGround + Mathf.Max(0.02f, clearance);
        if (lowestBottom >= targetBottom)
        {
            return false;
        }

        correctedPosition = transform.position + Vector3.up * (targetBottom - lowestBottom);
        return true;
    }

    private void SampleTerrainAtXZ(float x, float z, ref float highestGround)
    {
        if (TrySampleTerrainAtXZ(x, z, out float terrainY))
        {
            highestGround = Mathf.Max(highestGround, terrainY);
        }
    }

    private bool TrySampleTerrainAtXZ(float x, float z, out float terrainY)
    {
        terrainY = 0f;
        ResolveTerrainReferences();

        if (_terrainCollider != null || _terrainRenderer != null)
        {
            Bounds terrainBounds = _terrainCollider != null ? _terrainCollider.bounds : _terrainRenderer.bounds;
            Vector3 origin = new Vector3(x, terrainBounds.max.y + Mathf.Max(terrainProbeHeight, 30f), z);
            float distance = terrainBounds.size.y + Mathf.Max(terrainProbeDistance, 120f) + terrainProbeHeight;

            if (_terrainCollider != null)
            {
                Ray ray = new Ray(origin, Vector3.down);
                if (_terrainCollider.Raycast(ray, out RaycastHit terrainHit, distance))
                {
                    terrainY = terrainHit.point.y;
                    return true;
                }
            }

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit fallbackHit, distance, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (fallbackHit.collider != null && !fallbackHit.collider.transform.IsChildOf(transform))
                {
                    terrainY = fallbackHit.point.y;
                    return true;
                }
            }

            return false;
        }

        Vector3 genericOrigin = new Vector3(x, transform.position.y + Mathf.Max(terrainProbeHeight, 30f), z);
        RaycastHit[] hits = Physics.RaycastAll(genericOrigin, Vector3.down, terrainProbeHeight + terrainProbeDistance, groundMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            terrainY = hits[i].point.y;
            return true;
        }

        return false;
    }

    private void ResolveTerrainReferences()
    {
        if (_terrainSource == null)
        {
            _terrainSource = Object.FindAnyObjectByType<CraterTerrain>();
        }

        if (_terrainSource == null)
        {
            _terrainCollider = null;
            _terrainRenderer = null;
            return;
        }

        if (_terrainCollider == null)
        {
            _terrainCollider = _terrainSource.GetComponent<Collider>();
        }

        if (_terrainRenderer == null)
        {
            _terrainRenderer = _terrainSource.GetComponent<Renderer>();
        }
    }

    private void HandleTurretAndBarrelInput()
    {
        if (_turretController != null && _aimController != null)
        {
            _turretController.TickPlayerInput(Time.deltaTime);
            _aimController.TickPlayerInput(Time.deltaTime);
            _turretYaw = _turretController.DesiredYawDegrees;
            _barrelElevation = _aimController.ElevationDegrees;
            return;
        }

        if (turretYawPivot != null)
        {
            float mouseX = Input.GetAxisRaw("Mouse X");
            _turretYaw += mouseX * mouseYawDegreesPerSecond * Time.deltaTime;

            Quaternion targetLocalYaw = Quaternion.Euler(0f, _turretYaw, 0f);
            turretYawPivot.localRotation = Quaternion.RotateTowards(
                turretYawPivot.localRotation,
                targetLocalYaw,
                turretYawSpeed * Time.deltaTime
            );
        }

        if (barrelPitchPivot != null)
        {
            float pitchInput = 0f;
            pitchInput += Input.GetAxisRaw("Mouse ScrollWheel") * mouseWheelPitchSensitivity;

            if (Input.GetKey(KeyCode.PageUp)) pitchInput += keyboardPitchSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.PageDown)) pitchInput -= keyboardPitchSpeed * Time.deltaTime;

            _barrelElevation = Mathf.Clamp(
                _barrelElevation + pitchInput,
                minBarrelElevation,
                maxBarrelElevation
            );

            barrelPitchPivot.localRotation = Quaternion.Euler(-_barrelElevation, 0f, 0f);
        }
    }

    private void HandleCannonInput()
    {
        bool pressedSpace = Input.GetKeyDown(cannonKey);
        bool pressedMouse = leftClickFiresCannon && Input.GetMouseButtonDown(0);
        if (!pressedSpace && !pressedMouse) return;
        if (Time.time < _nextCannonTime) return;

        if (ProjectileCameraController.ActivePlayerProjectile != null) return;

        FireCannon();
        _nextCannonTime = Time.time + cannonCooldown;
    }

    public void FireCannon()
    {
        if (shellPrefab == null || cannonFirePoint == null)
        {
            Debug.LogWarning("[TankController] Cannot fire cannon. shellPrefab or cannonFirePoint is missing.");
            return;
        }

        if (_audio != null && playerFireSound != null)
        {
            _audio.PlayOneShot(playerFireSound);
        }

        GameObject shell = Instantiate(shellPrefab, cannonFirePoint.position, cannonFirePoint.rotation);
        if (shell.GetComponent<ProjectileAudioController>() == null)
        {
            shell.AddComponent<ProjectileAudioController>();
        }
        GetComponent<TankVisualAnimator>()?.TriggerRecoil();
        IgnoreShellOwnerCollision(shell);

        ProjectileCameraController projectileCamera = shell.GetComponent<ProjectileCameraController>();
        if (projectileCamera != null)
        {
            projectileCamera.trackingBase = transform;
            projectileCamera.launchPowerPercentage = powerPercentage;
        }

        Rigidbody shellBody = shell.GetComponent<Rigidbody>();
        if (shellBody != null)
        {
            shellBody.interpolation = RigidbodyInterpolation.Interpolate;
            shellBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            float launchSpeed = TankBallistics.GetMuzzleSpeed(maxPower, powerPercentage);
            Vector3 launchVelocity = cannonFirePoint.forward * launchSpeed + ReadVelocity();
            SetVelocity(shellBody, launchVelocity);
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

    // Compatibility method used by GameplayTestApi / AI MCP smoke testing.
    public void PrepareForGameplay()
    {
        enabled = true;
        _dead = false;

        if (_health <= 0f)
        {
            _health = maxHealth;
        }
        _damageStateController?.ApplyHealthRatio(maxHealth > 0f ? _health / maxHealth : 1f);

        _nextCannonTime = 0f;

        if (_rb == null) _rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();
        EnsureFocusedControllers();
        AutoWireReferences();
        SnapAboveTerrain(startTerrainClearance);

        if (_rb != null)
        {
            _rb.WakeUp();
        }
    }

    // Compatibility method used by GameplayTestApi.ResetBattle().
    public void ResetForBattle(Vector3 resetPosition, Quaternion resetRotation)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();

        _dead = false;
        _health = maxHealth;
        _damageStateController?.ApplyHealthRatio(1f);
        _nextCannonTime = 0f;

        transform.SetPositionAndRotation(resetPosition, resetRotation);
        Physics.SyncTransforms();
        SnapAboveTerrain(startTerrainClearance);

        if (_rb != null)
        {
            SetVelocity(Vector3.zero);
            _rb.angularVelocity = Vector3.zero;
            _rb.Sleep();
            _rb.WakeUp();
        }

        AutoWireReferences();

        _turretYaw = 0f;
        _barrelElevation = Mathf.Clamp(defaultBattleElevation, minBarrelElevation, maxBarrelElevation);

        if (turretYawPivot != null)
        {
            turretYawPivot.localRotation = Quaternion.identity;
        }

        if (barrelPitchPivot != null)
        {
            barrelPitchPivot.localRotation = Quaternion.Euler(-_barrelElevation, 0f, 0f);
        }

        _turretController?.SetDesiredYaw(_turretYaw);
        _aimController?.SetElevation(_barrelElevation);

        PrepareForGameplay();
    }

    // Compatibility method used by GameplayTestApi.FireCannonAtNearestEnemy().
    public void FireAtPointForTest(Vector3 worldPoint, float testPowerPercentage)
    {
        AutoWireReferences();

        if (cannonFirePoint == null)
        {
            Debug.LogWarning("[TankController] FireAtPointForTest failed because cannonFirePoint is missing.");
            return;
        }

        float oldPower = powerPercentage;
        powerPercentage = Mathf.Clamp(testPowerPercentage, 1f, 100f);
        AimTurretAndBarrelAtPoint(worldPoint);
        FireCannon();
        powerPercentage = oldPower;
    }

    public void AimTurretAndBarrelAtPoint(Vector3 worldPoint)
    {
        AutoWireReferences();

        if (turretYawPivot != null)
        {
            Vector3 flatToTarget = worldPoint - turretYawPivot.position;
            flatToTarget.y = 0f;

            if (flatToTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 localFlatDirection = transform.InverseTransformDirection(flatToTarget.normalized);
                _turretYaw = Mathf.Atan2(localFlatDirection.x, localFlatDirection.z) * Mathf.Rad2Deg;
                turretYawPivot.localRotation = Quaternion.Euler(0f, _turretYaw, 0f);
                _turretController?.SetDesiredYaw(_turretYaw);
            }
        }

        if (barrelPitchPivot != null)
        {
            float muzzleSpeed = TankBallistics.GetMuzzleSpeed(maxPower, powerPercentage);
            if (cannonFirePoint != null
                && TankBallistics.TrySolve(cannonFirePoint.position, worldPoint, muzzleSpeed, Physics.gravity, false, out TankBallistics.Solution solution))
            {
                _barrelElevation = Mathf.Clamp(solution.ElevationDegrees, minBarrelElevation, maxBarrelElevation);
                barrelPitchPivot.localRotation = Quaternion.Euler(-_barrelElevation, 0f, 0f);
                _aimController?.SetElevation(_barrelElevation);
            }
        }
    }

    private void UpdateOptionalUi()
    {
        if (elevationText != null) elevationText.text = "Elev: " + _barrelElevation.ToString("F0") + "°";
        if (angleText != null) angleText.text = "Turret: " + TurretYawDegrees.ToString("F0") + "°";
        if (powerText != null) powerText.text = "Power: " + powerPercentage.ToString("F0") + "%";
    }

    public void ApplyBulletDamage(float damage, Vector3 worldPoint, Vector3 worldNormal)
    {
        ApplyDamage(Mathf.Max(0.05f, damage), worldPoint, worldNormal, false);
    }

    public void ApplyProjectileDamage(float impactForce, Vector3 worldPoint, Vector3 worldNormal)
    {
        float damage = Mathf.Max(projectileDirectDamage, impactForce);
        ApplyDamage(damage, worldPoint, worldNormal, true);
    }

    public void ApplyExplosionDamage(float explosionForce, Vector3 explosionPoint, Vector3 worldNormal, float distanceFactor = 1f)
    {
        float damage = Mathf.Max(projectileBlastDamage * Mathf.Clamp01(distanceFactor), explosionForce * Mathf.Clamp01(distanceFactor));
        ApplyDamage(damage, explosionPoint, worldNormal, true);
    }

    public void ApplyCollisionDamage(float impactForce, Vector3 worldPoint, Vector3 worldNormal)
    {
        ApplyDamage(Mathf.Max(tankCollisionDamage, impactForce), worldPoint, worldNormal, false);
    }

    private void ApplyDamage(float amount, Vector3 worldPoint, Vector3 worldNormal, bool fatalImpactAllowed)
    {
        if (_dead) return;

        _health -= Mathf.Max(0.01f, amount);

        if (fatalImpactAllowed && amount >= maxHealth * fatalImpactThreshold)
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
        if (_dead) return;

        _dead = true;
        _damageStateController?.ApplyHealthRatio(0f);
        Debug.Log("[TankController] Player tank destroyed.");

        if (_rb != null)
        {
            _rb.AddExplosionForce(Mathf.Max(200f, force * 8f), worldPoint, 8f, 1f, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_dead || collision == null || collision.contactCount == 0) return;

        EnemyTankAI enemy = collision.collider.GetComponentInParent<EnemyTankAI>();
        if (enemy == null) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact > 4f)
        {
            ContactPoint contact = collision.GetContact(0);
            ApplyCollisionDamage(impact * 4f, contact.point, contact.normal);
        }
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }

    private Vector3 ReadVelocity()
    {
        if (_rb == null) return Vector3.zero;
#if UNITY_6000_0_OR_NEWER
        return _rb.linearVelocity;
#else
        return _rb.velocity;
#endif
    }

    private void SetVelocity(Vector3 value)
    {
        if (_rb == null) return;
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = value;
#else
        _rb.velocity = value;
#endif
    }

    private static void SetVelocity(Rigidbody body, Vector3 value)
    {
        if (body == null) return;
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = value;
#else
        body.velocity = value;
#endif
    }
}
