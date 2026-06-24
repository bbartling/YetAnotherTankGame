#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class MainMenuSceneBuilder
{
  public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

  [MenuItem("Tools/Silly Tank/Rebuild Main Menu Scene")]
  public static void RebuildMainMenuScene()
  {
    Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    scene.name = "MainMenu";

    BuildLighting();
    BuildEnvironment();
    BuildMenuCamera();
    GameObject menuRoot = BuildMenuUi();
    BuildEventSystem();

    EditorSceneManager.SaveScene(scene, MainMenuScenePath);
    EnsureBuildScenes();
    AssetDatabase.SaveAssets();
  }

  private static void BuildLighting()
  {
    GameObject sun = new GameObject("Menu Sun", typeof(Light));
    Light light = sun.GetComponent<Light>();
    light.type = LightType.Directional;
    light.intensity = 1.1f;
    light.color = new Color(1f, 0.94f, 0.82f, 1f);
    sun.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
    RenderSettings.ambientLight = new Color(0.22f, 0.26f, 0.32f, 1f);
    RenderSettings.fog = true;
    RenderSettings.fogMode = FogMode.Exponential;
    RenderSettings.fogDensity = 0.0045f;
    RenderSettings.fogColor = new Color(0.18f, 0.22f, 0.28f, 1f);
  }

  private static void BuildEnvironment()
  {
    GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    floor.name = "Menu Platform";
    floor.transform.position = new Vector3(0f, -0.2f, 0f);
    floor.transform.localScale = new Vector3(24f, 0.35f, 24f);
    ApplyColor(floor, new Color(0.28f, 0.24f, 0.2f, 1f));

    GameObject backdrop = new GameObject("MainMenuBackdrop");
    MainMenuBackdrop backdropComponent = backdrop.AddComponent<MainMenuBackdrop>();
    backdropComponent.displayTankRoot = BakeMenuDisplayTank(backdrop.transform);

    GameObject leftBerm = GameObject.CreatePrimitive(PrimitiveType.Cube);
    leftBerm.name = "Left Berm";
    leftBerm.transform.position = new Vector3(-8f, 1.2f, -6f);
    leftBerm.transform.localScale = new Vector3(10f, 2.4f, 18f);
    ApplyColor(leftBerm, new Color(0.34f, 0.28f, 0.2f, 1f));

    GameObject rightBerm = GameObject.CreatePrimitive(PrimitiveType.Cube);
    rightBerm.name = "Right Berm";
    rightBerm.transform.position = new Vector3(8f, 1.2f, -6f);
    rightBerm.transform.localScale = new Vector3(10f, 2.4f, 18f);
    ApplyColor(rightBerm, new Color(0.34f, 0.28f, 0.2f, 1f));
  }

  private static void BuildMenuCamera()
  {
    GameObject cameraObject = new GameObject("MainMenuCamera", typeof(Camera), typeof(AudioListener));
    Camera camera = cameraObject.GetComponent<Camera>();
    camera.tag = "MainCamera";
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = new Color(0.12f, 0.16f, 0.2f, 1f);
    cameraObject.transform.position = new Vector3(0f, 4.2f, -10.5f);
    cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
  }

  private static GameObject BuildMenuUi()
  {
    GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
    Canvas canvas = canvasObject.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1600f, 900f);

    GameObject overlay = CreatePanel(canvasObject.transform, "BackdropOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.04f, 0.07f, 0.1f, 0.55f));

    CreateText(overlay.transform, "SILLY TANK", 64, FontStyles.Bold, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(900f, 90f), new Color(1f, 0.88f, 0.42f, 1f));
    CreateText(overlay.transform, "Physics cannon. Hill-climb tracks. Target range. All-out war.", 22, FontStyles.Italic, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(900f, 40f), new Color(0.9f, 0.92f, 0.95f, 0.95f));

    GameObject leftPanel = CreatePanel(overlay.transform, "ControlsPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(420f, 520f), new Color(0.08f, 0.11f, 0.15f, 0.9f));
  CreateText(leftPanel.transform, "CONTROLS", 24, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -16f), new Vector2(-36f, 34f), new Color(1f, 0.86f, 0.45f, 1f));
    TextMeshProUGUI controls = CreateText(leftPanel.transform, GetControlsCopy(), 17, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 18f), new Vector2(-36f, -58f), new Color(0.92f, 0.94f, 0.96f, 1f));
    controls.alignment = TextAlignmentOptions.TopLeft;

    GameObject rightPanel = CreatePanel(overlay.transform, "ModesPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(420f, 520f), new Color(0.08f, 0.11f, 0.15f, 0.9f));
    CreateText(rightPanel.transform, "GAME MODES", 24, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -16f), new Vector2(-36f, 34f), new Color(1f, 0.86f, 0.45f, 1f));
    TextMeshProUGUI modes = CreateText(rightPanel.transform, GetModesCopy(), 17, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 18f), new Vector2(-36f, -58f), new Color(0.92f, 0.94f, 0.96f, 1f));
    modes.alignment = TextAlignmentOptions.TopLeft;

    GameObject buttonRow = CreatePanel(overlay.transform, "ButtonRow", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(760f, 72f), new Color(0f, 0f, 0f, 0f));
    Button driveButton = CreateModeButton(buttonRow.transform, "DrivingPracticeButton", "DRIVE", new Vector2(-250f, 0f), new Color(0.16f, 0.52f, 0.34f, 1f));
    Button cannonButton = CreateModeButton(buttonRow.transform, "CannonPracticeButton", "CANNON", new Vector2(0f, 0f), new Color(0.18f, 0.34f, 0.62f, 1f));
    Button warButton = CreateModeButton(buttonRow.transform, "WarButton", "WAR", new Vector2(250f, 0f), new Color(0.62f, 0.2f, 0.16f, 1f));

    GameObject mainPanel = overlay;
    GameObject menuRoot = new GameObject("MenuManagerRoot");
    MenuManager menu = menuRoot.AddComponent<MenuManager>();
    menu.mainPanel = mainPanel;
    menu.drivingPracticeButton = driveButton;
    menu.cannonPracticeButton = cannonButton;
    menu.warButton = warButton;
    menu.playButton = warButton;
    menu.instructionsText = controls;
    menu.modeTutorialText = modes;
    menu.loadDedicatedPracticeScenes = true;
    menu.controlsCopy = GetControlsCopy();
    menu.drivingPracticeCopy = "DRIVING PRACTICE\nHuge obstacle lap with whoops, jumps, bridge, and hill-climb physics. Hold W to build RPM and unleash overdrive smoke boost.";
    menu.cannonPracticeCopy = "CANNON PRACTICE\nLong-range targets from 120m to 520m. Right-click scope for sniper shots.";
    menu.warCopy = "WAR\nSpawn enemy tanks and defend the castle.";
    return menuRoot;
  }

  private static void BuildEventSystem()
  {
    if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
    {
      return;
    }

    new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
  }

  private static string GetControlsCopy()
  {
    return "W / S - forward and reverse\nA / D - steer tracks\nShift - low gear crawl\nHold W 3s - RPM overdrive boost\nMouse X - turret yaw\nQ / E - barrel pitch\nMouse wheel - fine elevation\n+ / - - cannon power\nRight click - scope\nLeft click / Space - fire\nF - machine gun";
  }

  private static string GetModesCopy()
  {
    return "DRIVE - giant obstacle lap with traversable whoops, jumps, and bridge.\n\nCANNON - extended shooting lanes with exploding targets.\n\nWAR - full battlefield with enemy waves.";
  }

  private static Transform BakeMenuDisplayTank(Transform parent)
  {
    Scene menuScene = SceneManager.GetActiveScene();
    Scene practiceScene = EditorSceneManager.OpenScene(PracticeSceneBuilder.WarScenePath, OpenSceneMode.Additive);
    GameObject sourceTank = FindPlayerTankInScene(practiceScene);

    GameObject displayRoot = new GameObject("MenuDisplayTank");
    displayRoot.transform.SetParent(parent, false);
    displayRoot.transform.localPosition = new Vector3(0f, 0.35f, 2.8f);
    displayRoot.transform.localRotation = Quaternion.Euler(0f, 135f, 0f);

    if (sourceTank != null)
    {
      GameObject clone = Object.Instantiate(sourceTank);
      clone.name = "MenuTankVisual";
      clone.transform.SetParent(displayRoot.transform, false);
      clone.transform.localPosition = Vector3.zero;
      clone.transform.localRotation = Quaternion.identity;
      clone.transform.localScale = Vector3.one;
      StripMenuDisplayComponents(clone);
    }

    EditorSceneManager.CloseScene(practiceScene, true);
    EditorSceneManager.SetActiveScene(menuScene);
    return displayRoot.transform;
  }

  private static GameObject FindPlayerTankInScene(Scene scene)
  {
    foreach (GameObject root in scene.GetRootGameObjects())
    {
      Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
      for (int i = 0; i < transforms.Length; i++)
      {
        if (transforms[i].name == "PlayerTank")
        {
          return transforms[i].gameObject;
        }
      }
    }

    return null;
  }

  private static void StripMenuDisplayComponents(GameObject root)
  {
    Component[] components = root.GetComponentsInChildren<Component>(true);
    for (int i = components.Length - 1; i >= 0; i--)
    {
      Component component = components[i];
      if (component == null)
      {
        continue;
      }

      if (component is Transform || component is MeshFilter || component is MeshRenderer)
      {
        continue;
      }

      Object.DestroyImmediate(component);
    }
  }

  private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
  {
    GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
    panel.transform.SetParent(parent, false);
    RectTransform rt = panel.GetComponent<RectTransform>();
    rt.anchorMin = anchorMin;
    rt.anchorMax = anchorMax;
    rt.pivot = new Vector2(anchorMin.x < 1f ? 0f : 1f, 0.5f);
  if (anchorMin == anchorMax)
    {
      rt.pivot = new Vector2(0.5f, anchorMin.y < 1f ? 0f : 1f);
    }
    rt.anchoredPosition = anchoredPosition;
    rt.sizeDelta = sizeDelta;
    panel.GetComponent<Image>().color = color;
    return panel;
  }

  private static TextMeshProUGUI CreateText(Transform parent, string text, float size, FontStyles style, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
  {
    GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
    go.transform.SetParent(parent, false);
    RectTransform rt = go.GetComponent<RectTransform>();
    rt.anchorMin = anchorMin;
    rt.anchorMax = anchorMax;
    rt.pivot = new Vector2(0f, 1f);
    rt.anchoredPosition = anchoredPosition;
    rt.sizeDelta = sizeDelta;
    TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
    label.text = text;
    label.fontSize = size;
    label.fontStyle = style;
    label.color = color;
    label.raycastTarget = false;
    return label;
  }

  private static Button CreateModeButton(Transform parent, string name, string label, Vector2 position, Color color)
  {
    GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
    buttonGo.transform.SetParent(parent, false);
    RectTransform rt = buttonGo.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.5f, 0.5f);
    rt.anchorMax = new Vector2(0.5f, 0.5f);
    rt.pivot = new Vector2(0.5f, 0.5f);
    rt.anchoredPosition = position;
    rt.sizeDelta = new Vector2(220f, 64f);
    buttonGo.GetComponent<Image>().color = color;
    TextMeshProUGUI text = CreateText(buttonGo.transform, label, 24, FontStyles.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
    text.alignment = TextAlignmentOptions.Center;
    return buttonGo.GetComponent<Button>();
  }

  private static void ApplyColor(GameObject target, Color color)
  {
    Renderer renderer = target.GetComponent<Renderer>();
    if (renderer == null)
    {
      return;
    }

    Material material = new Material(Shader.Find("Standard"));
    material.color = color;
    renderer.sharedMaterial = material;
  }

  private static void EnsureBuildScenes()
  {
    HashSet<string> required = new HashSet<string>
    {
      MainMenuScenePath,
      PracticeSceneBuilder.WarScenePath,
      PracticeSceneBuilder.DrivingScenePath,
      PracticeSceneBuilder.TargetScenePath
    };

    List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
    foreach (string path in required)
    {
      EditorBuildSettingsScene existing = scenes.FirstOrDefault(scene => scene.path == path);
      if (existing == null)
      {
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
      }
      else
      {
        existing.enabled = true;
      }
    }

    scenes = scenes
      .OrderByDescending(scene => scene.path == MainMenuScenePath)
      .ThenBy(scene => scene.path)
      .ToList();
    EditorBuildSettings.scenes = scenes.ToArray();
  }
}
#endif
