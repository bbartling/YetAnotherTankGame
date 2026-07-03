using UnityEngine;

[DisallowMultipleComponent]
public class TankOverdriveController : MonoBehaviour
{
    public bool allowOverdrive = true;
    public Transform exhaustPoint;

    public float SpeedMultiplier { get; private set; } = 1f;
    public float AccelerationMultiplier { get; private set; } = 1f;
    public float ClimbSlopeMultiplier { get; private set; } = 1f;
    public float WheelVisualMultiplier { get; private set; } = 1f;
    public float TerrainAssistStrength { get; private set; }
    public float DisplayRpm { get; private set; } = TankGameplayTuning.RpmIdle;
    public float RpmRatio { get; private set; }
    public float ChargeRatio { get; private set; }
    public bool IsOverdriveActive { get; private set; }
    public bool IsWheelSpinOut { get; private set; }
    public float ForwardHoldSeconds { get; private set; }

    private static readonly Color SmokeBlack = new Color(0.08f, 0.08f, 0.08f, 0.95f);
    private static readonly Color SmokeDarkGray = new Color(0.18f, 0.18f, 0.18f, 0.55f);

    private TankDriveController _drive;
    private TankController _tank;
    private WheeledSuspensionController _suspension;
    private ParticleSystem _burstSmoke;
    private Material _smokeMaterial;
    private float _releaseGraceTimer;
    private bool _boostUnlockedThisHold;

    private void Awake()
    {
        _drive = GetComponent<TankDriveController>();
        _tank = GetComponent<TankController>();
        _suspension = GetComponent<WheeledSuspensionController>();
        if (exhaustPoint == null)
        {
            exhaustPoint = TankOverdriveSetup.EnsureExhaustPoint(transform);
        }

        DisplayRpm = TankGameplayTuning.RpmIdle;
        ApplyMultiplierState();
    }

    private void OnDestroy()
    {
        if (_smokeMaterial != null)
        {
            Destroy(_smokeMaterial);
            _smokeMaterial = null;
        }
    }

    private void Update()
    {
        if (!allowOverdrive || _drive == null)
        {
            DisplayRpm = TankGameplayTuning.RpmIdle;
            RpmRatio = 0f;
            ChargeRatio = 0f;
            IsOverdriveActive = false;
            ApplyMultiplierState();
            return;
        }

        _drive.ReadInput(out float throttle, out _, out _);
        bool holdingForward = throttle > 0.25f;

        if (!holdingForward)
        {
            ForwardHoldSeconds = 0f;
            _boostUnlockedThisHold = false;
            ChargeRatio = 0f;
            DisplayRpm = Mathf.MoveTowards(DisplayRpm, TankGameplayTuning.RpmIdle, Time.deltaTime * 2400f);
            RpmRatio = Mathf.InverseLerp(TankGameplayTuning.RpmIdle, TankGameplayTuning.RpmRedline, DisplayRpm);

            if (IsOverdriveActive)
            {
                _releaseGraceTimer -= Time.deltaTime;
                if (_releaseGraceTimer <= 0f)
                {
                    EndOverdrive();
                }
            }

            ApplyMultiplierState();
            return;
        }

        ForwardHoldSeconds += Time.deltaTime;
        ChargeRatio = Mathf.Clamp01(ForwardHoldSeconds / Mathf.Max(0.1f, TankGameplayTuning.OverdriveHoldSeconds));
        DisplayRpm = Mathf.Lerp(TankGameplayTuning.RpmIdle, TankGameplayTuning.RpmRedline, ChargeRatio);
        RpmRatio = ChargeRatio;

        if (!_boostUnlockedThisHold && ChargeRatio >= 1f)
        {
            _boostUnlockedThisHold = true;
            BeginOverdrive();
        }

        if (IsOverdriveActive)
        {
            _releaseGraceTimer = TankGameplayTuning.OverdriveReleaseGraceSeconds;
        }

        ApplyMultiplierState();
    }

    private void ApplyMultiplierState()
    {
        if (!allowOverdrive)
        {
            SpeedMultiplier = 1f;
            AccelerationMultiplier = 1f;
            ClimbSlopeMultiplier = 1f;
            WheelVisualMultiplier = 1f;
            TerrainAssistStrength = 0f;
            IsWheelSpinOut = false;
            return;
        }

        if (IsOverdriveActive)
        {
            SpeedMultiplier = TankGameplayTuning.OverdriveSpeedMultiplier;
            AccelerationMultiplier = TankGameplayTuning.OverdriveAccelerationMultiplier;
            ClimbSlopeMultiplier = TankGameplayTuning.OverdriveClimbMultiplier;
            WheelVisualMultiplier = TankGameplayTuning.OverdriveWheelVisualMultiplier;
            TerrainAssistStrength = 1f;

            if (_drive != null && _tank != null)
            {
                _drive.ReadInput(out float throttle, out _, out _);
                IsWheelSpinOut = throttle > 0.25f
                    && _tank.IsGrounded
                    && _tank.CurrentGroundSpeed < TankGameplayTuning.OverdriveStuckSpeedThreshold;
            }
            else
            {
                IsWheelSpinOut = false;
            }

            if (IsWheelSpinOut)
            {
                ClimbSlopeMultiplier = TankGameplayTuning.OverdriveClimbMultiplier * TankGameplayTuning.OverdriveStuckClimbMultiplier;
            }

            return;
        }

        float charge = ChargeRatio;
        SpeedMultiplier = Mathf.Lerp(1f, TankGameplayTuning.OverdriveChargeSpeedMultiplier, charge);
        AccelerationMultiplier = Mathf.Lerp(1f, TankGameplayTuning.OverdriveChargeAccelerationMultiplier, charge);
        ClimbSlopeMultiplier = Mathf.Lerp(1f, TankGameplayTuning.OverdriveChargeClimbMultiplier, charge);
        WheelVisualMultiplier = Mathf.Lerp(1f, 1.4f, charge);
        TerrainAssistStrength = charge * 0.25f;
        IsWheelSpinOut = false;
    }

    private void BeginOverdrive()
    {
        IsOverdriveActive = true;
        DisplayRpm = TankGameplayTuning.RpmRedline;
        RpmRatio = 1f;
        ChargeRatio = 1f;
        _releaseGraceTimer = TankGameplayTuning.OverdriveReleaseGraceSeconds;
        ApplyOverdriveBurst();
        PlayRandomBlackSmokePuffs();
    }

    private void ApplyOverdriveBurst()
    {
        if (_tank == null || !_tank.IsGrounded)
        {
            return;
        }

        Rigidbody body = _tank.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(_tank.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float impulse = TankGameplayTuning.OverdriveBurstPush * body.mass;
        body.AddForce(forward * impulse, ForceMode.Impulse);
    }

    private void EndOverdrive()
    {
        IsOverdriveActive = false;
        ApplyMultiplierState();
    }

    /// <summary>
    /// A few random black exhaust puffs from the rear when speed/climb overdrive unlocks.
    /// Smoke only — no fire. Explicit black tint so Unity never shows pink missing-material particles.
    /// </summary>
    private void PlayRandomBlackSmokePuffs()
    {
        if (exhaustPoint == null)
        {
            exhaustPoint = TankOverdriveSetup.EnsureExhaustPoint(transform);
        }

        Transform anchor = exhaustPoint != null ? exhaustPoint : transform;
        if (_burstSmoke == null)
        {
            _burstSmoke = CreateBlackSmokePuffSystem(anchor);
        }

        _burstSmoke.transform.SetParent(anchor, false);
        _burstSmoke.transform.localRotation = Quaternion.identity;
        _burstSmoke.gameObject.SetActive(true);

        ParticleSystemRenderer renderer = _burstSmoke.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetBlackSmokeMaterial();
        }

        _burstSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _burstSmoke.Clear(true);

        int puffCount = Random.Range(2, 5);
        for (int i = 0; i < puffCount; i++)
        {
            _burstSmoke.transform.localPosition = new Vector3(
                Random.Range(-0.35f, 0.35f),
                Random.Range(-0.1f, 0.25f),
                Random.Range(-0.15f, 0.15f));

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
            {
                startColor = SmokeBlack,
                startSize = Random.Range(1.6f, 4.4f),
                startLifetime = Random.Range(1.2f, 2.4f),
                velocity = anchor.TransformDirection(new Vector3(
                    Random.Range(-0.8f, 0.8f),
                    Random.Range(0.4f, 1.6f),
                    Random.Range(1.5f, 4.5f)))
            };
            _burstSmoke.Emit(emit, Random.Range(8, 22));
        }

        _burstSmoke.transform.localPosition = Vector3.zero;
    }

    private ParticleSystem CreateBlackSmokePuffSystem(Transform anchor)
    {
        GameObject effect = new GameObject("TankOverdriveBlackSmokePuff");
        effect.transform.SetParent(anchor, false);

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = 96;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.4f, 3.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
        main.startColor = SmokeBlack;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.35f;
        // ExhaustPoint faces rear (yaw 180); emit along local +Z out the back.
        shape.rotation = Vector3.zero;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.55f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 1.7f)));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(SmokeBlack, 0f),
                new GradientColorKey(SmokeDarkGray, 0.45f),
                new GradientColorKey(new Color(0.28f, 0.28f, 0.28f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.55f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        // All velocity axes must use the same MinMaxCurve mode or Unity spam-logs
        // "Particle Velocity curves must all be in the same mode".
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(2.5f);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetBlackSmokeMaterial();
            renderer.sortingFudge = -10f;
        }

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    private Material GetBlackSmokeMaterial()
    {
        if (_smokeMaterial != null)
        {
            return _smokeMaterial;
        }

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        _smokeMaterial = new Material(shader);
        _smokeMaterial.name = "TankOverdriveBlackSmokeMaterial";
        _smokeMaterial.mainTexture = Texture2D.whiteTexture;
        ApplySmokeColor(_smokeMaterial, SmokeBlack);

        // Never leave emission/tint at default pink-missing values.
        if (_smokeMaterial.HasProperty("_EmissionColor"))
        {
            _smokeMaterial.SetColor("_EmissionColor", Color.black);
            _smokeMaterial.DisableKeyword("_EMISSION");
        }

        return _smokeMaterial;
    }

    private static void ApplySmokeColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", color);
        }

        material.color = color;
    }
}
