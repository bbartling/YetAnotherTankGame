#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarMenuScenePatcher
{
    [MenuItem("Tools/Silly Tank/Ensure Menu Canvas And Buttons")]
    public static void EnsureMenuCanvasAndButtons()
    {
        PatchScene(MainMenuSceneBuilder.MainMenuScenePath);
        PatchScene(PracticeSceneBuilder.WarScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[WarMenuScenePatcher] Ensured menu canvas bootstrap and mode buttons on MainMenu + Practice scenes.");
    }

    private static void PatchScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        MenuManager menu = Object.FindAnyObjectByType<MenuManager>();
        if (menu == null)
        {
            Debug.LogWarning("[WarMenuScenePatcher] No MenuManager found in " + scenePath);
            return;
        }

        EnsureCanvasBootstrap(menu.mainPanel);
        EnsureModeButtons(menu);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureCanvasBootstrap(GameObject mainPanel)
    {
        if (mainPanel == null)
        {
            return;
        }

        Canvas canvas = mainPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (canvas.GetComponent<MenuCanvasScalerBootstrap>() == null)
        {
            canvas.gameObject.AddComponent<MenuCanvasScalerBootstrap>();
        }
    }

    private static void EnsureModeButtons(MenuManager menu)
    {
        if (menu.mainPanel == null)
        {
            return;
        }

        menu.mainPanel.SetActive(true);

        Transform buttonRow = menu.mainPanel.transform.Find("ButtonRow");
        if (buttonRow == null)
        {
            GameObject rowObject = new GameObject("ButtonRow", typeof(RectTransform));
            rowObject.transform.SetParent(menu.mainPanel.transform, false);
            buttonRow = rowObject.transform;
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0f);
            rowRect.anchorMax = new Vector2(0.5f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.anchoredPosition = new Vector2(0f, 42f);
            rowRect.sizeDelta = new Vector2(760f, 72f);
        }

        menu.drivingPracticeButton = EnsureButton(
            buttonRow,
            menu.drivingPracticeButton,
            "DrivingPracticeButton",
            "DRIVE",
            new Vector2(-220f, 0f),
            new Color(0.16f, 0.52f, 0.34f, 1f));
        menu.cannonPracticeButton = EnsureButton(
            buttonRow,
            menu.cannonPracticeButton,
            "CannonPracticeButton",
            "CANNON",
            new Vector2(0f, 0f),
            new Color(0.18f, 0.34f, 0.62f, 1f));
        menu.warButton = EnsureButton(
            buttonRow,
            menu.warButton != null ? menu.warButton : menu.playButton,
            "WarButton",
            "WAR",
            new Vector2(220f, 0f),
            new Color(0.62f, 0.2f, 0.16f, 1f));
        menu.playButton = menu.warButton;

        Transform legacyPlayButton = menu.mainPanel.transform.Find("PlayButton");
        if (legacyPlayButton != null && legacyPlayButton != menu.warButton.transform)
        {
            legacyPlayButton.gameObject.SetActive(false);
        }

        EditorUtility.SetDirty(menu);
    }

    private static Button EnsureButton(
        Transform parent,
        Button existing,
        string name,
        string label,
        Vector2 anchoredPosition,
        Color color)
    {
        if (existing != null)
        {
            ConfigureButton(existing, label, anchoredPosition, color);
            if (existing.transform.parent != parent)
            {
                existing.transform.SetParent(parent, false);
            }

            return existing;
        }

        Transform found = parent.Find(name);
        if (found != null && found.TryGetComponent(out Button foundButton))
        {
            ConfigureButton(foundButton, label, anchoredPosition, color);
            return foundButton;
        }

        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        Button button = buttonGo.GetComponent<Button>();
        ConfigureButton(button, label, anchoredPosition, color);
        return button;
    }

    private static void ConfigureButton(Button button, string label, Vector2 anchoredPosition, Color color)
    {
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(190f, 56f);

        Image image = button.GetComponent<Image>();
        image.color = color;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null)
        {
            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(button.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text = textGo.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
        }

        text.text = label;
        text.raycastTarget = false;
        button.gameObject.SetActive(true);
    }
}
#endif
