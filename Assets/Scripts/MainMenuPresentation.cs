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
        GameAudioVolume.LoadAndApply();
        EnsureMainMenuCutscene();
        ApplyLayout();
    }

    public void ApplyLayout()
    {
        HideExtraUi();
        RebuildTitleBlock();
        EnsureVolumeControl();
        StyleModeButtons();
        RepositionModeButtons();
    }

    private void EnsureVolumeControl()
    {
        Transform existing = transform.Find("GameVolumeControl");
        if (existing != null)
        {
            return;
        }

        GameObject row = new GameObject("GameVolumeControl", typeof(RectTransform));
        row.transform.SetParent(transform, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(1f, 0f);
        rowRect.anchorMax = new Vector2(1f, 0f);
        rowRect.pivot = new Vector2(1f, 0f);
        rowRect.anchoredPosition = new Vector2(-24f, 24f);
        rowRect.sizeDelta = new Vector2(280f, 44f);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(row.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(92f, 28f);
        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = "GAME VOL";
        label.fontSize = 14f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.9f, 0.94f, 0.98f, 0.95f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;

        GameObject sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(row.transform, false);
        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(44f, 0f);
        sliderRect.sizeDelta = new Vector2(-52f, 18f);

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = GameAudioVolume.MasterVolume;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(sliderGo.transform, false);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.16f, 0.85f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(6f, 0f);
        fillAreaRect.offsetMax = new Vector2(-6f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.35f, 0.72f, 1f, 0.95f);

        GameObject handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlideArea.transform.SetParent(sliderGo.transform, false);
        RectTransform handleAreaRect = handleSlideArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(8f, 0f);
        handleAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleSlideArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 22f);
        handle.GetComponent<Image>().color = new Color(0.95f, 0.92f, 0.78f, 1f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.onValueChanged.AddListener(value => GameAudioVolume.MasterVolume = value);
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
        DisableLegacySubtitleText();

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

    private void DisableLegacySubtitleText()
    {
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

            string text = label.text ?? string.Empty;
            if (text.IndexOf("hill-climb", System.StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("silly physics", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                label.gameObject.SetActive(false);
            }
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
