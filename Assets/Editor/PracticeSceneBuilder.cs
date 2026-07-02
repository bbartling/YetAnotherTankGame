#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PracticeSceneBuilder
{
    public const string WarScenePath = "Assets/Scenes/Practice.unity";
    public const string DrivingScenePath = "Assets/Scenes/TankDrivingPractice.unity";
    public const string TargetScenePath = "Assets/Scenes/TankTargetPractice.unity";
    private const float LaneHalfWidth = 17f;

    private static PhysicsMaterial _coursePhysicsMaterial;

    [MenuItem("Tools/Silly Tank/Rebuild Dedicated Practice Scenes")]
    public static void RebuildDedicatedPracticeScenes()
    {
        EditorSceneManager.OpenScene(WarScenePath, OpenSceneMode.Single);
        GameObject sourceTank = GameObject.Find("PlayerTank");
        if (sourceTank == null)
        {
            throw new MissingReferenceException("Practice scene must contain PlayerTank before rebuilding dedicated practice scenes.");
        }

        BuildDrivingPracticeScene(sourceTank);

        EditorSceneManager.OpenScene(WarScenePath, OpenSceneMode.Single);
        sourceTank = GameObject.Find("PlayerTank");
        BuildTargetPracticeScene(sourceTank);

        EnsureBuildScenes();
        EditorSceneManager.OpenScene(WarScenePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildDrivingPracticeScene(GameObject sourceTank)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scene.name = "TankDrivingPractice";
        EditorSceneManager.SetActiveScene(scene);
        BuildLighting("Driving Sun", new Vector3(50f, -35f, 0f));
        BuildDrivingCourseFloor();
        BuildExpandedStadiumWalls();
        BuildLapMotocrossCourse();
        BuildFinishLine(new Vector3(0f, 0.35f, -660f), new Vector3(34f, 1.2f, 3f));

        GameObject tank = InstantiatePracticeTank(sourceTank, new Vector3(0f, 2.2f, -672f), Quaternion.identity);
        ConfigureDrivingTank(tank);
        AddPracticePolicy(tank, PracticeModeInputPolicy.PracticeControlMode.DrivingOnly, "DrivingPracticePolicy");
        BuildDrivingPracticeTestHarness(tank);
        BuildDrivingPracticeHint("Driving Practice - hold W to build RPM boost. Hit the tabletop jumps at speed.");

        EditorSceneManager.SaveScene(scene, DrivingScenePath);
    }

    private static void BuildTargetPracticeScene(GameObject sourceTank)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scene.name = "TankTargetPractice";
        EditorSceneManager.SetActiveScene(scene);
        BuildLighting("Range Sun", new Vector3(42f, -25f, 0f));
        BuildFloor("Tank Range Concrete", new Vector3(0f, -0.08f, 240f), new Vector3(90f, 0.16f, 620f), new Color(0.36f, 0.36f, 0.34f, 1f));
        BuildExtendedRangeLanes();

        GameObject tank = InstantiatePracticeTank(sourceTank, new Vector3(0f, 1.35f, -40f), Quaternion.identity);
        ConfigureTankForScene(tank);
        ConfigureTargetRangeTank(tank);
        AddPracticePolicy(tank, PracticeModeInputPolicy.PracticeControlMode.TargetPractice, "TargetPracticePolicy");
        BuildExtendedShootingTargets(-40f);

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

        EditorSceneManager.SaveScene(scene, TargetScenePath);
    }

    private static void ConfigureTargetRangeTank(GameObject tank)
    {
        TankController controller = tank.GetComponent<TankController>();

        TargetRangeTankAnchor anchor = tank.GetComponent<TargetRangeTankAnchor>();
        if (anchor == null)
        {
            anchor = tank.AddComponent<TargetRangeTankAnchor>();
        }

        anchor.tank = controller;
        Physics.SyncTransforms();
        anchor.ApplyNow();
        Physics.SyncTransforms();

        if (controller != null && controller.transform.position.y > 4f)
        {
            float pivotToBottom = anchor.GetPivotToBottomOffset();
            Vector3 corrected = controller.transform.position;
            corrected.y = 0.08f + anchor.hullClearance + pivotToBottom;
            controller.transform.position = corrected;
            Rigidbody body = tank.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = corrected;
            }
        }
    }

    private static GameObject InstantiatePracticeTank(GameObject sourceTank, Vector3 position, Quaternion rotation)
    {
        GameObject tank = Object.Instantiate(sourceTank);
        tank.name = "PlayerTank";
        tank.transform.SetPositionAndRotation(position, rotation);
        tank.SetActive(true);

        TankController controller = tank.GetComponent<TankController>();
        if (controller != null)
        {
            controller.enabled = true;
            controller.continuousTerrainSnapEnabled = false;
        }

        Rigidbody body = tank.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = false;
        }

        return tank;
    }

    private static void ConfigureDrivingTank(GameObject tank)
    {
        ConfigureTankForScene(tank);
        TankOverdriveSetup.Configure(tank);

        TankController controller = tank.GetComponent<TankController>();
        if (controller != null)
        {
            controller.ApplyPracticeDrivingTuning();
            PracticeStuckRecovery recovery = tank.GetComponent<PracticeStuckRecovery>();
            if (recovery == null)
            {
                recovery = tank.AddComponent<PracticeStuckRecovery>();
            }

            recovery.tank = controller;
        }
    }

    private static void ConfigureTankForScene(GameObject tank)
    {
        TankController controller = tank.GetComponent<TankController>();
        Camera camera = tank.GetComponentInChildren<Camera>(true);
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("GameplayCamera");
            cameraObject.transform.SetParent(tank.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 10f, -24f);
            cameraObject.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.gameObject.SetActive(true);
        camera.tag = "MainCamera";
        if (controller != null)
        {
            controller.gameplayCamera = camera;
        }

        camera.transform.SetParent(null, true);

        AudioListener listener = camera.GetComponent<AudioListener>();
        if (listener == null)
        {
            camera.gameObject.AddComponent<AudioListener>();
        }
    }

    private static void AddPracticePolicy(GameObject tank, PracticeModeInputPolicy.PracticeControlMode mode, string name)
    {
        GameObject policyObject = new GameObject(name);
        PracticeModeInputPolicy policy = policyObject.AddComponent<PracticeModeInputPolicy>();
        policy.mode = mode;
        policy.playerTank = tank.GetComponent<TankController>();
    }

    private static void BuildDrivingPracticeTestHarness(GameObject tank)
    {
        GameObject probeObject = new GameObject("DrivingPracticeProbe");
        DrivingPracticeProbe probe = probeObject.AddComponent<DrivingPracticeProbe>();
        probe.playerTank = tank.GetComponent<TankController>();
        probe.logStatus = true;

        DrivingPracticeAutopilot autopilot = probeObject.AddComponent<DrivingPracticeAutopilot>();
        autopilot.playerTank = probe.playerTank;
        autopilot.autoRunOnPlay = false;
        autopilot.muteAudioDuringTest = true;
    }

    private static void BuildDrivingPracticeHint(string message)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        GameObject label = new GameObject("PracticeSceneLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI), typeof(CanvasGroup), typeof(PracticeSceneHint));
        label.transform.SetParent(canvasObject.transform, false);
        RectTransform rt = label.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -18f);
        rt.sizeDelta = new Vector2(620f, 56f);
        TMPro.TextMeshProUGUI text = label.GetComponent<TMPro.TextMeshProUGUI>();
        text.text = message;
        text.fontSize = 24f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.color = new Color(1f, 0.86f, 0.45f, 0.95f);
        text.raycastTarget = false;
    }

    private static void BuildMinimalCanvas(string title)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        GameObject label = new GameObject("PracticeSceneLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
        label.transform.SetParent(canvasObject.transform, false);
        RectTransform rt = label.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -18f);
        rt.sizeDelta = new Vector2(520f, 44f);
        TMPro.TextMeshProUGUI text = label.GetComponent<TMPro.TextMeshProUGUI>();
        text.text = title;
        text.fontSize = 28f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.color = new Color(1f, 0.86f, 0.45f, 0.95f);
        text.raycastTarget = false;

        GameObject eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        eventSystem.SetActive(true);
    }

    private static void BuildLighting(string name, Vector3 euler)
    {
        GameObject lightObject = new GameObject(name, typeof(Light));
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.color = new Color(1f, 0.93f, 0.82f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(euler);
        RenderSettings.ambientLight = new Color(0.38f, 0.42f, 0.47f, 1f);
    }

    private static void BuildFloor(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.transform.position = position;
        floor.transform.localScale = scale;
        ApplyColor(floor, color);
        FinalizeTerrainObstacle(floor);
    }

    private static void BuildDrivingCourseFloor()
    {
        Color dirt = new Color(0.42f, 0.34f, 0.24f, 1f);
        float lane = LaneHalfWidth * 2f;
        BuildFloor("Course Safety Base", new Vector3(0f, -0.35f, 60f), new Vector3(420f, 0.5f, 1720f), new Color(0.34f, 0.28f, 0.2f, 1f));
        BuildFloor("South Start Pad", new Vector3(0f, -0.08f, -660f), new Vector3(lane + 6f, 0.16f, 48f), dirt);
        BuildFloor("Outbound Lane", new Vector3(0f, -0.08f, 60f), new Vector3(lane + 6f, 0.16f, 1680f), dirt);
        BuildFloor("Return Lane", new Vector3(-210f, -0.08f, 60f), new Vector3(lane * 0.9f, 0.16f, 1520f), new Color(0.38f, 0.31f, 0.22f, 1f));
        BuildFloor("Return Connector Pad", new Vector3(-120f, -0.08f, -620f), new Vector3(80f, 0.16f, 40f), dirt);
        BuildFloor("Bridge Pit", new Vector3(0f, -2.8f, 660f), new Vector3(lane + 8f, 0.16f, 70f), new Color(0.22f, 0.18f, 0.14f, 1f));
        BuildFloor("North Turn Pad", new Vector3(-90f, -0.08f, 760f), new Vector3(180f, 0.16f, 60f), dirt);
        BuildFloor("North Bridge Runout", new Vector3(-60f, -0.08f, 820f), new Vector3(lane + 80f, 0.16f, 140f), dirt);
    }

    private static void BuildExpandedStadiumWalls()
    {
        ApplyColor(CreateBox("North Stadium Wall", new Vector3(0f, 3f, 880f), new Vector3(760f, 6f, 2f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateBox("South Stadium Wall", new Vector3(0f, 3f, -840f), new Vector3(760f, 6f, 2f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateBox("West Stadium Wall", new Vector3(-380f, 3f, 0f), new Vector3(2f, 6f, 1680f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateBox("East Stadium Wall", new Vector3(380f, 3f, 0f), new Vector3(2f, 6f, 1680f)), new Color(0.48f, 0.10f, 0.08f, 1f));

        for (int i = 0; i < 48; i++)
        {
            float z = -750f + i * 32f;
            ApplyColor(CreateBox("BleacherStripe_L_" + i, new Vector3(-365f, 6.5f, z), new Vector3(2f, 1f, 14f)), new Color(0.9f, 0.72f, 0.24f, 1f));
            ApplyColor(CreateBox("BleacherStripe_R_" + i, new Vector3(365f, 6.5f, z), new Vector3(2f, 1f, 14f)), new Color(0.9f, 0.72f, 0.24f, 1f));
        }
    }

    private static void BuildLapMotocrossCourse()
    {
        CreateRamp("Entry Ramp Up", new Vector3(0f, 1.1f, -540f), new Vector3(30f, 2.2f, 26f), -12f);
        CreateRamp("Entry Ramp Down", new Vector3(0f, 0.9f, -458f), new Vector3(30f, 1.8f, 26f), 10f);
        ApplyColor(CreateTraversableBox("Entry Ramp Blend", new Vector3(0f, 0.35f, -500f), new Vector3(32f, 0.7f, 18f)), new Color(0.44f, 0.32f, 0.20f, 1f));

        for (int i = 0; i < 22; i++)
        {
            float z = -360f + i * 18f;
            CreateTraversableWhoop("Whoop_" + i, new Vector3(0f, 0f, z), 2.4f, LaneHalfWidth * 2f + 4f);
        }

        CreateRamp("Long Grade Climb", new Vector3(0f, 8f, 40f), new Vector3(30f, 16f, 210f), -24f);
        ApplyColor(CreateTraversableBox("Crest Plateau", new Vector3(0f, 15f, 220f), new Vector3(28f, 1.2f, 24f)), new Color(0.46f, 0.32f, 0.18f, 1f));
        CreateRamp("Crest Descent", new Vector3(0f, 8f, 310f), new Vector3(30f, 16f, 120f), 22f);

        for (int i = 0; i < 8; i++)
        {
            float z = 390f + i * 18f;
            CreateTraversableHump("Speed Hump_" + i, new Vector3((i % 2 == 0) ? -8f : 8f, 0f, z), 3.2f, 14f, 6f);
        }

        BuildTabletopJump(new Vector3(0f, 0f, 488f));

        BuildBridge(new Vector3(0f, 7f, 660f));
        CreateRamp("Bridge Exit", new Vector3(-20f, 5.2f, 732f), new Vector3(30f, 10.4f, 28f), 9f);

        ApplyColor(CreateTraversableBox("North Hairpin Wall Inner", new Vector3(40f, 2.5f, 780f), new Vector3(2f, 5f, 40f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateTraversableBox("North Hairpin Wall Outer", new Vector3(-170f, 2.5f, 780f), new Vector3(2f, 5f, 40f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateTraversableBox("Return Connector", new Vector3(-120f, 0.45f, 760f), new Vector3(28f, 0.9f, 14f)), new Color(0.40f, 0.30f, 0.20f, 1f));

        for (int i = 0; i < 18; i++)
        {
            float z = 700f - i * 50f;
            CreateTraversableHump("Return Hump_" + i, new Vector3(-210f, 0f, z), 2.8f, 16f, 6f);
        }

        CreateRamp("Return Downhill", new Vector3(-210f, 5f, 90f), new Vector3(24f, 10f, 120f), 18f);
        CreateRamp("Return Final Climb", new Vector3(-210f, 1.6f, -440f), new Vector3(24f, 3.2f, 80f), -14f);
        ApplyColor(CreateTraversableBox("Return To Finish", new Vector3(-100f, 0.35f, -620f), new Vector3(50f, 0.7f, 16f)), new Color(0.44f, 0.32f, 0.20f, 1f));
    }

    private static void BuildTabletopJump(Vector3 center)
    {
        CreateRamp("Jump Launch", center + new Vector3(0f, 1.5f, -8f), new Vector3(22f, 3f, 12f), -20f);
        ApplyColor(CreateTraversableBox("Jump Crest", center + new Vector3(0f, 2.35f, 8f), new Vector3(20f, 0.55f, 4f)), new Color(0.55f, 0.38f, 0.22f, 1f));
        CreateRamp("Jump Landing", center + new Vector3(0f, 1.15f, 20f), new Vector3(22f, 2.3f, 14f), 14f);
    }

    private static void BuildBridge(Vector3 deckCenter)
    {
        ApplyColor(CreateTraversableBox("Bridge Deck", deckCenter, new Vector3(34f, 0.9f, 140f)), new Color(0.34f, 0.30f, 0.24f, 1f));
        ApplyColor(CreateTraversableBox("Bridge Left Rail", deckCenter + new Vector3(-17.5f, 1.1f, 0f), new Vector3(0.6f, 1.6f, 140f)), new Color(0.62f, 0.52f, 0.18f, 1f));
        ApplyColor(CreateTraversableBox("Bridge Right Rail", deckCenter + new Vector3(17.5f, 1.1f, 0f), new Vector3(0.6f, 1.6f, 140f)), new Color(0.62f, 0.52f, 0.18f, 1f));

        Vector3[] supports =
        {
            deckCenter + new Vector3(-12f, -3.2f, -50f),
            deckCenter + new Vector3(12f, -3.2f, -50f),
            deckCenter + new Vector3(-12f, -3.2f, 0f),
            deckCenter + new Vector3(12f, -3.2f, 0f),
            deckCenter + new Vector3(-12f, -3.2f, 50f),
            deckCenter + new Vector3(12f, -3.2f, 50f)
        };

        for (int i = 0; i < supports.Length; i++)
        {
            ApplyColor(CreateTraversableBox("Bridge Support_" + i, supports[i], new Vector3(1.8f, 6.4f, 1.8f)), new Color(0.28f, 0.24f, 0.20f, 1f));
        }

        CreateRamp("Bridge Approach", new Vector3(0f, 3.6f, 590f), new Vector3(30f, 7.2f, 18f), -14f);
    }

    private static void BuildFinishLine(Vector3 position, Vector3 scale)
    {
        GameObject finish = CreateBox("Finish Line", position, scale);
        ApplyColor(finish, new Color(0.95f, 0.95f, 0.95f, 1f));
        ApplyColor(CreateBox("Finish Stripe A", position + new Vector3(-4f, 0.35f, 0f), new Vector3(3f, 0.15f, scale.z)), new Color(0.12f, 0.12f, 0.12f, 1f));
        ApplyColor(CreateBox("Finish Stripe B", position + new Vector3(4f, 0.35f, 0f), new Vector3(3f, 0.15f, scale.z)), new Color(0.12f, 0.12f, 0.12f, 1f));

        BoxCollider trigger = finish.GetComponent<BoxCollider>();
        if (trigger == null)
        {
            trigger = finish.AddComponent<BoxCollider>();
        }

        trigger.isTrigger = true;
        PracticeFinishLine line = finish.AddComponent<PracticeFinishLine>();
        line.finishMessage = "DRIVING_PRACTICE_FINISH";
        CheckerFlagVisual.EnsureAtFinish(line);
    }

    private static void CreateTraversableWhoop(string name, Vector3 center, float peakHeight, float width)
    {
        float rampWidth = width + 2f;
        float rampDepth = 12f;
        CreateRamp(name + "_Lead", center + new Vector3(0f, peakHeight * 0.45f, -2.5f), new Vector3(rampWidth, peakHeight * 0.9f, rampDepth), -22f);
        CreateRamp(name + "_Trail", center + new Vector3(0f, peakHeight * 0.45f, 2.5f), new Vector3(rampWidth, peakHeight * 0.9f, rampDepth), 22f);
    }

    private static void CreateTraversableHump(string name, Vector3 center, float peakHeight, float width, float depth)
    {
        CreateRamp(name + "_Lead", center + new Vector3(0f, peakHeight * 0.45f, -depth * 0.35f), new Vector3(width, peakHeight * 0.9f, depth), -22f);
        CreateRamp(name + "_Trail", center + new Vector3(0f, peakHeight * 0.45f, depth * 0.35f), new Vector3(width, peakHeight * 0.9f, depth), 22f);
    }

    private static GameObject CreateTraversableBox(string name, Vector3 position, Vector3 scale)
    {
        GameObject box = CreateBox(name, position, scale);
        FinalizeTerrainObstacle(box);
        return box;
    }

    private static void FinalizeTerrainObstacle(GameObject obstacle)
    {
        Collider collider = obstacle.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false;
            collider.material = GetCoursePhysicsMaterial();
        }

        if (ShouldRoundObstacle(obstacle))
        {
            CourseRoundedColliderBuilder.ApplyRoundedCollider(obstacle);
            ApplyPhysicsMaterialToChildren(obstacle.transform);
        }
    }

    private static bool ShouldRoundObstacle(GameObject obstacle)
    {
        if (obstacle == null)
        {
            return false;
        }

        string name = obstacle.name;
        if (name.Contains("Floor")
            || name.Contains("Lane")
            || name.Contains("Pad")
            || name.Contains("Safety")
            || name.Contains("Pit")
            || name.Contains("Stadium Wall")
            || name.Contains("Bleacher")
            || name.Contains("Finish"))
        {
            return false;
        }

        Vector3 scale = obstacle.transform.lossyScale;
        return Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) >= 0.8f;
    }

    private static void ApplyPhysicsMaterialToChildren(Transform root)
    {
        PhysicsMaterial material = GetCoursePhysicsMaterial();
        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].material = material;
            }
        }
    }

    private static PhysicsMaterial GetCoursePhysicsMaterial()
    {
        if (_coursePhysicsMaterial == null)
        {
            _coursePhysicsMaterial = new PhysicsMaterial("DrivingCourseGrip")
            {
                dynamicFriction = 0.62f,
                staticFriction = 0.68f,
                bounciness = 0.04f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        return _coursePhysicsMaterial;
    }

    private static void BuildExtendedRangeLanes()
    {
        for (int i = -3; i <= 3; i++)
        {
            ApplyColor(CreateBox("Lane Divider_" + i, new Vector3(i * 8f, 0.08f, 240f), new Vector3(0.35f, 0.16f, 560f)), new Color(0.92f, 0.86f, 0.52f, 1f));
        }

        ApplyColor(CreateBox("Backstop Berm", new Vector3(0f, 6f, 520f), new Vector3(78f, 12f, 8f)), new Color(0.36f, 0.24f, 0.15f, 1f));
        ApplyColor(CreateBox("Left Range Wall", new Vector3(-46f, 2.5f, 240f), new Vector3(2f, 5f, 560f)), new Color(0.18f, 0.24f, 0.23f, 1f));
        ApplyColor(CreateBox("Right Range Wall", new Vector3(46f, 2.5f, 240f), new Vector3(2f, 5f, 560f)), new Color(0.18f, 0.24f, 0.23f, 1f));

        float[] markerDistances = { 120f, 200f, 320f, 440f, 520f };
        for (int i = 0; i < markerDistances.Length; i++)
        {
            float z = markerDistances[i];
            ApplyColor(CreateBox("Range Marker Post L_" + i, new Vector3(-40f, 1.2f, z), new Vector3(0.4f, 2.4f, 0.4f)), new Color(0.95f, 0.82f, 0.18f, 1f));
            ApplyColor(CreateBox("Range Marker Post R_" + i, new Vector3(40f, 1.2f, z), new Vector3(0.4f, 2.4f, 0.4f)), new Color(0.95f, 0.82f, 0.18f, 1f));
            ApplyColor(CreateBox("Range Marker Sign_" + i, new Vector3(-36f, 2.2f, z), new Vector3(6f, 0.8f, 0.2f)), new Color(0.92f, 0.92f, 0.86f, 1f));
        }
    }

    private static void BuildExtendedShootingTargets(float tankSpawnZ)
    {
        (float distance, float x, float height, Color color)[] targets =
        {
            (120f, -18f, 2.4f, new Color(0.92f, 0.92f, 0.86f, 1f)),
            (200f, 0f, 2.8f, new Color(0.85f, 0.18f, 0.12f, 1f)),
            (320f, 16f, 3.2f, new Color(0.92f, 0.92f, 0.86f, 1f)),
            (440f, -12f, 3.6f, new Color(0.85f, 0.18f, 0.12f, 1f)),
            (520f, 8f, 4f, new Color(0.92f, 0.92f, 0.86f, 1f))
        };

        for (int i = 0; i < targets.Length; i++)
        {
            float z = tankSpawnZ + targets[i].distance;
            Vector3 position = new Vector3(targets[i].x, targets[i].height, z);
            GameObject target = CreateBox("Cannon Target_" + i + "_" + (int)targets[i].distance + "m", position, new Vector3(6f, 5f, 0.6f));
            ApplyColor(target, targets[i].color);
            CastleDamageReceiver receiver = target.AddComponent<CastleDamageReceiver>();
            receiver.skipModelInstaller = true;
            receiver.practiceTargetExplosion = true;
            receiver.maxHealth = 140f;
            receiver.cannonImpactThreshold = 8f;
            receiver.directCannonHitsToCollapse = 1;
            receiver.crumblePieceCount = 36;
            receiver.practiceExplosionPieces = 52;
            receiver.practiceExplosionForce = 46f;

            GameObject post = CreateBox("Target Post_" + i, position + new Vector3(0f, -targets[i].height, 0.2f), new Vector3(0.6f, targets[i].height * 2f, 0.6f));
            ApplyColor(post, new Color(0.25f, 0.18f, 0.11f, 1f));
        }
    }

    private static GameObject CreateRamp(string name, Vector3 position, Vector3 scale, float xAngle)
    {
        GameObject ramp = CreateBox(name, position, scale);
        ramp.transform.rotation = Quaternion.Euler(xAngle, 0f, 0f);
        ApplyColor(ramp, new Color(0.50f, 0.34f, 0.18f, 1f));
        FinalizeTerrainObstacle(ramp);
        return ramp;
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 scale)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        return box;
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
            WarScenePath,
            DrivingScenePath,
            TargetScenePath
        };

        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        foreach (string path in required)
        {
            EditorBuildSettingsScene existing = scenes.FirstOrDefault(scene => scene.path == path);
            if (existing == null)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            else
            {
                existing.enabled = true;
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
