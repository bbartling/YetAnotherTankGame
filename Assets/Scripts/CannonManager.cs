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

    [Header("UI (Player Only)")]
    public Canvas mainUICanvas;
    public Slider elevationSlider;
    public Slider angleSlider;
    public Slider powerSlider;
    public TextMeshProUGUI elevationText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI powerText;

    [Header("Sound Effects")]
    public AudioClip cannonFireSound;

    private const int N_TRAJECTORY_POINTS = 20;
    private Camera _mainCam;
    private AudioSource _audio;
    private float _ballMass = 1f;

    private float _elevationDeg;
    private float _traverseDeg;
    private float _powerImpulse;

    void Awake()
    {
        _mainCam = Camera.main;
        _audio = GetComponent<AudioSource>();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (cannonBallPrefab?.GetComponent<Rigidbody>() != null)
        {
            _ballMass = cannonBallPrefab.GetComponent<Rigidbody>().mass;
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = N_TRAJECTORY_POINTS;
            lineRenderer.enabled = true;
        }

        SetElevation();
        SetAngle();
        SetPower();
    }

    void Update()
    {
        UpdateTrajectoryPreview();
    }

    public void SetElevation()
    {
        if (elevationSlider == null) return;
        _elevationDeg = elevationSlider.value;
        if (elevationText != null) elevationText.text = $"{_elevationDeg:F0}°";
        ApplyAim();
    }

    public void SetAngle()
    {
        if (angleSlider == null) return;
        _traverseDeg = angleSlider.value;
        if (angleText != null) angleText.text = $"{_traverseDeg:F0}°";
        ApplyAim();
    }

    public void SetPower()
    {
        if (powerSlider == null) return;
        _powerImpulse = powerSlider.value;
        if (powerText != null) powerText.text = $"{_powerImpulse:F0}";
    }

    public void Fire()
    {
        // REMOVED: No longer hiding the UI
        if (cannonFireSound != null) _audio.PlayOneShot(cannonFireSound);

        Vector3 impulse = transform.forward * _powerImpulse;
        GameObject ball = Instantiate(cannonBallPrefab, firePoint.position, transform.rotation);

        var pcc = ball.GetComponent<ProjectileCameraController>();
        if (pcc != null)
        {
            pcc.owner = ProjectileCameraController.OwnerType.Player;
            pcc.mainCamera = _mainCam; // For the player's FPS camera view
        }

        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(impulse, ForceMode.Impulse);
    }

    private void ApplyAim()
    {
        transform.localRotation = Quaternion.Euler(-_elevationDeg, _traverseDeg, 0f);
    }

    private void UpdateTrajectoryPreview()
    {
        if (lineRenderer == null || firePoint == null) return;

        Vector3 v0 = (transform.forward * _powerImpulse) / Mathf.Max(_ballMass, 0.0001f);
        Vector3 p0 = firePoint.position;

        for (int i = 0; i < N_TRAJECTORY_POINTS; i++)
        {
            float t = i * 0.1f;
            Vector3 p = p0 + v0 * t + 0.5f * Physics.gravity * (t * t);
            lineRenderer.SetPosition(i, p);
        }
    }
}