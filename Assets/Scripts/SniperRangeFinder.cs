using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SniperRangeFinder : MonoBehaviour
{
    [Header("References")]
    public TankController tank;
    public Camera aimCamera;
    public Canvas targetCanvas;

    [Header("Range")]
    public float maxRange = 1200f;
    public float tickSpacing = 50f;
    public float majorTickSpacing = 100f;
    public float simulationStep = 0.06f;
    public LayerMask hitMask = ~0;

    [Header("UI")]
    public float panelWidth = 280f;
    public float panelHeight = 560f;
    public float panelInset = 0f;
    public Color panelColor = new Color(0f, 0f, 0f, 0.28f);
    public Color tickColor = new Color(1f, 1f, 1f, 0.72f);
    public Color majorTickColor = new Color(1f, 1f, 1f, 0.95f);
    public Color indicatorColor = new Color(1f, 0.87f, 0.2f, 1f);
    public int fontSize = 24;

    private RectTransform _root;
    private RectTransform _ticksRoot;
    private Image _indicator;
    private TextMeshProUGUI _rangeLabel;
    private TextMeshProUGUI _hintLabel;
    private readonly List<Image> _ticks = new List<Image>();
    private readonly List<TextMeshProUGUI> _tickLabels = new List<TextMeshProUGUI>();
    private float _lastRange;
    private TankBallistics.Solution _lastSolution;
    private bool _hasSolution;

    private void Awake()
    {
        if (tank == null)
        {
            tank = GetComponent<TankController>();
        }

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (targetCanvas == null)
        {
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo != null)
            {
                targetCanvas = canvasGo.GetComponent<Canvas>();
            }
        }

        BuildUI();
    }

    private void Update()
    {
        bool sniping = Input.GetMouseButton(1);
        if (_root != null)
        {
            _root.gameObject.SetActive(sniping);
        }

        if (!sniping)
        {
            return;
        }

        float estimatedRange = EstimateBallisticRange();
        _lastRange = estimatedRange;
        UpdateBallisticSolution();
        UpdateRangeUI(estimatedRange);
    }

    private void BuildUI()
    {
        if (targetCanvas == null)
        {
            return;
        }

        GameObject rootGo = new GameObject("SniperRangeFinder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootGo.transform.SetParent(targetCanvas.transform, false);

        _root = rootGo.GetComponent<RectTransform>();
        _root.anchorMin = new Vector2(0.5f, 0.5f);
        _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0.5f);
        _root.sizeDelta = new Vector2(panelWidth, panelHeight);
        _root.anchoredPosition = new Vector2(0f, 0f);

        Image background = rootGo.GetComponent<Image>();
        background.color = panelColor;

        GameObject ticksGo = new GameObject("Ticks", typeof(RectTransform));
        ticksGo.transform.SetParent(rootGo.transform, false);
        _ticksRoot = ticksGo.GetComponent<RectTransform>();
        _ticksRoot.anchorMin = Vector2.zero;
        _ticksRoot.anchorMax = Vector2.one;
        _ticksRoot.offsetMin = new Vector2(18f, 18f);
        _ticksRoot.offsetMax = new Vector2(-18f, -18f);

        CreateLabels();
        CreateTicks();
        CreateIndicator();
        CreateHint();

        _root.gameObject.SetActive(false);
    }

    private void CreateLabels()
    {
        _rangeLabel = CreateText("RangeLabel", _root, new Vector2(-18f, panelHeight * 0.5f - 20f), TextAlignmentOptions.Right);
        _rangeLabel.fontSize = fontSize + 6;
        _rangeLabel.color = Color.white;
        _rangeLabel.text = "RANGE --";
        _rangeLabel.enableAutoSizing = true;
        _rangeLabel.fontStyle = FontStyles.Bold;

        TextMeshProUGUI leftLabel = CreateText("NearLabel", _root, new Vector2(-panelWidth + 78f, -panelHeight * 0.5f + 20f), TextAlignmentOptions.Left);
        leftLabel.fontSize = fontSize - 6;
        leftLabel.color = new Color(1f, 1f, 1f, 0.7f);
        leftLabel.text = "NEAR";

        TextMeshProUGUI farLabel = CreateText("FarLabel", _root, new Vector2(-panelWidth + 78f, panelHeight * 0.5f - 40f), TextAlignmentOptions.Left);
        farLabel.fontSize = fontSize - 6;
        farLabel.color = new Color(1f, 1f, 1f, 0.7f);
        farLabel.text = "FAR";
    }

    private void CreateIndicator()
    {
        GameObject indicatorGo = new GameObject("Indicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        indicatorGo.transform.SetParent(_ticksRoot, false);

        RectTransform rt = indicatorGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(26f, 10f);
        rt.anchoredPosition = new Vector2(-2f, 0f);

        _indicator = indicatorGo.GetComponent<Image>();
        _indicator.color = indicatorColor;
    }

    private void CreateHint()
    {
        _hintLabel = CreateText("Hint", _root, new Vector2(-18f, -panelHeight * 0.5f + 26f), TextAlignmentOptions.Right);
        _hintLabel.fontSize = fontSize - 7;
        _hintLabel.color = new Color(1f, 1f, 1f, 0.65f);
        _hintLabel.text = "RMB range finder";
    }

    private void CreateTicks()
    {
        for (float distance = 0f; distance <= maxRange + 0.01f; distance += tickSpacing)
        {
            bool majorTick = Mathf.Abs(distance % majorTickSpacing) < 0.01f;
            float normalized = Mathf.Clamp01(distance / maxRange);
            float y = Mathf.Lerp(-panelHeight * 0.5f + 28f, panelHeight * 0.5f - 28f, normalized);

            Image tick = CreateTick(distance.ToString("F0"), y, majorTick);
            _ticks.Add(tick);
        }
    }

    private Image CreateTick(string label, float y, bool majorTick)
    {
        GameObject tickGo = new GameObject($"Tick_{label}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tickGo.transform.SetParent(_ticksRoot, false);

        RectTransform rt = tickGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(majorTick ? 42f : 22f, majorTick ? 4f : 2f);
        rt.anchoredPosition = new Vector2(0f, y);

        Image image = tickGo.GetComponent<Image>();
        image.color = majorTick ? majorTickColor : tickColor;

        TextMeshProUGUI tickLabel = CreateText($"Label_{label}", tickGo.transform, new Vector2(-52f, 0f), TextAlignmentOptions.Right);
        tickLabel.fontSize = majorTick ? fontSize - 2 : fontSize - 8;
        tickLabel.color = majorTick ? majorTickColor : new Color(1f, 1f, 1f, 0.45f);
        tickLabel.text = majorTick ? label : string.Empty;
        tickLabel.margin = Vector4.zero;
        _tickLabels.Add(tickLabel);

        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPosition, TextAlignmentOptions alignment)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(180f, 36f);
        rt.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.margin = Vector4.zero;
        return text;
    }

    private void UpdateRangeUI(float range)
    {
        if (_rangeLabel != null)
        {
            if (range >= maxRange)
            {
                _rangeLabel.text = $"RANGE {maxRange:0}+ u";
            }
            else
            {
                _rangeLabel.text = $"RANGE {range:0} u";
            }
        }

        if (_indicator != null)
        {
            float normalized = Mathf.Clamp01(range / maxRange);
            float y = Mathf.Lerp(-panelHeight * 0.5f + 28f, panelHeight * 0.5f - 28f, normalized);
            RectTransform rt = _indicator.rectTransform;
            rt.anchoredPosition = new Vector2(-2f, y);

            Color c = indicatorColor;
            if (range >= maxRange)
            {
                c = new Color(1f, 0.45f, 0.15f, 1f);
            }
            _indicator.color = c;
        }

        if (_hintLabel != null)
        {
            _hintLabel.text = _hasSolution
                ? $"ELEV {_lastSolution.ElevationDegrees:0.0}°  TOF {_lastSolution.TimeOfFlight:0.0}s"
                : "NO BALLISTIC SOLUTION";
        }
    }

    private void UpdateBallisticSolution()
    {
        _hasSolution = false;
        if (tank == null || tank.cannonFirePoint == null || aimCamera == null)
        {
            return;
        }

        Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(aimRay, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore)
            ? hit.point
            : aimRay.GetPoint(Mathf.Min(_lastRange, maxRange));

        _hasSolution = TankBallistics.TrySolve(
            tank.cannonFirePoint.position,
            targetPoint,
            TankBallistics.GetMuzzleSpeed(tank.maxPower, tank.powerPercentage),
            Physics.gravity,
            false,
            out _lastSolution);
    }

    private float EstimateBallisticRange()
    {
        Transform muzzle = tank != null ? tank.cannonFirePoint : null;
        if (muzzle == null && tank != null)
        {
            muzzle = tank.firePoint;
        }

        if (muzzle == null)
        {
            return 0f;
        }

        Vector3 origin = muzzle.position;
        float muzzleSpeed = TankBallistics.GetMuzzleSpeed(tank.maxPower, tank.powerPercentage);
        Vector3 velocity = muzzle.forward * muzzleSpeed + GetTankVelocity();
        Vector3 current = origin;
        float traveled = 0f;
        float step = Mathf.Max(0.02f, simulationStep);

        for (int i = 0; i < 500; i++)
        {
            velocity += Physics.gravity * step;
            Vector3 next = current + velocity * step;
            Vector3 segment = next - current;
            float segmentLength = segment.magnitude;

            if (segmentLength > 0.0001f && Physics.Raycast(current, segment / segmentLength, out RaycastHit hit, segmentLength, hitMask, QueryTriggerInteraction.Ignore))
            {
                traveled += hit.distance;
                return traveled;
            }

            traveled += segmentLength;
            current = next;

            if (traveled >= maxRange)
            {
                return maxRange;
            }
        }

        return Mathf.Min(traveled, maxRange);
    }

    private Vector3 GetTankVelocity()
    {
        if (tank == null)
        {
            return Vector3.zero;
        }

        Rigidbody rb = tank.GetComponent<Rigidbody>();
        if (rb == null)
        {
            return Vector3.zero;
        }

#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }
}
