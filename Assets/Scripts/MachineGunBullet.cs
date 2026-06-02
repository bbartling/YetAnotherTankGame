using UnityEngine;

[DisallowMultipleComponent]
public class MachineGunBullet : MonoBehaviour
{
    public TankController ownerTank;
    public float damage = 12f;
    public float maxLifetime = 3.5f;
    public float maxDistance = 120f;
    public int maxRicochets = 4;
    public float ricochetLoss = 0.78f;

    private Rigidbody _rb;
    private float _spawnTime;
    private Vector3 _spawnPosition;
    private int _ricochetCount;
    private bool _spent;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _spawnTime = Time.time;
        _spawnPosition = transform.position;
    }

    private void Update()
    {
        if (_spent)
        {
            return;
        }

        if (Time.time - _spawnTime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (Vector3.Distance(_spawnPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_spent || collision == null || collision.contactCount == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Transform hitTransform = collision.collider.transform;

        if (ownerTank != null && hitTransform != null && hitTransform.IsChildOf(ownerTank.transform))
        {
            return;
        }

        EnemyTankAI enemyTank = collision.collider.GetComponentInParent<EnemyTankAI>();
        if (enemyTank != null)
        {
            enemyTank.ApplyProjectileDamage(damage, contact.point, contact.normal);
            PopImpact(contact.point, contact.normal, new Color(1f, 0.72f, 0.18f, 1f), 0.65f);
            Finish();
            return;
        }

        EnemyTurret turret = collision.collider.GetComponentInParent<EnemyTurret>();
        if (turret != null)
        {
            PopImpact(contact.point, contact.normal, new Color(1f, 0.52f, 0.2f, 1f), 0.75f);
            CrumbleAndDestroy(turret.gameObject, contact.point, contact.normal, 10, 2.8f);
            Finish();
            return;
        }

        TankController otherTank = collision.collider.GetComponentInParent<TankController>();
        if (otherTank != null)
        {
            if (otherTank != ownerTank)
            {
                otherTank.ApplyProjectileDamage(damage, contact.point, contact.normal);
            }

            PopImpact(contact.point, contact.normal, new Color(1f, 0.74f, 0.22f, 1f), 0.5f);
            Finish();
            return;
        }

        BreakableTree tree = collision.collider.GetComponentInParent<BreakableTree>();
        if (tree != null)
        {
            tree.ApplyImpact(contact.point, contact.normal, damage * 1.4f, false);
            PopImpact(contact.point, contact.normal, new Color(0.52f, 0.32f, 0.12f, 1f), 0.35f);
            if (damage >= 10f)
            {
                CrumbleAndDestroy(tree.gameObject, contact.point, contact.normal, 8, 1.4f);
            }

            Finish();
            return;
        }

        CastleDamageReceiver castle = collision.collider.GetComponentInParent<CastleDamageReceiver>();
        if (castle != null)
        {
            castle.ApplyImpact(contact.point, contact.normal, damage * 2.2f);
            PopImpact(contact.point, contact.normal, new Color(0.82f, 0.58f, 0.32f, 1f), 0.8f);
            if (damage >= 14f)
            {
                CrumbleAndDestroy(castle.gameObject, contact.point, contact.normal, 18, 5.5f);
            }

            Finish();
            return;
        }

        if (_ricochetCount >= maxRicochets || _rb == null)
        {
            PopImpact(contact.point, contact.normal, new Color(0.95f, 0.8f, 0.18f, 1f), 0.2f);
            Finish();
            return;
        }

        Vector3 incoming = _rb.linearVelocity;
        Vector3 reflected = Vector3.Reflect(incoming, contact.normal).normalized;
        Vector3 bounced = reflected * Mathf.Max(6f, incoming.magnitude * ricochetLoss);

        _rb.linearVelocity = bounced;
        transform.position = contact.point + contact.normal * 0.05f;
        _ricochetCount++;

        if (_ricochetCount >= maxRicochets || bounced.magnitude < 8f)
        {
            PopImpact(contact.point, contact.normal, new Color(0.95f, 0.8f, 0.18f, 1f), 0.2f);
            Finish();
        }
    }

    private void PopImpact(Vector3 point, Vector3 normal, Color color, float scale)
    {
        GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.transform.position = point + normal * 0.06f;
        burst.transform.localScale = Vector3.one * scale;

        Collider collider = burst.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = burst.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.material = material;
        }

        Destroy(burst, 0.12f);
    }

    private void CrumbleAndDestroy(GameObject target, Vector3 point, Vector3 normal, int pieces, float force)
    {
        if (target == null)
        {
            return;
        }

        Bounds bounds = GetTargetBounds(target);
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        Color baseColor = SampleColor(target);

        for (int i = 0; i < pieces; i++)
        {
            GameObject chunk = GameObject.CreatePrimitive(Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere);
            chunk.name = target.name + "_Chunk";
            chunk.transform.position = Vector3.Lerp(center, point, 0.25f) + Random.insideUnitSphere * Mathf.Max(0.18f, extents.magnitude * 0.12f);
            chunk.transform.localScale = Vector3.one * Random.Range(0.18f, 0.5f) * Mathf.Max(0.9f, extents.magnitude * 0.12f);

            Collider collider = chunk.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = chunk.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = baseColor;
                renderer.material = material;
            }

            Rigidbody rb = chunk.AddComponent<Rigidbody>();
            rb.mass = Random.Range(0.05f, 0.2f);
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.1f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Vector3 toss = (chunk.transform.position - point).normalized + normal * 0.75f + Random.insideUnitSphere * 0.35f;
            rb.AddForce(toss.normalized * Random.Range(force * 0.6f, force), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * force * 0.15f, ForceMode.Impulse);

            Destroy(chunk, Random.Range(1.5f, 4f));
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Destroy(target, 0.05f);
    }

    private Bounds GetTargetBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(target.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private Color SampleColor(GameObject target)
    {
        Renderer renderer = target.GetComponentInChildren<Renderer>(true);
        if (renderer != null && renderer.material != null)
        {
            return renderer.material.color;
        }

        return new Color(0.65f, 0.6f, 0.55f, 1f);
    }

    private void Finish()
    {
        _spent = true;
        Destroy(gameObject, 0.02f);
    }
}
