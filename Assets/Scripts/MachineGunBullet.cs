using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class MachineGunBullet : MonoBehaviour
{
    public TankController ownerTank;
    public float damage = 2.5f;
    public float maxLifetime = 3f;
    public float maxDistance = 120f;
    public int maxRicochets = 2;
    public float ricochetLoss = 0.72f;

    private Rigidbody _rb;
    private float _spawnTime;
    private Vector3 _spawnPosition;
    private int _ricochets;
    private bool _spent;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _spawnTime = Time.time;
        _spawnPosition = transform.position;

        if (_rb != null)
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    private void Update()
    {
        if (_spent) return;

        if (Time.time - _spawnTime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (Vector3.Distance(_spawnPosition, transform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_spent || collision == null || collision.contactCount == 0) return;

        ContactPoint contact = collision.GetContact(0);

        if (ownerTank != null && contact.otherCollider != null && contact.otherCollider.transform.IsChildOf(ownerTank.transform))
        {
            return;
        }

        EnemyTankAI enemyTank = collision.collider.GetComponentInParent<EnemyTankAI>();
        if (enemyTank != null)
        {
            enemyTank.ApplyMachineGunDamage(damage, contact.point, contact.normal);
            Finish();
            return;
        }

        TankController tank = collision.collider.GetComponentInParent<TankController>();
        if (tank != null && tank != ownerTank)
        {
            tank.ApplyBulletDamage(damage, contact.point, contact.normal);
            Finish();
            return;
        }

        BreakableTree tree = collision.collider.GetComponentInParent<BreakableTree>();
        if (tree != null)
        {
            tree.ApplyImpact(contact.point, contact.normal, damage, false);
            Finish();
            return;
        }

        CastleDamageReceiver castle = collision.collider.GetComponentInParent<CastleDamageReceiver>();
        if (castle != null)
        {
            castle.ApplyImpact(contact.point, contact.normal, damage);
            Finish();
            return;
        }

        if (_rb == null || _ricochets >= maxRicochets)
        {
            Finish();
            return;
        }

        Vector3 velocity = GetVelocity();
        if (velocity.magnitude < 5f)
        {
            Finish();
            return;
        }

        Vector3 reflected = Vector3.Reflect(velocity.normalized, contact.normal);
        SetVelocity(reflected * velocity.magnitude * ricochetLoss);
        transform.position = contact.point + contact.normal * 0.08f;
        _ricochets++;
    }

    private void Finish()
    {
        _spent = true;
        Destroy(gameObject);
    }

    private Vector3 GetVelocity()
    {
        if (_rb == null) return Vector3.zero;
#if UNITY_6000_0_OR_NEWER
        return _rb.linearVelocity;
#else
        return _rb.velocity;
#endif
    }

    private void SetVelocity(Vector3 value)
    {
        if (_rb == null) return;
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = value;
#else
        _rb.velocity = value;
#endif
    }
}
