using UnityEngine;

public class EnemyTurret : MonoBehaviour
{
    public Transform turretBase;
    public Transform barrel;
    public Transform firePoint;
    public GameObject shellPrefab;
    
    public float detectRange = 200f;
    public float fireRate = 0.65f;
    public float shellPower = 62f;
    public AudioClip fireSound;
    public float accuracy = 0.5f; // 1.0 is perfect, 0.0 is very bad
    public float targetLeadSeconds = 0.35f;
    public float fireJitter = 0.14f;
    public float bulletDamage = 5f;
    public float bulletRadius = 0.1f;
    public float bulletLifetime = 5f;

    private Transform _player;
    private float _nextFireTime;
    private AudioSource _audio;
    private BattlefieldDirector _director;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _director = Object.FindAnyObjectByType<BattlefieldDirector>();
        GameObject p = GameObject.Find("PlayerTank");
        if (p != null) _player = p.transform;

        gameObject.tag = "EnemyTurret";
        int fireOffsetHash = Mathf.Abs((name + transform.position.ToString()).GetHashCode());
        _nextFireTime = Time.time + (fireOffsetHash % 17) * 0.05f;
    }

    void Update()
    {
        if (_player == null)
        {
            GameObject p = GameObject.Find("PlayerTank");
            if (p != null)
            {
                _player = p.transform;
            }
        }

        if (_player == null)
        {
            return;
        }

        float difficulty = _director != null ? _director.GetDifficultyMultiplier() : 1f;
        float effectiveRange = detectRange * Mathf.Lerp(1f, 1.12f, Mathf.Clamp01(difficulty - 1f));
        float effectiveFireRate = Mathf.Max(0.4f, fireRate / difficulty);

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist < effectiveRange)
        {
            AimAtPlayer();

            if (Time.time > _nextFireTime)
            {
                Fire();
                _nextFireTime = Time.time + effectiveFireRate + Random.Range(0f, fireJitter);
            }
        }
    }

    void AimAtPlayer()
    {
        Vector3 targetPoint = _director != null
            ? _director.GetSharedTurretAimPoint(transform.position, targetLeadSeconds)
            : _player.position + Vector3.up * 1.1f;

        if (turretBase != null)
        {
            Vector3 direction = targetPoint - turretBase.position;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                turretBase.rotation = Quaternion.Slerp(turretBase.rotation, targetRot, Time.deltaTime * (2f * accuracy));
            }
        }

        if (barrel != null)
        {
            Vector3 targetDir = targetPoint - barrel.position;
            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            barrel.rotation = Quaternion.Slerp(barrel.rotation, targetRot, Time.deltaTime * (2f * accuracy));
        }
    }

    void Fire()
    {
        AudioClip shot = fireSound;
        if (shot == null)
        {
            CombatSoundSlots slots = Object.FindAnyObjectByType<CombatSoundSlots>();
            if (slots != null)
            {
                shot = slots.turretShot;
            }
        }

        if (shot != null) _audio.PlayOneShot(shot);

        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "CastleTurretBullet";
        shell.transform.position = firePoint.position;
        shell.transform.rotation = firePoint.rotation;
        shell.transform.localScale = Vector3.one * bulletRadius * 2f;

        Collider bulletCollider = shell.GetComponent<Collider>();
        Collider[] turretColliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < turretColliders.Length; i++)
        {
            if (bulletCollider != null && turretColliders[i] != null)
            {
                Physics.IgnoreCollision(turretColliders[i], bulletCollider);
            }
        }

        Renderer renderer = shell.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(1f, 0.78f, 0.24f, 1f);
            renderer.material = material;
        }

        var rb = shell.AddComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 0.08f;
            rb.useGravity = true;
            rb.linearDamping = 0.01f;
            rb.angularDamping = 0.02f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            MachineGunBullet bullet = shell.AddComponent<MachineGunBullet>();
            bullet.damage = bulletDamage;
            bullet.maxDistance = detectRange * 1.15f;
            bullet.maxLifetime = bulletLifetime;
            bullet.maxRicochets = 1;
            bullet.ricochetLoss = 0.55f;

            Vector3 target = _director != null
                ? _director.GetSharedTurretAimPoint(firePoint.position, targetLeadSeconds)
                : _player.position + Vector3.up * 1.1f;
            Vector3 velocity;
            if (TryGetBallisticVelocity(firePoint.position, target, shellPower, Physics.gravity, false, out velocity))
            {
                Vector3 scatter = Random.insideUnitSphere * (1f - accuracy) * 0.6f;
                SetVelocity(rb, velocity + scatter);
            }
            else
            {
                Vector3 forceDir = firePoint.forward + Random.insideUnitSphere * (1f - accuracy) * 0.1f;
                SetVelocity(rb, forceDir.normalized * shellPower);
            }
        }
    }


    private bool TryGetBallisticVelocity(Vector3 origin, Vector3 target, float speed, Vector3 gravity, bool highArc, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float x = toTargetXZ.magnitude;
        float y = toTarget.y;
        float g = Mathf.Abs(gravity.y);

        if (x < 0.01f || speed <= 0.01f)
        {
            return false;
        }

        float speedSqr = speed * speed;
        float discriminant = speedSqr * speedSqr - g * (g * x * x + 2f * y * speedSqr);
        if (discriminant < 0f)
        {
            return false;
        }

        float sqrt = Mathf.Sqrt(discriminant);
        float tanTheta = highArc
            ? (speedSqr + sqrt) / (g * x)
            : (speedSqr - sqrt) / (g * x);

        float cos = 1f / Mathf.Sqrt(1f + tanTheta * tanTheta);
        float sin = tanTheta * cos;
        Vector3 xzDir = toTargetXZ.normalized;
        velocity = xzDir * (speed * cos) + Vector3.up * (speed * sin);
        return true;
    }

    private void SetVelocity(Rigidbody body, Vector3 value)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = value;
#else
        body.velocity = value;
#endif
    }
}
