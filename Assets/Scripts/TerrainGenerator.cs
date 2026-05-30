using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int width = 100;
    public int length = 80;
    public float scale = 20f;
    public float heightMultiplier = 15f;
    public float wallHeight = 100f;
    public Material terrainMaterial;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;

    void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (_meshFilter != null) return;

        _meshFilter = gameObject.GetComponent<MeshFilter>();
        if (_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();
        
        _meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        _meshCollider = gameObject.GetComponent<MeshCollider>();
        if (_meshCollider == null) _meshCollider = gameObject.AddComponent<MeshCollider>();
        
        if (terrainMaterial != null) _meshRenderer.material = terrainMaterial;
        gameObject.tag = "Mountain";
    }

    public void GenerateTerrain()
    {
        Init();
        CreateBoundaryWalls();
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(width + 1) * (length + 1)];
        int[] triangles = new int[width * length * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        float xOffset = Random.Range(0, 1000f);
        float zOffset = Random.Range(0, 1000f);

        for (int i = 0, z = 0; z <= length; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float xCoord = (float)x / width * scale + xOffset;
                float zCoord = (float)z / length * scale + zOffset;
                
                // Base noise - REMOVED for flat terrain
                float y = 0f;
                
                // Wall logic: boost height at edges
                if (x == 0 || x == width || z == 0 || z == length)
                {
                    y = wallHeight;
                }
                else if (x < 10 || x > width - 10 || z < 10 || z > length - 10)
                {
                    float edgeDistX = Mathf.Min(x, width - x);
                    float edgeDistZ = Mathf.Min(z, length - z);
                    float edgeDist = Mathf.Min(edgeDistX, edgeDistZ);
                    y = Mathf.Lerp(wallHeight, 0f, edgeDist / 10f);
                }

                // Double map size: center at 0, spread to 1000x800
                float posX = (x * (1000f / width)) - 500f;
                float posZ = (z * (800f / length)) - 400f;
                vertices[i] = new Vector3(posX, y, posZ);
                uvs[i] = new Vector2((float)x / width, (float)z / length);
                i++;
            }
        }

        int vert = 0;
        int tris = 0;
        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        _meshFilter.mesh = mesh;
        _meshCollider.sharedMesh = mesh;
        
        // Ensure collider is updated
        _meshCollider.enabled = false;
        _meshCollider.enabled = true;
    }

    private void CreateBoundaryWalls()
    {
        // Cleanup existing walls
        var oldWalls = GameObject.Find("BoundaryWalls");
        if (oldWalls != null)
        {
            if (Application.isPlaying) Destroy(oldWalls);
            else DestroyImmediate(oldWalls);
        }

        GameObject root = new GameObject("BoundaryWalls");
root.transform.SetParent(this.transform);

        float mapWidth = 1000f;
        float mapLength = 800f;
        float wallThickness = 10f;
        float height = 1000f; // Very tall

        // Front wall
        SpawnWall("Wall_Front", new Vector3(0, height / 2, mapLength / 2 + wallThickness / 2), new Vector3(mapWidth + wallThickness * 2, height, wallThickness), root.transform);
        // Back wall
        SpawnWall("Wall_Back", new Vector3(0, height / 2, -mapLength / 2 - wallThickness / 2), new Vector3(mapWidth + wallThickness * 2, height, wallThickness), root.transform);
        // Left wall
        SpawnWall("Wall_Left", new Vector3(-mapWidth / 2 - wallThickness / 2, height / 2, 0), new Vector3(wallThickness, height, mapLength), root.transform);
        // Right wall
        SpawnWall("Wall_Right", new Vector3(mapWidth / 2 + wallThickness / 2, height / 2, 0), new Vector3(wallThickness, height, mapLength), root.transform);
    }

    private void SpawnWall(string name, Vector3 pos, Vector3 scale, Transform parent)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        
        var col = wall.AddComponent<BoxCollider>();
        // No MeshRenderer makes it invisible
        wall.tag = "Mountain"; 
    }
}
