using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PracticeReturnController : MonoBehaviour
{
    public const string MainMenuSceneName = "MainMenu";

    public PracticeModeInputPolicy.PracticeControlMode mode = PracticeModeInputPolicy.PracticeControlMode.DrivingOnly;
    public TankController playerTank;
    public float returnDelaySeconds = 2.2f;
    public float rolloverReturnDelaySeconds = 1.6f;

    private bool _returning;
    private Canvas _overlayCanvas;
    private TextMeshProUGUI _statusLabel;

    public static PracticeReturnController Ensure(
        PracticeModeInputPolicy.PracticeControlMode practiceMode,
        TankController tank)
    {
        PracticeReturnController existing = Object.FindAnyObjectByType<PracticeReturnController>();
        if (existing != null)
        {
            existing.mode = practiceMode;
            existing.playerTank = tank;
            return existing;
        }

        GameObject host = new GameObject("PracticeReturnController");
        PracticeReturnController controller = host.AddComponent<PracticeReturnController>();
        controller.mode = practiceMode;
        controller.playerTank = tank;
        return controller;
    }

    private void Update()
    {
        if (_returning)
        {
            return;
        }

        if (playerTank == null)
        {
            playerTank = Object.FindAnyObjectByType<TankController>();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BeginReturn("MENU", 0.35f, false);
            return;
        }

        if (mode == PracticeModeInputPolicy.PracticeControlMode.DrivingOnly
            && PracticeFinishLine.CourseCompleted)
        {
            BeginReturn("YAH!", returnDelaySeconds, true, "LAP COMPLETE!");
            return;
        }

        if (mode == PracticeModeInputPolicy.PracticeControlMode.TargetPractice
            && AreAllPracticeTargetsCleared())
        {
            BeginReturn("YAH!", returnDelaySeconds, true, "ALL TARGETS HIT!");
            return;
        }

        if (playerTank != null && IsTankOof())
        {
            BeginReturn("TANK OOF!", rolloverReturnDelaySeconds, true, "nice try...");
        }
    }

    public void TriggerVoidFallReturn()
    {
        BeginReturn("TANK OOF!", rolloverReturnDelaySeconds, true, "nice try...");
    }

    public void TriggerVoidFallReturn(float delaySeconds)
    {
        BeginReturn("TANK OOF!", delaySeconds, true, "nice try...");
    }

    private bool IsTankOof()
    {
        if (playerTank == null)
        {
            return false;
        }

        // Only OOF when the tank is fully upside-down, not merely steeply tipped.
        bool fullyUpsideDown = playerTank.transform.up.y < -0.35f
            && playerTank.RolloverAngle >= playerTank.rolloverDefeatAngle;
        return fullyUpsideDown && playerTank.RolloverSeconds >= playerTank.rolloverDefeatDelay;
    }

    private static bool AreAllPracticeTargetsCleared()
    {
        CastleDamageReceiver[] targets = Object.FindObjectsByType<CastleDamageReceiver>();
        if (targets == null || targets.Length == 0)
        {
            return false;
        }

        int practiceTargets = 0;
        for (int i = 0; i < targets.Length; i++)
        {
            CastleDamageReceiver target = targets[i];
            if (target == null || !target.practiceTargetExplosion)
            {
                continue;
            }

            practiceTargets++;
            if (!target.IsCollapsed)
            {
                return false;
            }
        }

        return practiceTargets > 0;
    }

    private void BeginReturn(string message, float delay, bool celebrate, string subline = "returning to menu...")
    {
        if (_returning)
        {
            return;
        }

        _returning = true;
        if (playerTank != null)
        {
            playerTank.enabled = false;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        ShowStatus(message, subline);
        if (celebrate)
        {
            PracticeCompletionFanfare.Play(message, subline);
        }

        StartCoroutine(ReturnToMainMenuAfter(delay));
    }

    private IEnumerator ReturnToMainMenuAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void ShowStatus(string message, string subline)
    {
        EnsureOverlay();
        if (_statusLabel != null)
        {
            _statusLabel.text = message + "\n<size=18><color=#D7E4F2>" + subline + "</color></size>";
        }
    }

    private void EnsureOverlay()
    {
        if (_overlayCanvas != null)
        {
            return;
        }

        GameObject canvasGo = new GameObject("PracticeReturnOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _overlayCanvas = canvasGo.GetComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.08f, 0.72f);

        GameObject labelGo = new GameObject("Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(760f, 180f);

        _statusLabel = labelGo.GetComponent<TextMeshProUGUI>();
        _statusLabel.fontSize = 54f;
        _statusLabel.fontStyle = FontStyles.Bold;
        _statusLabel.alignment = TextAlignmentOptions.Center;
        _statusLabel.color = new Color(1f, 0.92f, 0.55f, 1f);
        _statusLabel.raycastTarget = false;
    }
}
