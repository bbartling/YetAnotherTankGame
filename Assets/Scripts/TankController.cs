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
    [Tooltip("Offset applied to the rigidbody centre of mass for stability")] public Vector3 centerOfMassOffset = new Vector3(0f, 0.01f, 0f);
    [Tooltip("Force applied when accelerating forward/backward")] public float accelerationForce = 150f;
    [Tooltip("Torque applied when rotating the tank")]
    public float turnTorque = 100f;
    [Tooltip("Maximum linear speed for the tank (prevents runaway acceleration)")]
    public float maxVelocity = 15f;

    [Header("Turret Settings")]
    public float turretTurnSpeed = 40f;
    public float barrelElevationSpeed = 30f;
    public float minElevation = -10f;
    public float maxElevation = 60f;

    [Header("Firing Settings")]
    public float maxPower = 150f; // Reduced from 2000 for better arc
    public float powerPercentage = 50f;
    public float mass = 1f;
    public AudioClip fireSound;
    
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

    private Rigidbody _rb;
    private AudioSource _audio;
    private float _currentElevation = 0f;
    private float _currentRotation = 0f;

    private bool _isEngineStarting = false;
    private bool _isEngineRunning = false;
    private float _stopTimer = 0f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();

        // Adjust the centre of mass. A higher CoM makes it possible to topple over.
        if (_rb != null)
        {
            _rb.centerOfMass += centerOfMassOffset;
            _rb.linearDamping = 0.5f; 
            _rb.angularDamping = 0.8f; 
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        
        // Ensure LineRenderer is set up but hidden
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = trajectoryPointCount;
            lineRenderer.enabled = false;
        }
    }

    void Update()
    {
        HandleInput();
        HandleAudio();
        UpdateTrajectory();
    }

    private void UpdateTrajectory()
    {
        if (lineRenderer == null) return;

        // Show trajectory only when Right Click (Sniper Mode) is active
        bool isSniperMode = Input.GetMouseButton(1);
        lineRenderer.enabled = isSniperMode;

        if (isSniperMode)
        {
            Vector3 startPos = firePoint.position;
            float actualPower = (powerPercentage / 100f) * maxPower;
            Vector3 startVelocity = firePoint.forward * (actualPower / mass);

            for (int i = 0; i < trajectoryPointCount; i++)
            {
                float t = i * trajectoryTimeStep;
                Vector3 point = startPos + startVelocity * t + 0.5f * Physics.gravity * t * t;
                lineRenderer.SetPosition(i, point);
            }
        }
    }
    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        // Turret Rotation (A/D)
        float rotInput = 0;
        if (Input.GetKey(KeyCode.D)) rotInput = 1;
        if (Input.GetKey(KeyCode.A)) rotInput = -1;

        _currentRotation += rotInput * turretTurnSpeed * Time.deltaTime;
        if (turret != null) turret.localRotation = Quaternion.Euler(0, _currentRotation, 0);

        // Elevation (W/S)
        float elevInput = 0;
        if (Input.GetKey(KeyCode.W)) elevInput = 1;
        if (Input.GetKey(KeyCode.S)) elevInput = -1;

        _currentElevation += elevInput * barrelElevationSpeed * Time.deltaTime;
        _currentElevation = Mathf.Clamp(_currentElevation, minElevation, maxElevation);
        
        if (barrel != null) barrel.localRotation = Quaternion.Euler(-_currentElevation, 0, 0);

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

    private void HandleAudio()
    {
        // Engine sound logic: True if any movement key is held
        bool driveInput = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || 
                          Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
        
        if (driveInput)
        {
            // Reset the timer while any key is pressed
            _stopTimer = engineStopDelay;

            if (!_isEngineStarting && !_isEngineRunning)
            {
                StartEngine();
            }
        }
        else
        {
            // If the engine is running or starting, but no keys are held, count down
            if (_isEngineStarting || _isEngineRunning)
            {
                _stopTimer -= Time.deltaTime;
                if (_stopTimer <= 0)
                {
                    StopEngine();
                }
            }
        }

        // Turret sound logic
        bool isRotating = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
        ToggleAudio(turretAudioSource, rotateTurretClip, isRotating);

        // Elevation sound logic
        bool isElevating = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);
        ToggleAudio(elevationAudioSource, adjustElevationClip, isElevating);
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

    private void ToggleAudio(AudioSource source, AudioClip clip, bool shouldPlay)
    {
        if (source == null || clip == null) return;
        
        if (shouldPlay)
        {
            if (!source.isPlaying || source.clip != clip)
            {
                source.clip = clip;
                source.loop = true;
                source.Play();
            }
        }
        else
        {
            if (source.isPlaying && source.clip == clip)
            {
                source.Stop();
            }
        }
    }

    private void HandleMovement()
    {
        // Handle forward/backward input using arrow keys or WASD
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) moveInput = 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) moveInput = -1f;

        // Handle left/right turning input
        float turnInput = 0f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) turnInput = 1f;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) turnInput = -1f;

        // Apply forward/backward force for acceleration. 
        if (moveInput != 0f)
        {
            Vector3 force = transform.forward * moveInput * accelerationForce;
            // Only accelerate if under the max velocity
            if (_rb.linearVelocity.magnitude < maxVelocity)
            {
                _rb.AddForce(force, ForceMode.Force);
            }
        }

        // Apply torque for rotation.
        if (turnInput != 0f)
        {
            float torqueAmount = turnInput * turnTorque;
            _rb.AddTorque(Vector3.up * torqueAmount, ForceMode.Force);
        }
    }

    public void Fire()
    {
        if (fireSound != null) _audio.PlayOneShot(fireSound);

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        
        // Ignore collision with all colliders on the tank
        Collider[] tankColliders = GetComponentsInChildren<Collider>();
        Collider shellCollider = shell.GetComponent<Collider>();
        if (shellCollider != null)
        {
            foreach (var c in tankColliders)
            {
                Physics.IgnoreCollision(c, shellCollider);
            }
        }

        var pcc = shell.GetComponent<ProjectileCameraController>();
if (pcc != null)
        {
            pcc.trackingBase = transform; 
        }

        var rb = shell.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = mass;
            float actualPower = (powerPercentage / 100f) * maxPower;
            rb.AddForce(firePoint.forward * actualPower, ForceMode.Impulse);
        }
    }
}
