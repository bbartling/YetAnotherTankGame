using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class MiniMapHUD : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTarget;
    public Transform turretTarget;
    public Transform castleTarget;
    public TankController tankController;
    public CraterTerrain terrainSource;
    public Canvas targetCanvas;

    [Header("UI")]
    public Vector2 panelSize = new Vector2(300f, 230f);
    public Vector2 mapSize = new Vector2(190f, 190f);
    public Vector2 compassSize = new Vector2(74f, 74f);
    public Vector2 panelMargin = new Vector2(18f, 18f);
    public Color panelColor = new Color(0f, 0f, 0f, 0.55f);
    public Color playerColor = new Color(0.35f, 0.85f, 1f, 1f);
    public Color castleColor = new Color(1f, 0.84f, 0.25f, 1f);
    public Color turretColor = new Color(1f, 0.55f, 0.2f, 1f);
    public Color borderColor = new Color(1f, 1f, 1f, 0f);

    private RectTransform _root;
    private RectTransform _mapRect;
    private RectTransform _compassRoot;
    private RectTransform _compassNeedle;
    private TextMeshProUGUI _compassDegreeLabel;
    private RectTransform _playerMarker;
    private RectTransform _castleMarker;
    private TextMeshProUGUI _titleLabel;

    private void Awake()
    {
        AutoWire();
        BuildUI();
    }

    private void AutoWire()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.Find("PlayerTank");
            if (player != null)
            {
                playerTarget = player.transform;
                if (tankController == null)
                {
                    tankController = player.GetComponent<TankController>();
                }
            }
        }

        if (tankController == null && playerTarget != null)
        {
            tankController = playerTarget.GetComponent<TankController>();
        }

        if (turretTarget == null && playerTarget != null)
        {
            Transform foundTurret = playerTarget.Find("Turret");
            if (foundTurret != null)
            {
                turretTarget = foundTurret;
            }
        }

        if (castleTarget == null)
        {
            GameObject castle = GameObject.Find("Castle");
            if (castle == null)
            {
                castle = GameObject.Find("Castle(Clone)");
            }

            if (castle != null)
            {
                castleTarget = castle.transform;
            }
        }

        if (terrainSource == null)
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground != null)
            {
                terrainSource = ground.GetComponent<CraterTerrain>();
            }
        }

        if (targetCanvas == null)
        {
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo != null)
            {
                targetCanvas = canvasGo.GetComponent<Canvas>();
            }
        }
    }

    private void BuildUI()
    {
        if (targetCanvas == null)
        {
            return;
        }

        GameObject rootGo = new GameObject("MiniMapHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootGo.transform.SetParent(targetCanvas.transform, false);

        _root = rootGo.GetComponent<RectTransform>();
        _root.anchorMin = new Vector2(1f, 1f);
        _root.anchorMax = new Vector2(1f, 1f);
        _root.pivot = new Vector2(1f, 1f);
        _root.sizeDelta = panelSize;
        _root.anchoredPosition = -panelMargin;

        Image bg = rootGo.GetComponent<Image>();
        bg.color = panelColor;

        GameObject mapGo = new GameObject("Map", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mapGo.transform.SetParent(rootGo.transform, false);
        _mapRect = mapGo.GetComponent<RectTransform>();
        _mapRect.anchorMin = new Vector2(0f, 1f);
        _mapRect.anchorMax = new Vector2(0f, 1f);
        _mapRect.pivot = new Vector2(0f, 1f);
        _mapRect.sizeDelta = mapSize;
        _mapRect.anchoredPosition = new Vector2(10f, -28f);

        Image mapBg = mapGo.GetComponent<Image>();
        mapBg.color = new Color(0.1f, 0.1f, 0.1f, 0.55f);

        BuildCompass(rootGo.transform);
        CreateTitle(rootGo.transform);
        _playerMarker = CreateMarker("PlayerMarker", _mapRect, playerColor, 10f);
        _castleMarker = CreateMarker("CastleMarker", _mapRect, castleColor, 10f);
    }

    private void BuildCompass(Transform parent)
    {
        GameObject compassGo = new GameObject("TurretCompass", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        compassGo.transform.SetParent(parent, false);

        _compassRoot = compassGo.GetComponent<RectTransform>();
        _compassRoot.anchorMin = new Vector2(0f, 1f);
        _compassRoot.anchorMax = new Vector2(0f, 1f);
        _compassRoot.pivot = new Vector2(0f, 1f);
        _compassRoot.sizeDelta = compassSize;
        _compassRoot.anchoredPosition = new Vector2(mapSize.x + 24f, -28f);

        Image bg = compassGo.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.72f);

        CreateCompassLabel(compassGo.transform, "N", new Vector2(0f, compassSize.y * 0.5f - 10f));
        CreateCompassLabel(compassGo.transform, "E", new Vector2(compassSize.x * 0.5f - 10f, 0f));
        CreateCompassLabel(compassGo.transform, "S", new Vector2(0f, -compassSize.y * 0.5f + 10f));
        CreateCompassLabel(compassGo.transform, "W", new Vector2(-compassSize.x * 0.5f + 10f, 0f));

        GameObject needleGo = new GameObject("Needle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        needleGo.transform.SetParent(compassGo.transform, false);
        _compassNeedle = needleGo.GetComponent<RectTransform>();
        _compassNeedle.anchorMin = new Vector2(0.5f, 0.5f);
        _compassNeedle.anchorMax = new Vector2(0.5f, 0.5f);
        _compassNeedle.pivot = new Vector2(0.5f, 0.1f);
        _compassNeedle.sizeDelta = new Vector2(6f, compassSize.y * 0.36f);
        _compassNeedle.anchoredPosition = Vector2.zero;
        Image needleImg = needleGo.GetComponent<Image>();
        needleImg.color = turretColor;

        GameObject degreeGo = new GameObject("DegreeReadout", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        degreeGo.transform.SetParent(compassGo.transform, false);
        RectTransform degreeRt = degreeGo.GetComponent<RectTransform>();
        degreeRt.anchorMin = new Vector2(0.5f, 0.5f);
        degreeRt.anchorMax = new Vector2(0.5f, 0.5f);
        degreeRt.pivot = new Vector2(0.5f, 0f);
        degreeRt.sizeDelta = new Vector2(compassSize.x - 8f, 20f);
        degreeRt.anchoredPosition = new Vector2(0f, -compassSize.y * 0.38f);
        _compassDegreeLabel = degreeGo.GetComponent<TextMeshProUGUI>();
        _compassDegreeLabel.fontSize = 12f;
        _compassDegreeLabel.alignment = TextAlignmentOptions.Center;
        _compassDegreeLabel.color = new Color(1f, 1f, 1f, 0.9f);
        _compassDegreeLabel.raycastTarget = false;
        _compassDegreeLabel.text = "000°";
    }

    private void CreateCompassLabel(Transform parent, string text, Vector2 anchoredPosition)
    {
        GameObject labelGo = new GameObject($"Label_{text}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(parent, false);
        RectTransform rt = labelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(18f, 18f);
        rt.anchoredPosition = anchoredPosition;
        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 11f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 1f, 1f, 0.75f);
        label.raycastTarget = false;
    }

    private RectTransform CreateMarker(string name, RectTransform parent, Color color, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        Image img = go.GetComponent<Image>();
        img.color = color;
        return rt;
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(parent, false);

        RectTransform rt = titleGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 20f);
        rt.anchoredPosition = new Vector2(0f, -2f);

        _titleLabel = titleGo.GetComponent<TextMeshProUGUI>();
        _titleLabel.text = "MAP";
        _titleLabel.fontSize = 12f;
        _titleLabel.alignment = TextAlignmentOptions.Center;
        _titleLabel.color = new Color(1f, 1f, 1f, 0.8f);
        _titleLabel.raycastTarget = false;
    }

    private void Update()
    {
        if (_root == null)
        {
            return;
        }

        if (playerTarget == null || castleTarget == null)
        {
            AutoWire();
        }

        if (playerTarget == null || castleTarget == null || terrainSource == null)
        {
            _root.gameObject.SetActive(false);
            return;
        }

        _root.gameObject.SetActive(true);
        UpdateMarker(_playerMarker, playerTarget.position);
        UpdateMarker(_castleMarker, castleTarget.position);
        UpdateCompass();
    }

    private void UpdateCompass()
    {
        if (_compassNeedle == null)
        {
            return;
        }

        float turretYawDegrees = 0f;
        bool hasYaw = false;

        if (tankController != null)
        {
            turretYawDegrees = tankController.TurretYawDegrees;
            hasYaw = true;
        }
        else if (turretTarget == null && playerTarget != null)
        {
            Transform foundTurret = playerTarget.Find("Turret");
            if (foundTurret != null)
            {
                turretTarget = foundTurret;
            }
        }

        if (!hasYaw)
        {
            if (turretTarget == null)
            {
                return;
            }

            Vector3 flatForward = Vector3.ProjectOnPlane(turretTarget.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                return;
            }

            turretYawDegrees = Vector3.SignedAngle(Vector3.forward, flatForward.normalized, Vector3.up);
        }

        float normalizedYaw = Mathf.Repeat(turretYawDegrees, 360f);
        _compassNeedle.localRotation = Quaternion.Euler(0f, 0f, -normalizedYaw);

        if (_compassDegreeLabel != null)
        {
            _compassDegreeLabel.text = normalizedYaw.ToString("000") + "°";
        }
    }

    private void UpdateMarker(RectTransform marker, Vector3 worldPosition)
    {
        if (marker == null)
        {
            return;
        }

        float halfWidth = Mathf.Max(1f, terrainSource.terrainWidth * 0.5f);
        float halfLength = Mathf.Max(1f, terrainSource.terrainLength * 0.5f);
        Vector3 local = worldPosition - terrainSource.transform.position;

        float x = Mathf.Clamp(local.x / halfWidth, -1f, 1f);
        float z = Mathf.Clamp(local.z / halfLength, -1f, 1f);

        RectTransform map = marker.parent as RectTransform;
        if (map == null)
        {
            return;
        }

        float width = map.rect.width;
        float height = map.rect.height;
        float px = x * (width * 0.5f);
        float py = z * (height * 0.5f);
        marker.anchoredPosition = new Vector2(px, py);
    }
}
