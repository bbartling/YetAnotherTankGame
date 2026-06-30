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

    private TankDriveController _drive;
    private TankController _tank;
    private WheeledSuspensionController _suspension;
    private GameObject _exhaustSmoke;
    private Material _smokeMaterial;
    private float _releaseGraceTimer;
    private bool _boostUnlockedThisHold;
    private float _lastBumpSmokeTime;

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

    private void FixedUpdate()
    {
        if (!IsOverdriveActive || _suspension == null)
        {
            return;
        }

        TryPlayOverdriveBumpSmoke();
    }

    private void TryPlayOverdriveBumpSmoke()
    {
        if (!IsOverdriveActive || _suspension == null)
        {
            return;
        }

        if (_suspension.RecentBumpStrength < 0.012f)
        {
            return;
        }

        if (Time.time - _lastBumpSmokeTime < 0.18f)
        {
            return;
        }

        _lastBumpSmokeTime = Time.time;
        PlayBlackSmokePuff(Mathf.Clamp(_suspension.RecentBumpStrength * 18f, 12f, 28f), 0.75f);
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
        PlayBlackSmokePuff(56f, 1f);
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

    private void PlayBlackSmokePuff(float emitCount, float sizeScale)
    {
        if (exhaustPoint == null)
        {
            exhaustPoint = TankOverdriveSetup.EnsureExhaustPoint(transform);
        }

        Transform anchor = exhaustPoint != null ? exhaustPoint : transform;
        if (_exhaustSmoke == null)
        {
            _exhaustSmoke = CreateBlackSmokePuffSystem(anchor);
        }

        _exhaustSmoke.transform.SetParent(anchor, false);
        _exhaustSmoke.transform.localPosition = Vector3.zero;
        _exhaustSmoke.transform.localRotation = Quaternion.identity;
        _exhaustSmoke.SetActive(true);

        ParticleSystem burst = _exhaustSmoke.GetComponent<ParticleSystem>();
        if (burst == null)
        {
            return;
        }

        ParticleSystem.MainModule main = burst.main;
        main.startSize = 3.6f * sizeScale;

        burst.Clear(true);
        burst.Emit(Mathf.RoundToInt(emitCount));
    }

    private GameObject CreateBlackSmokePuffSystem(Transform anchor)
    {
        GameObject effect = new GameObject("TankOverdriveSmoke");
        effect.transform.SetParent(anchor, false);

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = 72;
        main.startLifetime = 3.1f;
        main.startSize = 3.6f;
        main.startSpeed = 6.5f;
        main.startColor = new Color(0.12f, 0.12f, 0.12f, 0.92f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0.22f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.55f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 1.55f)));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 0f),
                new GradientColorKey(new Color(0.16f, 0.16f, 0.16f), 0.35f),
                new GradientColorKey(new Color(0.22f, 0.22f, 0.22f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.5f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetBlackSmokeMaterial();
        }

        return effect;
    }

    private Material GetBlackSmokeMaterial()
    {
        if (_smokeMaterial != null)
        {
            return _smokeMaterial;
        }

        _smokeMaterial = BattlefieldEffectController.CreateTintedParticleMaterial(
            "TankOverdriveBlackSmoke",
            new Color(0.12f, 0.12f, 0.12f, 0.95f));

        if (_smokeMaterial.HasProperty("_EmissionColor"))
        {
            _smokeMaterial.SetColor("_EmissionColor", Color.black);
        }

        return _smokeMaterial;
    }
}
