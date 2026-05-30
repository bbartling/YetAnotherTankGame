using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(TrailRenderer))] // Ensures a Trail Renderer is attached
public class ProjectileCameraController : MonoBehaviour
{
    [Header("References")]
    public Camera projectileCamera; 
    public Transform trackingBase;
    public Vector3 cameraPositionOffset = new Vector3(0, 2f, -5f); 
    public bool enableCameraSwitching = true; // NEW: Allow disabling camera switch for AI

    public static ProjectileCameraController ActivePlayerProjectile { get; private set; }

    [Header("Visuals")] 
    public Gradient fireGradient;
    public float trailFlickerSpeed = 10f;

    [Header("Audio")]
    public AudioClip flyingShellSound;
    public AudioClip explosionSound;
    public float soundActivationDistance = 100f;

    [Header("Cinematics")]
    public float explosionLingerTime = 2.0f;

    [Header("Explosion")]
    public GameObject explosionVFX;
    public float explosionRadius = 10f;
    public float explosionForce = .7f;
    public float upwardsModifier = 1.0f;

    private AudioSource _audio;
    private TrailRenderer _trailRenderer; 
    private bool _isDestroying = false;
    private const float SELF_DESTRUCT_S = 8f;

    private Transform _projCamTransform;
    private LineRenderer _cannonLineRenderer;

    private Camera _tankCamera; // Reference to store the tank's camera

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        _trailRenderer = GetComponent<TrailRenderer>(); 
        _audio.loop = true;

        // Try to find the ProjectileCamera
        if (projectileCamera == null)
        {
            GameObject camGo = GameObject.Find("ProjectileCamera");
            if (camGo != null) projectileCamera = camGo.GetComponent<Camera>();
        }

        // IMPROVED: Find the tank camera by looking at the trackingBase or using Camera.main
        if (trackingBase != null)
        {
            _tankCamera = trackingBase.GetComponentInChildren<Camera>(true);
        }
        
        if (_tankCamera == null)
        {
            _tankCamera = Camera.main;
        }

        if (enableCameraSwitching && projectileCamera != null && _tankCamera != null)
        {
            ActivePlayerProjectile = this;
            _projCamTransform = projectileCamera.transform;
            
            // Switch cameras
            projectileCamera.enabled = true;
            _tankCamera.enabled = false;
            
            // Move projectile camera to starting position behind projectile
            projectileCamera.transform.position = transform.position - transform.forward * 5f + Vector3.up * 2f;
            projectileCamera.transform.LookAt(transform.position);
            
            Debug.Log("[Projectile] Camera switched to ProjectileCamera.");
        }
        else if (enableCameraSwitching)
        {
            Debug.LogWarning("[Projectile] Camera switch failed. ProjCam: " + (projectileCamera != null) + ", TankCam: " + (_tankCamera != null));
        }

        GameObject cannonObject = GameObject.Find("cannon");
        if (cannonObject != null)
        {
            _cannonLineRenderer = cannonObject.GetComponent<LineRenderer>();
            if (_cannonLineRenderer != null) _cannonLineRenderer.enabled = false;
        }

        if (flyingShellSound != null)
        {
            _audio.clip = flyingShellSound;
        }

        Invoke(nameof(SelfDestruct), SELF_DESTRUCT_S);
    }

    void LateUpdate()
    {
        // --- Camera Tracking Logic ---
        if (_projCamTransform != null && !_isDestroying)
        {
            // Follow from behind
            Vector3 targetPos = transform.position - transform.forward * 5f + Vector3.up * 2f;
            _projCamTransform.position = Vector3.Lerp(_projCamTransform.position, targetPos, Time.deltaTime * 10f);
            _projCamTransform.LookAt(transform.position);
        }

        // --- Proximity Audio Logic ---
        HandleProximitySound();

        // --- Trail Color Logic ---
        UpdateTrailColor();
    }

    private void UpdateTrailColor()
    {
        if (_trailRenderer == null) return;

        // Use PingPong to create a value that cycles back and forth between 0 and 1
        float t = Mathf.PingPong(Time.time * trailFlickerSpeed, 1f);

        // Evaluate the gradient at that point in the cycle to get a color
        Color color = fireGradient.Evaluate(t);

        // Apply the new color to the trail
        _trailRenderer.startColor = color;
        _trailRenderer.endColor = color;
    }

    private void HandleProximitySound()
    {
        if (_isDestroying || trackingBase == null || _audio == null || flyingShellSound == null) return;

        float distance = Vector3.Distance(transform.position, trackingBase.position);

        if (distance <= soundActivationDistance && !_audio.isPlaying)
        {
            _audio.Play();
        }
        else if (distance > soundActivationDistance && _audio.isPlaying)
        {
            _audio.Stop();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_isDestroying)
        {
            // Stop movement immediately on any collision
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (collision.gameObject.CompareTag("EnemyTurret"))
            {
                Destroy(collision.gameObject);
                if (LevelManager.Instance != null) LevelManager.Instance.TurretDestroyed();
                StartCoroutine(ExplosionSequence(false));
            }
            else
            {
                // Ground, Buildings, or anything else
                StartCoroutine(ExplosionSequence(false));
            }
        }
    }

    void SelfDestruct()
    {
        if (!_isDestroying) StartCoroutine(ExplosionSequence(false));
    }

    private IEnumerator ExplosionSequence(bool isInstant)
    {
        _isDestroying = true;
        CancelInvoke(nameof(SelfDestruct));

        if (_audio != null) _audio.Stop();

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (!isInstant)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var h in hits)
            {
                var hrb = h.attachedRigidbody;
                if (hrb != null)
                    hrb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
            }
        }

        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Stop all particle systems on impact
        var systems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in systems)
        {
            var emission = ps.emission;
            emission.enabled = false;
        }

        if (explosionSound != null && _audio != null && _audio.enabled)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        if (isInstant) yield return null;
        else yield return new WaitForSeconds(explosionLingerTime);

        if (projectileCamera != null) projectileCamera.enabled = false;
        if (_tankCamera != null) _tankCamera.enabled = true;

        if (_cannonLineRenderer != null)
{
            _cannonLineRenderer.enabled = true;
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (ActivePlayerProjectile == this)
        {
            ActivePlayerProjectile = null;
        }
    }
}

