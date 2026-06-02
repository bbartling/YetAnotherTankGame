using UnityEngine;

public class EnemyTurret : MonoBehaviour
{
    public Transform turretBase;
    public Transform barrel;
    public Transform firePoint;
    public GameObject shellPrefab;
    
    public float detectRange = 200f;
    public float fireRate = 3f;
    public float shellPower = 50f;
    public AudioClip fireSound;
    public float accuracy = 0.5f; // 1.0 is perfect, 0.0 is very bad
    public float targetLeadSeconds = 0.35f;
    public float fireJitter = 0.14f;

    private Transform _player;
    private float _nextFireTime;
    private AudioSource _audio;
    private BattlefieldDirector _director;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _director = Object.FindFirstObjectByType<BattlefieldDirector>();
        GameObject p = GameObject.Find("PlayerTank");
        if (p != null) _player = p.transform;

        gameObject.tag = "EnemyTurret";
        _nextFireTime = Time.time + (Mathf.Abs(GetInstanceID()) % 17) * 0.05f;
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
        if (fireSound != null) _audio.PlayOneShot(fireSound);

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);

        var pcc = shell.GetComponent<ProjectileCameraController>();
        if (pcc != null) pcc.enableCameraSwitching = false;

        var rb = shell.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 target = _director != null
                ? _director.GetSharedTurretAimPoint(firePoint.position, targetLeadSeconds)
                : _player.position + Vector3.up * 1.1f;
            Vector3 velocity;
            if (TryGetBallisticVelocity(firePoint.position, target, shellPower, Physics.gravity, true, out velocity))
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
