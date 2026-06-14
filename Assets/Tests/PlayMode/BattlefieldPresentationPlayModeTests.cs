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
    public void TankVisualAnimator_RotatesEightWheelVisualsWhileMoving()
    {
        GameObject root = new GameObject("WheelTank");
        TankVisualAnimator animator = root.AddComponent<TankVisualAnimator>();
        animator.wheels = new Transform[8];
        Quaternion[] initial = new Quaternion[8];
        for (int i = 0; i < animator.wheels.Length; i++)
        {
            animator.wheels[i] = new GameObject($"Wheel_{i}").transform;
            animator.wheels[i].SetParent(root.transform, false);
            initial[i] = animator.wheels[i].localRotation;
        }

        animator.TickVisuals(0.1f, 2f, 0f);

        for (int i = 0; i < animator.wheels.Length; i++)
        {
            Assert.That(Quaternion.Angle(initial[i], animator.wheels[i].localRotation), Is.GreaterThan(0f));
        }
        Object.DestroyImmediate(root);
    }

    [Test]
    public void TankVisualAnimator_ForwardAndReverseRotateWheelsAroundAxleInOppositeDirections()
    {
        GameObject root = new GameObject("SignedWheelTank");
        TankVisualAnimator animator = root.AddComponent<TankVisualAnimator>();
        Transform wheel = new GameObject("Wheel").transform;
        wheel.SetParent(root.transform, false);
        animator.wheels = new[] { wheel };

        animator.TickVisuals(0.1f, 2f, 0f);
        Vector3 forwardAngles = wheel.localEulerAngles;
        wheel.localRotation = Quaternion.identity;
        animator.TickVisuals(0.1f, -2f, 0f);
        Vector3 reverseAngles = wheel.localEulerAngles;

        Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, forwardAngles.x)), Is.GreaterThan(1f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, forwardAngles.y)), Is.LessThan(0.1f));
        Assert.That(Mathf.Sign(Mathf.DeltaAngle(0f, forwardAngles.x)), Is.Not.EqualTo(Mathf.Sign(Mathf.DeltaAngle(0f, reverseAngles.x))));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void VisualPivotFollower_PreservesRestPoseAndFollowsTargetDelta()
    {
        GameObject root = new GameObject("PivotFollower");
        Transform target = new GameObject("Target").transform;
        target.SetParent(root.transform, false);
        Transform visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        visual.rotation = Quaternion.Euler(5f, 10f, 2f);
        VisualPivotFollower follower = visual.gameObject.AddComponent<VisualPivotFollower>();
        follower.Bind(target);

        target.rotation = Quaternion.Euler(0f, 45f, 0f);
        follower.SendMessage("LateUpdate");

        Assert.That(Mathf.Abs(Mathf.DeltaAngle(55f, visual.eulerAngles.y)), Is.LessThan(0.5f));
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
