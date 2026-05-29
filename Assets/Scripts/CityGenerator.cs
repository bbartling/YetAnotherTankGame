using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    public GameObject boxPrefab;
    public GameObject enemyTankPrefab;
    public int buildingCount = 50;
    public int enemyCount = 10;
public Vector2 spawnArea = new Vector2(500, 400);
    public float minBuildingHeight = 3;
    public float maxBuildingHeight = 8;

    void Start()
    {
        GenerateCity();
    }

    public void GenerateCity()
    {
        for (int i = 0; i < buildingCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
                0,
                Random.Range(-spawnArea.y / 2, spawnArea.y / 2)
            );

            // Larger safe zone around center for the player
            if (pos.magnitude < 50f) continue;

            int type = Random.Range(0, 4); // 0: Building, 1: Tower, 2: Ramp, 3: Bridge
            switch (type)
            {
                case 0: SpawnBuilding(pos); break;
                case 1: SpawnTower(pos); break;
                case 2: SpawnRamp(pos); break;
                case 3: SpawnBridge(pos); break;
            }
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
                2,
                Random.Range(-spawnArea.y / 2, spawnArea.y / 2)
            );

            if (pos.magnitude < 50f) continue;
            if (enemyTankPrefab != null) Instantiate(enemyTankPrefab, pos, Quaternion.identity);
        }
    }

    void SpawnBuilding(Vector3 pos)
    {
        int width = Random.Range(2, 5);
        int depth = Random.Range(2, 5);
        int height = Random.Range((int)minBuildingHeight, (int)maxBuildingHeight);

        GameObject buildingRoot = new GameObject("Building");
        buildingRoot.transform.position = pos;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    CreateBox(new Vector3(x, y + 0.5f, z), buildingRoot.transform);
                }
            }
        }
    }

    void SpawnTower(Vector3 pos)
    {
        int height = Random.Range(5, 12);
        GameObject towerRoot = new GameObject("Tower");
        towerRoot.transform.position = pos;

        for (int y = 0; y < height; y++)
        {
            CreateBox(new Vector3(0, y + 0.5f, 0), towerRoot.transform);
        }
    }

    void SpawnRamp(Vector3 pos)
    {
        GameObject rampRoot = new GameObject("Ramp");
        rampRoot.transform.position = pos;
        rampRoot.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        int length = Random.Range(5, 10);
        for (int i = 0; i < length; i++)
        {
            GameObject box = CreateBox(new Vector3(0, (i * 0.5f) + 0.25f, i), rampRoot.transform);
            box.transform.localScale = new Vector3(3, 0.5f, 1);
            // Freeze it or make it heavy so it stays? Actually, the user wants "ramps to drive across".
            // If they are regular rigidbodies they will fall. 
            // I'll make them kinematic or static for ramps/bridges to be driveable? 
            // Or just make them very heavy. 
            // User said "random piles of the boxes that resemble buildings".
            // Let's make the ramp pieces non-kinematic but heavy.
            var rb = box.GetComponent<Rigidbody>();
            if (rb != null) rb.mass = 500f; 
        }
    }

    void SpawnBridge(Vector3 pos)
    {
        GameObject bridgeRoot = new GameObject("Bridge");
        bridgeRoot.transform.position = pos;
        bridgeRoot.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        // Two pillars
        for (int y = 0; y < 5; y++)
        {
            CreateBox(new Vector3(-5, y + 0.5f, 0), bridgeRoot.transform);
            CreateBox(new Vector3(5, y + 0.5f, 0), bridgeRoot.transform);
        }

        // Bridge deck
        GameObject deck = CreateBox(new Vector3(0, 5.25f, 0), bridgeRoot.transform);
        deck.transform.localScale = new Vector3(12, 0.5f, 3);
        var rb = deck.GetComponent<Rigidbody>();
        if (rb != null) rb.mass = 1000f;
    }

    GameObject CreateBox(Vector3 localPos, Transform parent)
    {
        GameObject box;
        if (boxPrefab != null)
        {
            box = Instantiate(boxPrefab, parent);
            box.transform.localPosition = localPos;
        }
        else
        {
            box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(parent);
            box.transform.localPosition = localPos;
            box.AddComponent<Rigidbody>();
        }
        return box;
    }
}
