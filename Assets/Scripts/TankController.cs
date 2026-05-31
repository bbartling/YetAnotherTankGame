using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Require a Rigidbody so physics works correctly and a BoxCollider so the tank can collide with the terrain
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

    // Advanced movement options for a more arcadey, Hill‑Climb‑style feel.
    [Header("Advanced Movement Settings")]
[Tooltip("Offset applied to the rigidbody centre of mass for stability")] public Vector3 centerOfMassOffset = new Vector3(0f, -0.2f, 0f);
[Tooltip("Torque applied when rotating the tank")]
    public float turnTorque = 35f;
[Tooltip("Force applied when accelerating forward/backward")] public float accelerationForce = 130f;



    [Tooltip("Maximum linear speed for the tank (prevents runaway acceleration)")]
    public float maxVelocity = 15f;

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
    public Vector3 overviewCameraLocalPosition = new Vector3(0f, 7.5f, -12f);
    public Vector3 overviewCameraLocalEuler = new Vector3(18f, 0f, 0f);
    public float overviewCameraFov = 52f;

    [Header("Firing Settings")]
    public float maxPower = 75f; // Cannon shells should arc, not laser-beam forward
    public float powerPercentage = 50f;
    public float mass = 1f;
    public AudioClip playerFireSound;
    public AudioClip treeSmashSound;
    
    [Header("Trajectory Settings")]
    public int trajectoryPointCount = 30;
    public float trajectoryTimeStep = 0.1f;

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

    [Header("Spawn")]
    public string spawnGroundName = "Ground";
    public Vector3 spawnOffset = new Vector3(0f, 0.35f, 0f);
    public float spawnProbeHeight = 25f;

    private Rigidbody _rb;
    private AudioSource _audio;
    private float _currentElevation = 0f;
    private float _currentRotation = 0f;
    private Transform _turretYawPivot;

    private bool _isEngineStarting = false;
    private bool _isEngineRunning = false;
    private bool _isOverviewView = false;
    private float _stopTimer = 0f;
    private bool _wasDriveInput = false;
    private bool _wasRotatingInput = false;
    private bool _wasElevatingInput = false;
    private float _nextTreeSmashTime = 0f;
    private float _nextElevationSoundTime = 0f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();

        // Keep the hull stable and prevent turret input from tipping the chassis.
        if (_rb != null)
        {
            _rb.centerOfMass += centerOfMassOffset;
            _rb.linearDamping = 0.75f;
            _rb.angularDamping = 2.5f;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        EnsureAudioSources();
        EnsureTurretYawPivot();
        EnsureGameplayCamera();
        ApplyCameraView();
        
        // Ensure LineRenderer is set up but hidden
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = trajectoryPointCount;
            lineRenderer.enabled = false;
        }
    }

    private void Start()
    {
        PlaceOnGround();
    }

    void Update()
    {
        HandleViewToggle();
        HandleInput();
        HandleAudio();
        UpdateTrajectory();
    }

private void UpdateTrajectory()
    {
        if (lineRenderer == null || firePoint == null)
        {
            return;
        }

        bool isSniperMode = Input.GetMouseButton(1);
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
    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        // Mouse drives the turret now: horizontal for yaw, vertical for elevation.
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

        _currentElevation += mouseWheel * mouseWheelBarrelSensitivity * barrelElevationSpeed;
        _currentElevation += keyboardElevation * keyboardBarrelSensitivity * Time.deltaTime;
        _currentElevation = Mathf.Clamp(_currentElevation, minElevation, maxElevation);

        if (barrel != null)
        {
            barrel.localRotation = Quaternion.Euler(-_currentElevation, 0, 0);
        }

        // Power Adjustment (Q/E)
        if (Input.GetKey(KeyCode.E)) powerPercentage += 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) powerPercentage -= 20f * Time.deltaTime;
        powerPercentage = Mathf.Clamp(powerPercentage, 0f, 100f);

        // Firing
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            // Check if a player projectile already exists
            if (ProjectileCameraController.ActivePlayerProjectile == null)
            {
                Fire();
            }
        }

        // Update UI if connected
        if (elevationText != null) elevationText.text = "Elev: " + _currentElevation.ToString("F0") + "°";
        if (angleText != null) angleText.text = "Angle: " + _currentRotation.ToString("F0") + "°";
        if (powerText != null) powerText.text = "Power: " + powerPercentage.ToString("F0") + "%";
    }

    public float TurretYawDegrees
    {
        get { return Mathf.Repeat(_currentRotation, 360f); }
    }

    private void HandleViewToggle()
    {
        if (Input.GetKeyDown(viewToggleKey))
        {
            _isOverviewView = !_isOverviewView;
            ApplyCameraView();
        }
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

    private void HandleAudio()
    {
        // Engine sound logic: True if any movement key is held
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
            if (_stopTimer <= 0)
            {
                StopEngine();
            }
        }

        // Turret sound logic
        bool isRotating = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f;
        if (isRotating && !_wasRotatingInput)
        {
            PlayLoopAudio(turretAudioSource, rotateTurretClip);
        }
        else if (!isRotating && _wasRotatingInput)
        {
            StopLoopAudio(turretAudioSource, rotateTurretClip);
        }

        // Elevation sound logic: short one-shot ticks with a small repeat gap.
        bool isElevating = Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f || Input.GetKey(KeyCode.PageUp) || Input.GetKey(KeyCode.PageDown);
        if (isElevating && Time.time >= _nextElevationSoundTime && adjustElevationClip != null && elevationAudioSource != null)
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
        _isEngineStarting = true;
        _isEngineRunning = false;
        
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
        if (!_isEngineStarting) return; // Stopped before finished starting
        
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
        _isEngineStarting = false;
        _isEngineRunning = false;
        
        if (engineAudioSource != null)
        {
            engineAudioSource.Stop();
            if (engineStopClip != null)
            {
                engineAudioSource.PlayOneShot(engineStopClip);
            }
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
        float lift = bodyCollider != null ? Mathf.Max(0.25f, bodyCollider.bounds.extents.y + 0.05f) : 0.5f;

        GameObject groundObject = GameObject.Find(spawnGroundName);
        if (groundObject == null)
        {
            return;
        }

        Collider groundCollider = groundObject.GetComponent<Collider>();
        Bounds spawnBounds;
        if (groundCollider != null)
        {
            spawnBounds = groundCollider.bounds;
        }
        else
        {
            Renderer groundRenderer = groundObject.GetComponent<Renderer>();
            if (groundRenderer == null)
            {
                return;
            }

            spawnBounds = groundRenderer.bounds;
        }

        Vector3 target = transform.position + spawnOffset;
        Vector3 origin = new Vector3(target.x, spawnBounds.max.y + spawnProbeHeight, target.z);
        Vector3 groundedPosition = transform.position;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, spawnProbeHeight * 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            groundedPosition.y = hit.point.y + lift;
        }
        else
        {
            groundedPosition.y = spawnBounds.max.y + lift;
        }

        transform.position = groundedPosition;
        _rb.position = groundedPosition;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    private void HandleMovement()
    {
        // Tank movement uses WASD like a classic tank control scheme.
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.W)) moveInput = 1f;
        if (Input.GetKey(KeyCode.S)) moveInput = -1f;

        // Tank steering uses A/D.
        float turnInput = 0f;
        if (Input.GetKey(KeyCode.D)) turnInput = 1f;
        if (Input.GetKey(KeyCode.A)) turnInput = -1f;

        if (moveInput != 0f)
        {
            Vector3 force = transform.forward * moveInput * accelerationForce;
            if (_rb.linearVelocity.magnitude < maxVelocity)
            {
                _rb.AddForce(force, ForceMode.Force);
            }
        }

        if (turnInput != 0f)
        {
            _rb.AddTorque(Vector3.up * (turnInput * turnTorque), ForceMode.Force);
        }
    }

    private Vector3 GetTankVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return _rb != null ? _rb.linearVelocity : Vector3.zero;
#else
        return _rb != null ? _rb.velocity : Vector3.zero;
#endif
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (treeSmashSound == null || Time.time < _nextTreeSmashTime || collision.collider == null)
        {
            return;
        }

        if (!collision.collider.CompareTag("Tree"))
        {
            return;
        }

        _nextTreeSmashTime = Time.time + 0.22f;
        if (_audio != null)
        {
            _audio.PlayOneShot(treeSmashSound);
        }
    }


public void Fire()
    {
        if (shellPrefab == null || firePoint == null)
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
            foreach (var c in tankColliders)
            {
                Physics.IgnoreCollision(c, shellCollider);
            }
        }

        ProjectileCameraController pcc = shell.GetComponent<ProjectileCameraController>();
        if (pcc != null)
        {
            pcc.trackingBase = transform;
        }

        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = mass;
            float muzzleSpeed = Mathf.Lerp(maxPower * 0.45f, maxPower, powerPercentage / 100f);
            Vector3 launchVelocity = firePoint.forward * muzzleSpeed + GetTankVelocity();
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = launchVelocity;
#else
            rb.velocity = launchVelocity;
#endif
        }
    }
}
