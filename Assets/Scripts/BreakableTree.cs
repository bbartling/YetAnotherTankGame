using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BreakableTree : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip smashSound;

    [Header("Health")]
    public float maxHealth = 18f;
    public float tankCollisionDamage = 4f;
    public float shellCollisionDamage = 12f;
    public float destroyDelay = 4f;

    [Header("Impulse")]
    public float tankPushForce = 12f;
    public float shellPushForce = 24f;
    public float upwardsModifier = 0.35f;

    private Rigidbody _rb;
    private AudioSource _audio;
    private float _health;
    private bool _broken;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();
        _health = maxHealth;

        _rb.mass = Mathf.Max(0.5f, maxHealth * 0.06f);
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (_audio != null)
        {
            _audio.playOnAwake = false;
            _audio.loop = false;
            _audio.spatialBlend = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_broken || collision.contactCount == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        if (collision.collider.GetComponentInParent<TankController>() != null)
        {
            ApplyImpact(contact.point, contact.normal, tankCollisionDamage, true);
            return;
        }

        if (collision.collider.GetComponentInParent<ProjectileCameraController>() != null)
        {
            ApplyImpact(contact.point, contact.normal, shellCollisionDamage, false);
        }
    }

    public void ApplyImpact(Vector3 worldPoint, Vector3 worldNormal, float force, bool fromTank)
    {
        if (_broken)
        {
            return;
        }

        if (_audio != null && smashSound != null && !_audio.isPlaying)
        {
            _audio.PlayOneShot(smashSound);
        }

        Vector3 away = transform.position - worldPoint;
        if (away.sqrMagnitude < 0.001f)
        {
            away = worldNormal;
        }

        away.y = Mathf.Max(away.y, upwardsModifier);
        away.Normalize();

        float push = fromTank ? tankPushForce : shellPushForce;
        if (_rb != null)
        {
            _rb.AddForceAtPosition(away * (force * push), worldPoint, ForceMode.Impulse);
            _rb.AddTorque(Random.insideUnitSphere * (force * 0.12f), ForceMode.Impulse);
        }

        _health -= force;
        if (_health <= 0f || !fromTank && force >= shellCollisionDamage * 0.9f)
        {
            BreakTree();
        }
    }

    private void BreakTree()
    {
        if (_broken)
        {
            return;
        }

        _broken = true;

        if (_rb != null)
        {
            _rb.useGravity = true;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Destroy(gameObject, destroyDelay);
    }
}
