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
    public bool randomizeSeedEachRun = true;

    [Header("Crater Settings")]
    public float baseRadius = 6.5f;
    public float baseDepth = 4.2f;
    public float forceRadiusScale = 0.075f;
    public float forceDepthScale = 0.06f;
    public float rimLift = 0.05f;
    public float roughness = 0.06f;
    public float chunkThreshold = 16f;
    public float craterCoreShape = 1.7f;
    public float craterRimWidth = 0.32f;
    public float craterCorePunchScale = 0.85f;
    public float craterNoiseFrequency = 0.018f;
    public float craterNoiseScale = 0.45f;

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
    public float hillRadiusHeightScale = 2.75f;
    public float hillBaseShape = 1.4f;
    public float hillPeakFlattening = 0.65f;
    public float hillNoiseFrequency = 0.0075f;
    public float hillNoiseScale = 0.22f;

    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private Mesh _runtimeMesh;
    private Vector3[] _vertices;
    private Vector3[] _baseVertices;
    private Vector2[] _uvs;
    private int[] _triangles;
    private int _vertexCountX;
    private int _vertexCountZ;
    private float _halfWidth;
    private float _halfLength;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();

        // Keep the world scale baked into the mesh so play mode does not depend on transform scale.
        transform.localScale = Vector3.one;

        if (generateOnAwake)
        {
            RandomizeFeatureSeedIfNeeded();
            RegenerateTerrain();
        }
    }

    [ContextMenu("Regenerate Terrain")]
    public void RegenerateTerrain()
    {
        EnsureRuntimeReferences();
        RandomizeFeatureSeedIfNeeded();
        BuildTerrainMesh();
        SeedBattlefieldNoise();
    }

    [ContextMenu("Clear Craters And Hills")]
    public void ClearTerrainDamage()
    {
        EnsureRuntimeReferences();
        BuildTerrainMesh();
    }

    private void SeedBattlefieldNoise()
    {
        EnsureRuntimeReferences();
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
        EnsureRuntimeReferences();
        if (_meshFilter == null)
        {
            return;
        }

        _runtimeMesh = new Mesh
        {
            name = "Crater Terrain Runtime Mesh"
        };
        _runtimeMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        _vertexCountX = Mathf.Max(2, xSegments + 1);
        _vertexCountZ = Mathf.Max(2, zSegments + 1);
        _vertices = new Vector3[_vertexCountX * _vertexCountZ * 2];
        _baseVertices = new Vector3[_vertices.Length];
        _uvs = new Vector2[_vertices.Length];
        _triangles = new int[xSegments * zSegments * 12];

        _halfWidth = terrainWidth * 0.5f;
        _halfLength = terrainLength * 0.5f;

        int v = 0;
        for (int z = 0; z < _vertexCountZ; z++)
        {
            float zT = z / (float)(_vertexCountZ - 1);
            float zPos = Mathf.Lerp(-_halfLength, _halfLength, zT);

            for (int x = 0; x < _vertexCountX; x++)
            {
                float xT = x / (float)(_vertexCountX - 1);
                float xPos = Mathf.Lerp(-_halfWidth, _halfWidth, xT);

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
        int stride = _vertexCountX * 2;
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

    private void RandomizeFeatureSeedIfNeeded()
    {
        if (!randomizeSeedEachRun)
        {
            return;
        }

        randomFeatureSeed = unchecked((int)System.DateTime.UtcNow.Ticks) ^ System.Environment.TickCount;
    }

    private void RaiseTerrain(Vector3 worldPoint, float radius, float height)
    {
        EnsureRuntimeReferences();
        if (_runtimeMesh == null || _vertices == null || _vertices.Length == 0)
        {
            return;
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        radius = Mathf.Max(0.75f, radius + height * hillRadiusHeightScale);
        height = Mathf.Max(0.1f, height);
        float noiseFrequency = hillNoiseFrequency / Mathf.Max(1f, radius * 0.25f);
        float noiseScale = hillNoiseScale * Mathf.Clamp01(height / 16f);
        float sigma = Mathf.Max(0.5f, radius * Mathf.Lerp(0.42f, 0.62f, Mathf.Clamp01(height / 18f)));

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
            float dome = Mathf.Exp(-(dist * dist) / (2f * sigma * sigma));
            float shape = Mathf.Pow(Mathf.Clamp01(dome), hillBaseShape);
            float plateau = Mathf.SmoothStep(0.35f, 1f, falloff);
            float noise = (Mathf.PerlinNoise(vertex.x * noiseFrequency + height * 0.031f, vertex.z * noiseFrequency - height * 0.031f) - 0.5f) * 2f;
            float lift = height * (shape * 0.78f + plateau * 0.22f);
            lift += noise * noiseScale * shape;
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
        EnsureRuntimeReferences();
        if (_runtimeMesh == null || _vertices == null || _vertices.Length == 0)
        {
            return;
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        float radius = Mathf.Max(0.75f, baseRadius + force * forceRadiusScale);
        float depth = Mathf.Max(0.1f, baseDepth + force * forceDepthScale);
        float noiseAmount = roughness * Mathf.Clamp01(force / 100f);
        float corePunch = Mathf.SmoothStep(chunkThreshold, chunkThreshold * 1.9f, force);
        float rimStart = Mathf.Clamp01(1f - craterRimWidth);
        float coreShape = Mathf.Max(0.5f, craterCoreShape);
        float noiseFrequency = craterNoiseFrequency / Mathf.Max(1f, radius * 0.25f);

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
            float centerBias = Mathf.Pow(Mathf.Clamp01(falloff), coreShape);
            float bowl = Mathf.SmoothStep(0f, 1f, centerBias);
            float rim = Mathf.SmoothStep(rimStart, 1f, falloff);
            float noise = (Mathf.PerlinNoise(vertex.x * noiseFrequency + force * 0.031f, vertex.z * noiseFrequency - force * 0.031f) - 0.5f) * 2f;

            float sink = depth * (0.72f * bowl + 0.28f * bowl * bowl);
            sink += depth * corePunch * craterCorePunchScale * bowl * bowl * 0.6f;
            sink += noise * noiseAmount * craterNoiseScale * centerBias;
            sink -= depth * rimLift * rim * 0.8f;

            vertex.y -= sink;

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

    private void EnsureRuntimeReferences()
    {
        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        if (_meshCollider == null)
        {
            _meshCollider = GetComponent<MeshCollider>();
        }
    }
}
