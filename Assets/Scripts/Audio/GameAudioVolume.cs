using UnityEngine;

/// <summary>
/// In-game master volume (Unity audio only — does not change OS volume).
/// </summary>
public static class GameAudioVolume
{
    private const string PrefKey = "SillyTankGameVolume";
    private const float DefaultVolume = 0.85f;

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(PrefKey, DefaultVolume);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefKey, clamped);
            PlayerPrefs.Save();
            Apply(clamped);
        }
    }

    public static void LoadAndApply()
    {
        Apply(MasterVolume);
    }

    public static void Apply(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }
}
