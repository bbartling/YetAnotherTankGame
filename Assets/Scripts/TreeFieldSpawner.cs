using UnityEngine;

[DisallowMultipleComponent]
public class TreeFieldSpawner : MonoBehaviour
{
    [Header("References")]
    public CraterTerrain terrainSource;
    public Transform playerTarget;
    public Transform castleTarget;

    [Header("Generation")]
    public bool generateOnAwake = true;

    [Header("Trees")]
    public int treeCount = 384;
    public float treeMinHeight = 1.8f;
    public float treeMaxHeight = 3.4f;
    public float treeMinTrunkRadius = 0.14f;
    public float treeMaxTrunkRadius = 0.24f;
    public float treeMinCanopyRadius = 0.55f;
    public float treeMaxCanopyRadius = 0.9f;
    public float treeGroundSink = 0.03f;
    public float clearRadiusFromPlayer = 110f;
    public float clearRadiusFromCastle = 70f;
    public int randomSeed = 4242;
    public AudioClip treeSmashSound;

    private Transform _treeRoot;

    private void Awake()
    {
        AutoWire();
        if (generateOnAwake)
        {
            SpawnTrees();
        }
    }

    [ContextMenu("Respawn Trees")]
    public void RespawnTrees()
    {
        SpawnTrees();
    }

    [ContextMenu("Clear Trees")]
    public void ClearTrees()
    {
        GameObject existingRoot = GameObject.Find("TreeField");
        if (existingRoot != null)
        {
            DestroyTreeObject(existingRoot);
        }

        if (_treeRoot != null)
        {
            DestroyTreeObject(_treeRoot.gameObject);
            _treeRoot = null;
        }
    }

    private void AutoWire()
    {
        if (terrainSource == null)
        {
            terrainSource = GetComponent<CraterTerrain>();
            if (terrainSource == null)
            {
                terrainSource = GetComponentInParent<CraterTerrain>();
            }
        }

        if (playerTarget == null)
        {
            GameObject player = GameObject.Find("PlayerTank");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        if (castleTarget == null)
        {
            GameObject castle = GameObject.Find("Castle");
            if (castle == null)
            {
                castle = GameObject.Find("Castle(Clone)");
            }

            if (castle != null)
            {
                castleTarget = castle.transform;
            }
        }
    }

    public void SpawnTrees()
    {
        if (terrainSource == null)
        {
            return;
        }

        ClearTrees();

        GameObject rootGo = new GameObject("TreeField");
        _treeRoot = rootGo.transform;
        _treeRoot.SetParent(transform, false);

        System.Random rng = new System.Random(randomSeed);
        int placed = 0;
        int attempts = 0;
        int maxAttempts = Mathf.Max(treeCount * 20, 120);

        while (placed < treeCount && attempts < maxAttempts)
        {
            attempts++;

            Vector3 position = GetRandomPoint(rng);
            if (TooCloseToClearZone(position))
            {
                continue;
            }

            if (!TryProjectToGround(position, out Vector3 groundPoint))
            {
                continue;
            }

            CreateTree(groundPoint, rng, placed);
            placed++;
        }
    }

    private bool TooCloseToClearZone(Vector3 worldPoint)
    {
        if (playerTarget != null && Vector3.Distance(worldPoint, playerTarget.position) < clearRadiusFromPlayer)
        {
            return true;
        }

        if (castleTarget != null && Vector3.Distance(worldPoint, castleTarget.position) < clearRadiusFromCastle)
        {
            return true;
        }

        return false;
    }

    private void DestroyTreeObject(GameObject treeObject)
    {
        if (treeObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(treeObject);
        }
        else
        {
            DestroyImmediate(treeObject);
        }
    }

    private Vector3 GetRandomPoint(System.Random rng)
    {
        float halfWidth = terrainSource.terrainWidth * 0.5f;
        float halfLength = terrainSource.terrainLength * 0.5f;

        float x = RandomRange(rng, -halfWidth + 60f, halfWidth - 60f);
        float z = RandomRange(rng, -halfLength + 60f, halfLength - 60f);
        return terrainSource.transform.TransformPoint(new Vector3(x, terrainSource.transform.position.y + 150f, z));
    }

    private bool TryProjectToGround(Vector3 worldPoint, out Vector3 hitPoint)
    {
        Vector3 origin = worldPoint;
        Collider terrainCollider = terrainSource != null ? terrainSource.GetComponent<Collider>() : null;
        Renderer terrainRenderer = terrainSource != null ? terrainSource.GetComponent<Renderer>() : null;

        if (terrainCollider != null)
        {
            Bounds bounds = terrainCollider.bounds;
            Vector3 terrainOrigin = new Vector3(worldPoint.x, bounds.max.y + 80f, worldPoint.z);
            float distance = bounds.size.y + 180f;
            if (terrainCollider.Raycast(new Ray(terrainOrigin, Vector3.down), out RaycastHit terrainHit, distance))
            {
                hitPoint = terrainHit.point;
                return true;
            }
        }

        if (terrainRenderer != null)
        {
            Bounds bounds = terrainRenderer.bounds;
            Vector3 terrainOrigin = new Vector3(worldPoint.x, bounds.max.y + 80f, worldPoint.z);
            float distance = bounds.size.y + 180f;
            RaycastHit[] hits = Physics.RaycastAll(terrainOrigin, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore);
            float nearestDistance = float.MaxValue;
            RaycastHit nearest = default;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || !hits[i].collider.transform.IsChildOf(terrainSource.transform))
                {
                    continue;
                }

                if (hits[i].distance < nearestDistance)
                {
                    nearestDistance = hits[i].distance;
                    nearest = hits[i];
                }
            }

            if (nearest.collider != null)
            {
                hitPoint = nearest.point;
                return true;
            }
        }

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 400f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider != null
            && terrainSource != null
            && hit.collider.transform.IsChildOf(terrainSource.transform))
        {
            hitPoint = hit.point;
            return true;
        }

        hitPoint = default;
        return false;
    }

    private void CreateTree(Vector3 position, System.Random rng, int index)
    {
        float height = RandomRange(rng, treeMinHeight, treeMaxHeight);
        float trunkRadius = RandomRange(rng, treeMinTrunkRadius, treeMaxTrunkRadius);
        float canopyRadius = RandomRange(rng, treeMinCanopyRadius, treeMaxCanopyRadius);
        float yaw = RandomRange(rng, 0f, 360f);
        float trunkHueJitter = RandomRange(rng, 0f, 1f);
        float canopyHue = RandomRange(rng, 0.24f, 0.42f);
        float canopySaturation = RandomRange(rng, 0.55f, 0.95f);
        float canopyValue = RandomRange(rng, 0.42f, 0.82f);
        float trunkTint = RandomRange(rng, 0.18f, 0.32f);

        GameObject tree = new GameObject($"Tree_{index:00}");
        tree.transform.SetParent(_treeRoot, true);
        tree.transform.position = position - Vector3.up * treeGroundSink;
        tree.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        Rigidbody rb = tree.AddComponent<Rigidbody>();
        rb.mass = 0.8f + height * 0.12f;
        rb.linearDamping = 0.4f;
        rb.angularDamping = 0.8f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        CapsuleCollider collider = tree.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, height * 0.45f, 0f);
        collider.radius = trunkRadius * 1.15f;
        collider.height = height * 1.1f;
        collider.direction = 1;

        AudioSource audio = tree.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.loop = false;
        audio.spatialBlend = 0f;

        BreakableTree breakable = tree.AddComponent<BreakableTree>();
        breakable.smashSound = treeSmashSound;
        breakable.maxHealth = 16f + height * 2f;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, height * 0.45f, 0f);
        trunk.transform.localRotation = Quaternion.identity;
        trunk.transform.localScale = new Vector3(trunkRadius * 2f, height * 0.45f, trunkRadius * 2f);
        Renderer trunkRenderer = trunk.GetComponent<Renderer>();
        if (trunkRenderer != null)
        {
            float tint = Mathf.Clamp01(trunkTint + trunkHueJitter * 0.06f);
            ApplyRendererColor(trunkRenderer, new Color(0.26f + tint * 0.25f, 0.17f + tint * 0.16f, 0.08f + tint * 0.10f, 1f));
        }
        DestroyComponentSafe(trunk.GetComponent<Collider>());

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(tree.transform, false);
        canopy.transform.localPosition = new Vector3(0f, height * 0.95f, 0f);
        canopy.transform.localScale = Vector3.one * (canopyRadius * 2f);
        Renderer canopyRenderer = canopy.GetComponent<Renderer>();
        if (canopyRenderer != null)
        {
            Color canopyColor = Color.HSVToRGB(canopyHue, canopySaturation, canopyValue);
            canopyColor.a = 1f;
            ApplyRendererColor(canopyRenderer, canopyColor);
        }
        DestroyComponentSafe(canopy.GetComponent<Collider>());
    }

    private float RandomRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private void ApplyRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            renderer.material.color = color;
            return;
        }

        Material material = renderer.sharedMaterial;
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            renderer.sharedMaterial = material;
        }

        material.color = color;
    }

    private void DestroyComponentSafe(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
