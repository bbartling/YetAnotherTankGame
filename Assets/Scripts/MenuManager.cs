using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject mainPanel;
    public Button playButton;
    public TankController playerTank;
    public TextMeshProUGUI instructionsText;

    [TextArea(4, 12)]
    public string controlsCopy = "Controls:\nW/S - hold drive forward/back\nA/D - tap while driving to turn tracks\nMouse X - rotate turret\nMouse wheel - elevate barrel\nPageUp / PageDown - fine barrel elevation\nEsc - toggle camera view\nC - controls during game\nLeft click / Space - fire";

    private void Start()
    {
        if (playerTank != null)
        {
            playerTank.enabled = false;
        }

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
