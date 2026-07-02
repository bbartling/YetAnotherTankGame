using UnityEngine;

public class TankCombatMechanicalAudio : MonoBehaviour
{
    public TankController tank;
    public float turretSoundThreshold = 0.35f;
    public float barrelSoundThreshold = 0.25f;
    public float soundCooldown = 0.18f;

    private AudioSource _source;
    private float _lastTurretYaw;
    private float _lastBarrelPitch;
    private float _nextTurretSoundTime;
    private float _nextBarrelSoundTime;
    private bool _initialized;

    private void Awake()
    {
        if (tank == null)
        {
            tank = GetComponent<TankController>();
        }

        _source = GetComponent<AudioSource>();
        if (_source == null)
        {
            _source = gameObject.AddComponent<AudioSource>();
        }

        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;
        _source.dopplerLevel = 0f;
    }

    private void LateUpdate()
    {
        if (tank == null)
        {
            tank = GetComponent<TankController>();
        }

        if (tank == null)
        {
            return;
        }

        InitializeAngles();
        TrackTurretMotion();
        TrackBarrelMotion();
    }

    private void InitializeAngles()
    {
        if (_initialized)
        {
            return;
        }

        _lastTurretYaw = ReadTurretYaw();
        _lastBarrelPitch = ReadBarrelPitch();
        _initialized = true;
    }

    private void TrackTurretMotion()
    {
        float yaw = ReadTurretYaw();
        float delta = Mathf.Abs(Mathf.DeltaAngle(_lastTurretYaw, yaw));
        _lastTurretYaw = yaw;

        if (delta < turretSoundThreshold || Time.time < _nextTurretSoundTime)
        {
            return;
        }

        PlayClip(TankEngineAudioLibrary.TurretRotate, 0.42f);
        _nextTurretSoundTime = Time.time + soundCooldown;
    }

    private void TrackBarrelMotion()
    {
        float pitch = ReadBarrelPitch();
        float delta = Mathf.Abs(pitch - _lastBarrelPitch);
        _lastBarrelPitch = pitch;

        if (delta < barrelSoundThreshold || Time.time < _nextBarrelSoundTime)
        {
            return;
        }

        PlayClip(TankEngineAudioLibrary.BarrelAdjust, 0.38f);
        _nextBarrelSoundTime = Time.time + soundCooldown;
    }

    private float ReadTurretYaw()
    {
        if (tank != null && tank.turretYawPivot != null)
        {
            return tank.turretYawPivot.localEulerAngles.y;
        }

        return 0f;
    }

    private float ReadBarrelPitch()
    {
        if (tank != null && tank.barrelPitchPivot != null)
        {
            return tank.barrelPitchPivot.localEulerAngles.x;
        }

        return 0f;
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null || _source == null)
        {
            return;
        }

        _source.PlayOneShot(clip, volume);
    }
}
