using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(TrailRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class ProjectileCameraController : MonoBehaviour
{
    [Header("References")]
    public Camera projectileCamera;
    public Transform trackingBase;
    public bool enableCameraSwitching = true;

    public static ProjectileCameraController ActivePlayerProjectile { get; private set; }

    [Header("Smooth Projectile Camera")]
    public Vector3 cameraOffset = new Vector3(0f, 3f, -9f);
    public float positionSmoothTime = 0.16f;
    public float rotationSmoothSpeed = 7f;
    public float lookAheadDistance = 5f;
    public float minVelocityForDirection = 0.5f;
    public bool logCameraSwitch = false;

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
    public float explosionForce = 0.7f;
    
    [Header("Impact")]
    public float groundImpactMultiplier = 1.0f;
    public float castleImpactMultiplier = 1.0f;
public float upwardsModifier = 1.0f;

    private AudioSource _audio;
    private TrailRenderer _trailRenderer;
    private Rigidbody _rb;

    private bool _isDestroying = false;
    private const float SELF_DESTRUCT_S = 8f;

    private Transform _projCamTransform;
    private LineRenderer _cannonLineRenderer;
    private Camera _tankCamera;

    private Vector3 _cameraVelocity;
    private Vector3 _lastGoodFlightDirection;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        _trailRenderer = GetComponent<TrailRenderer>();
        _rb = GetComponent<Rigidbody>();

        _audio.loop = true;

        // Big help for smoother visual motion on fast physics objects.
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _lastGoodFlightDirection = transform.forward;

        if (projectileCamera == null)
        {
            GameObject camGo = GameObject.Find("ProjectileCamera");
            if (camGo != null)
                projectileCamera = camGo.GetComponent<Camera>();
        }

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

            projectileCamera.enabled = true;
            _tankCamera.enabled = false;

            Vector3 flightDir = GetFlightDirection();
            Vector3 startPos = transform.position
                             - flightDir * Mathf.Abs(cameraOffset.z)
                             + Vector3.up * cameraOffset.y
                             + transform.right * cameraOffset.x;

            projectileCamera.transform.position = startPos;
            projectileCamera.transform.rotation = Quaternion.LookRotation(
                transform.position + flightDir * lookAheadDistance - startPos,
                Vector3.up
            );

            if (logCameraSwitch)
            {
                Debug.Log("[Projectile] Camera switched to ProjectileCamera.");
            }
        }
        else if (enableCameraSwitching)
        {
            Debug.LogWarning("[Projectile] Camera switch failed. ProjCam: "
                + (projectileCamera != null)
                + ", TankCam: "
                + (_tankCamera != null));
        }

        GameObject cannonObject = GameObject.Find("cannon");
        if (cannonObject != null)
        {
            _cannonLineRenderer = cannonObject.GetComponent<LineRenderer>();
            if (_cannonLineRenderer != null)
                _cannonLineRenderer.enabled = false;
        }

        if (flyingShellSound != null)
        {
            _audio.clip = flyingShellSound;
        }

        Invoke(nameof(SelfDestruct), SELF_DESTRUCT_S);
    }

    void LateUpdate()
    {
        UpdateProjectileCamera();
        HandleProximitySound();
        UpdateTrailColor();
    }

    private void UpdateProjectileCamera()
    {
        if (_projCamTransform == null || _isDestroying)
            return;

        Vector3 flightDir = GetFlightDirection();

        Vector3 targetPos = transform.position
                          - flightDir * Mathf.Abs(cameraOffset.z)
                          + Vector3.up * cameraOffset.y
                          + transform.right * cameraOffset.x;

        _projCamTransform.position = Vector3.SmoothDamp(
            _projCamTransform.position,
            targetPos,
            ref _cameraVelocity,
            positionSmoothTime
        );

        Vector3 lookTarget = transform.position + flightDir * lookAheadDistance;
        Vector3 lookDir = lookTarget - _projCamTransform.position;

        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

            _projCamTransform.rotation = Quaternion.Slerp(
                _projCamTransform.rotation,
                targetRot,
                Time.deltaTime * rotationSmoothSpeed
            );
        }
    }

    private Vector3 GetFlightDirection()
    {
        Vector3 velocity = GetRigidbodyVelocity();

        if (velocity.magnitude > minVelocityForDirection)
        {
            _lastGoodFlightDirection = velocity.normalized;
        }

        if (_lastGoodFlightDirection.sqrMagnitude < 0.001f)
        {
            _lastGoodFlightDirection = transform.forward;
        }

        return _lastGoodFlightDirection;
    }

    private Vector3 GetRigidbodyVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return _rb.linearVelocity;
#else
        return _rb.velocity;
#endif
    }

    private void SetRigidbodyVelocity(Vector3 value)
    {
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = value;
#else
        _rb.velocity = value;
#endif
    }

    private void UpdateTrailColor()
    {
        if (_trailRenderer == null)
            return;

        float t = Mathf.PingPong(Time.time * trailFlickerSpeed, 1f);
        Color color = fireGradient.Evaluate(t);

        _trailRenderer.startColor = color;
        _trailRenderer.endColor = color;
    }

    private void HandleProximitySound()
    {
        if (_isDestroying || trackingBase == null || _audio == null || flyingShellSound == null)
            return;

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

    private void ApplyImpactEffects(Collision collision)
    {
        if (collision == null || collision.contactCount == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        float impactForce = Mathf.Max(1f, _rb != null ? _rb.mass * GetRigidbodyVelocity().magnitude : 1f);

        CraterTerrain craterTerrain = collision.collider.GetComponentInParent<CraterTerrain>();
        if (craterTerrain != null)
        {
            craterTerrain.ApplyImpact(contact.point, contact.normal, impactForce * groundImpactMultiplier);
        }
        else
        {
            DestructibleGround ground = collision.collider.GetComponentInParent<DestructibleGround>();
            if (ground != null)
            {
                ground.ApplyImpact(contact.point, contact.normal, impactForce * groundImpactMultiplier);
            }
        }

        CastleDamageReceiver castle = collision.collider.GetComponentInParent<CastleDamageReceiver>();
        if (castle != null)
        {
            castle.ApplyImpact(contact.point, contact.normal, impactForce * castleImpactMultiplier);
        }

        if (collision.collider.CompareTag("Tree"))
        {
            Rigidbody treeRb = collision.collider.attachedRigidbody;
            if (treeRb != null)
            {
                treeRb.AddExplosionForce(impactForce * 1.1f, contact.point, explosionRadius, upwardsModifier, ForceMode.Impulse);
            }

            if (impactForce >= 8f)
            {
                Destroy(collision.collider.gameObject, 0.35f);
            }
        }
    }


    void OnCollisionEnter(Collision collision)
    {
        if (_isDestroying)
            return;

        ApplyImpactEffects(collision);

        if (_rb != null && !_rb.isKinematic)
        {
            SetRigidbodyVelocity(Vector3.zero);
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        if (collision.gameObject.CompareTag("EnemyTurret"))
        {
            Destroy(collision.gameObject);
        }

        StartCoroutine(ExplosionSequence(false));
    }

    void SelfDestruct()
    {
        if (!_isDestroying)
        {
            StartCoroutine(ExplosionSequence(false));
        }
    }

    private IEnumerator ExplosionSequence(bool isInstant)
    {
        _isDestroying = true;
        CancelInvoke(nameof(SelfDestruct));

        if (_audio != null)
            _audio.Stop();

        if (_rb != null)
            _rb.isKinematic = true;

        if (!isInstant)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (var h in hits)
            {
                Rigidbody hitRb = h.attachedRigidbody;

                if (hitRb != null)
                {
                    hitRb.AddExplosionForce(
                        explosionForce,
                        transform.position,
                        explosionRadius,
                        upwardsModifier,
                        ForceMode.Impulse
                    );
                }
            }
        }

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
            mr.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in systems)
        {
            var emission = ps.emission;
            emission.enabled = false;
        }

        if (explosionSound != null && _audio != null && _audio.enabled)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        if (isInstant)
            yield return null;
        else
            yield return new WaitForSeconds(explosionLingerTime);

        if (projectileCamera != null)
            projectileCamera.enabled = false;

        if (_tankCamera != null)
            _tankCamera.enabled = true;

        if (_cannonLineRenderer != null)
            _cannonLineRenderer.enabled = true;

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
