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
    public static int PlayerCameraActivationCount { get; private set; }

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

    [Header("Lifetime")]
    public float baseSelfDestructSeconds = 8f;
    [Range(0f, 100f)]
    public float launchPowerPercentage = 50f;

    [Header("Explosion")]
    public GameObject explosionVFX;
    public float explosionRadius = 10f;
    public float explosionForce = 0.7f;
    
    [Header("Impact")]
    public float groundImpactMultiplier = 4.5f;
    public float castleImpactMultiplier = 3.5f;
    public float upwardsModifier = 1.0f;

    [Header("Wind")]
    public bool applyWindDrift = false;
    public float windDriftMultiplier = 0.12f;
    public float windStabilization = 0.08f;

    private AudioSource _audio;
    private TrailRenderer _trailRenderer;
    private Rigidbody _rb;

    private bool _isDestroying = false;

    private Transform _projCamTransform;
    private LineRenderer _cannonLineRenderer;
    private Camera _tankCamera;

    private Vector3 _cameraVelocity;
    private Vector3 _lastGoodFlightDirection;
    private Vector3 _impactLookPoint;
    private bool _holdingImpactView;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        ProjectileAudioController projectileAudio = GetComponent<ProjectileAudioController>();
        if (projectileAudio == null) projectileAudio = gameObject.AddComponent<ProjectileAudioController>();
        projectileAudio.EnsureFallbackClip();
        if (flyingShellSound == null) flyingShellSound = projectileAudio.whistleClip;
        if (explosionSound == null) explosionSound = ProceduralBattlefieldAudio.CreateImpact();
        _audio.clip = flyingShellSound;
        if (!_audio.isPlaying) _audio.Play();
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
            PlayerCameraActivationCount++;
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

        float selfDestructSeconds = GetSelfDestructSeconds();
        if (!float.IsPositiveInfinity(selfDestructSeconds))
        {
            Invoke(nameof(SelfDestruct), selfDestructSeconds);
        }
    }

    void LateUpdate()
    {
        if (ActivePlayerProjectile == this && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelProjectileCamera();
            return;
        }

        UpdateProjectileCamera();
        HandleProximitySound();
        UpdateTrailColor();
    }

    void FixedUpdate()
    {
        ApplyWindDrift();
    }

    private void UpdateProjectileCamera()
    {
        if (_projCamTransform == null)
            return;

        if (_holdingImpactView)
        {
            Vector3 impactLookDir = _impactLookPoint - _projCamTransform.position;
            if (impactLookDir.sqrMagnitude > 0.001f)
            {
                Quaternion impactRotation = Quaternion.LookRotation(impactLookDir.normalized, Vector3.up);
                _projCamTransform.rotation = Quaternion.Slerp(
                    _projCamTransform.rotation,
                    impactRotation,
                    Time.deltaTime * rotationSmoothSpeed);
            }

            return;
        }

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

    private float GetSelfDestructSeconds()
    {
        return float.PositiveInfinity;
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

    private void ApplyWindDrift()
    {
        if (_isDestroying || !applyWindDrift || _rb == null)
        {
            return;
        }

        BattlefieldWind wind = BattlefieldWind.Instance;
        if (wind == null)
        {
            wind = Object.FindAnyObjectByType<BattlefieldWind>();
        }

        if (wind == null)
        {
            return;
        }

        Vector3 windVector = wind.GetWindVector();
        if (windVector.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 horizontalWind = Vector3.ProjectOnPlane(windVector, Vector3.up);
        Vector3 driftForce = horizontalWind.normalized * (windVector.magnitude * windDriftMultiplier);
        driftForce += horizontalWind * windStabilization;
        _rb.AddForce(driftForce, ForceMode.Acceleration);
    }

    private void ApplyImpactEffects(Collision collision)
    {
        if (collision == null || collision.contactCount == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        _impactLookPoint = contact.point;
        _holdingImpactView = true;
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

        TankController tank = collision.collider.GetComponentInParent<TankController>();
        if (tank != null)
        {
            tank.ApplyProjectileDamage(Mathf.Max(15f, impactForce * 1.4f), contact.point, contact.normal);
            return;
        }

        EnemyTankAI enemyTank = collision.collider.GetComponentInParent<EnemyTankAI>();
        if (enemyTank != null)
        {
            enemyTank.ApplyProjectileDamage(Mathf.Max(15f, impactForce * 1.4f), contact.point, contact.normal);
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

    public void CancelProjectileCamera()
    {
        if (!_isDestroying)
        {
            StartCoroutine(ExplosionSequence(true));
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

                TankController tank = h.GetComponentInParent<TankController>();
                if (tank != null)
                {
                    float distance = Vector3.Distance(transform.position, tank.transform.position);
                    float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, explosionRadius));
                    float damage = Mathf.Max(10f, explosionForce * 70f * falloff + GetRigidbodyVelocity().magnitude * 0.6f);
                    tank.ApplyExplosionDamage(damage, transform.position, Vector3.up, falloff);
                    continue;
                }

                EnemyTankAI enemyTank = h.GetComponentInParent<EnemyTankAI>();
                if (enemyTank != null)
                {
                    float distance = Vector3.Distance(transform.position, enemyTank.transform.position);
                    float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, explosionRadius));
                    float damage = Mathf.Max(10f, explosionForce * 70f * falloff + GetRigidbodyVelocity().magnitude * 0.6f);
                    enemyTank.ApplyExplosionDamage(damage, transform.position, Vector3.up, falloff);
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

        EnsureReturnCamera();

        if (_tankCamera != null)
            _tankCamera.enabled = true;

        if (projectileCamera != null && projectileCamera != _tankCamera)
            projectileCamera.enabled = false;

        if (_cannonLineRenderer != null)
            _cannonLineRenderer.enabled = true;

        Destroy(gameObject);
    }

    private void EnsureReturnCamera()
    {
        if (_tankCamera != null)
        {
            return;
        }

        if (trackingBase != null)
        {
            _tankCamera = trackingBase.GetComponentInChildren<Camera>(true);
        }

        if (_tankCamera == null)
        {
            _tankCamera = Camera.main;
        }

        if (_tankCamera == null)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i] != projectileCamera)
                {
                    _tankCamera = cameras[i];
                    return;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (ActivePlayerProjectile == this)
        {
            ActivePlayerProjectile = null;
        }
    }
}
