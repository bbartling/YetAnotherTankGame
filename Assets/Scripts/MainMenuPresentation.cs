using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuPresentation : MonoBehaviour
{
    public const float DefaultButtonRowY = 138f;
    public const float DefaultButtonStagger = 16f;

    public float buttonRowY = DefaultButtonRowY;
    public float buttonStagger = DefaultButtonStagger;
    public float titleTopOffset = -48f;
    public float letterFontSize = 88f;
    public float letterSpacing = 70f;
    public float letterWave = 12f;
    public Color titleColor = new Color(1f, 0.88f, 0.42f, 1f);

    private static readonly string[] TitleLetters = { "Y", "A", "T", "G" };
    private static readonly float[] LetterWavePattern = { 1f, -1f, 1f, -1f };
    private static readonly float[] ButtonStaggerPattern = { 1f, -1f, 1f };

    private void Awake()
    {
        EnsureMainMenuCutscene();
        ApplyLayout();
    }

    public void ApplyLayout()
    {
        HideExtraUi();
        RebuildTitleBlock();
        StyleModeButtons();
        RepositionModeButtons();
    }

    private void EnsureMainMenuCutscene()
    {
        if (FindAnyObjectByType<MainMenuActionCutscene>() != null)
        {
            return;
        }

        GameObject host = new GameObject("MainMenuActionCutscene");
        host.AddComponent<MainMenuActionCutscene>();
    }

    private void HideExtraUi()
    {
        DisableNamedChild("ControlsPanel");
        DisableNamedChild("ModesPanel");
        DisableNamedChild("EnemyCountPanel");
        DisableNamedChild("InstructionsText");
        DisableNamedChild("ModeTutorialText");
        DisableNamedChild("MenuTankIcon");

        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TextMeshProUGUI label = labels[i];
            if (label == null || IsProtectedLabel(label))
            {
                continue;
            }

            if (label.transform.parent != null && label.transform.parent.GetComponent<Button>() != null)
            {
                continue;
            }

            label.gameObject.SetActive(false);
        }
    }

    private bool IsProtectedLabel(TextMeshProUGUI label)
    {
        Transform current = label.transform;
        while (current != null && current != transform)
        {
            if (current.name == "YatgTitle")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void DisableNamedChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void RebuildTitleBlock()
    {
        Transform existing = transform.Find("YatgTitle");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject titleRoot = new GameObject("YatgTitle", typeof(RectTransform));
        titleRoot.transform.SetParent(transform, false);
        RectTransform titleRect = titleRoot.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, titleTopOffset);
        titleRect.sizeDelta = new Vector2(760f, 170f);

        CreateTitleLine(titleRoot.transform, "FullTitle", "YET ANOTHER TANK GAME", 20f, new Vector2(0f, -8f), FontStyles.Bold, new Color(0.95f, 0.84f, 0.5f, 0.98f), 6f);

        float startX = -letterSpacing * (TitleLetters.Length - 1) * 0.5f;
        for (int i = 0; i < TitleLetters.Length; i++)
        {
            float y = -72f + LetterWavePattern[i] * letterWave;
            CreateTitleLine(
                titleRoot.transform,
                "Letter_" + TitleLetters[i],
                TitleLetters[i],
                letterFontSize,
                new Vector2(startX + letterSpacing * i, y),
                FontStyles.Bold,
                titleColor,
                0f);
        }

        CreateTitleLine(
            titleRoot.transform,
            "Subtitle",
            "· silly physics hill-climb tank war ·",
            15f,
            new Vector2(0f, -138f),
            FontStyles.Italic,
            new Color(0.86f, 0.9f, 0.96f, 0.92f),
            0f);

        titleRoot.transform.SetAsFirstSibling();
    }

    private static void CreateTitleLine(
        Transform parent,
        string objectName,
        string text,
        float fontSize,
        Vector2 anchoredPosition,
        FontStyles style,
        Color color,
        float characterSpacing)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(720f, fontSize + 24f);

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.characterSpacing = characterSpacing;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
    }

    private void StyleModeButtons()
    {
        StylePracticeButton("DrivingPracticeButton", "DRIVE", new Color(0.2f, 0.78f, 0.45f, 0.95f));
        StylePracticeButton("CannonPracticeButton", "CANNON", new Color(0.35f, 0.62f, 1f, 0.95f));
        StyleWarButton("WarButton");
    }

    private void StylePracticeButton(string buttonName, string verb, Color outlineColor)
    {
        Transform buttonTransform = FindButtonTransform(buttonName);
        if (buttonTransform == null)
        {
            return;
        }

        TextMeshProUGUI label = buttonTransform.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = "<size=24><b>" + verb + "</b></size>\n<size=13><color=#FFE082>PRACTICE</color></size>";
            label.alignment = TextAlignmentOptions.Center;
            label.lineSpacing = -6f;
        }

        Outline outline = buttonTransform.GetComponent<Outline>();
        if (outline == null)
        {
            outline = buttonTransform.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(2.5f, -2.5f);
    }

    private void StyleWarButton(string buttonName)
    {
        Transform buttonTransform = FindButtonTransform(buttonName);
        if (buttonTransform == null)
        {
            return;
        }

        TextMeshProUGUI label = buttonTransform.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = "<size=28><b>WAR</b></size>";
            label.alignment = TextAlignmentOptions.Center;
        }

        if (buttonTransform.GetComponent<MainMenuWarButtonPulse>() == null)
        {
            buttonTransform.gameObject.AddComponent<MainMenuWarButtonPulse>();
        }
    }

    private Transform FindButtonTransform(string buttonName)
    {
        Transform buttonTransform = transform.Find("ButtonRow/" + buttonName);
        if (buttonTransform == null)
        {
            buttonTransform = transform.Find(buttonName);
        }

        return buttonTransform;
    }

    private void RepositionModeButtons()
    {
        Transform buttonRow = transform.Find("ButtonRow");
        if (buttonRow != null)
        {
            RectTransform rowRect = buttonRow.GetComponent<RectTransform>();
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0.5f, 0f);
                rowRect.anchorMax = new Vector2(0.5f, 0f);
                rowRect.pivot = new Vector2(0.5f, 0f);
                rowRect.anchoredPosition = new Vector2(0f, buttonRowY);
            }
        }

        RepositionButton("DrivingPracticeButton", new Vector2(-220f, buttonStagger * ButtonStaggerPattern[0]));
        RepositionButton("CannonPracticeButton", new Vector2(0f, buttonStagger * ButtonStaggerPattern[1]));
        RepositionButton("WarButton", new Vector2(220f, buttonStagger * ButtonStaggerPattern[2]));
    }

    private void RepositionButton(string buttonName, Vector2 anchoredPosition)
    {
        Transform buttonTransform = FindButtonTransform(buttonName);
        if (buttonTransform == null || !buttonTransform.TryGetComponent(out RectTransform rect))
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(190f, 68f);
        buttonTransform.gameObject.SetActive(true);
    }
}
