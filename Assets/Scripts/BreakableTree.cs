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
    public int crumblePieceCount = 7;
    public float crumbleForce = 18f;

    private Rigidbody _rb;
    private AudioSource _audio;
    private float _health;
    private bool _broken;

    public bool IsBroken => _broken;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();
        _health = maxHealth;
        SillyModelInstaller.Ensure(gameObject, "Models/Trees/SillyTreeKit", 1f, true);

        _rb.mass = Mathf.Max(0.5f, maxHealth * 0.06f);
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.useGravity = false;
        _rb.isKinematic = true;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

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
            BreakTree(worldPoint, worldNormal, force);
        }
    }

    private void BreakTree(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        if (_broken)
        {
            return;
        }

        _broken = true;

        SpawnCrumblePieces(worldPoint, worldNormal, force);

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }

        Destroy(gameObject, destroyDelay);
    }

    private void SpawnCrumblePieces(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        Vector3 baseCenter = transform.position;
        for (int i = 0; i < crumblePieceCount; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere);
            piece.name = name + "_Shard";
            BattlefieldEffectController.RegisterTemporary(piece, "Debris", 64);
            piece.transform.position = baseCenter + Random.insideUnitSphere * 0.45f;
            piece.transform.localScale = Vector3.one * Random.Range(0.12f, 0.32f);

            Collider collider = piece.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = piece.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.31f, 0.19f, 0.08f, 1f);
                renderer.material = material;
            }

            Rigidbody rb = piece.AddComponent<Rigidbody>();
            rb.mass = 0.04f;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.15f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            Vector3 toss = (piece.transform.position - worldPoint).normalized + worldNormal * 0.9f + Random.insideUnitSphere * 0.3f;
            rb.AddForce(toss.normalized * Mathf.Max(crumbleForce, force * 0.7f), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * force * 0.1f, ForceMode.Impulse);

            Destroy(piece, Random.Range(1.2f, 3.5f));
        }
    }
}
