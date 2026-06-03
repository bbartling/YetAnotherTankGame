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
}
