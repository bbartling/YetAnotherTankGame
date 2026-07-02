#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneHierarchyCleanup
{
    private static readonly string[] ScenePaths =
    {
        MainMenuSceneBuilder.MainMenuScenePath,
        PracticeSceneBuilder.WarScenePath,
        PracticeSceneBuilder.DrivingScenePath,
        PracticeSceneBuilder.TargetScenePath
    };

    private static readonly HashSet<string> DeleteExactNames = new HashSet<string>
    {
        "Menu Platform",
        "Left Berm",
        "Right Berm",
        "MainMenuBackdrop",
        "MainMenuCamera",
        "ModesPanel",
        "ControlsPanel",
        "EnemyCountPanel",
        "InstructionsText",
        "ModeTutorialText",
        "MenuTankIcon",
        "ElevationSlider",
        "MassSlider",
        "AngleSlider",
        "TargetCamera",
        "YatgTitle"
    };

    [MenuItem("Tools/Silly Tank/Clean Up All Scene Hierarchies")]
    public static void CleanAllScenes()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("[SceneHierarchyCleanup] Starting cleanup for all gameplay scenes.");

        string originalScene = SceneManager.GetActiveScene().path;
        int totalDeleted = 0;
        int totalComponentsRemoved = 0;
        int totalMissingScriptsRemoved = 0;

        for (int i = 0; i < ScenePaths.Length; i++)
        {
            string scenePath = ScenePaths[i];
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int deleted = 0;
            int componentsRemoved = 0;
            int missingScriptsRemoved = 0;

            deleted += DeleteLegacyObjects();
            deleted += DeleteInactiveLegacyText();
            deleted += DeleteInactiveDuplicateSniperRangeFinder();
            componentsRemoved += RemoveDebugGizmoComponents();
            missingScriptsRemoved += RemoveMissingScripts();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            report.AppendLine(" - " + scene.name + ": deleted=" + deleted
                + " debugComponents=" + componentsRemoved
                + " missingScripts=" + missingScriptsRemoved);
            Debug.Log("[SceneHierarchyCleanup] " + scene.name + ": deleted=" + deleted
                + " debugComponents=" + componentsRemoved
                + " missingScripts=" + missingScriptsRemoved);

            totalDeleted += deleted;
            totalComponentsRemoved += componentsRemoved;
            totalMissingScriptsRemoved += missingScriptsRemoved;
        }

        if (!string.IsNullOrEmpty(originalScene))
        {
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
        }

        report.AppendLine("[SceneHierarchyCleanup] Done. deleted=" + totalDeleted
            + " debugComponents=" + totalComponentsRemoved
            + " missingScripts=" + totalMissingScriptsRemoved);
        Debug.Log("[SceneHierarchyCleanup] Done. deleted=" + totalDeleted
            + " debugComponents=" + totalComponentsRemoved
            + " missingScripts=" + totalMissingScriptsRemoved);
        Debug.Log(report.ToString());
    }

    private static int DeleteLegacyObjects()
    {
        List<GameObject> toDelete = new List<GameObject>();
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate == null)
            {
                continue;
            }

            if (DeleteExactNames.Contains(candidate.name))
            {
                toDelete.Add(candidate);
            }
        }

        for (int i = 0; i < toDelete.Count; i++)
        {
            Object.DestroyImmediate(toDelete[i]);
        }

        return toDelete.Count;
    }

    private static int DeleteInactiveLegacyText()
    {
        List<GameObject> toDelete = new List<GameObject>();
        TextMeshProUGUI[] labels = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        for (int i = 0; i < labels.Length; i++)
        {
            TextMeshProUGUI label = labels[i];
            if (label == null || label.gameObject.activeSelf)
            {
                continue;
            }

            if (label.transform.parent != null && label.transform.parent.GetComponent<UnityEngine.UI.Button>() != null)
            {
                continue;
            }

            string text = label.text ?? string.Empty;
            bool legacyCopy = label.gameObject.name == "Text"
                || text.IndexOf("hill-climb", System.StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("silly physics", System.StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("controls:", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (legacyCopy)
            {
                toDelete.Add(label.gameObject);
            }
        }

        for (int i = 0; i < toDelete.Count; i++)
        {
            Object.DestroyImmediate(toDelete[i]);
        }

        return toDelete.Count;
    }

    private static int DeleteInactiveDuplicateSniperRangeFinder()
    {
        List<GameObject> toDelete = new List<GameObject>();
        TankController playerTank = Object.FindAnyObjectByType<TankController>(FindObjectsInactive.Include);
        Transform playerRoot = playerTank != null ? playerTank.transform : null;

        // Resources.FindObjectsOfTypeAll finds components on inactive-in-hierarchy objects;
        // FindObjectsByType(Include) can miss those when the GameObject is disabled.
        SniperRangeFinder[] finders = Resources.FindObjectsOfTypeAll<SniperRangeFinder>();
        for (int i = 0; i < finders.Length; i++)
        {
            SniperRangeFinder finder = finders[i];
            if (finder == null || !finder.gameObject.scene.IsValid())
            {
                continue;
            }

            if (finder.gameObject.activeSelf)
            {
                continue;
            }

            if (playerRoot != null && finder.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            toDelete.Add(finder.gameObject);
        }

        for (int i = 0; i < toDelete.Count; i++)
        {
            Object.DestroyImmediate(toDelete[i]);
        }

        return toDelete.Count;
    }

    private static int RemoveDebugGizmoComponents()
    {
        int removed = 0;
        TankPlayableDebugGizmos[] gizmos = Object.FindObjectsByType<TankPlayableDebugGizmos>(FindObjectsInactive.Include);
        for (int i = 0; i < gizmos.Length; i++)
        {
            if (gizmos[i] != null)
            {
                Object.DestroyImmediate(gizmos[i], true);
                removed++;
            }
        }

        return removed;
    }

    private static int RemoveMissingScripts()
    {
        int removed = 0;
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject target = allObjects[i];
            if (target == null)
            {
                continue;
            }

            int before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);
            if (before <= 0)
            {
                continue;
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
            removed += before;
        }

        return removed;
    }
}
#endif
