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
        BuildFinishLine(new Vector3(0f, 0.35f, -132f), new Vector3(22f, 1.2f, 2.5f));

        GameObject tank = InstantiatePracticeTank(sourceTank, new Vector3(0f, 1.4f, -138f), Quaternion.identity);
        ConfigureTankForScene(tank);
        AddPracticePolicy(tank, PracticeModeInputPolicy.PracticeControlMode.DrivingOnly, "DrivingPracticePolicy");
        BuildMinimalCanvas("Driving Practice - lap: north over bridge, return west lane, finish south");

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
        AddPracticePolicy(tank, PracticeModeInputPolicy.PracticeControlMode.TargetPractice, "TargetPracticePolicy");
        BuildExtendedShootingTargets(-40f);
        BuildMinimalCanvas("Tank Target Practice - hold RMB to scope, targets at 120-520 m");

        EditorSceneManager.SaveScene(scene, TargetScenePath);
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
    }

    private static void BuildDrivingCourseFloor()
    {
        Color dirt = new Color(0.42f, 0.34f, 0.24f, 1f);
        BuildFloor("South Start Pad", new Vector3(0f, -0.08f, -132f), new Vector3(28f, 0.16f, 24f), dirt);
        BuildFloor("Outbound Lane", new Vector3(0f, -0.08f, 12f), new Vector3(28f, 0.16f, 260f), dirt);
        BuildFloor("Return Lane", new Vector3(-42f, -0.08f, 12f), new Vector3(24f, 0.16f, 260f), new Color(0.38f, 0.31f, 0.22f, 1f));
        BuildFloor("Bridge Pit", new Vector3(0f, -2.8f, 132f), new Vector3(34f, 0.16f, 34f), new Color(0.22f, 0.18f, 0.14f, 1f));
        BuildFloor("North Turn Pad", new Vector3(-18f, -0.08f, 152f), new Vector3(58f, 0.16f, 28f), dirt);
    }

    private static void BuildExpandedStadiumWalls()
    {
        ApplyColor(CreateBox("North Stadium Wall", new Vector3(0f, 3f, 168f), new Vector3(150f, 6f, 2f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateBox("South Stadium Wall", new Vector3(0f, 3f, -168f), new Vector3(150f, 6f, 2f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateBox("West Stadium Wall", new Vector3(-76f, 3f, 0f), new Vector3(2f, 6f, 336f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateBox("East Stadium Wall", new Vector3(76f, 3f, 0f), new Vector3(2f, 6f, 336f)), new Color(0.48f, 0.10f, 0.08f, 1f));

        for (int i = 0; i < 18; i++)
        {
            float z = -150f + i * 17f;
            ApplyColor(CreateBox("BleacherStripe_L_" + i, new Vector3(-73f, 6.5f, z), new Vector3(2f, 1f, 8f)), new Color(0.9f, 0.72f, 0.24f, 1f));
            ApplyColor(CreateBox("BleacherStripe_R_" + i, new Vector3(73f, 6.5f, z), new Vector3(2f, 1f, 8f)), new Color(0.9f, 0.72f, 0.24f, 1f));
        }
    }

    private static void BuildLapMotocrossCourse()
    {
        CreateRamp("Entry Ramp Up", new Vector3(0f, 1.2f, -108f), new Vector3(18f, 2.4f, 14f), -16f);
        CreateRamp("Entry Ramp Down", new Vector3(0f, 1.1f, -92f), new Vector3(18f, 2.2f, 14f), 14f);

        for (int i = 0; i < 10; i++)
        {
            float x = (i % 2 == 0) ? -9f : 9f;
            float z = -72f + i * 8f;
            ApplyColor(CreateCylinder("Rhythm Roller_" + i, new Vector3(x, 0.85f, z), new Vector3(4.5f, 14f, 4.5f), new Vector3(90f, 0f, 0f)), new Color(0.58f, 0.42f, 0.24f, 1f));
        }

        CreateRamp("Long Grade Climb", new Vector3(0f, 4.5f, 8f), new Vector3(20f, 9f, 52f), -24f);
        ApplyColor(CreateBox("Crest Plateau", new Vector3(0f, 8.2f, 44f), new Vector3(18f, 0.8f, 10f)), new Color(0.46f, 0.32f, 0.18f, 1f));
        CreateRamp("Crest Descent", new Vector3(0f, 4.8f, 62f), new Vector3(20f, 9f, 28f), 22f);

        for (int i = 0; i < 4; i++)
        {
            float z = 78f + i * 7f;
            ApplyColor(CreateBox("Speed Hump_" + i, new Vector3((i % 2 == 0) ? -8f : 8f, 0.55f, z), new Vector3(10f, 1.1f, 4f)), new Color(0.52f, 0.36f, 0.20f, 1f));
        }

        CreateRamp("Jump Launch", new Vector3(0f, 1.35f, 96f), new Vector3(16f, 2.7f, 10f), -18f);
        ApplyColor(CreateBox("Jump Crest", new Vector3(0f, 2.2f, 106f), new Vector3(14f, 0.5f, 3f)), new Color(0.55f, 0.38f, 0.22f, 1f));
        CreateRamp("Jump Landing", new Vector3(0f, 1.1f, 114f), new Vector3(16f, 2.2f, 12f), 12f);

        BuildBridge(new Vector3(0f, 5.2f, 132f));
        CreateRamp("Bridge Exit", new Vector3(0f, 2.4f, 148f), new Vector3(16f, 4.8f, 12f), 16f);

        ApplyColor(CreateBox("North Hairpin Wall Inner", new Vector3(8f, 2.5f, 156f), new Vector3(2f, 5f, 24f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateBox("North Hairpin Wall Outer", new Vector3(-34f, 2.5f, 156f), new Vector3(2f, 5f, 24f)), new Color(0.48f, 0.10f, 0.08f, 1f));
        ApplyColor(CreateBox("Return Connector", new Vector3(-24f, 0.45f, 152f), new Vector3(18f, 0.9f, 10f)), new Color(0.40f, 0.30f, 0.20f, 1f));

        for (int i = 0; i < 8; i++)
        {
            float z = 138f - i * 16f;
            ApplyColor(CreateBox("Return Hump_" + i, new Vector3(-42f, 0.55f, z), new Vector3(12f, 1.1f, 5f)), new Color(0.50f, 0.34f, 0.19f, 1f));
        }

        CreateRamp("Return Downhill", new Vector3(-42f, 3.2f, 18f), new Vector3(16f, 6.4f, 36f), 18f);
        CreateRamp("Return Final Climb", new Vector3(-42f, 1.2f, -88f), new Vector3(16f, 2.4f, 24f), -14f);
        ApplyColor(CreateBox("Return To Finish", new Vector3(-20f, 0.35f, -124f), new Vector3(34f, 0.7f, 12f)), new Color(0.44f, 0.32f, 0.20f, 1f));
    }

    private static void BuildBridge(Vector3 deckCenter)
    {
        ApplyColor(CreateBox("Bridge Deck", deckCenter, new Vector3(20f, 0.7f, 28f)), new Color(0.34f, 0.30f, 0.24f, 1f));
        ApplyColor(CreateBox("Bridge Left Rail", deckCenter + new Vector3(-10.2f, 0.9f, 0f), new Vector3(0.5f, 1.4f, 28f)), new Color(0.62f, 0.52f, 0.18f, 1f));
        ApplyColor(CreateBox("Bridge Right Rail", deckCenter + new Vector3(10.2f, 0.9f, 0f), new Vector3(0.5f, 1.4f, 28f)), new Color(0.62f, 0.52f, 0.18f, 1f));

        Vector3[] supports =
        {
            deckCenter + new Vector3(-8f, -2.4f, -10f),
            deckCenter + new Vector3(8f, -2.4f, -10f),
            deckCenter + new Vector3(-8f, -2.4f, 10f),
            deckCenter + new Vector3(8f, -2.4f, 10f)
        };

        for (int i = 0; i < supports.Length; i++)
        {
            ApplyColor(CreateBox("Bridge Support_" + i, supports[i], new Vector3(1.4f, 5f, 1.4f)), new Color(0.28f, 0.24f, 0.20f, 1f));
        }

        CreateRamp("Bridge Approach", new Vector3(0f, 2.8f, 118f), new Vector3(18f, 5.6f, 12f), -14f);
    }

    private static void BuildFinishLine(Vector3 position, Vector3 scale)
    {
        GameObject finish = CreateBox("Finish Line", position, scale);
        ApplyColor(finish, new Color(0.95f, 0.95f, 0.95f, 1f));
        ApplyColor(CreateBox("Finish Stripe A", position + new Vector3(-4f, 0.35f, 0f), new Vector3(3f, 0.15f, scale.z)), new Color(0.12f, 0.12f, 0.12f, 1f));
        ApplyColor(CreateBox("Finish Stripe B", position + new Vector3(4f, 0.35f, 0f), new Vector3(3f, 0.15f, scale.z)), new Color(0.12f, 0.12f, 0.12f, 1f));

        BoxCollider trigger = finish.GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        PracticeFinishLine line = finish.AddComponent<PracticeFinishLine>();
        line.finishMessage = "DRIVING_PRACTICE_FINISH";
    }

    private static void BuildStadiumWalls()
    {
        BuildExpandedStadiumWalls();
    }

    private static void BuildMotocrossObstacles()
    {
        BuildLapMotocrossCourse();
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

    private static void BuildRangeLanes()
    {
        BuildExtendedRangeLanes();
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

    private static void BuildShootingTargets()
    {
        BuildExtendedShootingTargets(-40f);
    }

    private static void BuildDisplayTank()
    {
        GameObject display = CreateBox("Background Tank Silhouette", new Vector3(-24f, 1.1f, 58f), new Vector3(8f, 2.2f, 4.5f));
        ApplyColor(display, new Color(0.16f, 0.32f, 0.28f, 1f));
        GameObject turret = CreateBox("Background Tank Turret", new Vector3(-24f, 2.65f, 58.3f), new Vector3(4f, 1.2f, 3f));
        ApplyColor(turret, new Color(0.12f, 0.26f, 0.22f, 1f));
        GameObject barrel = CreateBox("Background Tank Barrel", new Vector3(-24f, 2.7f, 54.2f), new Vector3(0.55f, 0.55f, 7f));
        ApplyColor(barrel, new Color(0.10f, 0.20f, 0.18f, 1f));
    }

    private static GameObject CreateRamp(string name, Vector3 position, Vector3 scale, float xAngle)
    {
        GameObject ramp = CreateBox(name, position, scale);
        ramp.transform.rotation = Quaternion.Euler(xAngle, 0f, 0f);
        ApplyColor(ramp, new Color(0.50f, 0.34f, 0.18f, 1f));
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

    private static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Vector3 euler)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.position = position;
        cylinder.transform.localScale = scale;
        cylinder.transform.rotation = Quaternion.Euler(euler);
        return cylinder;
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
