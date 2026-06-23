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
            float putt = Mathf.Repeat(time * 6f, 1f);
            float pulse = Mathf.Exp(-putt * 7.5f);
            float wobble = Mathf.Sin(time * Mathf.PI * 2f * 7f) * 0.035f;
            samples[i] = Mathf.Sin(time * Mathf.PI * 2f * 42f) * 0.12f +
                         Mathf.Sin(time * Mathf.PI * 2f * 68f) * 0.07f +
                         Mathf.Sin(time * Mathf.PI * 2f * 128f) * pulse * 0.11f +
                         wobble;
        }

        AudioClip clip = AudioClip.Create("GeneratedSillyTankEngine", samples.Length, 1, sampleRate, false);
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

    public static AudioClip CreateCannonballExplosion()
    {
        return CreateBurst("GeneratedCannonballExplosion", 0.62f, 96f, 30f, 0.42f, 0.18f, 17f);
    }

    public static AudioClip CreateTankExplosion()
    {
        return CreateBurst("GeneratedSillyTankExplosion", 0.95f, 72f, 22f, 0.52f, 0.24f, 11f);
    }

    public static AudioClip CreateTreeFlatten()
    {
        return CreateBurst("GeneratedTreeFlattenCrack", 0.42f, 180f, 58f, 0.34f, 0.16f, 23f);
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

    private static AudioClip CreateBurst(string name, float duration, float startFrequency, float endFrequency, float volume, float noiseAmount, float wobbleRate)
    {
        const int sampleRate = 11025;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        uint noise = 2166136261u;

        for (int i = 0; i < sampleCount; i++)
        {
            float progress = i / (float)sampleCount;
            float envelope = Mathf.Pow(1f - progress, 1.65f);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            float wobble = 1f + Mathf.Sin(progress * Mathf.PI * 2f * wobbleRate) * 0.08f;
            noise ^= (uint)(i + 374761393);
            noise *= 16777619u;
            float noiseSample = ((noise & 1023u) / 511.5f) - 1f;
            float tone = Mathf.Sin(progress * duration * Mathf.PI * 2f * frequency * wobble);
            samples[i] = Mathf.Clamp((tone * (1f - noiseAmount) + noiseSample * noiseAmount) * envelope * volume, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
