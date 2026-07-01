using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuWarButtonPulse : MonoBehaviour
{
    public Color baseColor = new Color(0.62f, 0.2f, 0.16f, 1f);
    public Color pulseColor = new Color(1f, 0.22f, 0.12f, 1f);
    public Color textPulseColor = new Color(1f, 0.95f, 0.55f, 1f);
    public float pulseSpeed = 3.6f;

    private Image _image;
    private TextMeshProUGUI _label;
    private Outline _outline;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _label = GetComponentInChildren<TextMeshProUGUI>(true);
        _outline = GetComponent<Outline>();
        if (_outline == null)
        {
            _outline = gameObject.AddComponent<Outline>();
        }

        _outline.effectColor = new Color(1f, 0.45f, 0.1f, 0.95f);
        _outline.effectDistance = new Vector2(3f, -3f);
    }

    private void Update()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
        if (_image != null)
        {
            _image.color = Color.Lerp(baseColor, pulseColor, pulse);
        }

        if (_label != null)
        {
            _label.color = Color.Lerp(Color.white, textPulseColor, pulse);
        }

        if (_outline != null)
        {
            float scale = Mathf.Lerp(2f, 5f, pulse);
            _outline.effectDistance = new Vector2(scale, -scale);
        }
    }
}
