using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PracticeSceneHint : MonoBehaviour
{
    public float displaySeconds = 30f;
    public float fadeSeconds = 1f;

    private CanvasGroup _group;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        if (_group == null)
        {
            _group = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, displaySeconds));

        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.deltaTime;
            _group.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeSeconds);
            yield return null;
        }

        Destroy(gameObject);
    }
}
