using UnityEngine;

public static class ProceduralBattlefieldAudio
{
    public static AudioClip CreateEngineLoop()
    {
        const int sampleRate = 11025;
        float[] samples = new float[sampleRate];
        for (int i = 0; i < samples.Length; i++)
        {
            float time = i / (float)sampleRate;
            samples[i] = Mathf.Sin(time * Mathf.PI * 2f * 48f) * 0.16f +
                         Mathf.Sin(time * Mathf.PI * 2f * 72f) * 0.08f;
        }

        AudioClip clip = AudioClip.Create("GeneratedTankEngine", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CreateShellWhistle()
    {
        return CreateTone("GeneratedShellWhistle", 0.45f, 520f, 960f, 0.12f);
    }

    public static AudioClip CreateCannonBoom()
    {
        return CreateTone("GeneratedCannonBoom", 0.55f, 78f, 34f, 0.32f);
    }

    public static AudioClip CreateImpact()
    {
        return CreateTone("GeneratedImpact", 0.35f, 145f, 52f, 0.24f);
    }

    private static AudioClip CreateTone(string name, float duration, float startFrequency, float endFrequency, float volume)
    {
        const int sampleRate = 11025;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float progress = i / (float)sampleCount;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            float envelope = 1f - progress;
            samples[i] = Mathf.Sin(progress * duration * Mathf.PI * 2f * frequency) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
