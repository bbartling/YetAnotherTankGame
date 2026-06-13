using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CastleDamageReceiver : MonoBehaviour
{
    [Header("Damage")]
    public float maxHealth = 500f;
    public float damageScale = 1.0f;
    public float cannonImpactThreshold = 80f;
    public int directCannonHitsToCollapse = 5;
    public float collapseThreshold = 0.35f;
    public float pieceBreakThreshold = 0.60f;
    public int crumblePieceCount = 28;
    public float crumbleForce = 24f;

    [Header("Impact Mark")]
    public float markLifetime = 30f;
    public float markScale = 1.2f;
    public Color scorchColor = new Color(0.18f, 0.13f, 0.08f, 0.95f);

    private float _health;
    private bool _collapsed;
    private DamageStateController _damageStateController;

    private void Awake()
    {
        _health = maxHealth;
        _damageStateController = GetComponent<DamageStateController>();
        if (_damageStateController == null)
        {
            _damageStateController = gameObject.AddComponent<DamageStateController>();
        }
        _damageStateController.ApplyHealthRatio(1f);
        SillyModelInstaller.Ensure(gameObject, "Models/Castle/SillyCastleKit", 1f, false);
    }

    public void ApplyImpact(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        if (_collapsed)
        {
            return;
        }

        float damage = force >= cannonImpactThreshold
            ? maxHealth / Mathf.Max(1, directCannonHitsToCollapse)
            : Mathf.Max(1f, force * damageScale);
        _health -= damage;
        _damageStateController?.ApplyHealthRatio(maxHealth > 0f ? _health / maxHealth : 0f);

        SpawnImpactMark(worldPoint, worldNormal, force);

        if (_health <= maxHealth * collapseThreshold)
        {
            BreakRandomPiece(worldPoint);
        }

        if (_health <= 0f)
        {
            CollapseCastle();
        }
    }

    private void SpawnImpactMark(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(mark.GetComponent<Collider>());
        mark.name = "CastleImpactMark";
        BattlefieldEffectController.RegisterTemporary(mark, "ImpactMarks", 24);
        mark.transform.SetParent(transform, true);
        mark.transform.position = worldPoint + worldNormal * 0.03f;
        mark.transform.rotation = Quaternion.LookRotation(worldNormal);

        float size = Mathf.Clamp(0.75f + force * 0.02f, 0.75f, 3.5f) * markScale;
        mark.transform.localScale = new Vector3(size, size, 1f);

        Renderer renderer = mark.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Unlit/Color"));
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
            }

            material.color = scorchColor;
            renderer.material = material;
        }

        Destroy(mark, markLifetime);
    }

    private void BreakRandomPiece(Vector3 worldPoint)
    {
        Transform bestChild = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!child.gameObject.activeSelf)
            {
                continue;
            }

            float distance = Vector3.Distance(child.position, worldPoint);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestChild = child;
            }
        }

        if (bestChild != null)
        {
            SpawnCrumblePieces(bestChild.position, worldPoint, 4);
            Destroy(bestChild.gameObject);
        }
    }

    private void CollapseCastle()
    {
        _collapsed = true;
        _damageStateController?.ApplyHealthRatio(0f);
        StartCoroutine(CollapseCastleSequence());
    }

    private IEnumerator CollapseCastleSequence()
    {
        SpawnCrumblePieces(transform.position, transform.position, crumblePieceCount);

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        float stepDelay = renderers.Length > 0 ? 2.8f / Mathf.Max(1, renderers.Length) : 0.1f;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled)
            {
                SpawnCrumblePieces(renderers[i].bounds.center, transform.position, 3);
            }

            renderers[i].enabled = false;
            yield return new WaitForSeconds(Mathf.Clamp(stepDelay, 0.04f, 0.18f));
        }
    }

    private void SpawnCrumblePieces(Vector3 origin, Vector3 impactPoint, int count)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Color baseColor = new Color(0.55f, 0.48f, 0.38f, 1f);
        if (renderers != null && renderers.Length > 0 && renderers[0] != null)
        {
            baseColor = renderers[0].material != null ? renderers[0].material.color : baseColor;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(Random.value > 0.4f ? PrimitiveType.Cube : PrimitiveType.Sphere);
            piece.name = name + "_Debris";
            BattlefieldEffectController.RegisterTemporary(piece, "Debris", 64);
            piece.transform.position = origin + Random.insideUnitSphere * 1.25f;
            piece.transform.localScale = Vector3.one * Random.Range(0.18f, 0.7f);

            Collider collider = piece.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer pieceRenderer = piece.GetComponent<Renderer>();
            if (pieceRenderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = Color.Lerp(baseColor, new Color(0.26f, 0.21f, 0.18f, 1f), Random.Range(0.15f, 0.55f));
                pieceRenderer.material = material;
            }

            Rigidbody rb = piece.AddComponent<Rigidbody>();
            rb.mass = Random.Range(0.12f, 0.55f);
            rb.linearDamping = 0.08f;
            rb.angularDamping = 0.08f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            Vector3 toss = (piece.transform.position - impactPoint).normalized + Vector3.up * 0.85f + Random.insideUnitSphere * 0.4f;
            rb.AddForce(toss.normalized * Random.Range(crumbleForce * 0.6f, crumbleForce), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * crumbleForce * 0.15f, ForceMode.Impulse);

            Destroy(piece, Random.Range(5f, 9f));
        }
    }


public bool IsCollapsed
    {
        get { return _collapsed; }
    }

    public float CurrentHealth
    {
        get { return Mathf.Max(0f, _health); }
    }

    public float HealthPercent
    {
        get { return maxHealth > 0f ? Mathf.Clamp01(_health / maxHealth) * 100f : 0f; }
    }

    public string CurrentDamageState
    {
        get
        {
            if (_collapsed || _health <= 0f) return "Collapsed";
            float ratio = maxHealth > 0f ? _health / maxHealth : 0f;
            if (ratio <= collapseThreshold) return "HeavilyDamaged";
            if (ratio < 1f) return "Cracked";
            return "Intact";
        }
    }


public void ResetForBattle()
    {
        StopAllCoroutines();
        _health = maxHealth;
        _collapsed = false;
        _damageStateController?.ApplyHealthRatio(1f);

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = true;
        }
    }
}
