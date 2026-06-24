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
        War
    }

    public GameObject mainPanel;
    public Button playButton;
    public Button drivingPracticeButton;
    public Button cannonPracticeButton;
    public Button warButton;
    public TankController playerTank;
    public EnemyTankSpawner enemyTankSpawner;
    public TextMeshProUGUI instructionsText;
    public TextMeshProUGUI modeTutorialText;
    public TMP_InputField enemyCountInput;
    public string drivingPracticeSceneName = "TankDrivingPractice";
    public string cannonPracticeSceneName = "TankTargetPractice";
    public string warSceneName = "Practice";
    public bool loadDedicatedPracticeScenes = true;

    [TextArea(4, 12)]
    public string controlsCopy = "Controls:\nW/S or arrows - heavy forward/reverse drive\nA/D or arrows - steer tracks\nShift - low gear / stabilized creeping\nMouse X - rotate turret\nQ/E - lower/raise cannon barrel\nMouse wheel or PageUp/PageDown - fine barrel elevation\n+/- - adjust cannon power from 100% default\nRight click - scope/rangefinder\nLeft click / Space - fire cannon\nF - machine gun\nC - controls during game";
    [TextArea(3, 8)]
    public string drivingPracticeCopy = "DRIVING PRACTICE\nObstacle course only. Climb ramps, cross bumps, test craters, and feel traction loss without enemies.";
    [TextArea(3, 8)]
    public string cannonPracticeCopy = "CANNON PRACTICE\nTarget range only. Use normal fire up close, then right-click scope to dial far shots with range/elevation/time-of-flight.";
    [TextArea(3, 8)]
    public string warCopy = "WAR\nRandom battle setup. Enemies spawn at range, use line of sight, and pressure the castle.";
    public int defaultEnemyCount = 5;
    public static bool AutoStartWarOnLoad;

    public GameplayMode SelectedMode { get; private set; } = GameplayMode.War;

    private void Start()
    {
        if (playerTank != null)
        {
            playerTank.enabled = false;
        }

        EnsureEnemyCountInput();
        EnsureModeButtons();

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(StartWar);
        }

        EnsureInstructionsText();
        EnsureModeTutorialText();

        if (AutoStartWarOnLoad)
        {
            AutoStartWarOnLoad = false;
            StartWarInCurrentScene();
            return;
        }

        ShowMain();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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

    private void StartMode(GameplayMode mode)
    {
        SelectedMode = mode;

        if (loadDedicatedPracticeScenes && TryLoadDedicatedSceneForMode(mode))
        {
            return;
        }

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

            if (enemyTankSpawner != null)
            {
                enemyTankSpawner.SpawnEnemyTanks(enemyCount);
            }
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

    private void StartWarInCurrentScene()
    {
        SelectedMode = GameplayMode.War;
        int enemyCount = GetDesiredEnemyCount();
        GameplayTestApi gameplayTestApi = Object.FindAnyObjectByType<GameplayTestApi>();
        if (gameplayTestApi != null)
        {
            gameplayTestApi.StartMode(GameplayMode.War, enemyCount);
            return;
        }

        if (enemyTankSpawner == null)
        {
            enemyTankSpawner = Object.FindAnyObjectByType<EnemyTankSpawner>();
        }

        if (enemyTankSpawner != null)
        {
            enemyTankSpawner.SpawnEnemyTanks(enemyCount);
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

    private void ShowMain()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        EnsureInstructionsText();
        if (instructionsText != null)
        {
            instructionsText.text = controlsCopy;
        }

        EnsureModeTutorialText();
        UpdateModeTutorial();
        EnsureEnemyCountInput();
        if (enemyCountInput != null && string.IsNullOrWhiteSpace(enemyCountInput.text))
        {
            enemyCountInput.text = defaultEnemyCount.ToString();
        }
    }

    private void EnsureInstructionsText()
    {
        if (instructionsText != null)
        {
            StretchInstructionsText(instructionsText.rectTransform);
            instructionsText.text = controlsCopy;
            return;
        }

        if (mainPanel == null)
        {
            return;
        }

        Transform existing = mainPanel.transform.Find("InstructionsText");
        if (existing != null)
        {
            instructionsText = existing.GetComponent<TextMeshProUGUI>();
            if (instructionsText != null)
            {
                StretchInstructionsText(instructionsText.rectTransform);
                instructionsText.text = controlsCopy;
                return;
            }
        }

        GameObject textGo = new GameObject("InstructionsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(mainPanel.transform, false);

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(28f, 28f);
        rt.offsetMax = new Vector2(-28f, -120f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        instructionsText = textGo.GetComponent<TextMeshProUGUI>();
        instructionsText.fontSize = 22f;
        instructionsText.alignment = TextAlignmentOptions.TopLeft;
        instructionsText.color = new Color(1f, 1f, 1f, 0.95f);
        instructionsText.enableAutoSizing = true;
        instructionsText.fontSizeMin = 14f;
        instructionsText.fontSizeMax = 22f;
        instructionsText.textWrappingMode = TextWrappingModes.Normal;
        instructionsText.text = controlsCopy;
        instructionsText.raycastTarget = false;
    }

    private void EnsureModeTutorialText()
    {
        if (modeTutorialText != null)
        {
            return;
        }

        if (mainPanel == null)
        {
            return;
        }

        Transform existing = mainPanel.transform.Find("ModeTutorialText");
        if (existing != null)
        {
            modeTutorialText = existing.GetComponent<TextMeshProUGUI>();
            if (modeTutorialText != null)
            {
                return;
            }
        }

        GameObject textGo = new GameObject("ModeTutorialText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(mainPanel.transform, false);

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 112f);
        rt.sizeDelta = new Vector2(760f, 180f);

        modeTutorialText = textGo.GetComponent<TextMeshProUGUI>();
        modeTutorialText.fontSize = 16f;
        modeTutorialText.alignment = TextAlignmentOptions.Center;
        modeTutorialText.color = new Color(1f, 0.94f, 0.78f, 1f);
        modeTutorialText.enableAutoSizing = true;
        modeTutorialText.fontSizeMin = 12f;
        modeTutorialText.fontSizeMax = 16f;
        modeTutorialText.textWrappingMode = TextWrappingModes.Normal;
        modeTutorialText.raycastTarget = false;
    }

    private void EnsureModeButtons()
    {
        if (mainPanel == null)
        {
            return;
        }

        drivingPracticeButton = drivingPracticeButton != null
            ? drivingPracticeButton
            : FindOrCreateModeButton("DrivingPracticeButton", "DRIVE", new Vector2(-220f, 42f));
        cannonPracticeButton = cannonPracticeButton != null
            ? cannonPracticeButton
            : FindOrCreateModeButton("CannonPracticeButton", "CANNON", new Vector2(0f, 42f));
        warButton = warButton != null
            ? warButton
            : playButton != null
                ? playButton
                : FindOrCreateModeButton("WarButton", "WAR", new Vector2(220f, 42f));
        if (warButton != null)
        {
            warButton.gameObject.name = "WarButton";
            RectTransform warRt = warButton.GetComponent<RectTransform>();
            if (warRt != null)
            {
                warRt.anchorMin = new Vector2(0.5f, 0f);
                warRt.anchorMax = new Vector2(0.5f, 0f);
                warRt.pivot = new Vector2(0.5f, 0.5f);
                warRt.anchoredPosition = new Vector2(220f, 42f);
                warRt.sizeDelta = new Vector2(190f, 56f);
            }
            SetButtonLabel(warButton, "WAR");
        }

        HookModeButton(drivingPracticeButton, GameplayMode.DrivingPractice);
        HookModeButton(cannonPracticeButton, GameplayMode.CannonPractice);
        HookModeButton(warButton, GameplayMode.War);
        if (playButton == null)
        {
            playButton = warButton;
        }
    }

    private Button FindOrCreateModeButton(string name, string label, Vector2 anchoredPosition)
    {
        Transform existing = mainPanel.transform.Find(name);
        if (existing != null && existing.TryGetComponent(out Button existingButton))
        {
            SetButtonLabel(existingButton, label);
            return existingButton;
        }

        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(mainPanel.transform, false);
        RectTransform rt = buttonGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(190f, 56f);

        Image image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.16f, 0.19f, 0.23f, 0.96f);

        Button button = buttonGo.GetComponent<Button>();
        SetButtonLabel(button, label);
        return button;
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null)
        {
            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(button.transform, false);
            RectTransform rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            text = textGo.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        text.text = label;
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

    private void UpdateModeTutorial()
    {
        if (modeTutorialText == null)
        {
            return;
        }

        modeTutorialText.text = drivingPracticeCopy + "\n\n" + cannonPracticeCopy + "\n\n" + warCopy;
    }

    private void EnsureEnemyCountInput()
    {
        if (mainPanel == null)
        {
            return;
        }

        if (enemyCountInput != null)
        {
            return;
        }

        Transform existing = mainPanel.transform.Find("EnemyCountPanel");
        if (existing != null)
        {
            enemyCountInput = existing.GetComponentInChildren<TMP_InputField>(true);
            if (enemyCountInput != null)
            {
                if (string.IsNullOrWhiteSpace(enemyCountInput.text))
                {
                    enemyCountInput.text = defaultEnemyCount.ToString();
                }
                return;
            }
        }

        GameObject panel = new GameObject("EnemyCountPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(mainPanel.transform, false);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(28f, -28f);
        panelRt.sizeDelta = new Vector2(280f, 86f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.08f, 0.10f, 0.14f, 0.92f);

        GameObject titleGo = new GameObject("EnemyCountLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panel.transform, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(12f, -10f);
        titleRt.sizeDelta = new Vector2(-24f, 20f);

        TextMeshProUGUI title = titleGo.GetComponent<TextMeshProUGUI>();
        title.text = "Enemy Tanks on Start";
        title.fontSize = 15f;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.86f, 0.56f, 1f);
        title.raycastTarget = false;

        GameObject fieldGo = new GameObject("EnemyCountInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        fieldGo.transform.SetParent(panel.transform, false);
        RectTransform fieldRt = fieldGo.GetComponent<RectTransform>();
        fieldRt.anchorMin = new Vector2(0f, 0f);
        fieldRt.anchorMax = new Vector2(1f, 0f);
        fieldRt.pivot = new Vector2(0f, 0f);
        fieldRt.anchoredPosition = new Vector2(12f, 12f);
        fieldRt.sizeDelta = new Vector2(-24f, 34f);

        Image fieldBackground = fieldGo.GetComponent<Image>();
        fieldBackground.color = new Color(0.16f, 0.18f, 0.22f, 1f);

        GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        placeholderGo.transform.SetParent(fieldGo.transform, false);
        RectTransform placeholderRt = placeholderGo.GetComponent<RectTransform>();
        placeholderRt.anchorMin = Vector2.zero;
        placeholderRt.anchorMax = Vector2.one;
        placeholderRt.offsetMin = new Vector2(10f, 4f);
        placeholderRt.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI placeholder = placeholderGo.GetComponent<TextMeshProUGUI>();
        placeholder.text = "5";
        placeholder.fontSize = 18f;
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);
        placeholder.raycastTarget = false;

        GameObject textGo = new GameObject("Text Area", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(fieldGo.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 4f);
        textRt.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI inputText = textGo.GetComponent<TextMeshProUGUI>();
        inputText.fontSize = 18f;
        inputText.color = new Color(1f, 1f, 1f, 1f);
        inputText.raycastTarget = false;

        enemyCountInput = fieldGo.GetComponent<TMP_InputField>();
        enemyCountInput.textComponent = inputText;
        enemyCountInput.placeholder = placeholder;
        enemyCountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        enemyCountInput.lineType = TMP_InputField.LineType.SingleLine;
        enemyCountInput.characterLimit = 2;
        enemyCountInput.text = defaultEnemyCount.ToString();
    }

    private int GetDesiredEnemyCount()
    {
        if (enemyCountInput == null)
        {
            return Mathf.Max(0, defaultEnemyCount);
        }

        if (!int.TryParse(enemyCountInput.text, out int count))
        {
            count = defaultEnemyCount;
        }

        return Mathf.Clamp(count, 0, 12);
    }

    private void StretchInstructionsText(RectTransform rt)
    {
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(28f, 28f);
        rt.offsetMax = new Vector2(-28f, -120f);
    }
}
