using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EnemyCannonAI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform playerCannonTarget;

    [Header("Ballistics")]
    public float shootingAngle = 45f;
    public float aimInaccuracy = 5f;

    [Header("Cannon Parts")]
    public GameObject cannonBallPrefab;
    public Transform firePoint;
    public LineRenderer lineRenderer;

    [Header("Audio")]
    public AudioClip enemyFireSound;

    [Header("Timing")]
    public float initialDelay = 3.0f;
    public float fireInterval = 5.0f;
    public float aimAndFireDelay = 3.0f; // ADDED: Delay between aiming and firing

    private const int N_TRAJECTORY_POINTS = 20;
    private float _cannonBallMass = 1f;
    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();

        if (cannonBallPrefab?.GetComponent<Rigidbody>() != null)
        {
            _cannonBallMass = cannonBallPrefab.GetComponent<Rigidbody>().mass;
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = N_TRAJECTORY_POINTS;
            lineRenderer.enabled = false;
        }

        StartCoroutine(AutonomousFireLoop());
    }

    private IEnumerator AutonomousFireLoop()
    {
        Debug.Log("AI: Starting autonomous fire loop.");
        yield return new WaitForSeconds(initialDelay);
        while (true)
        {
            Debug.Log($"AI: Next shot in {fireInterval} seconds.");
            yield return new WaitForSeconds(fireInterval);
            yield return StartCoroutine(FireSequence());
        }
    }

    private IEnumerator FireSequence()
    {
        if (playerCannonTarget == null || cannonBallPrefab == null || firePoint == null)
            yield break;

        Debug.Log("AI: Starting aim sequence...");

        Vector3 toPlayer = playerCannonTarget.position - firePoint.position;
        Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (flat.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

        float? v = CalculateLaunchSpeed(playerCannonTarget.position, shootingAngle);
        if (v.HasValue)
        {
            Debug.Log($"AI: Target in range. Calculated launch speed: {v.Value:F1}");
            float elev = shootingAngle + Random.Range(-aimInaccuracy, aimInaccuracy);
            float speed = v.Value * (1f + Random.Range(-aimInaccuracy, aimInaccuracy) * 0.02f);

            var e = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(-elev, e.y, 0f);

            DrawTrajectory(speed * _cannonBallMass);

            // UPDATED: Using the new adjustable delay
            Debug.Log($"AI: Aiming for {aimAndFireDelay} seconds before firing.");
            yield return new WaitForSeconds(aimAndFireDelay);

            Fire(speed * _cannonBallMass);
        }
        else
        {
            Debug.Log("AI: Target is out of range! Firing a desperation shot.");
            Fire(5000f);
        }

        if (aimInaccuracy > 0.5f) aimInaccuracy *= 0.9f;
    }

    // ... The rest of the script is unchanged ...
    private void Fire(float impulsePower)
    {
        Debug.Log("AI: Firing now!");

        if (enemyFireSound != null)
        {
            _audioSource.PlayOneShot(enemyFireSound);
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        GameObject ball = Instantiate(cannonBallPrefab, firePoint.position, transform.rotation);

        var pcc = ball.GetComponent<ProjectileCameraController>();
        if (pcc != null)
        {
            pcc.owner = ProjectileCameraController.OwnerType.Enemy;
        }

        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(transform.forward * impulsePower, ForceMode.Impulse);
        }
    }

    private void DrawTrajectory(float impulsePower)
    {
        if (lineRenderer == null || firePoint == null) return;

        lineRenderer.enabled = true;
        Vector3 launchVelocity = (transform.forward * impulsePower) / Mathf.Max(_cannonBallMass, 0.0001f);
        Vector3 startPosition = firePoint.position;

        for (int i = 0; i < N_TRAJECTORY_POINTS; i++)
        {
            float t = i * 0.1f;
            Vector3 pointPosition = startPosition + launchVelocity * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, pointPosition);
        }
    }

    private float? CalculateLaunchSpeed(Vector3 targetPosition, float angleDeg)
    {
        float g = Physics.gravity.magnitude;
        Vector3 disp = targetPosition - firePoint.position;
        float y = disp.y;
        disp.y = 0f;
        float x = disp.magnitude;
        float a = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(a);
        float tan = Mathf.Tan(a);
        float numerator = g * x * x;
        float denom = 2f * (x * tan - y) * (cos * cos);

        if (denom <= 0f) return null;
        return Mathf.Sqrt(numerator / denom);
    }
}