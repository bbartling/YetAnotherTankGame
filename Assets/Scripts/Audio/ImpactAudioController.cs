using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class ImpactAudioController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip impactClip;

    private void Awake()
    {
        EnsureFallbackClip();
    }

    public void EnsureFallbackClip()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (impactClip == null) impactClip = ProceduralBattlefieldAudio.CreateImpact();

        audioSource.clip = impactClip;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 90f;
    }
}
