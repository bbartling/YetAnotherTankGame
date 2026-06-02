using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(BoxCollider))]
public class TankController : MonoBehaviour
{
    [Header("Tank Parts")]
    public Transform turret;
    public Transform barrel;
    public Transform firePoint;
    public GameObject shellPrefab;
    public LineRenderer lineRenderer;

    [Header("Movement Settings")]
    public float moveSpeed = 100f;
    public float turnSpeed = 500f;

    [Header("Track Drive")]
    [Tooltip("Offset applied to the rigidbody centre of mass for stability")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0f);
    [Tooltip("Force applied to each track when moving")]
    public float trackDriveForce = 36f;
    [Tooltip("How much A/D biases the left and right tracks")]
    public float trackTurnInputScale = 1.05f;
    [Tooltip("Maximum horizontal speed for the hull")]
    public float trackMaxVelocity = 8.5f;
    [Tooltip("How long a turn tap remains active while moving")]
    public float trackTurnTapDuration = 0.18f;
    [Tooltip("How strongly one track slows or reverses during a tap")]
    public float trackTurnTapStrength = 1.15f;
    [Tooltip("How strongly the hull tries to sit upright on reasonable slopes")]
    public float slopeStabilization = 12f;
    [Tooltip("Maximum slope angle the tank can reliably climb")]
    public float maxClimbSlope = 36f;
    [Tooltip("Slope angle where forward motion begins to stall")]
    public float stallSlope = 48f;
    [Tooltip("Side slope angle where rollovers become fatal")]
    public float rolloverKillAngle = 63f;
    [Tooltip("How long the tank must remain overturned before dying")]
    public float rolloverGraceSeconds = 0.35f;
    [Tooltip("Layer mask used when probing ground under the tank")]
    public LayerMask groundMask = ~0;
    [Tooltip("Height above the tank used for the ground probe ray")]
    public float groundProbeHeight = 2.5f;

    [Header("Turret Settings")]
    public float turretTurnSpeed = 40f;
    public float barrelElevationSpeed = 30f;
    public float minElevation = -10f;
    public float maxElevation = 60f;
    public float mouseTurretSensitivity = 1.2f;
    public float mouseBarrelSensitivity = 2.0f;
    public float mouseWheelBarrelSensitivity = 0.225f;
    public float keyboardBarrelSensitivity = 24f;
    public float elevationSoundRepeatDelay = 0.16f;

    [Header("Camera View")]
    public Camera gameplayCamera;
    public KeyCode viewToggleKey = KeyCode.Escape;
    public Vector3 firstPersonCameraLocalPosition = new Vector3(0f, 1.55f, 0.35f);
    public Vector3 firstPersonCameraLocalEuler = new Vector3(8f, 0f, 0f);
    public float firstPersonCameraFov = 62f;
    [Tooltip("Temporary right-click sniper view positioned well in front of the turret")]
    public Vector3 sniperCameraLocalPosition = new Vector3(0f, 4f, 24f);
    public Vector3 sniperCameraLocalEuler = new Vector3(8f, 0f, 0f);
    public float sniperCameraFov = 15f;
    public Vector3 overviewCameraLocalPosition = new Vector3(0f, 4f, 24f);
    public Vector3 overviewCameraLocalEuler = new Vector3(8f, 0f, 0f);
    public float overviewCameraFov = 52f;

    [Header("Firing Settings")]
    public float maxPower = 75f;
    public float powerPercentage = 50f;
    public float mass = 1f;
    public AudioClip playerFireSound;
    public AudioClip treeSmashSound;

    [Header("Trajectory Settings")]
    public int trajectoryPointCount = 40;
    public float trajectoryTimeStep = 0.15f;

    [Header("UI (Optional/Legacy)")]
    public TextMeshProUGUI elevationText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI powerText;

    [Header("Audio Sources")]
    public AudioSource engineAudioSource;
    public AudioSource turretAudioSource;
    public AudioSource elevationAudioSource;

    [Header("Audio Clips")]
    public AudioClip engineRunningClip;
    public AudioClip engineStartClip;
    public AudioClip engineStopClip;
    public AudioClip rotateTurretClip;
    public AudioClip adjustElevationClip;

    [Header("Audio Settings")]
    public float engineStopDelay = 1.0f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float projectileDirectDamage = 55f;
    public float projectileBlastDamage = 42f;
    public float tankCollisionDamage = 24f;
    public float enemyTankCollisionKillSpeed = 9f;
    public float selfBlastDamageMultiplier = 1.35f;
    public float fatalImpactThreshold = 0.55f;

    [Header("Death UI")]
    public Canvas hudCanvas;
    public Vector2 healthBarAnchor = new Vector2(20f, 20f);
    public Vector2 healthBarSize = new Vector2(280f, 22f);
    public float deathRestartDelay = 2.2f;
    public float deathFlashFrequency = 7.5f;

    [Header("Wind UI")]
    public BattlefieldWind windSource;
    public Vector2 windWidgetAnchor = new Vector2(20f, 58f);
    public Vector2 windWidgetSize = new Vector2(280f, 60f);
    public float windProjectileDrift = 0.12f;

    [Header("Spawn")]
    public string spawnGroundName = "Ground";
    public Vector3 spawnOffset = new Vector3(0f, 0.35f, 0f);
    public float spawnProbeHeight = 25f;

    private Rigidbody _rb;
    private AudioSource _audio;
    private float _currentElevation;
    private float _currentRotation;
    private Transform _turretYawPivot;
    private float _health;
    private float _toppleTimer;

    private bool _isEngineStarting;
    private bool _isEngineRunning;
    private bool _isOverviewView;
    private bool _isSniperViewActive;
    private bool _isDead;
    private bool _restartQueued;
    private bool _engineMutedForProjectileView;
    private bool _isElevationAtLimit;
    private float _stopTimer;
    private bool _wasDriveInput;
    private bool _wasRotatingInput;
    private bool _wasElevatingInput;
    private float _nextElevationSoundTime;
    private float _nextTreeSmashTime;
    private float _trackTurnTapTimer;
    private float _trackTurnTapDirection;
    private bool _spawnPlacementComplete;

    private RectTransform _hudRoot;
    private Image _healthBarFill;
    private TextMeshProUGUI _healthLabel;
    private RectTransform _windRoot;
    private TextMeshProUGUI _windLabel;
    private TextMeshProUGUI _windValueLabel;
    private CanvasGroup _deathGroup;
    private TextMeshProUGUI _deathLabel;
    private TextMeshProUGUI _deathSubtitle;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();
        _health = maxHealth;

        if (_rb != null)
        {
            _rb.centerOfMass += centerOfMassOffset;
            _rb.linearDamping = 0.78f;
            _rb.angularDamping = 2.2f;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.maxAngularVelocity = 10f;
        }

        EnsureAudioSources();
        EnsureTurretYawPivot();
        EnsureGameplayCamera();
        if (windSource == null)
        {
            windSource = Object.FindFirstObjectByType<BattlefieldWind>();
        }
        EnsureHUD();
        ApplyCameraView();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = trajectoryPointCount;
            lineRenderer.enabled = false;
        }

        if (_rb != null)
        {
            _rb.isKinematic = true;
        }
    }

    private void Start()
    {
        if (!_spawnPlacementComplete && enabled)
        {
            StartCoroutine(InitializeSpawnPlacement());
        }
    }

    public void PrepareForGameplay()
    {
        if (_spawnPlacementComplete)
        {
            SetTankPhysicsActive(true);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(InitializeSpawnPlacement());
    }

    private IEnumerator InitializeSpawnPlacement()
    {
        SetTankPhysicsActive(false);
        yield return null;
        yield return new WaitForFixedUpdate();

        PlaceOnGround();
        Physics.SyncTransforms();
        SetTankPhysicsActive(true);

        _spawnPlacementComplete = true;
        UpdateHUD();
    }

    private void SetTankPhysicsActive(bool active)
    {
        if (_rb == null)
        {
            return;
        }

        if (active)
        {
            _rb.isKinematic = false;
            SetTankVelocity(Vector3.zero);
            _rb.angularVelocity = Vector3.zero;
        }
        else
        {
            _rb.isKinematic = true;
        }
    }

    private void Update()
    {
        if (!_spawnPlacementComplete)
        {
            return;
        }

        if (_isDead)
        {
            UpdateDeathState();
            return;
        }

        HandleViewToggle();
        HandleInput();
        HandleSniperViewOverride();
        SyncEngineAudioForProjectileView();
        HandleAudio();
        UpdateTrajectory();
        UpdateHUD();
    }

    private void FixedUpdate()
    {
        if (!_spawnPlacementComplete)
        {
            return;
        }

        if (_isDead)
        {
            return;
        }

        HandleMovement();
        CheckRollover();
    }

    private void HandleInput()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        float keyboardElevation = 0f;
        if (Input.GetKey(KeyCode.PageUp)) keyboardElevation += 1f;
        if (Input.GetKey(KeyCode.PageDown)) keyboardElevation -= 1f;

        _currentRotation += mouseX * mouseTurretSensitivity * turretTurnSpeed * Time.deltaTime;
        if (_turretYawPivot != null)
        {
            _turretYawPivot.localRotation = Quaternion.Euler(0f, _currentRotation, 0f);
        }

        float previousElevation = _currentElevation;
        _currentElevation += mouseWheel * mouseWheelBarrelSensitivity * barrelElevationSpeed;
        _currentElevation += keyboardElevation * keyboardBarrelSensitivity * Time.deltaTime;
        _currentElevation = Mathf.Clamp(_currentElevation, minElevation, maxElevation);
        _isElevationAtLimit = (Mathf.Abs(mouseWheel) > 0.01f || Mathf.Abs(keyboardElevation) > 0.01f) && Mathf.Approximately(_currentElevation, previousElevation);

        if (barrel != null)
        {
            barrel.localRotation = Quaternion.Euler(-_currentElevation, 0f, 0f);
        }

        if (Input.GetKey(KeyCode.E)) powerPercentage += 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) powerPercentage -= 20f * Time.deltaTime;
        powerPercentage = Mathf.Clamp(powerPercentage, 0f, 100f);

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && ProjectileCameraController.ActivePlayerProjectile == null)
        {
            Fire();
        }

        if (elevationText != null) elevationText.text = "Elev: " + _currentElevation.ToString("F0") + "°";
        if (angleText != null) angleText.text = "Angle: " + _currentRotation.ToString("F0") + "°";
        if (powerText != null) powerText.text = "Power: " + powerPercentage.ToString("F0") + "%";
    }

    public float TurretYawDegrees
    {
        get { return Mathf.Repeat(_currentRotation, 360f); }
    }

    public Rigidbody RigidbodyComponent
    {
        get { return _rb; }
    }

    public Vector3 CurrentVelocity
    {
        get { return GetTankVelocity(); }
    }

    private void HandleViewToggle()
    {
        if (Input.GetKeyDown(viewToggleKey))
        {
            _isOverviewView = !_isOverviewView;
            ApplyCameraView();
        }
    }

    private void HandleSniperViewOverride()
    {
        bool sniperHeld = Input.GetMouseButton(1);
        if (sniperHeld)
        {
            ApplySniperCameraView();
        }
        else if (_isSniperViewActive)
        {
            ApplyCameraView();
        }

        _isSniperViewActive = sniperHeld;
    }

    private void EnsureGameplayCamera()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = GetComponentInChildren<Camera>(true);
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
        {
            return;
        }

        SniperZoom sniperZoom = gameplayCamera.GetComponent<SniperZoom>();
        if (sniperZoom != null)
        {
            sniperZoom.enabled = false;
        }

        Transform parent = GetCameraParent();
        if (parent != null && gameplayCamera.transform.parent != parent)
        {
            gameplayCamera.transform.SetParent(parent, false);
        }
    }

    private Transform GetCameraParent()
    {
        if (turret != null)
        {
            return turret;
        }

        return transform;
    }

    private void ApplyCameraView()
    {
        if (gameplayCamera == null)
        {
            return;
        }

        Transform parent = GetCameraParent();
        if (parent != null && gameplayCamera.transform.parent != parent)
        {
            gameplayCamera.transform.SetParent(parent, false);
        }

        if (_isOverviewView)
        {
            gameplayCamera.transform.localPosition = overviewCameraLocalPosition;
            gameplayCamera.transform.localRotation = Quaternion.Euler(overviewCameraLocalEuler);
            gameplayCamera.fieldOfView = overviewCameraFov;
        }
        else
        {
            gameplayCamera.transform.localPosition = firstPersonCameraLocalPosition;
            gameplayCamera.transform.localRotation = Quaternion.Euler(firstPersonCameraLocalEuler);
            gameplayCamera.fieldOfView = firstPersonCameraFov;
        }
    }

    private void ApplySniperCameraView()
    {
        if (gameplayCamera == null)
        {
            return;
        }

        Transform parent = GetCameraParent();
        if (parent != null && gameplayCamera.transform.parent != parent)
        {
            gameplayCamera.transform.SetParent(parent, false);
        }

        gameplayCamera.transform.localPosition = sniperCameraLocalPosition;
        gameplayCamera.transform.localRotation = Quaternion.Euler(sniperCameraLocalEuler);
        gameplayCamera.fieldOfView = sniperCameraFov;
    }

    private void HandleAudio()
    {
        bool driveInput = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                          Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        if (driveInput && !_wasDriveInput)
        {
            _stopTimer = engineStopDelay;
            StartEngine();
        }
        else if (!driveInput && _wasDriveInput)
        {
            StopEngine();
        }
        else if (driveInput)
        {
            _stopTimer = engineStopDelay;
        }
        else if (_isEngineStarting || _isEngineRunning)
        {
            _stopTimer -= Time.deltaTime;
            if (_stopTimer <= 0f)
            {
                StopEngine();
            }
        }

        bool isRotating = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f;
        if (isRotating && !_wasRotatingInput)
        {
            PlayLoopAudio(turretAudioSource, rotateTurretClip);
        }
        else if (!isRotating && _wasRotatingInput)
        {
            StopLoopAudio(turretAudioSource, rotateTurretClip);
        }

        bool isElevating = Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f || Input.GetKey(KeyCode.PageUp) || Input.GetKey(KeyCode.PageDown);
        if (_isElevationAtLimit)
        {
            if (elevationAudioSource != null && elevationAudioSource.isPlaying)
            {
                elevationAudioSource.Stop();
            }
        }
        else if (isElevating && Time.time >= _nextElevationSoundTime && adjustElevationClip != null && elevationAudioSource != null)
        {
            if (elevationAudioSource.isPlaying)
            {
                elevationAudioSource.Stop();
            }

            elevationAudioSource.PlayOneShot(adjustElevationClip);
            _nextElevationSoundTime = Time.time + elevationSoundRepeatDelay;
        }

        _wasDriveInput = driveInput;
        _wasRotatingInput = isRotating;
        _wasElevatingInput = isElevating;
    }

    private void StartEngine()
    {
        CancelInvoke(nameof(TransitionToRunning));
        _isEngineStarting = true;
        _isEngineRunning = false;

        if (engineAudioSource != null)
        {
            engineAudioSource.Stop();
        }

        if (engineAudioSource != null && engineStartClip != null)
        {
            engineAudioSource.clip = engineStartClip;
            engineAudioSource.loop = false;
            engineAudioSource.Play();
            Invoke(nameof(TransitionToRunning), engineStartClip.length);
        }
        else
        {
            TransitionToRunning();
        }
    }

    private void TransitionToRunning()
    {
        if (!_isEngineStarting)
        {
            return;
        }

        _isEngineStarting = false;
        _isEngineRunning = true;

        if (engineAudioSource != null && engineRunningClip != null)
        {
            engineAudioSource.clip = engineRunningClip;
            engineAudioSource.loop = true;
            engineAudioSource.Play();
        }
    }

    private void StopEngine()
    {
        CancelInvoke(nameof(TransitionToRunning));
        bool hadEngineAudio = _isEngineStarting || _isEngineRunning;
        _isEngineStarting = false;
        _isEngineRunning = false;

        if (engineAudioSource != null)
        {
            engineAudioSource.Stop();
            engineAudioSource.loop = false;
            if (hadEngineAudio && engineStopClip != null)
            {
                engineAudioSource.clip = engineStopClip;
                engineAudioSource.Play();
            }
        }
    }

    private void SyncEngineAudioForProjectileView()
    {
        bool projectileViewActive = ProjectileCameraController.ActivePlayerProjectile != null;
        if (engineAudioSource == null)
        {
            return;
        }

        if (projectileViewActive && !_engineMutedForProjectileView)
        {
            engineAudioSource.mute = true;
            _engineMutedForProjectileView = true;
        }
        else if (!projectileViewActive && _engineMutedForProjectileView)
        {
            engineAudioSource.mute = false;
            _engineMutedForProjectileView = false;
        }
    }

    private void PlayLoopAudio(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
        {
            return;
        }

        if (!source.isPlaying || source.clip != clip)
        {
            source.clip = clip;
            source.loop = true;
            source.Play();
        }
    }

    private void StopLoopAudio(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
        {
            return;
        }

        if (source.isPlaying && source.clip == clip)
        {
            source.Stop();
        }
    }

    private void EnsureAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (engineAudioSource == null && sources.Length > 0)
        {
            engineAudioSource = sources[0];
        }

        if (turretAudioSource == null || turretAudioSource == engineAudioSource)
        {
            turretAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (elevationAudioSource == null || elevationAudioSource == engineAudioSource || elevationAudioSource == turretAudioSource)
        {
            elevationAudioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureAudioSource(engineAudioSource, true);
        ConfigureAudioSource(turretAudioSource, false);
        ConfigureAudioSource(elevationAudioSource, false);
    }

    private void EnsureTurretYawPivot()
    {
        if (turret == null)
        {
            return;
        }

        if (_turretYawPivot != null)
        {
            return;
        }

        if (turret.parent != null && turret.parent.name == "TurretYawPivot")
        {
            _turretYawPivot = turret.parent;
            return;
        }

        Transform parent = turret.parent;
        if (parent == null)
        {
            _turretYawPivot = turret;
            return;
        }

        GameObject pivotGo = new GameObject("TurretYawPivot");
        Transform pivot = pivotGo.transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = turret.localPosition;
        pivot.localRotation = Quaternion.identity;

        Vector3 parentScale = parent.lossyScale;
        pivot.localScale = new Vector3(
            parentScale.x != 0f ? 1f / parentScale.x : 1f,
            parentScale.y != 0f ? 1f / parentScale.y : 1f,
            parentScale.z != 0f ? 1f / parentScale.z : 1f
        );

        turret.SetParent(pivot, true);
        _turretYawPivot = pivot;
    }

    private void ConfigureAudioSource(AudioSource source, bool loop)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = 0.8f;
        source.Stop();
    }

    private void PlaceOnGround()
    {
        if (_rb == null)
        {
            return;
        }

        Collider bodyCollider = GetComponent<Collider>();
        float lift = bodyCollider != null ? Mathf.Max(0.25f, bodyCollider.bounds.extents.y + 0.08f) : 0.5f;

        GameObject groundObject = GameObject.Find(spawnGroundName);
        if (groundObject == null)
        {
            Debug.LogWarning($"[TankController] Could not find spawn ground '{spawnGroundName}'.");
            return;
        }

        Collider groundCollider = groundObject.GetComponent<Collider>();
        Renderer groundRenderer = groundObject.GetComponent<Renderer>();
        if (groundCollider == null && groundRenderer == null)
        {
            Debug.LogWarning($"[TankController] Spawn ground '{spawnGroundName}' has no Collider or Renderer.");
            return;
        }

        Bounds spawnBounds = groundCollider != null ? groundCollider.bounds : groundRenderer.bounds;
        Vector3 target = transform.position + spawnOffset;
        target.x = Mathf.Clamp(target.x, spawnBounds.min.x + 1f, spawnBounds.max.x - 1f);
        target.z = Mathf.Clamp(target.z, spawnBounds.min.z + 1f, spawnBounds.max.z - 1f);

        float rayHeight = Mathf.Max(spawnProbeHeight, spawnBounds.size.y + 20f);
        Vector3 origin = new Vector3(target.x, spawnBounds.max.y + rayHeight, target.z);
        float rayDistance = rayHeight + spawnBounds.size.y + 80f;
        Vector3 groundedPosition = new Vector3(target.x, transform.position.y, target.z);

        bool foundGround = false;
        if (groundCollider != null)
        {
            Ray ray = new Ray(origin, Vector3.down);
            if (groundCollider.Raycast(ray, out RaycastHit colliderHit, rayDistance))
            {
                groundedPosition.y = colliderHit.point.y + lift;
                foundGround = true;
            }
        }

        if (!foundGround && Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != bodyCollider && (groundCollider == null || hit.collider == groundCollider || hit.collider.transform.IsChildOf(groundObject.transform)))
            {
                groundedPosition.y = hit.point.y + lift;
                foundGround = true;
            }
        }

        if (!foundGround)
        {
            groundedPosition.y = spawnBounds.max.y + lift;
            Debug.LogWarning($"[TankController] Ground ray missed at {target}. Using terrain bounds fallback.");
        }

        transform.position = groundedPosition;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        _rb.position = groundedPosition;
        _rb.rotation = transform.rotation;
        SetTankVelocity(Vector3.zero);
        if (!_rb.isKinematic)
        {
            _rb.angularVelocity = Vector3.zero;
        }
    }

    private void HandleMovement()
    {
        if (_rb == null || _rb.isKinematic)
        {
            return;
        }

        float driveInput = 0f;
        if (Input.GetKey(KeyCode.W)) driveInput += 1f;
        if (Input.GetKey(KeyCode.S)) driveInput -= 1f;

        float turnInput = 0f;
        if (Input.GetKey(KeyCode.D)) turnInput += 1f;
        if (Input.GetKey(KeyCode.A)) turnInput -= 1f;

        if (Mathf.Abs(driveInput) < 0.001f && Mathf.Abs(turnInput) < 0.001f)
        {
            return;
        }

        Vector3 groundNormal = Vector3.up;
        float slopeDriveScale = 1f;
        if (TryGetGroundHit(out RaycastHit groundHit))
        {
            groundNormal = groundHit.normal.normalized;
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            slopeDriveScale = Mathf.Clamp01(1f - Mathf.Max(0f, slopeAngle - maxClimbSlope) / Mathf.Max(1f, stallSlope - maxClimbSlope));
            ApplySlopeStabilization(groundNormal, slopeAngle, 0f);
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }
        forward.Normalize();

        float leftTrack = Mathf.Clamp(driveInput + turnInput * trackTurnInputScale, -1f, 1f);
        float rightTrack = Mathf.Clamp(driveInput - turnInput * trackTurnInputScale, -1f, 1f);
        float averageTrack = (leftTrack + rightTrack) * 0.5f;
        float differentialTrack = (leftTrack - rightTrack) * 0.5f;

        Vector3 velocity = GetTankVelocity();
        Vector3 verticalVelocity = Vector3.Project(velocity, Vector3.up);
        Vector3 desiredPlanarVelocity = forward * (averageTrack * trackMaxVelocity * slopeDriveScale);
        Vector3 currentPlanarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        Vector3 planarVelocity = Vector3.MoveTowards(currentPlanarVelocity, desiredPlanarVelocity, trackDriveForce * Time.fixedDeltaTime);
        SetTankVelocity(planarVelocity + verticalVelocity);

        if (Mathf.Abs(differentialTrack) > 0.001f)
        {
            float yawDegrees = differentialTrack * turnSpeed * Time.fixedDeltaTime;
            Quaternion yaw = Quaternion.AngleAxis(yawDegrees, groundNormal);
            _rb.MoveRotation(yaw * _rb.rotation);
        }

        ClampHorizontalVelocity();
    }

    private void DriveOnFlatGround(float moveInput, float turnInput)
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float leftTrack = Mathf.Clamp(moveInput + turnInput * trackTurnInputScale, -1f, 1f);
        float rightTrack = Mathf.Clamp(moveInput - turnInput * trackTurnInputScale, -1f, 1f);
        float averageTrack = (leftTrack + rightTrack) * 0.5f;
        float differentialTrack = (leftTrack - rightTrack) * 0.5f;

        Vector3 velocity = GetTankVelocity();
        Vector3 desiredPlanarVelocity = forward * (averageTrack * trackMaxVelocity);
        Vector3 planarVelocity = Vector3.MoveTowards(new Vector3(velocity.x, 0f, velocity.z), desiredPlanarVelocity, trackDriveForce * Time.fixedDeltaTime);
        SetTankVelocity(new Vector3(planarVelocity.x, velocity.y, planarVelocity.z));

        if (Mathf.Abs(differentialTrack) > 0.001f)
        {
            _rb.MoveRotation(Quaternion.AngleAxis(differentialTrack * turnSpeed * Time.fixedDeltaTime, Vector3.up) * _rb.rotation);
        }

        ClampHorizontalVelocity();
    }

    private float GetTrackOffset()
    {
        Collider bodyCollider = GetComponent<Collider>();
        return bodyCollider != null ? Mathf.Max(0.45f, bodyCollider.bounds.extents.x * 0.65f) : 1.2f;
    }

    private void ApplySlopeStabilization(Vector3 groundNormal, float slopeAngle, float sideSlope)
    {
        if (slopeAngle > rolloverKillAngle * 0.78f)
        {
            return;
        }

        float stability = Mathf.Clamp01(1f - slopeAngle / Mathf.Max(1f, maxClimbSlope));
        stability *= Mathf.Clamp01(1f - sideSlope * 0.65f);

        Vector3 axis = Vector3.Cross(transform.up, groundNormal);
        if (axis.sqrMagnitude > 0.0001f)
        {
            _rb.AddTorque(axis.normalized * (slopeStabilization * stability), ForceMode.Force);
        }
    }

    private bool TryGetGroundHit(out RaycastHit hit)
    {
        Vector3 origin = _rb.worldCenterOfMass + Vector3.up * groundProbeHeight;
        return Physics.Raycast(origin, Vector3.down, out hit, groundProbeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void ClampHorizontalVelocity()
    {
        if (_rb == null)
        {
            return;
        }

        Vector3 velocity = GetTankVelocity();
        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);
        if (flatVelocity.magnitude <= trackMaxVelocity)
        {
            return;
        }

        Vector3 clampedFlat = flatVelocity.normalized * trackMaxVelocity;
        SetTankVelocity(new Vector3(clampedFlat.x, velocity.y, clampedFlat.z));
    }

    private Vector3 GetTankVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return _rb != null ? _rb.linearVelocity : Vector3.zero;
#else
        return _rb != null ? _rb.velocity : Vector3.zero;
#endif
    }

    private void SetTankVelocity(Vector3 velocity)
    {
        if (_rb == null || _rb.isKinematic)
        {
            return;
        }

#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = velocity;
#else
        _rb.velocity = velocity;
#endif
    }

    private void UpdateTrajectory()
    {
        if (lineRenderer == null || firePoint == null)
        {
            return;
        }

        bool isSniperMode = Input.GetMouseButton(1) && !_isDead;
        lineRenderer.enabled = isSniperMode;

        if (!isSniperMode)
        {
            return;
        }

        Vector3 startPos = firePoint.position;
        float muzzleSpeed = Mathf.Lerp(maxPower * 0.45f, maxPower, powerPercentage / 100f);
        Vector3 startVelocity = firePoint.forward * muzzleSpeed + GetTankVelocity();

        for (int i = 0; i < trajectoryPointCount; i++)
        {
            float t = i * trajectoryTimeStep;
            Vector3 point = startPos + startVelocity * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }

    private void CheckRollover()
    {
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        if (tiltAngle >= rolloverKillAngle)
        {
            _toppleTimer += Time.fixedDeltaTime;
            if (_toppleTimer >= rolloverGraceSeconds)
            {
                Die("Rolled over");
            }
        }
        else
        {
            _toppleTimer = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isDead || collision == null || collision.contactCount == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        float impactSpeed = collision.relativeVelocity.magnitude;

        ProjectileCameraController projectile = collision.collider.GetComponentInParent<ProjectileCameraController>();
        if (projectile != null)
        {
            ApplyProjectileDamage(Mathf.Max(projectileDirectDamage, impactSpeed * projectileDirectDamage * 0.8f), contact.point, contact.normal);
            return;
        }

        TankController otherTank = collision.collider.GetComponentInParent<TankController>();
        if (otherTank != null && otherTank != this)
        {
            float damage = Mathf.Max(tankCollisionDamage, impactSpeed * tankCollisionDamage);
            ApplyCollisionDamage(damage, contact.point, contact.normal);
            if (impactSpeed >= enemyTankCollisionKillSpeed)
            {
                Die("Crushed by tank");
            }

            return;
        }

        EnemyTankAI enemyTank = collision.collider.GetComponentInParent<EnemyTankAI>();
        if (enemyTank != null)
        {
            float damage = Mathf.Max(tankCollisionDamage, impactSpeed * tankCollisionDamage);
            ApplyCollisionDamage(damage, contact.point, contact.normal);
            if (impactSpeed >= enemyTankCollisionKillSpeed)
            {
                Die("Crushed by enemy tank");
            }
        }

        if (collision.collider.CompareTag("Tree") && treeSmashSound != null && Time.time >= _nextTreeSmashTime && _audio != null)
        {
            _nextTreeSmashTime = Time.time + 0.22f;
            _audio.PlayOneShot(treeSmashSound);
        }
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

        float damage = Mathf.Max(projectileBlastDamage * 0.5f, explosionForce * selfBlastDamageMultiplier * Mathf.Clamp01(distanceFactor));
        ApplyDamage(damage, explosionPoint, worldNormal, true);
    }

    public void ApplyCollisionDamage(float impactForce, Vector3 worldPoint, Vector3 worldNormal)
    {
        if (_isDead)
        {
            return;
        }

        float damage = Mathf.Max(tankCollisionDamage, impactForce);
        ApplyDamage(damage, worldPoint, worldNormal, false);
    }

    private void ApplyDamage(float amount, Vector3 worldPoint, Vector3 worldNormal, bool canTriggerFatalImpact)
    {
        if (_isDead)
        {
            return;
        }

        _health -= Mathf.Max(0.1f, amount);
        UpdateHUD();

        if (canTriggerFatalImpact && amount >= maxHealth * fatalImpactThreshold)
        {
            Die("Fatal hit");
            return;
        }

        if (_health <= 0f)
        {
            Die("Destroyed");
        }
    }

    private void Die(string reason)
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        _restartQueued = false;
        _toppleTimer = 0f;
        _health = 0f;

        StopEngine();
        StopLoopAudio(turretAudioSource, rotateTurretClip);
        if (elevationAudioSource != null)
        {
            elevationAudioSource.Stop();
        }

        SetTankVelocity(Vector3.zero);
        if (_rb != null)
        {
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        DisableTankColliders();
        SpawnDeathBurst();
        ShowDeathUI(reason);
        UpdateHUD();
    }

    private void DisableTankColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private void SpawnDeathBurst()
    {
        GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.name = "TankDeathBurst";
        burst.transform.position = transform.position + Vector3.up * 1.2f;
        burst.transform.localScale = Vector3.one * 2.4f;

        Collider collider = burst.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = burst.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(1f, 0.35f, 0.08f, 0.85f);
            renderer.material = material;
        }

        Destroy(burst, 0.35f);

        Collider[] nearby = Physics.OverlapSphere(transform.position, 6f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < nearby.Length; i++)
        {
            Rigidbody body = nearby[i].attachedRigidbody;
            if (body != null && body != _rb)
            {
                body.AddExplosionForce(180f, transform.position, 6f, 0.6f, ForceMode.Impulse);
            }
        }
    }

    private void ShowDeathUI(string reason)
    {
        if (_deathGroup == null)
        {
            return;
        }

        _deathGroup.gameObject.SetActive(true);
        _deathGroup.alpha = 1f;
        if (_deathLabel != null)
        {
            _deathLabel.text = "YOU DIED";
            _deathLabel.color = new Color(1f, 0.08f, 0.08f, 1f);
        }

        if (_deathSubtitle != null)
        {
            _deathSubtitle.text = reason;
            _deathSubtitle.color = new Color(1f, 0.65f, 0.65f, 0.95f);
        }
    }

    private void UpdateDeathState()
    {
        if (_deathGroup != null)
        {
            _deathGroup.gameObject.SetActive(true);
            _deathGroup.alpha = 1f;
        }

        if (_deathLabel != null)
        {
            float flash = Mathf.PingPong(Time.time * deathFlashFrequency, 1f);
            _deathLabel.alpha = Mathf.Lerp(0.25f, 1f, flash);
            _deathLabel.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.08f, flash);
        }

        if (_deathSubtitle != null)
        {
            _deathSubtitle.alpha = Mathf.Lerp(0.2f, 0.9f, Mathf.PingPong(Time.time * (deathFlashFrequency * 0.55f), 1f));
        }

        if (!_restartQueued && Time.timeSinceLevelLoad >= deathRestartDelay)
        {
            _restartQueued = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void EnsureHUD()
    {
        if (hudCanvas == null)
        {
            GameObject canvasGo = GameObject.Find("TankHUDCanvas");
            if (canvasGo != null)
            {
                hudCanvas = canvasGo.GetComponent<Canvas>();
            }
        }

        if (hudCanvas == null)
        {
            GameObject canvasGo = new GameObject("TankHUDCanvas");
            hudCanvas = canvasGo.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        BuildHealthBar();
        BuildWindWidget();
        BuildDeathOverlay();
    }

    private void BuildHealthBar()
    {
        GameObject root = new GameObject("TankHealthBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(hudCanvas.transform, false);

        _hudRoot = root.GetComponent<RectTransform>();
        _hudRoot.anchorMin = new Vector2(0f, 1f);
        _hudRoot.anchorMax = new Vector2(0f, 1f);
        _hudRoot.pivot = new Vector2(0f, 1f);
        _hudRoot.sizeDelta = healthBarSize;
        _hudRoot.anchoredPosition = healthBarAnchor;

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(root.transform, false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(3f, 3f);
        fillRt.offsetMax = new Vector2(-3f, -3f);

        _healthBarFill = fill.GetComponent<Image>();
        _healthBarFill.type = Image.Type.Filled;
        _healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        _healthBarFill.fillOrigin = 0;
        _healthBarFill.fillAmount = 1f;
        _healthBarFill.color = new Color(0.2f, 0.95f, 0.35f, 0.95f);

        _healthLabel = CreateHudText(root.transform, "HealthLabel", new Vector2(8f, -11f), TextAlignmentOptions.Left, 16f);
        _healthLabel.text = "TANK 100%";
        _healthLabel.color = Color.white;
    }

    private void BuildDeathOverlay()
    {
        GameObject overlay = new GameObject("DeathOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = overlay.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);

        _deathGroup = overlay.GetComponent<CanvasGroup>();
        _deathGroup.alpha = 0f;
        _deathGroup.gameObject.SetActive(false);

        _deathLabel = CreateHudText(overlay.transform, "DeathText", Vector2.zero, TextAlignmentOptions.Center, 72f);
        _deathLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _deathLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _deathLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _deathLabel.rectTransform.sizeDelta = new Vector2(900f, 120f);
        _deathLabel.rectTransform.anchoredPosition = new Vector2(0f, 40f);
        _deathLabel.text = "YOU DIED";
        _deathLabel.color = new Color(1f, 0.08f, 0.08f, 1f);

        _deathSubtitle = CreateHudText(overlay.transform, "DeathSubText", Vector2.zero, TextAlignmentOptions.Center, 24f);
        _deathSubtitle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _deathSubtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _deathSubtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _deathSubtitle.rectTransform.sizeDelta = new Vector2(700f, 48f);
        _deathSubtitle.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        _deathSubtitle.text = "Restarting...";
        _deathSubtitle.color = new Color(1f, 0.65f, 0.65f, 0.95f);
    }

private void BuildWindWidget()
    {
        if (hudCanvas == null)
        {
            return;
        }

        GameObject root = new GameObject("WindWidget", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(hudCanvas.transform, false);

        _windRoot = root.GetComponent<RectTransform>();
        _windRoot.anchorMin = new Vector2(0f, 0f);
        _windRoot.anchorMax = new Vector2(0f, 0f);
        _windRoot.pivot = new Vector2(0f, 0f);
        _windRoot.sizeDelta = windWidgetSize;
        _windRoot.anchoredPosition = windWidgetAnchor;

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.06f, 0.08f, 0.12f, 0.84f);

        GameObject frame = new GameObject("WindFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frame.transform.SetParent(root.transform, false);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        frame.GetComponent<Image>().color = new Color(1f, 0.72f, 0.18f, 0.18f);

        _windLabel = CreateHudText(root.transform, "WindLabel", new Vector2(10f, -8f), TextAlignmentOptions.Left, 18f);
        _windLabel.rectTransform.sizeDelta = new Vector2(windWidgetSize.x - 20f, 24f);
        _windLabel.text = "WIND";
        _windLabel.color = new Color(1f, 0.88f, 0.55f, 1f);
        _windLabel.fontStyle = FontStyles.Bold;

        _windValueLabel = CreateHudText(root.transform, "WindValue", new Vector2(10f, -32f), TextAlignmentOptions.Left, 22f);
        _windValueLabel.rectTransform.sizeDelta = new Vector2(windWidgetSize.x - 20f, 30f);
        _windValueLabel.text = "CALM";
        _windValueLabel.color = new Color(0.78f, 0.92f, 1f, 1f);
        _windValueLabel.fontStyle = FontStyles.Bold;
    }

    private TextMeshProUGUI CreateHudText(Transform parent, string name, Vector2 anchoredPosition, TextAlignmentOptions alignment, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(220f, 32f);
        rt.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private void UpdateHUD()
    {
        if (_healthBarFill != null)
        {
            float normalized = maxHealth > 0f ? Mathf.Clamp01(_health / maxHealth) : 0f;
            _healthBarFill.fillAmount = normalized;
            _healthBarFill.color = Color.Lerp(new Color(1f, 0.18f, 0.12f, 0.95f), new Color(0.2f, 0.95f, 0.35f, 0.95f), normalized);
        }

        if (_healthLabel != null)
        {
            float pct = maxHealth > 0f ? Mathf.Clamp01(_health / maxHealth) * 100f : 0f;
            _healthLabel.text = $"TANK {pct:0}%";
        }

        UpdateWindWidget();
    }

private void UpdateWindWidget()
    {
        if (_windValueLabel == null)
        {
            return;
        }

        BattlefieldWind source = windSource;
        if (source == null)
        {
            source = BattlefieldWind.Instance;
        }

        if (source == null)
        {
            source = Object.FindFirstObjectByType<BattlefieldWind>();
        }

        if (source == null)
        {
            _windLabel.text = "WIND";
            _windValueLabel.text = "CALM";
            _windLabel.color = new Color(1f, 0.88f, 0.55f, 1f);
            _windValueLabel.color = new Color(0.78f, 0.92f, 1f, 1f);
            return;
        }

        string readout = source.GetWindReadout();
        string arrow = source.GetArrowLabel();
        _windLabel.text = "WIND  " + arrow;
        _windValueLabel.text = readout;

        float normalizedSpeed = Mathf.InverseLerp(0f, Mathf.Max(0.01f, source.maxSpeed), source.GetDisplaySpeed());
        Color labelColor = Color.Lerp(new Color(1f, 0.88f, 0.55f, 1f), new Color(1f, 0.38f, 0.16f, 1f), normalizedSpeed);
        Color valueColor = Color.Lerp(new Color(0.78f, 0.92f, 1f, 1f), new Color(1f, 0.96f, 0.76f, 1f), normalizedSpeed);
        _windLabel.color = labelColor;
        _windValueLabel.color = valueColor;
    }

    private void PlaceDeathOverlay()
    {
        if (_deathGroup == null)
        {
            return;
        }

        _deathGroup.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (_deathGroup != null && _deathGroup.gameObject != null)
        {
            Destroy(_deathGroup.gameObject);
        }

        if (_hudRoot != null && _hudRoot.gameObject != null)
        {
            Destroy(_hudRoot.gameObject);
        }

        if (_windRoot != null && _windRoot.gameObject != null)
        {
            Destroy(_windRoot.gameObject);
        }
    }

    public void Fire()
    {
        if (_isDead || shellPrefab == null || firePoint == null)
        {
            return;
        }

        if (_audio != null && playerFireSound != null)
        {
            _audio.PlayOneShot(playerFireSound);
        }

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);

        Collider[] tankColliders = GetComponentsInChildren<Collider>();
        Collider shellCollider = shell.GetComponent<Collider>();
        if (shellCollider != null)
        {
            foreach (Collider c in tankColliders)
            {
                Physics.IgnoreCollision(c, shellCollider);
            }
        }

        ProjectileCameraController projectile = shell.GetComponent<ProjectileCameraController>();
        if (projectile != null)
        {
            projectile.trackingBase = transform;
            projectile.launchPowerPercentage = powerPercentage;
            projectile.applyWindDrift = true;
            projectile.windDriftMultiplier = windProjectileDrift;
        }

        Rigidbody shellRb = shell.GetComponent<Rigidbody>();
        if (shellRb != null)
        {
            shellRb.mass = mass;
            float muzzleSpeed = Mathf.Lerp(maxPower * 0.45f, maxPower, powerPercentage / 100f);
            Vector3 launchVelocity = firePoint.forward * muzzleSpeed + GetTankVelocity();
#if UNITY_6000_0_OR_NEWER
            shellRb.linearVelocity = launchVelocity;
#else
            shellRb.velocity = launchVelocity;
#endif
        }
    }
}
