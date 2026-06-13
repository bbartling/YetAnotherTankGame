#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class BattlefieldPresentationPlayModeTests
{
    [Test]
    public void TankVisualAnimator_RecoilsAndReturnsTowardRest()
    {
        GameObject root = new GameObject("VisualTank");
        GameObject barrel = new GameObject("Barrel");
        barrel.transform.SetParent(root.transform, false);
        GameObject antenna = new GameObject("Antenna");
        antenna.transform.SetParent(root.transform, false);
        TankVisualAnimator animator = root.AddComponent<TankVisualAnimator>();
        animator.barrel = barrel.transform;
        animator.antenna = antenna.transform;
        animator.CaptureRestPose();

        animator.TriggerRecoil();
        animator.TickVisuals(0.05f, 1f, 0.6f);
        Assert.That(barrel.transform.localPosition.z, Is.LessThan(0f));
        Assert.That(Quaternion.Angle(Quaternion.identity, antenna.transform.localRotation), Is.GreaterThan(0f));

        for (int i = 0; i < 60; i++)
        {
            animator.TickVisuals(0.05f, 0f, 0f);
        }
        Assert.That(barrel.transform.localPosition.magnitude, Is.LessThan(0.05f));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void BattlefieldAudioLimiter_CapsSimultaneousVoices()
    {
        BattlefieldAudioLimiter limiter = new BattlefieldAudioLimiter(2);

        Assert.That(limiter.TryAcquire(), Is.True);
        Assert.That(limiter.TryAcquire(), Is.True);
        Assert.That(limiter.TryAcquire(), Is.False);
        limiter.Release();
        Assert.That(limiter.TryAcquire(), Is.True);
    }

    [Test]
    public void TankAudioController_GeneratesReplaceableFallbackEngineClip()
    {
        GameObject root = new GameObject("AudioTank");
        root.AddComponent<AudioSource>();
        TankAudioController audio = root.AddComponent<TankAudioController>();

        audio.EnsureFallbackClips();

        Assert.That(audio.engineSource.clip, Is.Not.Null);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void InstalledModel_ActivatesAuthoredDamageVariants()
    {
        GameObject root = new GameObject("VariantTank");
        SillyModelInstaller installer = SillyModelInstaller.Ensure(root, "Models/Tanks/SillyPlayerTank", 1f, false);

        installer.ApplyDamageState(DamageStateController.State.BurningDisabled);

        Assert.That(installer.InstalledVisual.transform.Find("Hull").gameObject.activeSelf, Is.False);
        Assert.That(installer.InstalledVisual.transform.Find("Hull_Damaged").gameObject.activeSelf, Is.True);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void AudioControllers_CreateFallbackClipsAndLimitEnemyRange()
    {
        GameObject projectileObject = new GameObject("ProjectileAudio");
        projectileObject.AddComponent<AudioSource>();
        ProjectileAudioController projectile = projectileObject.AddComponent<ProjectileAudioController>();
        projectile.EnsureFallbackClip();
        Assert.That(projectile.audioSource.clip, Is.Not.Null);

        GameObject enemyObject = new GameObject("EnemyAudio");
        enemyObject.AddComponent<AudioSource>();
        EnemyAudioController enemy = enemyObject.AddComponent<EnemyAudioController>();
        enemy.ConfigureDistanceRolloff();
        Assert.That(enemy.audioSource.maxDistance, Is.LessThanOrEqualTo(60f));

        Object.DestroyImmediate(projectileObject);
        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void TankVisualAnimator_RotatesTrackVisualsWhileMoving()
    {
        GameObject root = new GameObject("TrackTank");
        GameObject leftTrack = new GameObject("LeftTrack");
        GameObject rightTrack = new GameObject("RightTrack");
        leftTrack.transform.SetParent(root.transform, false);
        rightTrack.transform.SetParent(root.transform, false);
        TankVisualAnimator animator = root.AddComponent<TankVisualAnimator>();
        animator.leftTrack = leftTrack.transform;
        animator.rightTrack = rightTrack.transform;
        Quaternion initial = leftTrack.transform.localRotation;

        animator.TickVisuals(0.1f, 2f, 0f);

        Assert.That(Quaternion.Angle(initial, leftTrack.transform.localRotation), Is.GreaterThan(0f));
        Assert.That(Quaternion.Angle(initial, rightTrack.transform.localRotation), Is.GreaterThan(0f));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void DestructionAnimator_CollapseMovesChunkFromRestPose()
    {
        GameObject root = new GameObject("CastleChunk");
        DestructionAnimator animator = root.AddComponent<DestructionAnimator>();
        animator.CaptureRestPose();

        animator.BeginCollapse();
        animator.TickCollapse(0.25f);

        Assert.That(root.transform.localPosition.y, Is.LessThan(0f));
        Assert.That(Quaternion.Angle(Quaternion.identity, root.transform.localRotation), Is.GreaterThan(0f));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void ImpactAudioController_CreatesReplaceableFallbackClip()
    {
        GameObject root = new GameObject("ImpactAudio");
        root.AddComponent<AudioSource>();
        ImpactAudioController impact = root.AddComponent<ImpactAudioController>();

        impact.EnsureFallbackClip();

        Assert.That(impact.audioSource.clip, Is.Not.Null);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void CombatSoundSlots_FillMissingClipsWithoutReplacingAuthoredClips()
    {
        GameObject root = new GameObject("CombatSounds");
        CombatSoundSlots slots = root.AddComponent<CombatSoundSlots>();
        AudioClip authored = AudioClip.Create("Authored", 32, 1, 8000, false);
        slots.playerCannonShot = authored;

        slots.EnsureFallbackClips();

        Assert.That(slots.playerCannonShot, Is.SameAs(authored));
        Assert.That(slots.enemyCannonShot, Is.Not.Null);
        Assert.That(slots.cannonGroundExplosion, Is.Not.Null);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(authored);
    }
}
#endif
