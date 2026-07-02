using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PracticeCompletionFanfare : MonoBehaviour
{
    private static PracticeCompletionFanfare _instance;

    public static void Play(string headline, string subline = "returning to menu...")
    {
        EnsureInstance().Show(headline, subline);
    }

    private Canvas _canvas;
    private TextMeshProUGUI _headline;
    private TextMeshProUGUI _subline;
    private RectTransform _headlineRect;
    private Coroutine _pulseRoutine;

    private static PracticeCompletionFanfare EnsureInstance()
    {
        if (_instance != null)
        {
            return _instance;
        }

        GameObject host = new GameObject("PracticeCompletionFanfare");
        _instance = host.AddComponent<PracticeCompletionFanfare>();
        _instance.BuildUi();
        return _instance;
    }

    private void BuildUi()
    {
        GameObject canvasGo = new GameObject("FanfareCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 600;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);

        GameObject headlineGo = new GameObject("Headline", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        headlineGo.transform.SetParent(canvasGo.transform, false);
        _headlineRect = headlineGo.GetComponent<RectTransform>();
        _headlineRect.anchorMin = new Vector2(0.5f, 0.58f);
        _headlineRect.anchorMax = new Vector2(0.5f, 0.58f);
        _headlineRect.pivot = new Vector2(0.5f, 0.5f);
        _headlineRect.sizeDelta = new Vector2(900f, 120f);
        _headline = headlineGo.GetComponent<TextMeshProUGUI>();
        _headline.fontSize = 72f;
        _headline.fontStyle = FontStyles.Bold;
        _headline.alignment = TextAlignmentOptions.Center;
        _headline.color = new Color(1f, 0.92f, 0.35f, 1f);
        _headline.raycastTarget = false;

        GameObject subGo = new GameObject("Subline", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(canvasGo.transform, false);
        RectTransform subRect = subGo.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.48f);
        subRect.anchorMax = new Vector2(0.5f, 0.48f);
        subRect.pivot = new Vector2(0.5f, 0.5f);
        subRect.sizeDelta = new Vector2(700f, 48f);
        _subline = subGo.GetComponent<TextMeshProUGUI>();
        _subline.fontSize = 22f;
        _subline.fontStyle = FontStyles.Italic;
        _subline.alignment = TextAlignmentOptions.Center;
        _subline.color = new Color(0.86f, 0.93f, 1f, 0.95f);
        _subline.raycastTarget = false;

        canvasGo.SetActive(false);
    }

    private void Show(string headline, string subline)
    {
        if (_canvas == null)
        {
            BuildUi();
        }

        _headline.text = headline;
        _subline.text = subline;
        _canvas.gameObject.SetActive(true);

        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
        }

        _pulseRoutine = StartCoroutine(PulseHeadline());
        AudioClip clip = ProceduralBattlefieldAudio.CreateTreeFlatten();
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.55f);
        }
    }

    private IEnumerator PulseHeadline()
    {
        float timer = 0f;
        while (timer < 2.4f)
        {
            timer += Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(timer * 14f) * 0.06f;
            _headlineRect.localScale = Vector3.one * pulse;
            yield return null;
        }

        _headlineRect.localScale = Vector3.one;
    }
}
