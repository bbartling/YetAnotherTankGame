using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TankRpmGaugeUI : MonoBehaviour
{
    public TankOverdriveController overdrive;
    public bool showOnlyWhileDriving = true;
    public float gaugeTiltDegrees = 225f;

    private RectTransform _root;
    private RectTransform _needlePivot;
    private RectTransform _needleShadowPivot;
    private Image _needleBody;
    private Image _needleTip;
    private TextMeshProUGUI _rpmLabel;
    private float _displayedRatio;
    private float _displayedAngle;

    private void Start()
    {
        if (overdrive == null)
        {
            overdrive = GetComponent<TankOverdriveController>();
        }

        BuildGauge();
    }

    private void Update()
    {
        if (_needlePivot == null || overdrive == null)
        {
            return;
        }

        TankController tank = GetComponent<TankController>();
        bool visible = tank == null || !showOnlyWhileDriving || tank.allowDrivingInput;
        _root.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        float targetRatio = Mathf.InverseLerp(TankGameplayTuning.RpmIdle, TankGameplayTuning.RpmRedline, overdrive.DisplayRpm);
        _displayedRatio = Mathf.Lerp(_displayedRatio, targetRatio, Time.deltaTime * 9f);
        float targetAngle = Mathf.Lerp(72f, -72f, _displayedRatio);
        _displayedAngle = Mathf.LerpAngle(_displayedAngle, targetAngle, Time.deltaTime * 12f);

        Quaternion rotation = Quaternion.Euler(0f, 0f, _displayedAngle);
        _needlePivot.localRotation = rotation;
        if (_needleShadowPivot != null)
        {
            _needleShadowPivot.localRotation = Quaternion.Euler(0f, 0f, _displayedAngle - 2.5f);
        }

        Color rpmColor = GetRpmGradientColor(_displayedRatio);
        if (_needleBody != null)
        {
            _needleBody.color = Color.Lerp(new Color(0.92f, 0.94f, 0.97f, 0.95f), rpmColor, 0.55f);
        }

        if (_needleTip != null)
        {
            _needleTip.color = rpmColor;
        }

        if (_rpmLabel != null)
        {
            _rpmLabel.text = Mathf.RoundToInt(overdrive.DisplayRpm) + " RPM";
            _rpmLabel.color = rpmColor;
        }
    }

    private static Color GetRpmGradientColor(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        Color green = new Color(0.22f, 0.88f, 0.38f, 0.95f);
        Color yellow = new Color(0.98f, 0.86f, 0.14f, 0.95f);
        Color red = new Color(0.96f, 0.16f, 0.1f, 0.98f);

        if (ratio < 0.5f)
        {
            return Color.Lerp(green, yellow, ratio * 2f);
        }

        return Color.Lerp(yellow, red, (ratio - 0.5f) * 2f);
    }

    private void BuildGauge()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("GameplayHudCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
        }

        GameObject root = new GameObject("RpmGauge", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        _root = root.GetComponent<RectTransform>();
        _root.anchorMin = new Vector2(1f, 0f);
        _root.anchorMax = new Vector2(1f, 0f);
        _root.pivot = new Vector2(0.5f, 0f);
        _root.anchoredPosition = new Vector2(-88f, 78f);
        _root.sizeDelta = new Vector2(148f, 148f);
        _root.localRotation = Quaternion.Euler(0f, 0f, gaugeTiltDegrees);

        GameObject rpmBack = new GameObject("RpmNumber", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        rpmBack.transform.SetParent(root.transform, false);
        RectTransform rpmRt = rpmBack.GetComponent<RectTransform>();
        rpmRt.anchorMin = new Vector2(0.5f, 0f);
        rpmRt.anchorMax = new Vector2(0.5f, 0f);
        rpmRt.pivot = new Vector2(0.5f, 0.5f);
        rpmRt.anchoredPosition = new Vector2(0f, 46f);
        rpmRt.sizeDelta = new Vector2(110f, 28f);
        _rpmLabel = rpmBack.GetComponent<TextMeshProUGUI>();
        _rpmLabel.text = "2000 RPM";
        _rpmLabel.fontSize = 17f;
        _rpmLabel.fontStyle = FontStyles.Bold;
        _rpmLabel.alignment = TextAlignmentOptions.Center;
        _rpmLabel.color = GetRpmGradientColor(0f);
        _rpmLabel.raycastTarget = false;

        GameObject shadowPivot = new GameObject("NeedleShadowPivot", typeof(RectTransform));
        shadowPivot.transform.SetParent(root.transform, false);
        _needleShadowPivot = shadowPivot.GetComponent<RectTransform>();
        _needleShadowPivot.anchorMin = new Vector2(0.5f, 0f);
        _needleShadowPivot.anchorMax = new Vector2(0.5f, 0f);
        _needleShadowPivot.pivot = new Vector2(0.5f, 0f);
        _needleShadowPivot.anchoredPosition = new Vector2(2f, 6f);
        _needleShadowPivot.sizeDelta = Vector2.zero;
        CreateBar(_needleShadowPivot, "Shadow", new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(5f, 82f), new Color(0f, 0f, 0f, 0.38f));

        GameObject pivot = new GameObject("NeedlePivot", typeof(RectTransform));
        pivot.transform.SetParent(root.transform, false);
        _needlePivot = pivot.GetComponent<RectTransform>();
        _needlePivot.anchorMin = new Vector2(0.5f, 0f);
        _needlePivot.anchorMax = new Vector2(0.5f, 0f);
        _needlePivot.pivot = new Vector2(0.5f, 0f);
        _needlePivot.anchoredPosition = new Vector2(0f, 6f);
        _needlePivot.sizeDelta = Vector2.zero;

        _needleBody = CreateBar(_needlePivot, "NeedleBody", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(3f, 78f), new Color(0.92f, 0.94f, 0.97f, 0.98f));
        _needleTip = CreateBar(_needlePivot, "NeedleTip", new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(2f, 14f), GetRpmGradientColor(0f));
        CreateBar(_needlePivot, "NeedleCounter", new Vector2(0.5f, 0f), new Vector2(0f, -8f), new Vector2(2f, 16f), new Color(0.4f, 0.44f, 0.5f, 0.8f));
        CreateBar(_needlePivot, "Hub", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(12f, 12f), new Color(0.2f, 0.22f, 0.26f, 1f));
    }

    private static Image CreateBar(Transform parent, string name, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = pivot;
        rt.anchorMax = pivot;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
