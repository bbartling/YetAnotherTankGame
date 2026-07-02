#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DrivingPracticeTestRunner
{
    private const string DrivingScenePath = PracticeSceneBuilder.DrivingScenePath;
    private const float DefaultTimeoutSeconds = 600f;

    private static bool _waiting;
    private static bool _playModeStarted;
    private static double _deadline;
    private static Action<bool, string> _completionCallback;

    [MenuItem("Tools/Silly Tank/Run Driving Course Autopilot Test")]
    public static void RunFromMenu()
    {
        RunAutopilotTest(DefaultTimeoutSeconds, (success, message) =>
        {
            Debug.Log("[DrivingPracticeTestRunner] " + (success ? "PASS" : "FAIL") + " " + message);
        });
    }

    public static void RunBatchAutopilotTest()
    {
        RunAutopilotTest(DefaultTimeoutSeconds, (success, message) =>
        {
            string resultPath = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "driving_practice_test_result.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(resultPath));
            System.IO.File.WriteAllText(resultPath, (success ? "PASS" : "FAIL") + "|" + message);
            Debug.Log("[DrivingPracticeTestRunner] BATCH " + (success ? "PASS" : "FAIL") + " " + message);
        });
    }

    public static void RunAutopilotTest(float timeoutSeconds, Action<bool, string> onComplete)
    {
        if (_waiting)
        {
            onComplete?.Invoke(false, "Test already running.");
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        EditorSceneManager.OpenScene(DrivingScenePath, OpenSceneMode.Single);
        EnsureAutopilotInScene();

        _completionCallback = onComplete;
        _playModeStarted = false;
        _deadline = EditorApplication.timeSinceStartup + timeoutSeconds + 120d;
        _waiting = true;
        EditorApplication.update += WaitForCompletion;
        EditorApplication.isPlaying = true;
    }

    private static void EnsureAutopilotInScene()
    {
        GameObject probe = GameObject.Find("DrivingPracticeProbe");
        if (probe == null)
        {
            probe = new GameObject("DrivingPracticeProbe");
            probe.AddComponent<DrivingPracticeProbe>();
        }

        DrivingPracticeAutopilot autopilot = probe.GetComponent<DrivingPracticeAutopilot>();
        if (autopilot == null)
        {
            autopilot = probe.AddComponent<DrivingPracticeAutopilot>();
        }

        autopilot.autoRunOnPlay = true;
        autopilot.muteAudioDuringTest = true;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void WaitForCompletion()
    {
        if (!_waiting)
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            if (!_playModeStarted)
            {
                _playModeStarted = true;
                _deadline = EditorApplication.timeSinceStartup + DefaultTimeoutSeconds;
            }
        }
        else if (_playModeStarted)
        {
            Finish(PracticeFinishLine.CourseCompleted, BuildFinishMessage());
            return;
        }

        if (_playModeStarted && EditorApplication.timeSinceStartup >= _deadline)
        {
            Finish(false, BuildFinishMessage() + " Timed out.");
            return;
        }

        if (PracticeFinishLine.CourseCompleted)
        {
            Finish(true, BuildFinishMessage());
        }
    }

    private static string BuildFinishMessage()
    {
        DrivingPracticeProbe probe = UnityEngine.Object.FindAnyObjectByType<DrivingPracticeProbe>();
        DrivingPracticeAutopilot autopilot = UnityEngine.Object.FindAnyObjectByType<DrivingPracticeAutopilot>();
        PracticeStuckRecovery recovery = UnityEngine.Object.FindAnyObjectByType<PracticeStuckRecovery>();
        string telemetry = probe != null ? probe.GetTelemetrySummary() : "telemetry=missing";
        string recoveries = "hardRecoveries=" + (recovery != null ? recovery.HardRecoveryCount.ToString() : "0");
        if (autopilot != null)
        {
            recoveries += " wp=" + (autopilot.WaypointIndex + 1) + "/" + autopilot.WaypointCount;
        }

        return (PracticeFinishLine.CourseCompleted ? "DRIVING_PRACTICE_FINISH " : "INCOMPLETE ")
            + telemetry + " " + recoveries;
    }

    private static void Finish(bool success, string message)
    {
        EditorApplication.update -= WaitForCompletion;
        _waiting = false;

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        Action<bool, string> callback = _completionCallback;
        _completionCallback = null;
        callback?.Invoke(success, message);

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
#endif
