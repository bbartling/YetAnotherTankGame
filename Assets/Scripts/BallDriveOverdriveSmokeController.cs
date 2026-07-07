using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallDriveOverdriveSmokeController : MonoBehaviour
{
    public Transform exhaustPoint;
    public bool allowSmokePuff = true;

    private static readonly Color SmokeBlack = new Color(0.08f, 0.08f, 0.08f, 0.95f);
    private static readonly Color SmokeDarkGray = new Color(0.18f, 0.18f, 0.18f, 0.55f);

    private ParticleSystem _burstSmoke;
    private Material _smokeMaterial;
    private float _forwardHoldSeconds;
    private bool _puffedThisHold;

    private void Awake()
    {
        EnsureExhaustPoint();
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
        if (!allowSmokePuff)
        {
            _forwardHoldSeconds = 0f;
            _puffedThisHold = false;
            return;
        }

        bool holdingForward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        if (!holdingForward)
        {
            _forwardHoldSeconds = 0f;
            _puffedThisHold = false;
            return;
        }

        _forwardHoldSeconds += Time.deltaTime;
        if (!_puffedThisHold && _forwardHoldSeconds >= TankGameplayTuning.OverdriveHoldSeconds)
        {
            _puffedThisHold = true;
            PlayRandomBlackSmokePuffs();
        }
    }

    public void EnsureExhaustPoint()
    {
        if (exhaustPoint != null)
        {
            return;
        }

        BallDriveTankVisualFollower follower = GetComponent<BallDriveTankVisualFollower>();
        Transform parent = follower != null && follower.visualRoot != null ? follower.visualRoot : transform;
        Transform existing = parent.Find("ExhaustPoint");
        if (existing != null)
        {
            exhaustPoint = existing;
            return;
        }

        GameObject exhaust = new GameObject("ExhaustPoint");
        exhaust.transform.SetParent(parent, false);
        exhaust.transform.localPosition = new Vector3(0f, 0.65f, -2.35f);
        exhaust.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        exhaustPoint = exhaust.transform;
    }

    public void PlayRandomBlackSmokePuffs()
    {
        EnsureExhaustPoint();
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

        int puffCount = Random.Range(
            TankGameplayTuning.OverdriveSmokeMinPuffs,
            TankGameplayTuning.OverdriveSmokeMaxPuffsExclusive);
        for (int i = 0; i < puffCount; i++)
        {
            _burstSmoke.transform.localPosition = new Vector3(
                Random.Range(-0.35f, 0.35f),
                Random.Range(-0.1f, 0.25f),
                Random.Range(-0.15f, 0.15f));

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
            {
                startColor = SmokeBlack,
                startSize = Random.Range(
                    TankGameplayTuning.OverdriveSmokeMinStartSize,
                    TankGameplayTuning.OverdriveSmokeMaxStartSize),
                startLifetime = Random.Range(
                    TankGameplayTuning.OverdriveSmokeMinLifetime,
                    TankGameplayTuning.OverdriveSmokeMaxLifetime),
                velocity = anchor.TransformDirection(new Vector3(
                    Random.Range(-1.6f, 1.6f),
                    Random.Range(0.8f, 3.2f),
                    Random.Range(3f, 9f)))
            };
            _burstSmoke.Emit(
                emit,
                Random.Range(
                    TankGameplayTuning.OverdriveSmokeMinParticlesPerPuff,
                    TankGameplayTuning.OverdriveSmokeMaxParticlesPerPuffExclusive));
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
        main.maxParticles = TankGameplayTuning.OverdriveSmokeMaxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(4.8f, 7.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
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
        shape.radius = 0.7f;
        shape.rotation = Vector3.zero;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.65f),
            new Keyframe(0.25f, 1.15f),
            new Keyframe(1f, 2.2f)));

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

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(1.6f);
        velocity.z = new ParticleSystem.MinMaxCurve(5f);

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
