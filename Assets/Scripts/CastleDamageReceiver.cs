using UnityEngine;

[DisallowMultipleComponent]
public class CastleDamageReceiver : MonoBehaviour
{
    [Header("Damage")]
    public float maxHealth = 260f;
    public float damageScale = 1.0f;
    public float collapseThreshold = 0.35f;
    public float pieceBreakThreshold = 0.60f;

    [Header("Impact Mark")]
    public float markLifetime = 30f;
    public float markScale = 1.2f;
    public Color scorchColor = new Color(0.18f, 0.13f, 0.08f, 0.95f);

    private float _health;
    private bool _collapsed;

    private void Awake()
    {
        _health = maxHealth;
    }

    public void ApplyImpact(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        if (_collapsed)
        {
            return;
        }

        float damage = Mathf.Max(8f, force * damageScale);
        _health -= damage;

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
            Destroy(bestChild.gameObject);
        }
    }

    private void CollapseCastle()
    {
        _collapsed = true;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }
}
