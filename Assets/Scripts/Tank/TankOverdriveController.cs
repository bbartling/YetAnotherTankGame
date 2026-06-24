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
    private GameObject _exhaustSmoke;
    private float _releaseGraceTimer;
    private bool _boostUnlockedThisHold;

    private void Awake()
    {
        _drive = GetComponent<TankDriveController>();
        _tank = GetComponent<TankController>();
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

    private void ApplyMultiplierState()
    {
        if (!allowOverdrive || !IsOverdriveActive)
        {
            SpeedMultiplier = 1f;
            AccelerationMultiplier = 1f;
            ClimbSlopeMultiplier = 1f;
            WheelVisualMultiplier = 1f;
            TerrainAssistStrength = 0f;
            IsWheelSpinOut = false;
            return;
        }

        SpeedMultiplier = TankGameplayTuning.OverdriveSpeedMultiplier;
        AccelerationMultiplier = TankGameplayTuning.OverdriveAccelerationMultiplier;
        ClimbSlopeMultiplier = TankGameplayTuning.OverdriveClimbMultiplier;
        WheelVisualMultiplier = TankGameplayTuning.OverdriveWheelVisualMultiplier;
        TerrainAssistStrength = 1f;

        if (_drive != null && _tank != null)
        {
            _drive.ReadInput(out float throttle, out _, out _);
            IsWheelSpinOut = throttle > 0.25f
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
    }

    private void BeginOverdrive()
    {
        IsOverdriveActive = true;
        DisplayRpm = TankGameplayTuning.RpmRedline;
        RpmRatio = 1f;
        ChargeRatio = 1f;
        _releaseGraceTimer = TankGameplayTuning.OverdriveReleaseGraceSeconds;
        ApplyOverdriveBurst();
        PlayExhaustBurst();
    }

    private void ApplyOverdriveBurst()
    {
        if (_tank == null)
        {
            return;
        }

        Rigidbody body = _tank.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        Vector3 forward = _tank.transform.forward;
        float impulse = TankGameplayTuning.OverdriveBurstPush * body.mass;
        body.AddForce(forward * impulse, ForceMode.Impulse);
    }

    private void EndOverdrive()
    {
        IsOverdriveActive = false;
        StopExhaustBurst();
        ApplyMultiplierState();
    }

    private void PlayExhaustBurst()
    {
        Transform anchor = exhaustPoint != null ? exhaustPoint : transform;
        if (_exhaustSmoke == null)
        {
            _exhaustSmoke = BattlefieldEffectController.CreateLoopingEffect(
                anchor,
                "TankOverdriveSmoke",
                new Color(0.55f, 0.55f, 0.55f, 0.85f),
                48,
                1.6f,
                1.4f);
            ParticleSystem particles = _exhaustSmoke.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                ParticleSystem.MainModule main = particles.main;
                main.startSpeed = 4.5f;
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTime = 42f;
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.angle = 28f;
                shape.radius = 0.55f;
            }
        }

        _exhaustSmoke.transform.SetParent(anchor, false);
        _exhaustSmoke.transform.localPosition = Vector3.zero;
        _exhaustSmoke.SetActive(true);
        ParticleSystem burst = _exhaustSmoke.GetComponent<ParticleSystem>();
        burst?.Play(true);
    }

    private void StopExhaustBurst()
    {
        if (_exhaustSmoke == null)
        {
            return;
        }

        ParticleSystem burst = _exhaustSmoke.GetComponent<ParticleSystem>();
        burst?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        _exhaustSmoke.SetActive(false);
    }
}
