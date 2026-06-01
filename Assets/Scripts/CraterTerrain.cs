using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class CraterTerrain : MonoBehaviour
{
    [Header("Mesh Resolution")]
    [Range(8, 256)] public int xSegments = 140;
    [Range(8, 256)] public int zSegments = 160;

    [Header("Terrain Shape")]
    public float terrainWidth = 1200f;
    public float terrainLength = 1400f;
    public float terrainThickness = 0.6f;

    [Header("Generation")]
    public bool generateOnAwake = true;

    [Header("Crater Settings")]
    public float baseRadius = 6.5f;
    public float baseDepth = 4.2f;
    public float forceRadiusScale = 0.075f;
    public float forceDepthScale = 0.06f;
    public float rimLift = 0.05f;
    public float roughness = 0.06f;
    public float chunkThreshold = 16f;

    [Header("Random Battlefield")]
    public bool seedRandomCraters = true;
    public bool seedRandomHills = true;
    public int randomCraterCount = 80;
    public int randomHillCount = 56;
    public int randomFeatureSeed = 1307;
    public float featureMargin = 120f;
    public float craterForceMin = 92f;
    public float craterForceMax = 165f;
    public float hillHeightMin = 3.5f;
    public float hillHeightMax = 12f;
    public float hillRadiusMin = 12f;
    public float hillRadiusMax = 34f;

    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private Mesh _runtimeMesh;
    private Vector3[] _vertices;
    private Vector3[] _baseVertices;
    private Vector2[] _uvs;
    private int[] _triangles;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();

        // Keep the world scale baked into the mesh so play mode does not depend on transform scale.
        transform.localScale = Vector3.one;

        if (generateOnAwake)
        {
            RegenerateTerrain();
        }
    }

    [ContextMenu("Regenerate Terrain")]
    public void RegenerateTerrain()
    {
        BuildTerrainMesh();
        SeedBattlefieldNoise();
    }

    [ContextMenu("Clear Craters And Hills")]
    public void ClearTerrainDamage()
    {
        BuildTerrainMesh();
    }

    private void SeedBattlefieldNoise()
    {
        if (_runtimeMesh == null)
        {
            return;
        }

        System.Random rng = new System.Random(randomFeatureSeed);

        if (seedRandomCraters)
        {
            for (int i = 0; i < randomCraterCount; i++)
            {
                Vector3 point = RandomWorldPoint(rng, featureMargin);
                float force = RandomRange(rng, craterForceMin, craterForceMax);
                ApplyImpact(point, Vector3.up, force);
            }
        }

        if (seedRandomHills)
        {
            for (int i = 0; i < randomHillCount; i++)
            {
                Vector3 point = RandomWorldPoint(rng, featureMargin);
                float radius = RandomRange(rng, hillRadiusMin, hillRadiusMax);
                float height = RandomRange(rng, hillHeightMin, hillHeightMax);
                RaiseTerrain(point, radius, height);
            }
        }
    }

    private Vector3 RandomWorldPoint(System.Random rng, float margin)
    {
        float halfWidth = terrainWidth * 0.5f - margin;
        float halfLength = terrainLength * 0.5f - margin;

        float x = RandomRange(rng, -halfWidth, halfWidth);
        float z = RandomRange(rng, -halfLength, halfLength);
        return transform.TransformPoint(new Vector3(x, 0.2f, z));
    }

    private float RandomRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private void BuildTerrainMesh()
    {
        if (_meshFilter == null)
        {
            return;
        }

        _runtimeMesh = new Mesh
        {
            name = "Crater Terrain Runtime Mesh"
        };
        _runtimeMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int vertCountX = Mathf.Max(2, xSegments + 1);
        int vertCountZ = Mathf.Max(2, zSegments + 1);
        _vertices = new Vector3[vertCountX * vertCountZ * 2];
        _baseVertices = new Vector3[_vertices.Length];
        _uvs = new Vector2[_vertices.Length];
        _triangles = new int[xSegments * zSegments * 12];

        float halfWidth = terrainWidth * 0.5f;
        float halfLength = terrainLength * 0.5f;

        int v = 0;
        for (int z = 0; z < vertCountZ; z++)
        {
            float zT = z / (float)(vertCountZ - 1);
            float zPos = Mathf.Lerp(-halfLength, halfLength, zT);

            for (int x = 0; x < vertCountX; x++)
            {
                float xT = x / (float)(vertCountX - 1);
                float xPos = Mathf.Lerp(-halfWidth, halfWidth, xT);

                Vector3 top = new Vector3(xPos, 0f, zPos);
                Vector3 bottom = new Vector3(xPos, -terrainThickness, zPos);

                _vertices[v] = top;
                _baseVertices[v] = top;
                _uvs[v] = new Vector2(xT, zT);
                v++;

                _vertices[v] = bottom;
                _baseVertices[v] = bottom;
                _uvs[v] = new Vector2(xT, zT);
                v++;
            }
        }

        int t = 0;
        int stride = vertCountX * 2;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int row = z * stride + x * 2;
                int nextRow = (z + 1) * stride + x * 2;

                // Top surface
                _triangles[t++] = row;
                _triangles[t++] = nextRow;
                _triangles[t++] = row + 2;

                _triangles[t++] = row + 2;
                _triangles[t++] = nextRow;
                _triangles[t++] = nextRow + 2;

                // Bottom surface
                _triangles[t++] = row + 1;
                _triangles[t++] = row + 3;
                _triangles[t++] = nextRow + 1;

                _triangles[t++] = row + 3;
                _triangles[t++] = nextRow + 3;
                _triangles[t++] = nextRow + 1;
            }
        }

        _runtimeMesh.vertices = _vertices;
        _runtimeMesh.uv = _uvs;
        _runtimeMesh.triangles = _triangles;
        _runtimeMesh.RecalculateNormals();
        _runtimeMesh.RecalculateBounds();

        _meshFilter.sharedMesh = _runtimeMesh;
        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _runtimeMesh;
        }
    }

    private void RaiseTerrain(Vector3 worldPoint, float radius, float height)
    {
        if (_runtimeMesh == null || _vertices == null || _vertices.Length == 0)
        {
            return;
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        radius = Mathf.Max(0.5f, radius);
        height = Mathf.Max(0.1f, height);

        for (int i = 0; i < _vertices.Length; i += 2)
        {
            Vector3 vertex = _baseVertices[i];
            float dx = vertex.x - localPoint.x;
            float dz = vertex.z - localPoint.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            if (dist > radius)
            {
                continue;
            }

            float falloff = 1f - (dist / radius);
            float lift = height * falloff * falloff;
            vertex.y += lift;

            _vertices[i] = vertex;
            _vertices[i + 1] = new Vector3(vertex.x, vertex.y - terrainThickness, vertex.z);
        }

        _runtimeMesh.vertices = _vertices;
        _runtimeMesh.RecalculateNormals();
        _runtimeMesh.RecalculateBounds();

        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _runtimeMesh;
        }
    }

    public void ApplyImpact(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        if (_runtimeMesh == null || _vertices == null || _vertices.Length == 0)
        {
            return;
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        float radius = Mathf.Max(0.4f, baseRadius + force * forceRadiusScale);
        float depth = Mathf.Max(0.1f, baseDepth + force * forceDepthScale);
        float noiseAmount = roughness * Mathf.Clamp01(force / 60f);
        bool punchHole = force >= chunkThreshold;

        for (int i = 0; i < _vertices.Length; i += 2)
        {
            Vector3 vertex = _baseVertices[i];
            float dx = vertex.x - localPoint.x;
            float dz = vertex.z - localPoint.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            if (dist > radius)
            {
                continue;
            }

            float falloff = 1f - (dist / radius);
            float centerBias = falloff * falloff;
            float noise = (Mathf.PerlinNoise(vertex.x * 17f + force * 0.03f, vertex.z * 17f - force * 0.03f) - 0.5f) * 2f;
            float lift = 0f;

            if (dist > radius * 0.68f)
            {
                float rimT = Mathf.InverseLerp(radius, radius * 0.68f, dist);
                lift = depth * rimLift * rimT;
            }

            float sink = depth * centerBias;
            sink += noise * noiseAmount * centerBias;

            if (punchHole && dist < radius * 0.34f)
            {
                sink += depth * 4.5f;
            }

            vertex.y -= sink;
            vertex.y += lift;

            if (punchHole && dist < radius * 0.48f)
            {
                Vector2 away2D = new Vector2(dx, dz);
                if (away2D.sqrMagnitude > 0.0001f)
                {
                    away2D.Normalize();
                    vertex.x += away2D.x * 0.01f * centerBias;
                    vertex.z += away2D.y * 0.01f * centerBias;
                }
            }

            _vertices[i] = vertex;
            _vertices[i + 1] = new Vector3(vertex.x, vertex.y - terrainThickness, vertex.z);
        }

        _runtimeMesh.vertices = _vertices;
        _runtimeMesh.RecalculateNormals();
        _runtimeMesh.RecalculateBounds();

        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _runtimeMesh;
        }
    }
}
