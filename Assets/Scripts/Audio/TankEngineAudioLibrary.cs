using UnityEngine;

public static class TankEngineAudioLibrary
{
    private const string ResourceRoot = "Audio/tank_sounds/";

    private static AudioClip _running;
    private static AudioClip _start;
    private static AudioClip _stop;

    public static AudioClip RunningLoop => Load(ref _running, "engine_running");
    public static AudioClip StartOneShot => Load(ref _start, "engine_start");
    public static AudioClip StopOneShot => Load(ref _stop, "engine_stop");

    private static AudioClip Load(ref AudioClip cache, string clipName)
    {
        if (cache != null)
        {
            return cache;
        }

        cache = Resources.Load<AudioClip>(ResourceRoot + clipName);
        return cache;
    }
}
