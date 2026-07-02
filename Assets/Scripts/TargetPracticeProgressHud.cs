using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TargetPracticeProgressHud : MonoBehaviour
{
    public TextMeshProUGUI progressLabel;
    private Canvas _canvas;

    public static TargetPracticeProgressHud Ensure()
    {
        TargetPracticeProgressHud existing = Object.FindAnyObjectByType<TargetPracticeProgressHud>();
        if (existing != null)
        {
            return existing;
        }

        GameObject host = new GameObject("TargetPracticeProgressHud");
        TargetPracticeProgressHud hud = host.AddComponent<TargetPracticeProgressHud>();
        hud.BuildUi();
        return hud;
    }

    private void Awake()
    {
        if (progressLabel == null)
        {
            BuildUi();
        }
    }

    private void Update()
    {
        if (progressLabel == null)
        {
            return;
        }

        int total = PracticeTargetTracker.CountPracticeTargets();
        int cleared = PracticeTargetTracker.CountClearedPracticeTargets();
        if (total <= 0)
        {
            progressLabel.text = "TARGETS  --";
            return;
        }

        int percent = Mathf.RoundToInt((cleared / (float)total) * 100f);
        progressLabel.text = "TARGETS  " + cleared + "/" + total + "  (" + percent + "%)";
    }

    private void BuildUi()
    {
        if (_canvas != null)
        {
            return;
        }

        GameObject canvasGo = new GameObject("TargetPracticeHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);

        GameObject panel = new GameObject("TargetPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -18f);
        panelRect.sizeDelta = new Vector2(320f, 56f);
        panel.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.82f);

        GameObject labelGo = new GameObject("TargetPercent", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 8f);
        labelRect.offsetMax = new Vector2(-12f, -8f);

        progressLabel = labelGo.GetComponent<TextMeshProUGUI>();
        progressLabel.fontSize = 26f;
        progressLabel.fontStyle = FontStyles.Bold;
        progressLabel.alignment = TextAlignmentOptions.Center;
        progressLabel.color = new Color(0.92f, 0.98f, 1f, 1f);
        progressLabel.text = "TARGETS  0/0";
        progressLabel.raycastTarget = false;
    }
}
