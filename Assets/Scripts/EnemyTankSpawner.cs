using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyTankSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject enemyTankPrefab;

    [Header("Spawn Control")]
    public int enemyCount = 5;
    public bool clearExistingEnemies = true;
    public Transform spawnParent;

    [Header("Spawn Area")]
    public float spawnMargin = 90f;
    public float spawnHeight = 45f;
    public float minDistanceFromPlayer = 135f;
    public float minDistanceFromCastle = 80f;
    public int maxAttemptsPerTank = 40;

    [Header("Fallback")]
    public Transform playerTank;
    public Transform castle;
    public BattlefieldDirector battlefieldDirector;

    private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();

    private void Awake()
    {
        ResolveReferences();
    }

    public void SpawnEnemyTanks(int desiredCount)
    {
        enemyCount = Mathf.Max(0, desiredCount);
        ResolveReferences();
        if (battlefieldDirector != null)
        {
            battlefieldDirector.SetEnemyCount(enemyCount);
        }

        if (clearExistingEnemies)
        {
            ClearExistingEnemies();
        }

        _spawnedEnemies.Clear();

        if (enemyTankPrefab == null)
        {
            Debug.LogWarning("[EnemyTankSpawner] No enemyTankPrefab assigned.");
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPosition = FindSpawnPosition(i);
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject enemy = Instantiate(enemyTankPrefab, spawnPosition, rotation, spawnParent);
            enemy.name = $"EnemyTank_{i + 1}";
            _spawnedEnemies.Add(enemy);

            EnemyTankAI ai = enemy.GetComponent<EnemyTankAI>();
            if (ai != null)
            {
                ApplySpawnProfile(ai, i, enemyCount);
            }
        }
    }

    private void ResolveReferences()
    {
        if (playerTank == null)
        {
            GameObject player = GameObject.Find("PlayerTank");
            if (player != null)
            {
                playerTank = player.transform;
            }
        }

        if (castle == null)
        {
            GameObject castleObject = GameObject.Find("Castle");
            if (castleObject == null)
            {
                castleObject = GameObject.Find("Castle(Clone)");
            }

            if (castleObject != null)
            {
                castle = castleObject.transform;
            }
        }

        if (battlefieldDirector == null)
        {
            battlefieldDirector = Object.FindFirstObjectByType<BattlefieldDirector>();
        }

        if (spawnParent == null)
        {
            GameObject mapRoot = GameObject.Find("MapRoot");
            if (mapRoot != null)
            {
                spawnParent = mapRoot.transform;
            }
        }
    }

    private void ClearExistingEnemies()
    {
        EnemyTankAI[] existing = Object.FindObjectsByType<EnemyTankAI>(FindObjectsSortMode.None);
        foreach (EnemyTankAI tank in existing)
        {
            if (tank != null)
            {
                Destroy(tank.gameObject);
            }
        }
    }

    private Vector3 FindSpawnPosition(int index)
    {
        CraterTerrain terrain = Object.FindFirstObjectByType<CraterTerrain>();
        float halfWidth = terrain != null ? terrain.terrainWidth * 0.5f : 520f;
        float halfLength = terrain != null ? terrain.terrainLength * 0.5f : 620f;
        Vector3 playerPosition = playerTank != null ? playerTank.position : Vector3.zero;
        Vector3 castlePosition = castle != null ? castle.position : Vector3.zero;
        Vector3 travelAxis = GetPlayerTravelAxis();
        Vector3 sideAxis = new Vector3(-travelAxis.z, 0f, travelAxis.x);
        bool sideLeft = (index % 2) == 0;
        bool forwardBias = (index % 4) < 2;

        for (int attempt = 0; attempt < maxAttemptsPerTank; attempt++)
        {
            float ringT = Random.value;
            float distance = Mathf.Lerp(ambushRingMin(), ambushRingMax(), ringT);
            float angleJitter = Random.Range(-35f, 35f);
            float arcJitter = Random.Range(-12f, 12f);
            float sideShift = Random.Range(-ambushSideSpread(), ambushSideSpread());

            Vector3 anchor = playerPosition + Quaternion.Euler(0f, angleJitter, 0f) * travelAxis * distance;
            anchor += sideAxis * (sideLeft ? -sideShift : sideShift);

            if (!forwardBias)
            {
                anchor = playerPosition + Quaternion.Euler(0f, arcJitter + (sideLeft ? -110f : 110f), 0f) * travelAxis * (distance * 0.92f);
            }

            Vector3 candidate = new Vector3(
                Mathf.Clamp(anchor.x, -halfWidth + spawnMargin, halfWidth - spawnMargin),
                spawnHeight,
                Mathf.Clamp(anchor.z, -halfLength + spawnMargin, halfLength - spawnMargin)
            );

            Vector3 groundPoint = SampleGround(candidate);
            if (groundPoint == Vector3.zero)
            {
                continue;
            }

            if (Vector3.Distance(groundPoint, playerPosition) < minDistanceFromPlayer)
            {
                continue;
            }

            if (castle != null && Vector3.Distance(groundPoint, castlePosition) < minDistanceFromCastle)
            {
                continue;
            }

            bool tooCloseToSpawn = false;
            for (int i = 0; i < _spawnedEnemies.Count; i++)
            {
                if (_spawnedEnemies[i] == null)
                {
                    continue;
                }

                if (Vector3.Distance(groundPoint, _spawnedEnemies[i].transform.position) < 28f)
                {
                    tooCloseToSpawn = true;
                    break;
                }
            }

            if (tooCloseToSpawn)
            {
                continue;
            }

            return groundPoint + Vector3.up * 0.18f;
        }

        Vector3 fallback = playerPosition + travelAxis * (ambushRingMin() + index * 10f);
        fallback.y = spawnHeight;
        Vector3 fallbackGround = SampleGround(fallback);
        if (fallbackGround != Vector3.zero)
        {
            return fallbackGround + Vector3.up * 0.18f;
        }

        return fallback;
    }

    private void ApplySpawnProfile(EnemyTankAI ai, int index, int totalCount)
    {
        float difficulty = battlefieldDirector != null ? battlefieldDirector.GetDifficultyMultiplier() : 1f;
        float normalizedCount = totalCount > 0 ? Mathf.Clamp01((float)index / Mathf.Max(1f, totalCount - 1f)) : 0f;

        if (castle != null)
        {
            ai.patrolCenter = castle;
        }
        else if (spawnParent != null)
        {
            ai.patrolCenter = spawnParent;
        }

        ai.patrolRadius = Mathf.Lerp(52f, 78f, normalizedCount) + totalCount * 0.8f;
        ai.patrolClockwise = (index % 2) == 0;
        ai.patrolStartAngle = Random.Range(0f, 360f);
        ai.patrolJitter = Mathf.Lerp(0.2f, 0.55f, normalizedCount);

        ai.detectionRange = Mathf.Min(300f, ai.detectionRange * Mathf.Lerp(1f, 1.18f, difficulty - 1f));
        ai.preferredDistance = Mathf.Clamp(ai.preferredDistance * Mathf.Lerp(1f, 0.92f, normalizedCount), 62f, 104f);
        ai.retreatDistance = Mathf.Clamp(ai.retreatDistance * Mathf.Lerp(1f, 0.88f, normalizedCount), 32f, ai.preferredDistance - 12f);
        ai.moveForce *= Mathf.Lerp(1f, 1.18f, difficulty - 1f);
        ai.turnTorque *= Mathf.Lerp(1f, 1.12f, difficulty - 1f);
        ai.strafeForce *= Mathf.Lerp(1f, 1.15f, normalizedCount);
        ai.shellPower *= Mathf.Lerp(1f, 1.2f, difficulty - 1f);
        ai.fireCooldown = Mathf.Max(0.95f, ai.fireCooldown / Mathf.Lerp(1f, 1.18f, difficulty - 1f));
        ai.aimSpeed *= Mathf.Lerp(1f, 1.12f, difficulty - 1f);
        ai.accuracy = Mathf.Clamp01(ai.accuracy + 0.04f * difficulty + (0.03f * normalizedCount));
        ai.lastKnownMemorySeconds = Mathf.Clamp(ai.lastKnownMemorySeconds + difficulty * 0.6f, 5f, 12f);
        ai.searchOrbitSpeed *= Mathf.Lerp(1f, 1.18f, normalizedCount);
        ai.searchMoveForce *= Mathf.Lerp(1f, 1.18f, difficulty - 1f);
        ai.searchTurnTorque *= Mathf.Lerp(1f, 1.16f, difficulty - 1f);
        ai.ambushRadius = Mathf.Lerp(16f, 26f, normalizedCount);
        ai.patrolMoveForce *= Mathf.Lerp(1f, 1.15f, difficulty - 1f);
        ai.patrolTurnTorque *= Mathf.Lerp(1f, 1.15f, difficulty - 1f);
        ai.maxHealth = Mathf.RoundToInt(Mathf.Lerp(90f, 130f, normalizedCount));
        ai.projectileDirectDamage = Mathf.Max(ai.projectileDirectDamage, 110f);
        ai.projectileBlastDamage = Mathf.Max(ai.projectileBlastDamage, 92f);
        ai.collisionDamage = Mathf.Max(ai.collisionDamage, 64f);
        ai.fatalImpactThreshold = Mathf.Clamp(ai.fatalImpactThreshold, 0.3f, 0.45f);
        ai.deathDelay = Mathf.Clamp(ai.deathDelay, 0.2f, 0.45f);
    }

    private Vector3 SampleGround(Vector3 origin)
    {
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, spawnHeight * 3f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return Vector3.zero;
    }


    private Vector3 GetPlayerTravelAxis()
    {
        if (playerTank == null || castle == null)
        {
            return Vector3.forward;
        }

        Vector3 axis = castle.position - playerTank.position;
        axis.y = 0f;
        if (axis.sqrMagnitude < 0.01f)
        {
            return Vector3.forward;
        }

        return axis.normalized;
    }

    private float ambushRingMin()
    {
        return Mathf.Max(120f, minDistanceFromPlayer + 20f);
    }

    private float ambushRingMax()
    {
        return Mathf.Max(220f, minDistanceFromPlayer + 120f);
    }

    private float ambushSideSpread()
    {
        return Mathf.Max(24f, spawnMargin * 0.45f);
    }
}
