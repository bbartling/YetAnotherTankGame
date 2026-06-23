using UnityEngine;

[DisallowMultipleComponent]
public class CombatSoundSlots : MonoBehaviour
{
    [Header("Player Weapons")]
    public AudioClip machineGun;
    public AudioClip playerCannonShot;

    [Header("Enemy Weapons")]
    public AudioClip enemyCannonShot;
    public AudioClip turretShot;
    public AudioClip enemyCannonNearMiss;

    [Header("Impacts")]
    public AudioClip cannonGroundExplosion;
    public AudioClip machineGunMetalRicochet;
    public AudioClip enemyTankKill;

    private void Awake()
    {
        EnsureFallbackClips();
    }

    public void EnsureFallbackClips()
    {
        if (playerCannonShot == null) playerCannonShot = ProceduralBattlefieldAudio.CreateCannonBoom();
        if (enemyCannonShot == null) enemyCannonShot = ProceduralBattlefieldAudio.CreateCannonBoom();
        if (turretShot == null) turretShot = ProceduralBattlefieldAudio.CreateCannonBoom();
        if (enemyCannonNearMiss == null) enemyCannonNearMiss = ProceduralBattlefieldAudio.CreateShellWhistle();
        if (cannonGroundExplosion == null) cannonGroundExplosion = ProceduralBattlefieldAudio.CreateCannonballExplosion();
        if (machineGunMetalRicochet == null) machineGunMetalRicochet = ProceduralBattlefieldAudio.CreateImpact();
        if (enemyTankKill == null) enemyTankKill = ProceduralBattlefieldAudio.CreateTankExplosion();
    }
}
