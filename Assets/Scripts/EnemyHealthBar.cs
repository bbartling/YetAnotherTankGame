using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    public EnemyTankAI enemyTank;
    public Vector3 worldOffset = new Vector3(0f, 3.2f, 0f);
    public Vector2 barSize = new Vector2(1.25f, 0.12f);
    public float maxVisibleDistance = 260f;
    public Color healthyColor = new Color(0.12f, 0.9f, 0.2f, 0.95f);
    public Color damageBackgroundColor = new Color(0.8f, 0.05f, 0.04f, 0.9f);

    private Canvas _canvas;
    private Image _fill;
    private TextMeshProUGUI _label;
    private Camera _camera;

    private void Awake()
    {
        if (enemyTank == null)
        {
            enemyTank = GetComponent<EnemyTankAI>();
        }

        BuildUI();
    }

    private void LateUpdate()
    {
        if (_canvas == null || enemyTank == null || enemyTank.IsDestroyed)
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
            return;
        }

        if (_camera == null || !_camera.isActiveAndEnabled)
        {
            _camera = Camera.main;
        }

        if (_camera == null)
        {
            return;
        }

        float distance = Vector3.Distance(_camera.transform.position, transform.position);
        bool visible = distance <= maxVisibleDistance;
        _canvas.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        Transform canvasTransform = _canvas.transform;
        canvasTransform.position = transform.position + worldOffset;
        canvasTransform.rotation = Quaternion.LookRotation(canvasTransform.position - _camera.transform.position, Vector3.up);

        float normalized = Mathf.Clamp01(enemyTank.HealthPercent / 100f);
        if (_fill != null)
        {
            _fill.fillAmount = normalized;
            _fill.color = healthyColor;
        }

        if (_label != null)
        {
            _label.text = enemyTank.HealthPercent.ToString("0") + "%";
        }
    }

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("EnemyHealthBarCanvas", typeof(RectTransform), typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 10;

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(160f, 28f);
        canvasRect.localScale = new Vector3(0.018f, 0.018f, 0.018f);

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(128f, 10f);
        bg.GetComponent<Image>().color = damageBackgroundColor;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(bg.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        _fill = fill.GetComponent<Image>();
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = 0;
        _fill.color = healthyColor;

        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(canvasGo.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 6f);
        labelRect.sizeDelta = new Vector2(140f, 18f);
        _label = label.GetComponent<TextMeshProUGUI>();
        _label.alignment = TextAlignmentOptions.Center;
        _label.fontSize = 12f;
        _label.color = new Color(1f, 1f, 1f, 0.9f);
        _label.raycastTarget = false;
        _label.text = "100%";
    }
}
