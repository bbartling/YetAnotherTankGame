using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Root canvases with CanvasScaler start at zero scale until the scaler runs.
/// This keeps menu UI visible in the Game view and on the first rendered frame.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
[ExecuteAlways]
public class MenuCanvasScalerBootstrap : MonoBehaviour
{
    private void OnEnable()
    {
        ApplyCanvasScale();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyCanvasScale();
    }

    private void Update()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null && rectTransform.localScale.sqrMagnitude < 0.001f)
        {
            ApplyCanvasScale();
        }
    }

    public void ApplyCanvasScale()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            return;
        }

        if (!scaler.enabled)
        {
            scaler.enabled = true;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null && rectTransform.localScale.sqrMagnitude < 0.001f)
        {
            rectTransform.localScale = Vector3.one;
        }
    }
}
