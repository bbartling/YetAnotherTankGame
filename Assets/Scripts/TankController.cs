using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class TankController : MonoBehaviour
{
    [Header("Tank Parts")]
    public Transform turret;
    public Transform barrel;
    public Transform firePoint;
    public GameObject shellPrefab;
    public LineRenderer lineRenderer;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float turnSpeed = 50f;

    [Header("Turret Settings")]
    public float turretTurnSpeed = 40f;
    public float barrelElevationSpeed = 30f;
    public float minElevation = -10f;
    public float maxElevation = 60f;

    [Header("Firing Settings")]
    public float maxPower = 100f; // NEW: Max possible power
    public float powerPercentage = 50f; // NEW: Percentage default to 50%
    public float mass = 1f;
    public AudioClip fireSound;
    
    [Header("UI (Optional/Legacy)")]
    public TextMeshProUGUI elevationText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI powerText; // NEW: Text display for power %

    [Header("Audio Sources")]
    public AudioSource engineAudioSource;
    public AudioSource turretAudioSource;
    public AudioSource elevationAudioSource;

    [Header("Audio Clips")]
    public AudioClip engineRunningClip;
    public AudioClip engineStartClip; // ADDED
    public AudioClip engineStopClip;  // ADDED
    public AudioClip rotateTurretClip;
    public AudioClip adjustElevationClip;

    private Rigidbody _rb;
    private AudioSource _audio;
    private float _currentElevation = 0f;
    private float _currentRotation = 0f;
    private const int N_TRAJECTORY_POINTS = 20;

    private bool _isEngineStarting = false;
    private bool _isEngineRunning = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();
        
        // Disable LineRenderer if it exists
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        // Configuration is now expected to be done in the Inspector or via Setup script
    }

    void Update()
    {
        HandleInput();
        HandleAudio();
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
        // Engine sound logic
        bool driveInput = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || 
                          Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
        
        if (driveInput)
        {
            if (!_isEngineStarting && !_isEngineRunning)
            {
                StartEngine();
            }
        }
        else
        {
            if (_isEngineStarting || _isEngineRunning)
            {
                StopEngine();
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
        // Driving (Arrow Keys)
        float moveInput = 0;
        if (Input.GetKey(KeyCode.UpArrow)) moveInput = 1;
        if (Input.GetKey(KeyCode.DownArrow)) moveInput = -1;

        float turnInput = 0;
        if (Input.GetKey(KeyCode.RightArrow)) turnInput = 1;
        if (Input.GetKey(KeyCode.LeftArrow)) turnInput = -1;

        Vector3 move = transform.forward * moveInput * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + move);

        Quaternion turn = Quaternion.Euler(0, turnInput * turnSpeed * Time.fixedDeltaTime, 0);
        _rb.MoveRotation(_rb.rotation * turn);
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
