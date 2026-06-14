using System.Collections;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class GameplayTestApi : MonoBehaviour
{
    public static GameplayTestApi Instance { get; private set; }

    [Header("References")]
    public TankController playerTank;
    public TankMachineGun machineGun;
    public EnemyTankSpawner enemySpawner;
    public MenuManager menuManager;
    public BattlefieldDirector battlefieldDirector;

    [Header("Smoke Test")]
    public int defaultEnemyCount = 6;
    public float closeSpawnDistance = 92f;
    public float weaponTestDamage = 32f;
    public bool autoRunOnPlay;

    [Header("Fast AI Test")]
    public int fastTestRounds = 10;
    public float fastTestPowerDamage = 9999f;

    [Header("Fall Through Detection")]
    public float fallThroughWorldY = -20f;
    public float fallThroughGroundTolerance = 8f;
    public float playerSpawnGroundClearance = 1.25f;

    public event System.Action<string> FallThroughDetected;

    private string _lastResult = "Idle";
    private bool _isRunningSmokeTest;
    private bool _isRunningFastRounds;
    private Vector3 _initialPlayerPosition;
    private Quaternion _initialPlayerRotation;
    private bool _hasInitialPlayerTransform;
    private bool _fallThroughDetected;
    private bool _playerFellThroughMap;

    public float PlayerTankSpeed => playerTank != null ? playerTank.CurrentGroundSpeed : 0f;
    public float PlayerTankSlopeAngle => playerTank != null ? playerTank.CurrentSlopeAngle : 0f;
    public bool PlayerTankGrounded => playerTank != null && playerTank.IsGrounded;
    public int PlayerGroundedWheelCount => playerTank != null ? playerTank.GroundedWheelCount : 0;
    public float PlayerRolloverAngle => playerTank != null ? playerTank.RolloverAngle : 0f;
    public float PlayerRolloverSeconds => playerTank != null ? playerTank.RolloverSeconds : 0f;
    public bool PlayerOverturned => playerTank != null && playerTank.IsOverturned;
    public bool ProjectileCameraActive => ProjectileCameraController.ActivePlayerProjectile != null;
    public int ProjectileCameraActivationCount => ProjectileCameraController.PlayerCameraActivationCount;
    public float EnemyDistanceToPlayer
    {
        get
        {
            EnemyTankAI enemy = FindNearestLiveEnemy();
            return enemy != null ? enemy.CurrentDistanceToPlayer : float.PositiveInfinity;
        }
    }
    public bool EnemyHasLineOfSight
    {
        get
        {
            EnemyTankAI enemy = FindNearestLiveEnemy();
            return enemy != null && enemy.CurrentHasLineOfSight;
        }
    }
    public string EnemyCurrentState
    {
        get
        {
            EnemyTankAI enemy = FindNearestLiveEnemy();
            return enemy != null ? enemy.CurrentOperationalState : "None";
        }
    }
    private string _fallThroughMessage = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
        CaptureInitialPlayerTransform();
    }

    private void Start()
    {
        ResolveReferences();
        CaptureInitialPlayerTransform();
        if (autoRunOnPlay)
        {
            StartCoroutine(RunSmokeTestRoutine(defaultEnemyCount));
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        MonitorPlayerFallThrough();
    }


    public void StartBattle(int enemyCount)
    {
        ResolveReferences();
        ResetBattle();

        if (enemySpawner != null)
        {
            enemySpawner.SpawnEnemyTanks(Mathf.Clamp(enemyCount, 0, 24));
        }

        HideMenuAndEnablePlayer();
        _lastResult = "Battle started";
    }

    public void ResetBattle()
    {
        ResolveReferences();

        if (battlefieldDirector != null)
        {
            battlefieldDirector.ResetBattleState();
        }

        if (enemySpawner != null)
        {
            enemySpawner.ClearExistingEnemies(true);
            enemySpawner.ClearTransientBattleObjects(true);
        }

        if (playerTank != null)
        {
            Vector3 resetPosition = _hasInitialPlayerTransform ? _initialPlayerPosition : playerTank.transform.position;
            Quaternion resetRotation = _hasInitialPlayerTransform ? _initialPlayerRotation : playerTank.transform.rotation;
            if (enemySpawner != null)
            {
                if (TryGetHighestPlayerFootprintGround(resetPosition, out float highestGroundY))
                {
                    float lift = GetPlayerGroundLift();
                    resetPosition = new Vector3(resetPosition.x, highestGroundY + lift, resetPosition.z);
                }
                else
                {
                    resetPosition.y = Mathf.Max(resetPosition.y, 20f);
                }
            }
            else
            {
                resetPosition.y = Mathf.Max(resetPosition.y, 20f);
            }
            playerTank.ResetForBattle(resetPosition, resetRotation);
        }

        CastleDamageReceiver castle = Object.FindFirstObjectByType<CastleDamageReceiver>();
        if (castle != null)
        {
            castle.ResetForBattle();
        }

        _fallThroughDetected = false;
        _playerFellThroughMap = false;
        _fallThroughMessage = string.Empty;
        _isRunningSmokeTest = false;
        _lastResult = "Battle reset";
    }

    public void SpawnCloseEnemies(int enemyCount)
    {
        ResolveReferences();
        ResetBattle();

        if (enemySpawner != null)
        {
            enemySpawner.SpawnCloseEnemyTanks(Mathf.Clamp(enemyCount, 0, 24), closeSpawnDistance);
        }

        HideMenuAndEnablePlayer();
        _lastResult = "Close enemies spawned";
    }

    public void SetFastAITestMode(bool enabled)
    {
        ResolveReferences();
        if (battlefieldDirector != null)
        {
            battlefieldDirector.SetFastAITestMode(enabled);
        }

        _lastResult = enabled ? "Fast AI test mode enabled" : "Human gameplay mode enabled";
    }

    public void FireCannonAtNearestEnemy()
    {
        ResolveReferences();
        EnemyTankAI target = FindNearestLiveEnemy();
        if (playerTank == null || target == null)
        {
            _lastResult = "No cannon target";
            return;
        }

        playerTank.FireAtPointForTest(target.transform.position + Vector3.up * 0.9f, 100f);
        _lastResult = "Cannon fired at " + target.name;
    }

    public void FireMachineGunAtNearestEnemy(int rounds)
    {
        ResolveReferences();
        EnemyTankAI target = FindNearestLiveEnemy();
        if (machineGun == null || target == null)
        {
            _lastResult = "No machine gun target";
            return;
        }

        machineGun.FireAtPointForTest(target.transform.position + Vector3.up * 0.7f, Mathf.Clamp(rounds, 1, 80));
        _lastResult = "Machine gun fired at " + target.name;
    }

    public string RunSmokeTest(int enemyCount)
    {
        if (!_isRunningSmokeTest)
        {
            StartCoroutine(RunSmokeTestRoutine(enemyCount));
        }

        return GetSnapshotJson();
    }

    public string RunFastAITestRounds(int rounds, int enemyCount)
    {
        if (!_isRunningFastRounds)
        {
            StartCoroutine(RunFastAITestRoutine(Mathf.Clamp(rounds, 1, 30), Mathf.Clamp(enemyCount, 1, 24)));
        }

        return GetSnapshotJson();
    }

    public void ForceWin()
    {
        ResolveReferences();
        if (battlefieldDirector != null)
        {
            battlefieldDirector.ForceVictory();
        }

        _lastResult = "Forced victory";
    }

    public void ForceLose()
    {
        ResolveReferences();
        if (battlefieldDirector != null)
        {
            battlefieldDirector.ForceDefeat("Forced defeat");
        }

        _lastResult = "Forced defeat";
    }

    public string GetSnapshotJson()
    {
        ResolveReferences();
        MonitorPlayerFallThrough();
        EnemyTankAI[] enemies = Object.FindObjectsByType<EnemyTankAI>(FindObjectsSortMode.None);
        CastleDamageReceiver castle = Object.FindFirstObjectByType<CastleDamageReceiver>();
        BattlefieldDirector director = battlefieldDirector;
        float snapshotGroundY = 0f;
        bool playerBelowMap = director != null && director.IsBattleRunning && IsPlayerBelowMap(out snapshotGroundY);

        StringBuilder builder = new StringBuilder(2048);
        builder.Append('{');
        builder.AppendFormat("\"result\":\"{0}\",", Escape(_lastResult));
        builder.AppendFormat("\"smokeTestRunning\":{0},", _isRunningSmokeTest ? "true" : "false");
        builder.AppendFormat("\"fastRoundsRunning\":{0},", _isRunningFastRounds ? "true" : "false");
        builder.AppendFormat("\"fastAITestMode\":{0},", director != null && director.fastAITestMode ? "true" : "false");
        builder.AppendFormat("\"fallThroughDetected\":{0},", _fallThroughDetected ? "true" : "false");
        builder.AppendFormat("\"playerFellThroughMap\":{0},", (_playerFellThroughMap || playerBelowMap) ? "true" : "false");
        builder.AppendFormat("\"fallThroughLog\":\"{0}\",", Escape(_fallThroughMessage));
        builder.AppendFormat("\"playerY\":{0:0.00},", playerTank != null ? playerTank.transform.position.y : 0f);
        builder.AppendFormat("\"playerGroundY\":{0:0.00},", snapshotGroundY);
        builder.AppendFormat("\"battleState\":\"{0}\",", director != null ? director.State.ToString() : "None");
        builder.AppendFormat("\"isVictory\":{0},", director != null && director.IsVictory ? "true" : "false");
        builder.AppendFormat("\"isDefeat\":{0},", director != null && director.IsDefeat ? "true" : "false");
        builder.AppendFormat("\"isResolved\":{0},", director != null && director.IsResolved ? "true" : "false");
        builder.AppendFormat("\"waveComplete\":{0},", director != null && director.WaveComplete ? "true" : "false");
        builder.AppendFormat("\"enemiesRemaining\":{0},", director != null ? director.EnemiesRemaining : CountLiveEnemies(enemies));
        builder.AppendFormat("\"objectiveProgress\":{0:0.000},", director != null ? director.ObjectiveProgress : 0f);
        builder.AppendFormat("\"enemiesDestroyed\":{0},", director != null ? director.EnemiesDestroyed : 0);
        builder.AppendFormat("\"totalEnemiesSpawned\":{0},", director != null ? director.TotalEnemiesSpawned : enemies.Length);
        builder.AppendFormat("\"shotsFired\":{0},", director != null ? director.ShotsFired : 0);
        builder.AppendFormat("\"damageDealt\":{0:0.0},", director != null ? director.DamageDealt : 0f);
        builder.AppendFormat("\"damageTaken\":{0:0.0},", director != null ? director.DamageTaken : 0f);
        builder.AppendFormat("\"elapsedTime\":{0:0.0},", director != null ? director.ElapsedBattleTime : 0f);
        builder.AppendFormat("\"playerAlive\":{0},", playerTank != null && !playerTank.IsDestroyed ? "true" : "false");
        builder.AppendFormat("\"playerHealth\":{0:0.0},", playerTank != null ? playerTank.HealthPercent : 0f);
        builder.AppendFormat("\"playerTankSpeed\":{0:0.00},", PlayerTankSpeed);
        builder.AppendFormat("\"playerTankSlopeAngle\":{0:0.00},", PlayerTankSlopeAngle);
        builder.AppendFormat("\"playerTankGrounded\":{0},", PlayerTankGrounded ? "true" : "false");
        builder.AppendFormat("\"playerGroundedWheelCount\":{0},", PlayerGroundedWheelCount);
        builder.AppendFormat("\"playerRolloverAngle\":{0:0.00},", PlayerRolloverAngle);
        builder.AppendFormat("\"playerRolloverSeconds\":{0:0.00},", PlayerRolloverSeconds);
        builder.AppendFormat("\"playerOverturned\":{0},", PlayerOverturned ? "true" : "false");
        builder.AppendFormat("\"projectileCameraActive\":{0},", ProjectileCameraActive ? "true" : "false");
        builder.AppendFormat("\"projectileCameraActivationCount\":{0},", ProjectileCameraActivationCount);
        builder.AppendFormat("\"castleHealth\":{0:0.0},", castle != null ? castle.HealthPercent : 0f);
        builder.AppendFormat("\"enemyCount\":{0},", enemies != null ? enemies.Length : 0);
        builder.Append("\"enemies\":[");

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyTankAI enemy = enemies[i];
            if (i > 0)
            {
                builder.Append(',');
            }

            Vector3 position = enemy != null ? enemy.transform.position : Vector3.zero;
            builder.Append('{');
            builder.AppendFormat("\"name\":\"{0}\",", Escape(enemy != null ? enemy.name : "null"));
            builder.AppendFormat("\"alive\":{0},", enemy != null && !enemy.IsDestroyed ? "true" : "false");
            builder.AppendFormat("\"health\":{0:0.0},", enemy != null ? enemy.HealthPercent : 0f);
            builder.AppendFormat("\"distance\":{0:0.0},", playerTank != null && enemy != null ? Vector3.Distance(playerTank.transform.position, enemy.transform.position) : 0f);
            builder.AppendFormat("\"x\":{0:0.0},\"y\":{1:0.0},\"z\":{2:0.0}", position.x, position.y, position.z);
            builder.Append('}');
        }

        builder.Append("]}");
        return builder.ToString();
    }

    private IEnumerator RunSmokeTestRoutine(int enemyCount)
    {
        _isRunningSmokeTest = true;
        SpawnCloseEnemies(Mathf.Max(1, enemyCount));
        yield return new WaitForSeconds(0.5f);

        EnemyTankAI target = FindNearestLiveEnemy();
        if (target != null)
        {
            FireMachineGunAtNearestEnemy(8);
            yield return new WaitForSeconds(0.4f);
            target.ApplyProjectileDamage(weaponTestDamage, target.transform.position + Vector3.up, Vector3.up);
            yield return new WaitForSeconds(0.25f);
            FireCannonAtNearestEnemy();
            yield return new WaitForSeconds(1.25f);
        }

        _lastResult = BuildSmokeSummary();
        _isRunningSmokeTest = false;
    }

    private IEnumerator RunFastAITestRoutine(int rounds, int enemyCount)
    {
        _isRunningFastRounds = true;
        SetFastAITestMode(true);
        int victories = 0;
        int defeats = 0;

        for (int i = 0; i < rounds; i++)
        {
            SpawnCloseEnemies(enemyCount);
            yield return new WaitForSeconds(0.35f);

            int guard = 0;
            while (battlefieldDirector != null && battlefieldDirector.IsBattleRunning && guard < 64)
            {
                EnemyTankAI target = FindNearestLiveEnemy();
                if (target != null)
                {
                    if (battlefieldDirector != null)
                    {
                        battlefieldDirector.RegisterDamageDealt(fastTestPowerDamage);
                    }

                    target.ApplyProjectileDamage(fastTestPowerDamage, target.transform.position + Vector3.up, Vector3.up);
                }

                guard++;
                yield return new WaitForSeconds(0.15f);
            }

            if (battlefieldDirector != null && battlefieldDirector.IsVictory)
            {
                victories++;
            }
            else if (battlefieldDirector != null && battlefieldDirector.IsDefeat)
            {
                defeats++;
            }
        }

        SetFastAITestMode(false);
        _lastResult = "Fast AI rounds complete: " + victories + " victories, " + defeats + " defeats / " + rounds + " rounds";
        _isRunningFastRounds = false;
    }

private void MonitorPlayerFallThrough()
    {
        if (_fallThroughDetected)
        {
            return;
        }

        if (battlefieldDirector == null || !battlefieldDirector.IsBattleRunning)
        {
            return;
        }

        if (!IsPlayerBelowMap(out float groundY))
        {
            return;
        }

        float playerY = playerTank != null ? playerTank.transform.position.y : 0f;
        string state = battlefieldDirector != null ? battlefieldDirector.State.ToString() : "None";
        _fallThroughDetected = true;
        _playerFellThroughMap = true;
        _fallThroughMessage = "FALL_THROUGH_MAP playerY=" + playerY.ToString("0.00") + " groundY=" + groundY.ToString("0.00") + " battleState=" + state;
        _lastResult = _fallThroughMessage;
        Debug.LogError("[GameplayTestApi] " + _fallThroughMessage);
        FallThroughDetected?.Invoke(_fallThroughMessage);
    }

    private bool IsPlayerBelowMap(out float groundY)
    {
        groundY = 0f;
        if (playerTank == null)
        {
            return false;
        }

        Vector3 playerPosition = playerTank.transform.position;
        if (playerPosition.y < fallThroughWorldY)
        {
            groundY = fallThroughWorldY;
            return true;
        }

        if (enemySpawner != null)
        {
            Vector3 sampledGround = enemySpawner.SampleGround(playerPosition + Vector3.up * 2f);
            if (sampledGround != Vector3.zero)
            {
                groundY = sampledGround.y;
                if (playerPosition.y < sampledGround.y - Mathf.Max(0.5f, fallThroughGroundTolerance))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private float GetPlayerGroundLift()
    {
        if (playerTank == null)
        {
            return 1f;
        }

        Collider[] colliders = playerTank.GetComponentsInChildren<Collider>();
        float lowestBottom = float.MaxValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider tankCollider = colliders[i];
            if (tankCollider == null || !tankCollider.enabled || tankCollider.isTrigger)
            {
                continue;
            }

            lowestBottom = Mathf.Min(lowestBottom, tankCollider.bounds.min.y);
        }

        if (lowestBottom == float.MaxValue)
        {
            return 1f;
        }

        return Mathf.Max(0.35f, playerTank.transform.position.y - lowestBottom + Mathf.Max(0.1f, playerSpawnGroundClearance));
    }

    private bool TryGetHighestPlayerFootprintGround(Vector3 resetPosition, out float highestGroundY)
    {
        highestGroundY = float.MinValue;
        if (playerTank == null || enemySpawner == null)
        {
            return false;
        }

        Vector3 currentPosition = playerTank.transform.position;
        Collider[] colliders = playerTank.GetComponentsInChildren<Collider>();
        bool foundGround = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider tankCollider = colliders[i];
            if (tankCollider == null || !tankCollider.enabled || tankCollider.isTrigger)
            {
                continue;
            }

            Bounds bounds = tankCollider.bounds;
            SampleFootprintGround(currentPosition, resetPosition, new Vector3(bounds.center.x, 0f, bounds.center.z), ref highestGroundY, ref foundGround);
            SampleFootprintGround(currentPosition, resetPosition, new Vector3(bounds.min.x, 0f, bounds.min.z), ref highestGroundY, ref foundGround);
            SampleFootprintGround(currentPosition, resetPosition, new Vector3(bounds.min.x, 0f, bounds.max.z), ref highestGroundY, ref foundGround);
            SampleFootprintGround(currentPosition, resetPosition, new Vector3(bounds.max.x, 0f, bounds.min.z), ref highestGroundY, ref foundGround);
            SampleFootprintGround(currentPosition, resetPosition, new Vector3(bounds.max.x, 0f, bounds.max.z), ref highestGroundY, ref foundGround);
        }

        return foundGround;
    }

    private void SampleFootprintGround(Vector3 currentPosition, Vector3 resetPosition, Vector3 currentSample, ref float highestGroundY, ref bool foundGround)
    {
        Vector3 offset = currentSample - new Vector3(currentPosition.x, 0f, currentPosition.z);
        Vector3 sampleOrigin = new Vector3(resetPosition.x + offset.x, resetPosition.y + 40f, resetPosition.z + offset.z);
        Vector3 groundPoint = enemySpawner.SampleGround(sampleOrigin);
        if (groundPoint == Vector3.zero)
        {
            return;
        }

        highestGroundY = Mathf.Max(highestGroundY, groundPoint.y);
        foundGround = true;
    }

    private string BuildSmokeSummary()
    {
        EnemyTankAI[] enemies = Object.FindObjectsByType<EnemyTankAI>(FindObjectsSortMode.None);
        int damaged = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].HealthPercent < 99f)
            {
                damaged++;
            }
        }

        return "Smoke test complete: " + damaged + " damaged enemies / " + enemies.Length + " present";
    }

    private EnemyTankAI FindNearestLiveEnemy()
    {
        EnemyTankAI[] enemies = Object.FindObjectsByType<EnemyTankAI>(FindObjectsSortMode.None);
        EnemyTankAI best = null;
        float bestDistance = float.MaxValue;
        Vector3 origin = playerTank != null ? playerTank.transform.position : Vector3.zero;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyTankAI enemy = enemies[i];
            if (enemy == null || enemy.IsDestroyed)
            {
                continue;
            }

            float distance = Vector3.Distance(origin, enemy.transform.position);
            if (distance < bestDistance)
            {
                best = enemy;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void HideMenuAndEnablePlayer()
    {
        if (menuManager != null && menuManager.mainPanel != null)
        {
            menuManager.mainPanel.SetActive(false);
        }

        if (playerTank != null)
        {
            playerTank.enabled = true;
            playerTank.PrepareForGameplay();
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void CaptureInitialPlayerTransform()
    {
        if (playerTank == null || _hasInitialPlayerTransform)
        {
            return;
        }

        if (playerTank.transform.position.y < fallThroughWorldY)
        {
            return;
        }

        _initialPlayerPosition = playerTank.transform.position;
        _initialPlayerRotation = playerTank.transform.rotation;
        _hasInitialPlayerTransform = true;
    }

    private int CountLiveEnemies(EnemyTankAI[] enemies)
    {
        int count = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && !enemies[i].IsDestroyed)
            {
                count++;
            }
        }

        return count;
    }

    private void ResolveReferences()
    {
        if (playerTank == null)
        {
            GameObject player = GameObject.Find("PlayerTank");
            if (player != null)
            {
                playerTank = player.GetComponent<TankController>();
            }
        }

        if (machineGun == null && playerTank != null)
        {
            machineGun = playerTank.GetComponent<TankMachineGun>();
        }

        if (enemySpawner == null)
        {
            enemySpawner = Object.FindFirstObjectByType<EnemyTankSpawner>();
        }

        if (menuManager == null)
        {
            menuManager = Object.FindFirstObjectByType<MenuManager>();
        }

        if (battlefieldDirector == null)
        {
            battlefieldDirector = Object.FindFirstObjectByType<BattlefieldDirector>();
        }
    }

    private string Escape(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
