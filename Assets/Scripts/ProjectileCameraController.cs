using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(TrailRenderer))]
public class ProjectileCameraController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Transform trackingBase;
    public Vector3 cameraPositionOffset = new Vector3(0, 2f, 0);

    [Header("Visuals")]
    public Gradient fireGradient;
    public float trailFlickerSpeed = 10f;
    public float trailTime = 0.55f;
    public float trailWidth = 0.45f;

    [Header("Audio")]
    public AudioClip flyingShellSound;
    public AudioClip explosionSound;
    public float soundActivationDistance = 100f;

    [Header("Cinematics")]
    public float explosionLingerTime = 2.0f;
    [Tooltip("Seconds before mid-air self-destruct if the ball never hits anything.")]
    public float selfDestructSeconds = 13f;

    [Header("Explosion")]
    public GameObject explosionVFX;
    public float explosionRadius = 10f;
    public float explosionForce = 0.7f;
    public float upwardsModifier = 1.0f;

    private AudioSource _audio;
    private TrailRenderer _trailRenderer;
    private ParticleSystem _fireParticles;
    private ParticleSystem _emberParticles;
    private Material _trailMaterial;
    private Material _particleMaterial;
    private bool _isDestroying;
    private bool _ownsTrailMaterial;
    private bool _ownsParticleMaterial;
    private bool _clearedMuzzle;
    private bool _failedClearance;
    private bool _skipCameraRestore;
    private CannonManager _owningCannon;
    private Vector3 _fireOrigin;
    private float _clearanceDistance = 3.25f;
    private float _clearanceSeconds = 0.55f;
    private float _spawnTime;

    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;
    private Transform _mainCamTransform;
    private LineRenderer _cannonLineRenderer;

    public void ConfigureMuzzleClearance(
        CannonManager owningCannon,
        Vector3 fireOrigin,
        float clearanceDistance,
        float clearanceSeconds)
    {
        _owningCannon = owningCannon;
        _fireOrigin = fireOrigin;
        _clearanceDistance = Mathf.Max(0.5f, clearanceDistance);
        _clearanceSeconds = Mathf.Max(0.1f, clearanceSeconds);
        _spawnTime = Time.time;
        _clearedMuzzle = false;
        _failedClearance = false;
    }

    public void AbortForCannonDestruction()
    {
        _failedClearance = true;
        _skipCameraRestore = true;
        _isDestroying = true;
        CancelInvoke(nameof(SelfDestruct));

        if (_audio != null)
        {
            _audio.Stop();
        }

        if (_fireParticles != null)
        {
            _fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (_emberParticles != null)
        {
            _emberParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false;
        }

        Destroy(gameObject);
    }

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        _trailRenderer = GetComponent<TrailRenderer>();
        if (_audio != null)
        {
            _audio.playOnAwake = false;
            _audio.loop = true;
            _audio.Stop();
        }

        EnsureFireGradient();
        ConfigureFireTrail();
        EnsureFireParticleSystems();

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
            if (_cannonLineRenderer != null)
            {
                _cannonLineRenderer.enabled = false;
            }
        }

        if (flyingShellSound != null)
        {
            _audio.clip = flyingShellSound;
        }

        Invoke(nameof(SelfDestruct), Mathf.Max(1f, selfDestructSeconds));
    }

    void OnDestroy()
    {
        if (_ownsTrailMaterial && _trailMaterial != null)
        {
            Destroy(_trailMaterial);
        }

        if (_ownsParticleMaterial && _particleMaterial != null)
        {
            Destroy(_particleMaterial);
        }
    }

    void LateUpdate()
    {
        // Goal-post / target camera only after a clean muzzle clear.
        if (trackingBase != null && _mainCamTransform != null && !_isDestroying && _clearedMuzzle && !_failedClearance)
        {
            _mainCamTransform.position = trackingBase.position + cameraPositionOffset;
            _mainCamTransform.LookAt(transform.position);
        }

        HandleProximitySound();
        UpdateTrailColor();
    }

    void FixedUpdate()
    {
        if (_isDestroying || _failedClearance || _clearedMuzzle || _owningCannon == null)
        {
            return;
        }

        float traveled = Vector3.Distance(transform.position, _fireOrigin);
        if (traveled >= _clearanceDistance)
        {
            _clearedMuzzle = true;
            return;
        }

        if (Time.time - _spawnTime >= _clearanceSeconds && IsOverlappingCannon())
        {
            FailMuzzleClearance();
        }
    }

    private void EnsureFireGradient()
    {
        if (fireGradient != null && fireGradient.colorKeys != null && fireGradient.colorKeys.Length >= 2)
        {
            return;
        }

        fireGradient = new Gradient();
        fireGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.45f), 0f),
                new GradientColorKey(new Color(1f, 0.45f, 0.05f), 0.35f),
                new GradientColorKey(new Color(0.75f, 0.08f, 0f), 0.7f),
                new GradientColorKey(new Color(0.15f, 0.05f, 0.02f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.4f),
                new GradientAlphaKey(0.25f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            });
    }

    private void ConfigureFireTrail()
    {
        if (_trailRenderer == null)
        {
            return;
        }

        _trailRenderer.time = trailTime;
        _trailRenderer.minVertexDistance = 0.05f;
        _trailRenderer.emitting = true;
        _trailRenderer.widthMultiplier = trailWidth;
        _trailRenderer.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.15f);
        _trailRenderer.colorGradient = fireGradient;
        _trailRenderer.material = GetOrCreateTrailMaterial();
        _trailRenderer.textureMode = LineTextureMode.Stretch;
        _trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private Material GetOrCreateTrailMaterial()
    {
        if (_trailMaterial != null)
        {
            return _trailMaterial;
        }

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        _trailMaterial = new Material(shader)
        {
            name = "CannonBallFireTrailMaterial",
            mainTexture = Texture2D.whiteTexture,
            color = new Color(1f, 0.55f, 0.1f, 1f)
        };
        _ownsTrailMaterial = true;

        if (_trailMaterial.HasProperty("_BaseColor"))
        {
            _trailMaterial.SetColor("_BaseColor", new Color(1f, 0.55f, 0.1f, 1f));
        }

        return _trailMaterial;
    }

    private Material GetOrCreateParticleMaterial()
    {
        if (_particleMaterial != null)
        {
            return _particleMaterial;
        }

        _particleMaterial = new Material(GetOrCreateTrailMaterial().shader)
        {
            name = "CannonBallFireParticleMaterial",
            mainTexture = Texture2D.whiteTexture,
            color = new Color(1f, 0.4f, 0.05f, 1f)
        };
        _ownsParticleMaterial = true;
        return _particleMaterial;
    }

    private void EnsureFireParticleSystems()
    {
        _fireParticles = EnsureParticleChild("FireCore", 48, 0.35f, 0.9f, new Color(1f, 0.55f, 0.08f, 0.95f), true);
        _emberParticles = EnsureParticleChild("FireEmbers", 28, 0.08f, 0.35f, new Color(1f, 0.85f, 0.25f, 0.9f), false);

        if (_fireParticles != null)
        {
            _fireParticles.Play(true);
        }

        if (_emberParticles != null)
        {
            _emberParticles.Play(true);
        }
    }

    private ParticleSystem EnsureParticleChild(
        string childName,
        int maxParticles,
        float minSize,
        float maxSize,
        Color tint,
        bool denseCore)
    {
        Transform existing = transform.Find(childName);
        GameObject host = existing != null ? existing.gameObject : new GameObject(childName);
        host.transform.SetParent(transform, false);
        host.transform.localPosition = Vector3.zero;

        ParticleSystem particles = host.GetComponent<ParticleSystem>();
        if (particles == null)
        {
            particles = host.AddComponent<ParticleSystem>();
        }

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(denseCore ? 0.4f : 1.2f, denseCore ? 1.6f : 3.5f);
        main.startColor = tint;
        main.gravityModifier = denseCore ? -0.15f : -0.35f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = denseCore ? 55f : 28f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = denseCore ? 0.12f : 0.18f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = fireGradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(denseCore ? 0.6f : 1.4f);
        // Emit slightly backward relative to flight (local -Z once rigidbody faces forward).
        velocity.z = new ParticleSystem.MinMaxCurve(denseCore ? -1.2f : -2.4f);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetOrCreateParticleMaterial();
            renderer.sortingFudge = -5f;
        }

        return particles;
    }

    private void UpdateTrailColor()
    {
        if (_trailRenderer == null || fireGradient == null)
        {
            return;
        }

        float t = Mathf.PingPong(Time.time * trailFlickerSpeed, 1f);
        Color color = fireGradient.Evaluate(t);
        _trailRenderer.startColor = color;
        _trailRenderer.endColor = new Color(color.r * 0.35f, color.g * 0.15f, color.b * 0.05f, color.a * 0.35f);
    }

    private void HandleProximitySound()
    {
        if (_isDestroying || trackingBase == null || _audio == null || flyingShellSound == null)
        {
            return;
        }

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
        if (_isDestroying || _failedClearance)
        {
            return;
        }

        if (!_clearedMuzzle && collision != null && collision.collider != null
            && _owningCannon != null
            && _owningCannon.IsPartOfCannon(collision.collider.transform))
        {
            // Ignore the instant spawn overlap; only punish real muzzle strikes.
            float traveled = Vector3.Distance(transform.position, _fireOrigin);
            if (Time.time - _spawnTime >= 0.08f || traveled >= 0.35f)
            {
                FailMuzzleClearance();
            }

            return;
        }

        StartCoroutine(ExplosionSequence());
    }

    void SelfDestruct()
    {
        if (!_isDestroying && !_failedClearance)
        {
            StartCoroutine(ExplosionSequence());
        }
    }

    private void FailMuzzleClearance()
    {
        if (_failedClearance || _isDestroying)
        {
            return;
        }

        _failedClearance = true;
        if (_owningCannon != null)
        {
            _owningCannon.NotifyFailedMuzzleClearance(this);
        }
        else
        {
            AbortForCannonDestruction();
        }
    }

    private bool IsOverlappingCannon()
    {
        if (_owningCannon == null)
        {
            return false;
        }

        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider == null)
        {
            return false;
        }

        Collider[] cannonColliders = _owningCannon.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cannonColliders.Length; i++)
        {
            Collider other = cannonColliders[i];
            if (other == null || !other.enabled || other.isTrigger)
            {
                continue;
            }

            if (Physics.ComputePenetration(
                    ballCollider,
                    transform.position,
                    transform.rotation,
                    other,
                    other.transform.position,
                    other.transform.rotation,
                    out _,
                    out float distance)
                && distance > 0.001f)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator ExplosionSequence()
    {
        _isDestroying = true;
        CancelInvoke(nameof(SelfDestruct));

        if (_audio != null)
        {
            _audio.Stop();
        }

        if (_fireParticles != null)
        {
            _fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (_emberParticles != null)
        {
            _emberParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var h in hits)
        {
            var hrb = h.attachedRigidbody;
            if (hrb != null)
            {
                hrb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
            }
        }

        var mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.enabled = false;
        }

        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (explosionSound != null && _audio != null && _audio.enabled)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }
        else
        {
            SpawnFallbackExplosionBurst(transform.position);
        }

        yield return new WaitForSeconds(explosionLingerTime);

        if (!_skipCameraRestore && _mainCamTransform != null)
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

    private void SpawnFallbackExplosionBurst(Vector3 position)
    {
        GameObject burstObject = new GameObject("CannonBallExplosionBurst");
        burstObject.transform.position = position;
        ParticleSystem burst = burstObject.AddComponent<ParticleSystem>();
        // AddComponent may auto-play; stop+clear before changing duration.
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = burst.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 14f);
        main.startColor = new Color(1f, 0.55f, 0.1f, 1f);
        main.gravityModifier = 0.4f;
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = burst.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36, 48) });

        ParticleSystem.ShapeModule shape = burst.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = burst.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = fireGradient;

        ParticleSystemRenderer renderer = burst.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetOrCreateParticleMaterial();
        }

        burst.Play(true);
        Destroy(burstObject, 2.5f);
    }
}
