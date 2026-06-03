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
    public KeyCode fireKey = KeyCode.F;

    [Header("Machine Gun Feel")]
    public float roundsPerSecond = 14f;
    public float damage = 2.5f;
    public float range = 95f;
    public float spreadDegrees = 1.1f;
    public float impactForce = 8f;
    public LayerMask hitMask = ~0;

    [Header("Tracer")]
    public bool drawTracers = true;
    public float tracerLifetime = 0.035f;
    public float tracerWidth = 0.035f;
    public Material tracerMaterial;

    private float _nextFireTime;

    private void Awake()
    {
        AutoWire();
    }

    private void AutoWire()
    {
        if (tankController == null)
        {
            tankController = GetComponent<TankController>();
        }

        if (firePoint == null && tankController != null)
        {
            firePoint = tankController.machineGunFirePoint != null
                ? tankController.machineGunFirePoint
                : tankController.cannonFirePoint;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
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
        if (tankController == null || firePoint == null) AutoWire();
        if (tankController == null || firePoint == null) return;
        if (tankController.IsDestroyed) return;

        if (Input.GetKey(fireKey) && Time.time >= _nextFireTime)
        {
            FireOneRound();
            _nextFireTime = Time.time + (1f / Mathf.Max(1f, roundsPerSecond));
        }
    }

    private void FireOneRound()
    {
        Vector3 direction = ApplySpread(firePoint.forward, spreadDegrees);
        FireOneRoundInDirection(direction);
    }

    private void FireOneRoundInDirection(Vector3 direction)
    {
        if (firePoint == null) return;

        if (machineGunSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(machineGunSound);
        }

        Vector3 origin = firePoint.position;
        direction = direction.normalized;
        Vector3 end = origin + direction * range;

        RaycastHit hit;
        bool didHit = Physics.Raycast(origin, direction, out hit, range, hitMask, QueryTriggerInteraction.Ignore);

        if (didHit)
        {
            end = hit.point;
            ApplyHitDamage(hit, direction);
        }

        if (drawTracers)
        {
            SpawnTracer(origin, end);
        }
    }

    // Compatibility method used by GameplayTestApi.FireMachineGunAtNearestEnemy().
    public void FireAtPointForTest(Vector3 worldPoint, int rounds)
    {
        AutoWire();

        if (firePoint == null)
        {
            Debug.LogWarning("[TankMachineGun] FireAtPointForTest failed because firePoint is missing.");
            return;
        }

        if (tankController != null)
        {
            tankController.AimTurretAndBarrelAtPoint(worldPoint);
        }

        Vector3 direction = worldPoint - firePoint.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = firePoint.forward;
        }

        int count = Mathf.Clamp(rounds, 1, 80);
        for (int i = 0; i < count; i++)
        {
            FireOneRoundInDirection(direction.normalized);
        }

        _nextFireTime = Time.time + (1f / Mathf.Max(1f, roundsPerSecond));
    }

    private Vector3 ApplySpread(Vector3 forward, float degrees)
    {
        if (degrees <= 0f) return forward.normalized;

        float yaw = Random.Range(-degrees, degrees);
        float pitch = Random.Range(-degrees, degrees);
        Quaternion spread = Quaternion.Euler(pitch, yaw, 0f);
        return (spread * forward).normalized;
    }

    private void ApplyHitDamage(RaycastHit hit, Vector3 shotDirection)
    {
        if (hit.collider == null) return;

        if (tankController != null && hit.transform.IsChildOf(tankController.transform))
        {
            return;
        }

        EnemyTankAI enemyTank = hit.collider.GetComponentInParent<EnemyTankAI>();
        if (enemyTank != null)
        {
            enemyTank.ApplyMachineGunDamage(damage, hit.point, hit.normal);
            PushRigidbody(hit, shotDirection);
            return;
        }

        TankController otherTank = hit.collider.GetComponentInParent<TankController>();
        if (otherTank != null && otherTank != tankController)
        {
            otherTank.ApplyBulletDamage(damage, hit.point, hit.normal);
            PushRigidbody(hit, shotDirection);
            return;
        }

        EnemyTurret enemyTurret = hit.collider.GetComponentInParent<EnemyTurret>();
        if (enemyTurret != null)
        {
            Destroy(enemyTurret.gameObject);
            return;
        }

        BreakableTree tree = hit.collider.GetComponentInParent<BreakableTree>();
        if (tree != null)
        {
            tree.ApplyImpact(hit.point, hit.normal, damage, false);
            PushRigidbody(hit, shotDirection);
            return;
        }

        CastleDamageReceiver castle = hit.collider.GetComponentInParent<CastleDamageReceiver>();
        if (castle != null)
        {
            castle.ApplyImpact(hit.point, hit.normal, damage);
            PushRigidbody(hit, shotDirection);
            return;
        }

        PushRigidbody(hit, shotDirection);
    }

    private void PushRigidbody(RaycastHit hit, Vector3 shotDirection)
    {
        Rigidbody body = hit.rigidbody;
        if (body != null)
        {
            body.AddForceAtPosition(shotDirection.normalized * impactForce, hit.point, ForceMode.Impulse);
        }
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        GameObject tracer = new GameObject("MachineGunTracer");
        LineRenderer line = tracer.AddComponent<LineRenderer>();

        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = tracerWidth;
        line.endWidth = tracerWidth * 0.4f;
        line.useWorldSpace = true;

        if (tracerMaterial != null)
        {
            line.material = tracerMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                line.material = new Material(shader);
            }
        }

        line.startColor = new Color(1f, 0.86f, 0.2f, 1f);
        line.endColor = new Color(1f, 0.25f, 0.05f, 0f);

        Destroy(tracer, tracerLifetime);
    }
}
