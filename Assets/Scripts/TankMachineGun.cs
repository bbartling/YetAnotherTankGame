using UnityEngine;

[DisallowMultipleComponent]
public class TankMachineGun : MonoBehaviour
{
    [Header("References")]
    public TankController tankController;
    public Transform firePoint;
    public AudioSource audioSource;
    public AudioClip machineGunSound;

    [Header("Input")]
    public KeyCode fireKey = KeyCode.LeftShift;

    [Header("Firing")]
    public float fireRate = 0.045f;
    public float bulletSpeedMultiplier = 0.42f;
    public float bulletLifetime = 2.65f;
    public float bulletDamage = 1f;
    public float bulletRadius = 0.08f;
    public float bulletSpread = 0.8f;
    public int maxRicochets = 3;
    public float ricochetLoss = 0.72f;
    public float fireKickback = 0.1f;
    public float maxDistance = 60f;

    private float _nextFireTime;
    private AudioSource _localAudio;

    private void Awake()
    {
        if (tankController == null)
        {
            tankController = GetComponent<TankController>();
        }

        if (firePoint == null && tankController != null)
        {
            firePoint = tankController.firePoint;
        }

        if (audioSource == null)
        {
            _localAudio = gameObject.GetComponent<AudioSource>();
            if (_localAudio == null)
            {
                _localAudio = gameObject.AddComponent<AudioSource>();
            }

            audioSource = _localAudio;
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;
        }
    }

    private void Update()
    {
        if (tankController == null || firePoint == null)
        {
            return;
        }

        if (Input.GetKey(fireKey) && Time.time >= _nextFireTime)
        {
            FireBullet();
            _nextFireTime = Time.time + fireRate;
        }
    }

    private void FireBullet()
    {
        if (machineGunSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(machineGunSound);
        }

        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "MachineGunBullet";
        bullet.transform.position = firePoint.position + firePoint.forward * 0.55f;
        bullet.transform.rotation = firePoint.rotation;
        bullet.transform.localScale = Vector3.one * bulletRadius * 2f;

        Renderer renderer = bullet.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.95f, 0.9f, 0.2f, 1f);
            renderer.material = material;
        }

        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null)
        {
            bulletCollider.material = null;
        }

        Rigidbody body = bullet.AddComponent<Rigidbody>();
        body.mass = 0.07f;
        body.useGravity = true;
        body.linearDamping = 0.01f;
        body.angularDamping = 0.02f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        MachineGunBullet projectile = bullet.AddComponent<MachineGunBullet>();
        projectile.ownerTank = tankController;
        projectile.damage = bulletDamage;
        projectile.maxRicochets = maxRicochets;
        projectile.ricochetLoss = ricochetLoss;
        projectile.maxLifetime = bulletLifetime;
        projectile.maxDistance = maxDistance;

        Collider[] tankColliders = tankController.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < tankColliders.Length; i++)
        {
            if (tankColliders[i] != null)
            {
                Physics.IgnoreCollision(tankColliders[i], bulletCollider);
            }
        }

        if (tankController.RigidbodyComponent != null)
        {
            Vector3 kickback = -firePoint.forward * fireKickback;
            tankController.RigidbodyComponent.AddForce(kickback, ForceMode.Impulse);
        }

        Vector3 aimDirection = (firePoint.forward + Random.insideUnitSphere * bulletSpread).normalized;
        Vector3 launchVelocity = aimDirection * (tankController.maxPower * bulletSpeedMultiplier) + tankController.CurrentVelocity;
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = launchVelocity;
#else
        body.velocity = launchVelocity;
#endif
    }


public void FireAtPointForTest(Vector3 targetPoint, int rounds)
    {
        if (tankController == null || firePoint == null)
        {
            return;
        }

        int clampedRounds = Mathf.Clamp(rounds, 1, 140);
        Vector3 baseDirection = targetPoint - firePoint.position;
        if (baseDirection.sqrMagnitude < 0.01f)
        {
            baseDirection = firePoint.forward;
        }

        for (int i = 0; i < clampedRounds; i++)
        {
            FireBulletForTest(baseDirection.normalized, i * 0.04f);
        }
    }

    private void FireBulletForTest(Vector3 direction, float lateralOffset)
    {
        if (machineGunSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(machineGunSound);
        }

        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "MachineGunBullet_Test";
        bullet.transform.position = firePoint.position + firePoint.forward * 0.55f + firePoint.right * lateralOffset;
        bullet.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        bullet.transform.localScale = Vector3.one * bulletRadius * 2f;

        Renderer renderer = bullet.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.95f, 0.9f, 0.2f, 1f);
            renderer.material = material;
        }

        Collider bulletCollider = bullet.GetComponent<Collider>();
        Rigidbody body = bullet.AddComponent<Rigidbody>();
        body.mass = 0.07f;
        body.useGravity = true;
        body.linearDamping = 0.01f;
        body.angularDamping = 0.02f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        MachineGunBullet projectile = bullet.AddComponent<MachineGunBullet>();
        projectile.ownerTank = tankController;
        projectile.damage = bulletDamage;
        projectile.maxRicochets = maxRicochets;
        projectile.ricochetLoss = ricochetLoss;
        projectile.maxLifetime = bulletLifetime;
        projectile.maxDistance = maxDistance;

        Collider[] tankColliders = tankController.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < tankColliders.Length; i++)
        {
            if (tankColliders[i] != null && bulletCollider != null)
            {
                Physics.IgnoreCollision(tankColliders[i], bulletCollider);
            }
        }

        Vector3 launchVelocity = direction.normalized * (tankController.maxPower * bulletSpeedMultiplier) + tankController.CurrentVelocity;
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = launchVelocity;
#else
        body.velocity = launchVelocity;
#endif
    }
}
