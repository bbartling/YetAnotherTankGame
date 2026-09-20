using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(TrailRenderer))] // Ensures a Trail Renderer is attached
public class ProjectileCameraController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Transform trackingBase;
    public Vector3 cameraPositionOffset = new Vector3(0, 2f, 0);

    [Header("Visuals")] // NEW SECTION
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
    private TrailRenderer _trailRenderer; // ADDED
    private bool _isDestroying = false;
    private const float SELF_DESTRUCT_S = 8f;

    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;
    private Transform _mainCamTransform;
    private LineRenderer _cannonLineRenderer;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        _trailRenderer = GetComponent<TrailRenderer>(); // ADDED
        _audio.loop = true;

        if (mainCamera != null)
        {
            _mainCamTransform = mainCamera.transform;
            _originalCamPos = _mainCamTransform.position;
            _originalCamRot = _mainCamTransform.rotation;
        }
        else
        {
            Debug.LogError("Main Camera reference is not set on the projectile!");
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
        if (trackingBase != null && _mainCamTransform != null && !_isDestroying)
        {
            _mainCamTransform.position = trackingBase.position + cameraPositionOffset;
            _mainCamTransform.LookAt(transform.position);
        }

        // --- Proximity Audio Logic ---
        HandleProximitySound();

        // --- ADDED: Trail Color Logic ---
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
            StartCoroutine(ExplosionSequence());
        }
    }

    void SelfDestruct()
    {
        if (!_isDestroying) StartCoroutine(ExplosionSequence());
    }

    private IEnumerator ExplosionSequence()
    {
        _isDestroying = true;
        CancelInvoke(nameof(SelfDestruct));

        if (_audio != null) _audio.Stop();

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var h in hits)
        {
            var hrb = h.attachedRigidbody;
            if (hrb != null)
                hrb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
        }

        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (explosionSound != null && _audio != null && _audio.enabled)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(explosionLingerTime);

        if (_mainCamTransform != null)
        {
            _mainCamTransform.position = _originalCamPos;
            _mainCamTransform.rotation = _originalCamRot;
        }

        if (_cannonLineRenderer != null)
        {
            _cannonLineRenderer.enabled = true;
        }

        Destroy(gameObject);
    }
}

