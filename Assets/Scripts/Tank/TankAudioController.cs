using UnityEngine;

public class TankAudioController : MonoBehaviour
{
    public AudioSource engineSource;
    public float idlePitch = 0.72f;
    public float strainedPitch = 1.05f;
    public float idleVolume = 0.25f;
    public float strainedVolume = 0.7f;

    private void Awake()
    {
        if (engineSource == null)
        {
            engineSource = GetComponent<AudioSource>();
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
