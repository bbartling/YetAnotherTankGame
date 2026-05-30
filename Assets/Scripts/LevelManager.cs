using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public TerrainGenerator terrainGen;
    public GameObject castlePrefab;
    public GameObject turretPrefab;
    public TankController player;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI statusText;

    private int _currentLevel = 1;
    private int _remainingTurrets = 0;
    private Coroutine _statusCoroutine;

    void Awake()
    {
        Instance = this;
    }

    public void StartGame()
    {
        _currentLevel = 1;
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        // Cleanup old level
        var oldTargets = GameObject.Find("TargetsContainer");
        if (oldTargets != null) 
        {
            if (Application.isPlaying) Destroy(oldTargets);
            else DestroyImmediate(oldTargets);
        }

        // Make player kinematic during generation to prevent falling through empty space
        Rigidbody playerRb = null;
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
                playerRb.isKinematic = true;
            }
        }

        // Generate Terrain
        if (terrainGen != null) terrainGen.GenerateTerrain();

        // Calculate positions
        Vector3 castlePos = new Vector3(Random.Range(-400f, 400f), 0, Random.Range(-300f, 300f));
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(castlePos.x, 300f, castlePos.z), Vector3.down, out hit, 600f))
        {
            castlePos.y = hit.point.y;
        }

        GameObject container = new GameObject("TargetsContainer");
        GameObject castle = null;
        if (castlePrefab != null)
        {
            castle = Instantiate(castlePrefab, castlePos, Quaternion.identity, container.transform);
        }
        else
        {
            castle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            castle.name = "PlaceholderCastle";
            castle.transform.position = castlePos + Vector3.up * 5f;
            castle.transform.localScale = new Vector3(20, 10, 20);
            castle.transform.SetParent(container.transform);
        }

        int turretCount = _currentLevel;
        _remainingTurrets = turretCount;
        float castleHeight = 10f;
        if (castle != null)
        {
            var rends = castle.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                castleHeight = Mathf.Max(castleHeight, r.bounds.size.y);
            }
        }

        for (int i = 0; i < turretCount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-8f, 8f), castleHeight + 2f, Random.Range(-8f, 8f));
            Vector3 turretPos = castle.transform.position + offset;

            if (turretPrefab != null)
            {
                GameObject turret = Instantiate(turretPrefab, turretPos, Quaternion.identity, container.transform);
                var ai = turret.GetComponent<EnemyTurret>();
                if (ai != null)
                {
                    ai.accuracy = Mathf.Clamp(0.2f + (_currentLevel * 0.1f), 0f, 1f);
                    ai.fireRate = Mathf.Max(1.5f, 5f - (_currentLevel * 0.5f));
                }
            }
        }

        // Reset Player to safe distance
        Vector3 playerDropZone;
        int attempts = 0;
        do
        {
            playerDropZone = new Vector3(Random.Range(-450f, 450f), 0, Random.Range(-350f, 350f));
            attempts++;
        } while (Vector3.Distance(playerDropZone, castlePos) < 400f && attempts < 100);

        // Spawn Plane and attach player
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.name = "AirdropPlane";
        plane.transform.localScale = new Vector3(10f, 2f, 20f); // Slightly larger plane
        
        // Position plane to fly over the drop zone
        float flightHeight = 110f; // As high as the mountains (100f) + a bit more
        Vector3 planeStartPos = playerDropZone + new Vector3(-300f, flightHeight, 0f);
        plane.transform.position = planeStartPos;
        plane.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        
        PlaneFlyby flyby = plane.AddComponent<PlaneFlyby>();
        flyby.speed = 120f; // Fast plane

        if (player != null)
        {
            player.transform.SetParent(plane.transform);
            player.transform.localPosition = new Vector3(0, -2f, 0); // Hang slightly below
            player.transform.localRotation = Quaternion.identity;
            
            // Fix Tank Scale and Collider
            player.transform.localScale = Vector3.one * 0.05f; 
            
            BoxCollider col = player.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.center = Vector3.zero;
                col.size = new Vector3(4f, 2f, 6f); // Match the body dimensions roughly
            }
        }

        // Show flashing status and handle drop timing
        if (_statusCoroutine != null) StopCoroutine(_statusCoroutine);
        _statusCoroutine = StartCoroutine(LevelTransitionSequence(plane, playerDropZone));

        UpdateUI();
    }

    private System.Collections.IEnumerator LevelTransitionSequence(GameObject plane, Vector3 dropPos)
    {
        if (statusText == null) yield break;
        
        // Instant terrain generation feel
        statusText.text = "LEVEL GENERATED";
        statusText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f); // Shortest possible pause for visual confirmation

        statusText.text = "INBOUND...";
        
        // Wait until plane is close to the drop position
        while (plane != null && plane.transform.position.x < dropPos.x)
        {
            yield return null;
        }

        statusText.text = "DROP!";
        if (player != null)
        {
            player.transform.SetParent(null);
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                // Give it forward momentum AND a downward push
                rb.linearVelocity = new Vector3(50f, -20f, 0f); 
            }
        }

        yield return new WaitForSeconds(0.5f);
        statusText.gameObject.SetActive(false);
        _statusCoroutine = null;
    }

    public void TurretDestroyed()
    {
        _remainingTurrets--;
        if (_remainingTurrets <= 0)
        {
            _currentLevel++;
            Debug.Log("Level Clear! Loading Level " + _currentLevel);
            Invoke(nameof(GenerateLevel), 3f);
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        if (levelText != null) 
            levelText.text = "Level: " + _currentLevel + "\nTurrets: " + _remainingTurrets;
    }
}
