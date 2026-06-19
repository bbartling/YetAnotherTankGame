using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Damageable : MonoBehaviour
{
    public float maxHealth = 100f;
    public DamageStateController damageStateController;

    public float CurrentHealth { get; private set; }
    public bool IsDestroyed => CurrentHealth <= 0f;
    public event Action<float> HealthRatioChanged;

    private void Awake()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        CurrentHealth = Mathf.Max(0.01f, maxHealth);
        PublishState();
    }

    public void ApplyDamage(float damage)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - Mathf.Max(0f, damage));
        PublishState();
    }

    private void PublishState()
    {
        float ratio = maxHealth > 0f ? Mathf.Clamp01(CurrentHealth / maxHealth) : 0f;
        damageStateController?.ApplyHealthRatio(ratio);
        HealthRatioChanged?.Invoke(ratio);
    }
}
