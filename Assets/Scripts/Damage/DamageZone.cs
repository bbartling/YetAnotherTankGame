using UnityEngine;

public class DamageZone : MonoBehaviour
{
    public enum ZoneType
    {
        Hull,
        Track,
        Turret,
        Barrel
    }

    public Damageable damageable;
    public float damageMultiplier = 1f;
    public ZoneType zoneType;
    public float zoneMaxHealth = 100f;

    public float CurrentZoneHealth { get; private set; } = 100f;
    public float HealthRatio => zoneMaxHealth > 0f ? Mathf.Clamp01(CurrentZoneHealth / zoneMaxHealth) : 0f;
    public float MovementMultiplier => zoneType == ZoneType.Track ? Mathf.Lerp(0.3f, 1f, HealthRatio) : 1f;
    public float TurretRotationMultiplier => zoneType == ZoneType.Turret ? Mathf.Lerp(0.35f, 1f, HealthRatio) : 1f;
    public float AccuracyMultiplier => zoneType == ZoneType.Barrel ? Mathf.Lerp(0.45f, 1f, HealthRatio) : 1f;

    private void Awake()
    {
        CurrentZoneHealth = Mathf.Max(0.01f, zoneMaxHealth);
    }

    public void ApplyDamage(float damage)
    {
        float scaledDamage = Mathf.Max(0f, damage) * Mathf.Max(0f, damageMultiplier);
        CurrentZoneHealth = Mathf.Max(0f, CurrentZoneHealth - scaledDamage);
        if (damageable != null)
        {
            damageable.ApplyDamage(scaledDamage);
        }
    }
}
