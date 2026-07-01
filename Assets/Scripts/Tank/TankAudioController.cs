using UnityEngine;

public class TankAudioController : MonoBehaviour
{
    public AudioSource engineSource;
    public float idlePitch = 0.72f;
    public float strainedPitch = 1.05f;
    public float idleVolume = 0.42f;
    public float strainedVolume = 0.9f;

    private AudioSource _oneShotSource;
    private bool _engineStarted;

    private void Awake()
    {
        idleVolume = Mathf.Max(idleVolume, 0.42f);
        strainedVolume = Mathf.Max(strainedVolume, idleVolume + 0.08f);
        EnsureEngineAudioSources();
        EnsureFallbackClips();
    }

    public void EnsureEngineAudioSources()
    {
        engineSource = ResolveEngineSource(engineSource);
        _oneShotSource = ResolveOneShotSource(_oneShotSource);
    }

    public void EnsureFallbackClips()
    {
        if (engineSource == null)
        {
            return;
        }

        engineSource.playOnAwake = false;
        engineSource.loop = true;
        engineSource.spatialBlend = 0f;
        engineSource.dopplerLevel = 0f;
        engineSource.minDistance = 1f;
        engineSource.maxDistance = 80f;

        AudioClip runningLoop = TankEngineAudioLibrary.RunningLoop;
        if (runningLoop != null)
        {
            engineSource.clip = runningLoop;
            return;
        }

        if (engineSource.clip == null)
        {
            engineSource.clip = ProceduralBattlefieldAudio.CreateEngineLoop();
        }
    }

    public void SetEngineStrain(float strain)
    {
        if (engineSource == null || engineSource.clip == null)
        {
            return;
        }

        strain = Mathf.Clamp01(strain);
        float overdriveBoost = 0f;
        TankOverdriveController overdrive = GetComponent<TankOverdriveController>();
        if (overdrive != null)
        {
            overdriveBoost = overdrive.IsOverdriveActive
                ? 0.35f
                : overdrive.ChargeRatio * 0.18f;
        }

        float audibleStrain = Mathf.Clamp01(Mathf.Max(strain, 0.06f) + overdriveBoost);
        engineSource.loop = true;
        engineSource.pitch = Mathf.Lerp(idlePitch, strainedPitch, audibleStrain);
        engineSource.volume = Mathf.Lerp(idleVolume, strainedVolume, audibleStrain);

        if (!_engineStarted && audibleStrain > 0.05f)
        {
            TryPlayEngineStart();
        }

        if (!engineSource.isPlaying)
        {
            engineSource.Play();
        }
    }

    public void StopEngine()
    {
        if (engineSource != null && engineSource.isPlaying)
        {
            engineSource.Stop();
        }

        AudioClip stopClip = TankEngineAudioLibrary.StopOneShot;
        if (stopClip != null && _oneShotSource != null)
        {
            _oneShotSource.PlayOneShot(stopClip, idleVolume);
        }

        _engineStarted = false;
    }

    private void TryPlayEngineStart()
    {
        _engineStarted = true;
        AudioClip startClip = TankEngineAudioLibrary.StartOneShot;
        if (startClip == null || _oneShotSource == null)
        {
            return;
        }

        _oneShotSource.PlayOneShot(startClip, idleVolume);
    }

    private AudioSource ResolveEngineSource(AudioSource preferred)
    {
        if (preferred != null && HasEngineClip(preferred))
        {
            return preferred;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null && HasEngineClip(sources[i]))
            {
                return sources[i];
            }
        }

        if (preferred != null)
        {
            return preferred;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null && sources[i].clip == null && !sources[i].isPlaying)
            {
                return sources[i];
            }
        }

        return CreateDedicatedEngineSource();
    }

    private AudioSource ResolveOneShotSource(AudioSource preferred)
    {
        if (preferred != null)
        {
            ConfigureOneShotSource(preferred);
            return preferred;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource candidate = sources[i];
            if (candidate != null && candidate != engineSource)
            {
                ConfigureOneShotSource(candidate);
                return candidate;
            }
        }

        AudioSource created = gameObject.AddComponent<AudioSource>();
        ConfigureOneShotSource(created);
        return created;
    }

    private AudioSource CreateDedicatedEngineSource()
    {
        Transform existing = transform.Find("TankEngineAudio");
        GameObject host = existing != null ? existing.gameObject : new GameObject("TankEngineAudio");
        if (existing == null)
        {
            host.transform.SetParent(transform, false);
        }

        AudioSource source = host.GetComponent<AudioSource>();
        if (source == null)
        {
            source = host.AddComponent<AudioSource>();
        }

        return source;
    }

    private static bool HasEngineClip(AudioSource source)
    {
        if (source == null || source.clip == null)
        {
            return false;
        }

        string clipName = source.clip.name.ToLowerInvariant();
        return clipName.Contains("engine");
    }

    private static void ConfigureOneShotSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
    }
}
