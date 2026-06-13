using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class EnemyAudioController : MonoBehaviour
{
    public AudioSource audioSource;
    public float maxAudibleDistance = 55f;

    private void Awake()
    {
        ConfigureDistanceRolloff();
    }

    public void ConfigureDistanceRolloff()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 5f;
        audioSource.maxDistance = Mathf.Min(maxAudibleDistance, 60f);
    }
}
