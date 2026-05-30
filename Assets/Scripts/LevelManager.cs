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
        if (oldTargets != null) Destroy(oldTargets);

        // Generate Terrain
        if (terrainGen != null) terrainGen.GenerateTerrain();

        // Show flashing status
        if (_statusCoroutine != null) StopCoroutine(_statusCoroutine);
        _statusCoroutine = StartCoroutine(LevelTransitionSequence());

        // Spawn Castle randomly
        GameObject container = new GameObject("TargetsContainer");
        Vector3 castlePos = new Vector3(Random.Range(-400f, 400f), 0, Random.Range(-300f, 300f));
        
        // Find safe y pos for castle (on ground)
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(castlePos.x, 300f, castlePos.z), Vector3.down, out hit, 600f))
        {
            castlePos.y = hit.point.y;
        }

        GameObject castle = null;
        if (castlePrefab != null)
        {
            castle = Instantiate(castlePrefab, castlePos, Quaternion.identity, container.transform);
        }
        else
        {
            // Placeholder castle
            castle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            castle.name = "PlaceholderCastle";
            castle.transform.position = castlePos + Vector3.up * 5f;
            castle.transform.localScale = new Vector3(20, 10, 20);
            castle.transform.SetParent(container.transform);
        }

        // Spawn Turrets on Castle
        int turretCount = _currentLevel;
        _remainingTurrets = turretCount;
        for (int i = 0; i < turretCount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-8f, 8f), 10f, Random.Range(-8f, 8f));
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

        // Reset Player to safe distance (far away from castle)
        if (player != null)
        {
            Vector3 playerPos;
            int attempts = 0;
            do
            {
                playerPos = new Vector3(Random.Range(-450f, 450f), 0, Random.Range(-350f, 350f));
                attempts++;
            } while (Vector3.Distance(playerPos, castlePos) < 400f && attempts < 100);

            // AIR DROP EFFECT: Start high up
            playerPos.y = 100f; 
            
            player.transform.position = playerPos;
            player.transform.rotation = Quaternion.identity;
            
            var playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }
        }

        UpdateUI();
    }

    private System.Collections.IEnumerator LevelTransitionSequence()
    {
        if (statusText == null) yield break;
        
        // 1. Generating Terrain
        statusText.text = "GENERATING TERRAIN...";
        statusText.gameObject.SetActive(true);
        float elapsed = 0;
        while (elapsed < 3f)
        {
            statusText.enabled = !statusText.enabled;
            yield return new WaitForSeconds(0.3f);
            elapsed += 0.3f;
        }

        // 2. Air Dropping
        statusText.enabled = true;
        statusText.text = "AIR DROPPING...";
        elapsed = 0;
        while (elapsed < 2f)
        {
            statusText.enabled = !statusText.enabled;
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        statusText.enabled = true;
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
