using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DrivingLapProgressHud : MonoBehaviour
{
    public TankController playerTank;
    public TextMeshProUGUI progressLabel;

    private IReadOnlyList<Vector3> _waypoints;
    private Canvas _canvas;

    public static DrivingLapProgressHud Ensure(TankController tank)
    {
        DrivingLapProgressHud existing = Object.FindAnyObjectByType<DrivingLapProgressHud>();
        if (existing != null)
        {
            existing.playerTank = tank;
            return existing;
        }

        GameObject host = new GameObject("DrivingLapProgressHud");
        DrivingLapProgressHud hud = host.AddComponent<DrivingLapProgressHud>();
        hud.playerTank = tank;
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

    private void Start()
    {
        _waypoints = DrivingCoursePath.BuildWaypoints();
    }

    private void Update()
    {
        if (playerTank == null)
        {
            playerTank = Object.FindAnyObjectByType<TankController>();
        }

        if (progressLabel == null || playerTank == null)
        {
            return;
        }

        if (_waypoints == null || _waypoints.Count == 0)
        {
            _waypoints = DrivingCoursePath.BuildWaypoints();
        }

        float ratio = DrivingCoursePath.ComputeCompletionRatio(playerTank.transform.position, _waypoints);
        int percent = Mathf.RoundToInt(ratio * 100f);
        progressLabel.text = "LAP  " + percent.ToString("0") + "%";
    }

    private void BuildUi()
    {
        if (_canvas != null)
        {
            return;
        }

        GameObject canvasGo = new GameObject("DrivingLapHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);

        GameObject panel = new GameObject("LapPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -18f);
        panelRect.sizeDelta = new Vector2(260f, 56f);
        panel.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.82f);

        GameObject labelGo = new GameObject("LapPercent", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 8f);
        labelRect.offsetMax = new Vector2(-12f, -8f);

        progressLabel = labelGo.GetComponent<TextMeshProUGUI>();
        progressLabel.fontSize = 28f;
        progressLabel.fontStyle = FontStyles.Bold;
        progressLabel.alignment = TextAlignmentOptions.Center;
        progressLabel.color = new Color(0.92f, 0.98f, 1f, 1f);
        progressLabel.text = "LAP  0%";
        progressLabel.raycastTarget = false;
    }
}
