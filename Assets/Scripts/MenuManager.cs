using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public enum GameplayMode
    {
        DrivingPractice,
        CannonPractice,
        War,
        BallDriveTest
    }

    [Header("Authored Menu References")]
    public GameObject mainPanel;
    public Button playButton;
    public Button drivingPracticeButton;
    public Button cannonPracticeButton;
    public Button warButton;
    public Button ballDriveTestButton;
    public TMP_InputField enemyCountInput;

    [Header("Gameplay References")]
    public TankController playerTank;
    public EnemyTankSpawner enemyTankSpawner;

    [Header("Scene Names")]
    public string drivingPracticeSceneName = "TankDrivingPractice";
    public string cannonPracticeSceneName = "TankTargetPractice";
    public string warSceneName = "Practice";
    public string ballDriveTestSceneName = "BallDriveTest";
    public bool loadDedicatedPracticeScenes = true;
    public int defaultEnemyCount = 5;

    public static bool AutoStartWarOnLoad;

    public GameplayMode SelectedMode { get; private set; } = GameplayMode.War;

    private void Start()
    {
        bool autoStartWar = AutoStartWarOnLoad || ShouldAutoStartWarInCurrentScene();
        if (playerTank != null && !autoStartWar)
        {
            playerTank.enabled = false;
        }

        HookAuthoredButtons();

        if (autoStartWar)
        {
            AutoStartWarOnLoad = false;
            StartWarInCurrentScene();
            return;
        }

        ShowMain();
    }

    public void StartDrivingPractice()
    {
        StartMode(GameplayMode.DrivingPractice);
    }

    public void StartCannonPractice()
    {
        StartMode(GameplayMode.CannonPractice);
    }

    public void StartWar()
    {
        StartMode(GameplayMode.War);
    }

    public void StartBallDriveTest()
    {
        StartMode(GameplayMode.BallDriveTest);
    }

    private void HookAuthoredButtons()
    {
        HookModeButton(drivingPracticeButton, GameplayMode.DrivingPractice);
        HookModeButton(cannonPracticeButton, GameplayMode.CannonPractice);
        HookModeButton(warButton != null ? warButton : playButton, GameplayMode.War);
        HookModeButton(ballDriveTestButton, GameplayMode.BallDriveTest);
    }

    private void HookModeButton(Button button, GameplayMode mode)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => StartMode(mode));
    }

    private void StartMode(GameplayMode mode)
    {
        SelectedMode = mode;

        if (loadDedicatedPracticeScenes && TryLoadDedicatedSceneForMode(mode))
        {
            return;
        }

        StartModeInCurrentScene(mode);
    }

    private void StartWarInCurrentScene()
    {
        SelectedMode = GameplayMode.War;
        StartModeInCurrentScene(GameplayMode.War);
    }

    private void StartModeInCurrentScene(GameplayMode mode)
    {
        int enemyCount = GetDesiredEnemyCount();
        GameplayTestApi gameplayTestApi = Object.FindAnyObjectByType<GameplayTestApi>();
        if (gameplayTestApi != null)
        {
            gameplayTestApi.StartMode(mode, enemyCount);
            return;
        }

        if (mode == GameplayMode.War)
        {
            if (enemyTankSpawner == null)
            {
                enemyTankSpawner = Object.FindAnyObjectByType<EnemyTankSpawner>();
            }

            enemyTankSpawner?.SpawnEnemyTanks(enemyCount);
        }

        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (playerTank != null)
        {
            playerTank.enabled = true;
            playerTank.PrepareForGameplay();
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private bool TryLoadDedicatedSceneForMode(GameplayMode mode)
    {
        string targetScene = mode switch
        {
            GameplayMode.DrivingPractice => drivingPracticeSceneName,
            GameplayMode.CannonPractice => cannonPracticeSceneName,
            GameplayMode.War => warSceneName,
            GameplayMode.BallDriveTest => ballDriveTestSceneName,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            return false;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == targetScene || activeScene.path.EndsWith("/" + targetScene + ".unity"))
        {
            return false;
        }

        if (mode == GameplayMode.War)
        {
            AutoStartWarOnLoad = true;
        }

        SceneManager.LoadScene(targetScene);
        return true;
    }

    private bool ShouldAutoStartWarInCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        bool isWarScene = activeScene.name == warSceneName || activeScene.path.EndsWith("/" + warSceneName + ".unity");
        return isWarScene && warButton == null && playButton == null;
    }

    private void ShowMain()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private int GetDesiredEnemyCount()
    {
        if (enemyCountInput == null || !int.TryParse(enemyCountInput.text, out int count))
        {
            count = defaultEnemyCount;
        }

        return Mathf.Clamp(count, 0, 12);
    }
}
