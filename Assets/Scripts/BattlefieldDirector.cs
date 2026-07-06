using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BattlefieldDirector : MonoBehaviour
{
    public static BattlefieldDirector Instance { get; private set; }

    public enum BattleState
    {
        Waiting,
        Running,
        Victory,
        Defeat
    }

    [Header("References")]
    public Transform playerTank;
    public Transform castle;

    [Header("Objective")]
    public int waveEnemyCount = 6;
    public float victoryDelay = 1.0f;
    public bool requireCastleSurvival = true;

    [Header("Difficulty")]
    public int enemyCountThisRun = 0;
    public float enemyCountDifficultyStep = 0.08f;
    public float timeDifficultyStepPerMinute = 0.06f;

    [Header("AI Test Mode")]
    public bool fastAITestMode;
    public float fastTimeScale = 8f;
    public bool muteAudioInFastMode = true;

    [Header("Objective UI")]
    public Canvas objectiveCanvas;
    public Vector2 objectivePanelSize = new Vector2(420f, 108f);
    public Vector2 objectivePanelAnchor = new Vector2(20f, -20f);

    private BattleState _state = BattleState.Waiting;
    private float _runStartTime;
    private float _battleEndTime;
    private int _totalEnemiesSpawned;
    private int _enemiesDestroyed;
    private int _shotsFired;
    private float _damageDealt;
    private float _damageTaken;
    private bool _battleStarted;

    private RectTransform _objectiveRoot;
    private TextMeshProUGUI _objectiveTitle;
    private TextMeshProUGUI _objectiveBody;
    private CanvasGroup _endGroup;
    private TextMeshProUGUI _endTitle;
    private TextMeshProUGUI _endBody;

    public BattleState State
    {
        get { return _state; }
    }

    public bool IsBattleRunning
    {
        get { return _state == BattleState.Running; }
    }

    public bool IsVictory
    {
        get { return _state == BattleState.Victory; }
    }

    public bool IsDefeat
    {
        get { return _state == BattleState.Defeat; }
    }

    public bool IsResolved
    {
        get { return _state == BattleState.Victory || _state == BattleState.Defeat; }
    }

    public bool WaveComplete
    {
        get { return _state == BattleState.Victory; }
    }

    public int EnemiesRemaining
    {
        get { return CountLiveEnemies(); }
    }

    public int EnemiesDestroyed
    {
        get { return GetResolvedDestroyedCount(); }
    }

    public int TotalEnemiesSpawned
    {
        get { return _totalEnemiesSpawned; }
    }

    public int ShotsFired
    {
        get { return _shotsFired; }
    }

    public float DamageDealt
    {
        get { return _damageDealt; }
    }

    public float DamageTaken
    {
        get { return _damageTaken; }
    }

    public float ElapsedBattleTime
    {
        get { return _battleStarted ? Mathf.Max(0f, Time.time - _runStartTime) : 0f; }
    }

    public float ObjectiveProgress
    {
        get
        {
            if (_totalEnemiesSpawned <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)GetResolvedDestroyedCount() / _totalEnemiesSpawned);
        }
    }

    private void Awake()
    {
        Instance = this;
        _runStartTime = Time.time;
        ResolveReferences();
        EnsureObjectiveUI();
        ApplyTestModeSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        Time.timeScale = 1f;
        AudioListener.volume = 1f;
    }

    private void Update()
    {
        ResolveReferences();
        ApplyTestModeSettings();
        UpdateObjectiveUI();

        if (_state == BattleState.Defeat && Input.GetMouseButtonDown(0))
        {
            RestartCurrentModeAfterDefeat();
            return;
        }

        if (_state != BattleState.Running)
        {
            return;
        }

        if (IsPlayerDestroyed())
        {
            CompleteDefeat("Player tank destroyed");
            return;
        }

        if (requireCastleSurvival && IsCastleCollapsed())
        {
            CompleteDefeat("Castle destroyed");
            return;
        }

        if (_totalEnemiesSpawned > 0 && CountLiveEnemies() <= 0 && Time.time - _runStartTime >= victoryDelay)
        {
            CompleteVictory();
        }
    }

    public void BeginBattle(int enemyCount)
    {
        ResolveReferences();
        waveEnemyCount = Mathf.Max(0, enemyCount);
        enemyCountThisRun = waveEnemyCount;
        _state = BattleState.Running;
        _battleStarted = true;
        _runStartTime = Time.time;
        _battleEndTime = 0f;
        _totalEnemiesSpawned = 0;
        _enemiesDestroyed = 0;
        _shotsFired = 0;
        _damageDealt = 0f;
        _damageTaken = 0f;
        HideEndOverlay();
        ApplyTestModeSettings();
    }

    public void ResetBattleState()
    {
        _state = BattleState.Waiting;
        _battleStarted = false;
        _runStartTime = Time.time;
        _battleEndTime = 0f;
        _totalEnemiesSpawned = 0;
        _enemiesDestroyed = 0;
        _shotsFired = 0;
        _damageDealt = 0f;
        _damageTaken = 0f;
        HideEndOverlay();
        Time.timeScale = 1f;
        AudioListener.volume = 1f;
    }

    public void ForceVictory()
    {
        CompleteVictory();
    }

    public void ForceDefeat(string reason = "Forced defeat")
    {
        CompleteDefeat(reason);
    }

    public void SetEnemyCount(int count)
    {
        enemyCountThisRun = Mathf.Max(0, count);
    }

    public void RegisterEnemySpawned(EnemyTankAI enemy)
    {
        if (enemy == null)
        {
            return;
        }

        _totalEnemiesSpawned++;
    }

    public void RegisterEnemyDestroyed(EnemyTankAI enemy)
    {
        if (enemy == null)
        {
            return;
        }

        _enemiesDestroyed = Mathf.Min(_totalEnemiesSpawned, _enemiesDestroyed + 1);
    }

    public void RegisterShotFired(bool playerShot)
    {
        if (playerShot)
        {
            _shotsFired++;
        }
    }

    public void RegisterDamageDealt(float amount)
    {
        _damageDealt += Mathf.Max(0f, amount);
    }

    public void RegisterDamageTaken(float amount)
    {
        _damageTaken += Mathf.Max(0f, amount);
    }

    public void SetFastAITestMode(bool enabled)
    {
        fastAITestMode = enabled;
        ApplyTestModeSettings();
    }

    public float GetDifficultyMultiplier()
    {
        float enemyPressure = enemyCountThisRun * enemyCountDifficultyStep;
        float timePressure = Mathf.Max(0f, Time.time - _runStartTime) / 60f * timeDifficultyStepPerMinute;
        return Mathf.Max(1f, 1f + enemyPressure + timePressure);
    }

    public Vector3 GetSharedTurretAimPoint(Vector3 sourcePosition, float leadSeconds = 0.35f)
    {
        if (TryGetPlayerTank(out Transform target))
        {
            Rigidbody body = target.GetComponent<Rigidbody>();
            Vector3 predicted = target.position + Vector3.up * 1.1f;
            if (body != null)
            {
                Vector3 velocity = GetVelocity(body);
                float distance = Vector3.Distance(sourcePosition, target.position);
                float lead = Mathf.Clamp(distance / 240f, 0.12f, 0.8f) * leadSeconds;
                predicted += velocity * lead;
            }

            return predicted;
        }

        return sourcePosition + Vector3.forward * 25f;
    }

    public bool TryGetPlayerTank(out Transform target)
    {
        if (playerTank != null)
        {
            target = playerTank;
            return true;
        }

        GameObject player = GameObject.Find("PlayerTank");
        if (player != null)
        {
            playerTank = player.transform;
            target = playerTank;
            return true;
        }

        target = null;
        return false;
    }

    public bool TryGetCastle(out Transform target)
    {
        if (castle != null)
        {
            target = castle;
            return true;
        }

        GameObject castleObject = GameObject.Find("Castle");
        if (castleObject == null)
        {
            castleObject = GameObject.Find("Castle(Clone)");
        }

        if (castleObject != null)
        {
            castle = castleObject.transform;
            target = castle;
            return true;
        }

        target = null;
        return false;
    }

    private void CompleteVictory()
    {
        if (IsResolved)
        {
            return;
        }

        _state = BattleState.Victory;
        _battleEndTime = Time.time;
        Time.timeScale = fastAITestMode ? Mathf.Min(2f, fastTimeScale) : 1f;
        LockResolvedPlayerInput();
        ShowEndOverlay("VICTORY", "Wave cleared");
    }

    private void CompleteDefeat(string reason)
    {
        if (IsResolved)
        {
            return;
        }

        _state = BattleState.Defeat;
        _battleEndTime = Time.time;
        Time.timeScale = 1f;
        AudioListener.volume = 1f;
        LockResolvedPlayerInput();
        ShowEndOverlay("DEFEAT", reason);
    }

    private void RestartCurrentModeAfterDefeat()
    {
        GameplayTestApi api = GameplayTestApi.Instance;
        if (api == null)
        {
            api = Object.FindAnyObjectByType<GameplayTestApi>();
        }

        if (api != null)
        {
            api.RestartCurrentMode();
        }
    }

    private void LockResolvedPlayerInput()
    {
        TankController tank = playerTank != null ? playerTank.GetComponent<TankController>() : null;
        if (tank != null)
        {
            tank.enabled = false;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ApplyTestModeSettings()
    {
        if (_state == BattleState.Running && fastAITestMode)
        {
            Time.timeScale = Mathf.Clamp(fastTimeScale, 1f, 30f);
            AudioListener.volume = muteAudioInFastMode ? 0f : 1f;
            return;
        }

        Time.timeScale = 1f;
        AudioListener.volume = 1f;
    }

    private int CountLiveEnemies()
    {
        EnemyTankAI[] enemies = Object.FindObjectsByType<EnemyTankAI>();
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

    private bool IsPlayerDestroyed()
    {
        TankController tank = playerTank != null ? playerTank.GetComponent<TankController>() : null;
        return tank != null && tank.IsDestroyed;
    }

    private bool IsCastleCollapsed()
    {
        CastleDamageReceiver receiver = castle != null ? castle.GetComponentInChildren<CastleDamageReceiver>() : null;
        if (receiver == null)
        {
            receiver = Object.FindAnyObjectByType<CastleDamageReceiver>();
        }

        return receiver != null && receiver.IsCollapsed;
    }

    private void ResolveReferences()
    {
        if (playerTank == null)
        {
            TryGetPlayerTank(out _);
        }

        if (castle == null)
        {
            TryGetCastle(out _);
        }
    }

    private Vector3 GetVelocity(Rigidbody body)
    {
#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }

    private void EnsureObjectiveUI()
    {
        if (objectiveCanvas == null)
        {
            GameObject canvasGo = new GameObject("BattleObjectiveCanvas");
            objectiveCanvas = canvasGo.AddComponent<Canvas>();
            objectiveCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        BuildObjectivePanel();
        BuildEndOverlay();
    }

    private void BuildObjectivePanel()
    {
        GameObject root = new GameObject("BattleObjectivePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(objectiveCanvas.transform, false);
        _objectiveRoot = root.GetComponent<RectTransform>();
        _objectiveRoot.anchorMin = new Vector2(1f, 0f);
        _objectiveRoot.anchorMax = new Vector2(1f, 0f);
        _objectiveRoot.pivot = new Vector2(1f, 0f);
        _objectiveRoot.sizeDelta = objectivePanelSize;
        _objectiveRoot.anchoredPosition = objectivePanelAnchor;
        root.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.08f, 0.78f);

        _objectiveTitle = CreateText(root.transform, "ObjectiveTitle", new Vector2(14f, -10f), 18f, TextAlignmentOptions.Left);
        _objectiveTitle.fontStyle = FontStyles.Bold;
        _objectiveBody = CreateText(root.transform, "ObjectiveBody", new Vector2(14f, -38f), 16f, TextAlignmentOptions.Left);
        _objectiveBody.rectTransform.sizeDelta = new Vector2(objectivePanelSize.x - 28f, 70f);
    }

    private void BuildEndOverlay()
    {
        GameObject overlay = new GameObject("BattleEndOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(objectiveCanvas.transform, false);
        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.52f);
        _endGroup = overlay.GetComponent<CanvasGroup>();
        _endGroup.alpha = 0f;
        _endGroup.gameObject.SetActive(false);

        _endTitle = CreateText(overlay.transform, "BattleEndTitle", Vector2.zero, 70f, TextAlignmentOptions.Center);
        _endTitle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _endTitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _endTitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _endTitle.rectTransform.sizeDelta = new Vector2(900f, 90f);
        _endTitle.rectTransform.anchoredPosition = new Vector2(0f, 90f);
        _endTitle.fontStyle = FontStyles.Bold;

        _endBody = CreateText(overlay.transform, "BattleEndBody", Vector2.zero, 24f, TextAlignmentOptions.Center);
        _endBody.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _endBody.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _endBody.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _endBody.rectTransform.sizeDelta = new Vector2(900f, 220f);
        _endBody.rectTransform.anchoredPosition = new Vector2(0f, -40f);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchoredPosition, float size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(380f, 28f);
        rt.anchoredPosition = anchoredPosition;
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.color = new Color(1f, 1f, 1f, 0.95f);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private void UpdateObjectiveUI()
    {
        if (_objectiveRoot == null)
        {
            return;
        }

        _objectiveRoot.gameObject.SetActive(_state == BattleState.Running || _state == BattleState.Waiting);
        if (_objectiveTitle != null)
        {
            _objectiveTitle.text = _state == BattleState.Running ? "DEFEND THE CASTLE" : "OBJECTIVE";
        }

        if (_objectiveBody != null)
        {
            _objectiveBody.text = "Enemies: " + GetResolvedDestroyedCount() + " / " + Mathf.Max(1, _totalEnemiesSpawned) +
                                  "\nRemaining: " + EnemiesRemaining +
                                  "\nCastle: " + GetCastleHealthText() +
                                  "\nMode: " + (fastAITestMode ? "FAST QA" : "HUMAN");
        }
    }

    private string GetCastleHealthText()
    {
        CastleDamageReceiver receiver = castle != null ? castle.GetComponentInChildren<CastleDamageReceiver>() : null;
        if (receiver == null)
        {
            receiver = Object.FindAnyObjectByType<CastleDamageReceiver>();
        }

        return receiver != null ? receiver.HealthPercent.ToString("0") + "%" : "--";
    }

    private void ShowEndOverlay(string title, string reason)
    {
        if (_endGroup == null)
        {
            return;
        }

        _endGroup.gameObject.SetActive(true);
        _endGroup.alpha = 1f;
        if (_endTitle != null)
        {
            _endTitle.text = title;
            _endTitle.color = title == "VICTORY" ? new Color(0.35f, 1f, 0.45f, 1f) : new Color(1f, 0.16f, 0.12f, 1f);
        }

        if (_endBody != null)
        {
            _endBody.text = reason +
                            "\nEnemies destroyed: " + GetResolvedDestroyedCount() + " / " + _totalEnemiesSpawned +
                            "\nShots fired: " + _shotsFired +
                            "\nDamage dealt: " + _damageDealt.ToString("0") +
                            "\nDamage taken: " + _damageTaken.ToString("0") +
                            "\nTime: " + Mathf.Max(0f, _battleEndTime - _runStartTime).ToString("0.0") + "s" +
                            "\nCastle: " + GetCastleHealthText();
        }
    }

    private void HideEndOverlay()
    {
        if (_endGroup != null)
        {
            _endGroup.alpha = 0f;
            _endGroup.gameObject.SetActive(false);
        }
    }


private int GetResolvedDestroyedCount()
    {
        if (_totalEnemiesSpawned <= 0)
        {
            return 0;
        }

        int inferredDestroyed = _totalEnemiesSpawned - CountLiveEnemies();
        return Mathf.Clamp(Mathf.Max(_enemiesDestroyed, inferredDestroyed), 0, _totalEnemiesSpawned);
    }
}
