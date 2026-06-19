using UnityEngine;

[DisallowMultipleComponent]
public class DamageStateController : MonoBehaviour
{
    public enum State
    {
        Intact,
        Smoking,
        BurningDisabled,
        Wrecked
    }

    public GameObject smokeEffect;
    public GameObject fireEffect;
    public GameObject intactModel;
    public GameObject wreckModel;
    public DestructibleModelSwap modelSwap;

    public State CurrentState { get; private set; } = State.Intact;

    private void Awake()
    {
        EnsureDefaultEffects();
        ApplyHealthRatio(1f);
    }

    public void EnsureDefaultEffects()
    {
        if (smokeEffect == null)
        {
            smokeEffect = BattlefieldEffectController.CreateLoopingEffect(
                transform,
                "DamageSmoke",
                new Color(0.22f, 0.22f, 0.22f, 0.8f),
                24,
                1.4f,
                3.5f);
        }

        if (fireEffect == null)
        {
            fireEffect = BattlefieldEffectController.CreateLoopingEffect(
                transform,
                "DamageFire",
                new Color(1f, 0.35f, 0.05f, 0.9f),
                20,
                0.8f,
                1.1f);
        }
    }

    public void ApplyHealthRatio(float healthRatio)
    {
        float ratio = Mathf.Clamp01(healthRatio);
        if (ratio <= 0f)
        {
            CurrentState = State.Wrecked;
        }
        else if (ratio <= 0.35f)
        {
            CurrentState = State.BurningDisabled;
        }
        else if (ratio <= 0.7f)
        {
            CurrentState = State.Smoking;
        }
        else
        {
            CurrentState = State.Intact;
        }

        SetActive(smokeEffect, CurrentState == State.Smoking || CurrentState == State.BurningDisabled);
        SetActive(fireEffect, CurrentState == State.BurningDisabled || CurrentState == State.Wrecked);
        SetActive(intactModel, CurrentState != State.Wrecked);
        SetActive(wreckModel, CurrentState == State.Wrecked);
        modelSwap?.ApplyState(CurrentState);
        GetComponent<SillyModelInstaller>()?.ApplyDamageState(CurrentState);

        SetParticlesPlaying(smokeEffect, smokeEffect != null && smokeEffect.activeSelf);
        SetParticlesPlaying(fireEffect, fireEffect != null && fireEffect.activeSelf);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private static void SetParticlesPlaying(GameObject target, bool shouldPlay)
    {
        if (target == null)
        {
            return;
        }

        ParticleSystem particles = target.GetComponent<ParticleSystem>();
        if (particles == null)
        {
            return;
        }

        if (shouldPlay && !particles.isPlaying)
        {
            particles.Play();
        }
        else if (!shouldPlay && particles.isPlaying)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
