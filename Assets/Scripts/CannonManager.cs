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

    [Header("UI Elements")]
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
    private Camera _cam;
    private AudioSource _audioSource;
    private float _cannonBallMass = 1f; // ADDED: Variable to store the cannonball's mass

    private float _elevationAngle;
    private float _traverseAngle;
    private float _launchPower;

    void Start()
    {
        _cam = Camera.main;
        _audioSource = GetComponent<AudioSource>();

        // ADDED: Get the mass from the cannonball prefab
        if (cannonBallPrefab != null)
        {
            Rigidbody rb = cannonBallPrefab.GetComponent<Rigidbody>();
            if (rb != null)
            {
                _cannonBallMass = rb.mass;
            }
        }

        lineRenderer.positionCount = N_TRAJECTORY_POINTS;
        lineRenderer.enabled = true;

        SetElevation();
        SetAngle();
        SetPower();
    }

    void Update()
    {
        _UpdateLineRenderer();
    }

    public void ShowUI()
    {
        if (mainUICanvas != null)
        {
            mainUICanvas.gameObject.SetActive(true);
        }
    }

    public void SetElevation()
    {
        _elevationAngle = elevationSlider.value;
        if (elevationText != null)
        {
            elevationText.text = _elevationAngle.ToString("F0") + "°";
        }
        AimCannon();
    }

    public void SetAngle()
    {
        _traverseAngle = angleSlider.value;
        if (angleText != null)
        {
            angleText.text = _traverseAngle.ToString("F0") + "°";
        }
        AimCannon();
    }

    public void SetPower()
    {
        _launchPower = powerSlider.value;
        if (powerText != null)
        {
            powerText.text = _launchPower.ToString("F0");
        }
    }

    private void AimCannon()
    {
        transform.localRotation = Quaternion.Euler(-_elevationAngle, _traverseAngle, 0f);
    }

    public void Fire()
    {
        if (mainUICanvas != null)
        {
            mainUICanvas.gameObject.SetActive(false);
        }

        if (cannonFireSound != null)
        {
            _audioSource.PlayOneShot(cannonFireSound);
        }

        // The actual firing logic doesn't change, as AddForce already uses mass.
        Vector3 initialVelocityImpulse = transform.forward * _launchPower;
        GameObject cannonBall = Instantiate(cannonBallPrefab, firePoint.position, transform.rotation);

        ProjectileCameraController pcc = cannonBall.GetComponent<ProjectileCameraController>();
        if (pcc != null)
        {
            pcc.mainCamera = _cam;
            pcc.launchElevation = _elevationAngle;
            pcc.cannonManager = this;
        }

        Rigidbody rb = cannonBall.GetComponent<Rigidbody>();
        rb.AddForce(initialVelocityImpulse, ForceMode.Impulse);
    }

    private void _UpdateLineRenderer()
    {
        // MODIFIED: Calculate the true initial velocity by dividing the impulse power by the mass
        Vector3 launchVelocity = (transform.forward * _launchPower) / _cannonBallMass;

        Vector3 startPosition = firePoint.position;

        for (int i = 0; i < N_TRAJECTORY_POINTS; i++)
        {
            float t = i * 0.1f;
            Vector3 pointPosition = startPosition + launchVelocity * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, pointPosition);
        }
    }
}