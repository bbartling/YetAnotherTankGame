using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class ProjectileAudioController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip whistleClip;

    private void Awake()
    {
        EnsureFallbackClip();
    }

    public void EnsureFallbackClip()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (whistleClip == null)
        {
            whistleClip = ProceduralBattlefieldAudio.CreateShellWhistle();
        }

        audioSource.clip = whistleClip;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 75f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }
}
