using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// First-run coach marks that point at the cannon sliders / Fire button.
/// Builds its own overlay UI at runtime under the scene Canvas.
/// </summary>
[DisallowMultipleComponent]
public class CannonControlsTutorial : MonoBehaviour
{
    public bool showOnStart = true;
    public KeyCode dismissKey = KeyCode.Escape;

    private CannonManager _cannon;
    private Canvas _canvas;
    private RectTransform _overlayRoot;
    private RectTransform _card;
    private RectTransform _arrow;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _body;
    private Button _nextButton;
    private Button _skipButton;
    private TextMeshProUGUI _nextLabel;

    private readonly List<Step> _steps = new List<Step>(5);
    private int _stepIndex;
    private bool _visible;

    private struct Step
    {
        public string Title;
        public string Body;
        public RectTransform Target;
    }

    private void Start()
    {
        _cannon = GetComponent<CannonManager>();
        if (_cannon == null)
        {
            _cannon = FindAnyObjectByType<CannonManager>();
        }

        _canvas = FindAnyObjectByType<Canvas>();
        if (_cannon == null || _canvas == null)
        {
            Debug.LogWarning("CannonControlsTutorial: missing CannonManager or Canvas.");
            enabled = false;
            return;
        }

        BuildSteps();
        if (_steps.Count == 0)
        {
            enabled = false;
            return;
        }

        BuildOverlay();
        if (showOnStart)
        {
            ShowStep(0);
        }
        else
        {
            SetVisible(false);
        }
    }

    private void Update()
    {
        if (!_visible)
        {
            return;
        }

        if (Input.GetKeyDown(dismissKey))
        {
            Dismiss();
            return;
        }

        PositionArrowAtCurrentTarget();
        FlashTutorialText();
    }

    private void FlashTutorialText()
    {
        // Pulse only the text alpha so the split-screen view stays clear.
        float pulse = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.5f));
        if (_title != null)
        {
            Color c = _title.color;
            c.a = pulse;
            _title.color = c;
        }

        if (_body != null)
        {
            Color c = _body.color;
            c.a = Mathf.Lerp(0.7f, 1f, pulse);
            _body.color = c;
        }
    }

    private void BuildSteps()
    {
        _steps.Clear();
        AddStep("Elevation", "Drag this slider to tilt the barrel up and down.", _cannon.elevationSlider);
        AddStep("Angle", "Drag this slider to traverse left and right.", _cannon.angleSlider);
        AddStep("Power", "Drag this slider to set how hard the shot launches.", _cannon.powerSlider);
        AddStep("Mass", "Drag this slider to change the ball mass (affects speed).", _cannon.massSlider);

        RectTransform fireRect = null;
        if (_cannon != null)
        {
            Button fireButton = FindFireButton();
            if (fireButton != null)
            {
                fireRect = fireButton.transform as RectTransform;
            }
        }

        if (fireRect != null)
        {
            _steps.Add(new Step
            {
                Title = "Fire",
                Body = "When you like the setup, press Fire and watch the shot.",
                Target = fireRect
            });
        }
    }

    private void AddStep(string title, string body, Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        _steps.Add(new Step
        {
            Title = title,
            Body = body,
            Target = slider.transform as RectTransform
        });
    }

    private Button FindFireButton()
    {
        Button[] buttons = _canvas.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == "FireButton")
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void BuildOverlay()
    {
        // Transparent root — no full-screen dim so the split-screen view stays visible.
        GameObject rootObject = new GameObject("CannonControlsTutorialOverlay", typeof(RectTransform));
        _overlayRoot = rootObject.GetComponent<RectTransform>();
        _overlayRoot.SetParent(_canvas.transform, false);
        _overlayRoot.SetAsLastSibling();
        StretchFull(_overlayRoot);

        GameObject cardObject = new GameObject("TutorialCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _card = cardObject.GetComponent<RectTransform>();
        _card.SetParent(_overlayRoot, false);
        // Center-left of the play view (left half of the typical split layout).
        _card.anchorMin = new Vector2(0.22f, 0.5f);
        _card.anchorMax = new Vector2(0.22f, 0.5f);
        _card.pivot = new Vector2(0.5f, 0.5f);
        _card.anchoredPosition = Vector2.zero;
        _card.sizeDelta = new Vector2(360f, 170f);
        Image cardImage = cardObject.GetComponent<Image>();
        cardImage.color = new Color(0.08f, 0.1f, 0.12f, 0.72f);
        cardImage.raycastTarget = true;

        _title = CreateTmp(_card, "Title", 26f, FontStyles.Bold, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(330f, 34f));
        _title.color = new Color(1f, 0.85f, 0.35f, 1f);
        _body = CreateTmp(_card, "Body", 17f, FontStyles.Normal, new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(330f, 58f));
        _body.alignment = TextAlignmentOptions.TopLeft;
        _body.color = Color.white;

        _nextButton = CreateButton(_card, "NextButton", "Next", new Vector2(1f, 0f), new Vector2(-18f, 14f));
        _skipButton = CreateButton(_card, "SkipButton", "Skip", new Vector2(0f, 0f), new Vector2(18f, 14f));
        _nextLabel = _nextButton.GetComponentInChildren<TextMeshProUGUI>();

        _nextButton.onClick.AddListener(Advance);
        _skipButton.onClick.AddListener(Dismiss);

        GameObject arrowObject = new GameObject("TutorialArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _arrow = arrowObject.GetComponent<RectTransform>();
        _arrow.SetParent(_overlayRoot, false);
        _arrow.sizeDelta = new Vector2(40f, 40f);
        _arrow.pivot = new Vector2(1f, 0.5f);
        Image arrowImage = arrowObject.GetComponent<Image>();
        arrowImage.sprite = CreateRightChevronSprite();
        arrowImage.color = new Color(1f, 0.75f, 0.2f, 1f);
        arrowImage.raycastTarget = false;
    }

    private static TextMeshProUGUI CreateTmp(
        RectTransform parent,
        string name,
        float fontSize,
        FontStyles style,
        Vector2 anchor,
        Vector2 anchoredPos,
        Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.text = "";
        return tmp;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(110f, 36f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.45f, 0.75f, 1f);

        Button button = go.GetComponent<Button>();
        TextMeshProUGUI text = CreateTmp(rect, "Label", 18f, FontStyles.Bold, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 30f));
        RectTransform textRect = text.rectTransform;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        text.alignment = TextAlignmentOptions.Center;
        text.text = label;
        text.raycastTarget = false;
        return button;
    }

    private void ShowStep(int index)
    {
        _stepIndex = Mathf.Clamp(index, 0, _steps.Count - 1);
        Step step = _steps[_stepIndex];
        _title.text = $"{step.Title}  ({_stepIndex + 1}/{_steps.Count})";
        _body.text = step.Body;
        if (_nextLabel != null)
        {
            _nextLabel.text = _stepIndex >= _steps.Count - 1 ? "Done" : "Next";
        }

        SetVisible(true);
        PositionArrowAtCurrentTarget();
    }

    private void Advance()
    {
        if (_stepIndex >= _steps.Count - 1)
        {
            Dismiss();
            return;
        }

        ShowStep(_stepIndex + 1);
    }

    private void Dismiss()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        if (_overlayRoot != null)
        {
            _overlayRoot.gameObject.SetActive(visible);
        }
    }

    private void PositionArrowAtCurrentTarget()
    {
        if (!_visible || _arrow == null || _stepIndex < 0 || _stepIndex >= _steps.Count)
        {
            return;
        }

        RectTransform target = _steps[_stepIndex].Target;
        if (target == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        // corners: 0=BL, 1=TL, 2=TR, 3=BR — sit just left of the control, mid-height.
        Vector3 leftMid = (corners[0] + corners[1]) * 0.5f;
        float bob = Mathf.Sin(Time.unscaledTime * 4f) * 6f;
        _arrow.position = leftMid + (Vector3.left * (14f + bob));
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite CreateRightChevronSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color solid = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        // Filled right-pointing chevron.
        for (int x = 8; x < 56; x++)
        {
            float t = (x - 8) / 48f;
            int halfHeight = Mathf.RoundToInt(Mathf.Lerp(22f, 2f, t));
            int center = size / 2;
            for (int y = center - halfHeight; y <= center + halfHeight; y++)
            {
                texture.SetPixel(x, y, solid);
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(1f, 0.5f), 64f);
    }
}
