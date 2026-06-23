using UnityEngine;

public class TankAudioController : MonoBehaviour
{
    public AudioSource engineSource;
    public float idlePitch = 0.72f;
    public float strainedPitch = 1.05f;
    public float idleVolume = 0.42f;
    public float strainedVolume = 0.9f;

    private void Awake()
    {
        if (engineSource == null)
        {
            engineSource = GetComponent<AudioSource>();
        }
        EnsureFallbackClips();
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
        engineSource.loop = true;
        engineSource.pitch = Mathf.Lerp(idlePitch, strainedPitch, strain);
        engineSource.volume = Mathf.Lerp(idleVolume, strainedVolume, strain);
        if (!engineSource.isPlaying)
        {
            engineSource.Play();
        }
    }
}
