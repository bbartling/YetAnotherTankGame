using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class CannonManager : MonoBehaviour
{
    [Header("Cannon Parts")]
    public GameObject cannonBallPrefab;
    public Transform firePoint;
    public LineRenderer lineRenderer;

    [Header("UI Controls")]
    public Slider elevationSlider;
    public Slider angleSlider;
    public Slider powerSlider;
    public Slider massSlider;
    public TextMeshProUGUI elevationText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI massText;

    [Header("Target Tracking")]
    public Transform baseForCamera; // ADDED: Will hold the 'BASE' object

    [Header("Sound Effects")]
    public AudioClip cannonFireSound;

    private const int N_TRAJECTORY_POINTS = 20;
    private Camera _mainCam;
    private AudioSource _audio;

    // Private variables to hold slider values
    private float _elevationDeg;
    private float _traverseDeg;
    private float _powerImpulse;
    private float _mass;

    void Awake()
    {
        _mainCam = Camera.main;
        _audio = GetComponent<AudioSource>();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = N_TRAJECTORY_POINTS;
            lineRenderer.enabled = true;
        }

        // Initialize all values from the sliders at the start
        SetElevation();
        SetAngle();
        SetPower();
        SetMass();
    }

    void Update()
    {
        // The trajectory preview now correctly updates whenever a slider changes
        UpdateTrajectoryPreview();
    }

    // --- Public Methods for UI Sliders to Call ---
    public void SetElevation()
    {
        if (elevationSlider == null) return;
        _elevationDeg = elevationSlider.value;
        if (elevationText != null) elevationText.text = $"{_elevationDeg:F0}�";
        ApplyAim();
    }

    public void SetAngle()
    {
        if (angleSlider == null) return;
        _traverseDeg = angleSlider.value;
        if (angleText != null) angleText.text = $"{_traverseDeg:F0}�";
        ApplyAim();
    }

    public void SetPower()
    {
        if (powerSlider == null) return;
        _powerImpulse = powerSlider.value;
        if (powerText != null) powerText.text = $"{_powerImpulse:F0}";
    }

    public void SetMass()
    {
        if (massSlider == null) return;
        _mass = massSlider.value;
        if (massText != null) massText.text = $"{_mass:F0} kg";
    }

    public void Fire()
    {
        // Check if a player projectile already exists
        if (ProjectileCameraController.ActivePlayerProjectile != null) return;

        if (cannonFireSound != null) _audio.PlayOneShot(cannonFireSound);

        // Instantiate the cannonball
        GameObject ball = Instantiate(cannonBallPrefab, firePoint.position, transform.rotation);

        // Set its camera controller properties
        var pcc = ball.GetComponent<ProjectileCameraController>();
        if (pcc != null)
        {
            pcc.trackingBase = baseForCamera; // MODIFIED: Pass the transform of the base
        }

        // Get its Rigidbody and set its mass from our slider value
        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = _mass;
            Vector3 impulse = transform.forward * _powerImpulse;
            rb.AddForce(impulse, ForceMode.Impulse);
        }
    }

    private void ApplyAim()
    {
        transform.localRotation = Quaternion.Euler(-_elevationDeg, _traverseDeg, 0f);
    }

    private void UpdateTrajectoryPreview()
    {
        if (lineRenderer == null || firePoint == null) return;

        // The initial velocity (v0) is the impulse (power) divided by the mass.
        Vector3 v0 = (transform.forward * _powerImpulse) / Mathf.Max(_mass, 0.0001f);
        Vector3 p0 = firePoint.position;

        for (int i = 0; i < N_TRAJECTORY_POINTS; i++)
        {
            float t = i * 0.1f;
            // Standard kinematic equation for projectile motion: p(t) = p0 + v0*t + 0.5*g*t^2
            Vector3 p = p0 + v0 * t + 0.5f * Physics.gravity * (t * t);
            lineRenderer.SetPosition(i, p);
        }
    }
}

