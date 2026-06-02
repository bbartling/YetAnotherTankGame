using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject mainPanel;
    public Button playButton;
    public TankController playerTank;
    public EnemyTankSpawner enemyTankSpawner;
    public TextMeshProUGUI instructionsText;
    public TMP_InputField enemyCountInput;

    [TextArea(4, 12)]
    public string controlsCopy = "Controls:\nW/S - hold drive forward/back\nA/D - tap while driving to turn tracks\nMouse X - rotate turret\nMouse wheel - elevate barrel\nPageUp / PageDown - fine barrel elevation\nEsc - toggle camera view\nC - controls during game\nLeft click / Space - fire";
    public int defaultEnemyCount = 5;

    private void Start()
    {
        if (playerTank != null)
        {
            playerTank.enabled = false;
        }

        EnsureEnemyCountInput();

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(PlayGame);
        }

        EnsureInstructionsText();
        ShowMain();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void PlayGame()
    {
        int enemyCount = GetDesiredEnemyCount();

        if (enemyTankSpawner == null)
        {
            enemyTankSpawner = Object.FindFirstObjectByType<EnemyTankSpawner>();
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
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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
