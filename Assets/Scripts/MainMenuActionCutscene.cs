using UnityEngine;

[DisallowMultipleComponent]
public class MainMenuActionCutscene : MonoBehaviour
{
    public float loopDuration = 5f;
    public Vector3 tankPosition = new Vector3(0f, 0.35f, 6f);
    public Vector3 targetPosition = new Vector3(0f, 1.4f, 42f);

    private Transform _cutsceneRoot;
    private Transform _turretPivot;
    private Transform _barrelPivot;
    private Transform _firePoint;
    private Transform _target;
    private Camera _cutsceneCamera;
    private float _loopTimer;
    private GameObject _activeShell;

    private void Start()
    {
        HideLegacyMenuEnvironment();
        BuildCutscene();
    }

    private void Update()
    {
        if (_cutsceneRoot == null || _cutsceneCamera == null)
        {
            return;
        }

        _loopTimer += Time.deltaTime;
        if (_loopTimer >= loopDuration)
        {
            ResetLoop();
        }

        float t = _loopTimer / loopDuration;
        AnimateCamera(t);
        AnimateTurret(t);
        TryFire(t);
    }

    private void HideLegacyMenuEnvironment()
    {
        string[] hideNames = { "Menu Platform", "Left Berm", "Right Berm", "MainMenuBackdrop" };
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate == null)
            {
                continue;
            }

            for (int n = 0; n < hideNames.Length; n++)
            {
                if (candidate.name == hideNames[n])
                {
                    candidate.SetActive(false);
                    break;
                }
            }
        }

        Camera menuCamera = Camera.main;
        if (menuCamera != null)
        {
            menuCamera.enabled = false;
            AudioListener listener = menuCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = false;
            }
        }
    }

    private void BuildCutscene()
    {
        _cutsceneRoot = new GameObject("MainMenuCutsceneRoot").transform;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "CutsceneGround";
        ground.transform.SetParent(_cutsceneRoot, false);
        ground.transform.position = new Vector3(0f, -0.12f, 24f);
        ground.transform.localScale = new Vector3(80f, 0.2f, 90f);
        ApplyColor(ground, new Color(0.36f, 0.34f, 0.3f, 1f));

        GameObject tankRoot = new GameObject("CutsceneTank");
        tankRoot.transform.SetParent(_cutsceneRoot, false);
        tankRoot.transform.position = tankPosition;
        tankRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        SillyModelInstaller.Ensure(tankRoot, "Models/Tanks/SillyPlayerTank", 3f, false);

        _turretPivot = FindDeepChild(tankRoot.transform, "TurretYawPivot") ?? tankRoot.transform;
        _barrelPivot = FindDeepChild(tankRoot.transform, "BarrelPitchPivot") ?? _turretPivot;
        _firePoint = FindDeepChild(tankRoot.transform, "CannonFirePoint") ?? _barrelPivot;

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "CutsceneTarget";
        target.transform.SetParent(_cutsceneRoot, false);
        target.transform.position = targetPosition;
        target.transform.localScale = new Vector3(2.4f, 2.4f, 0.6f);
        ApplyColor(target, new Color(0.72f, 0.18f, 0.14f, 1f));
        _target = target.transform;

        GameObject cameraGo = new GameObject("CutsceneCamera", typeof(Camera), typeof(AudioListener));
        cameraGo.transform.SetParent(_cutsceneRoot, false);
        _cutsceneCamera = cameraGo.GetComponent<Camera>();
        _cutsceneCamera.tag = "MainCamera";
        _cutsceneCamera.clearFlags = CameraClearFlags.SolidColor;
        _cutsceneCamera.backgroundColor = new Color(0.1f, 0.14f, 0.18f, 1f);
        _cutsceneCamera.fieldOfView = 52f;
    }

    private void ResetLoop()
    {
        _loopTimer = 0f;
        if (_activeShell != null)
        {
            Destroy(_activeShell);
            _activeShell = null;
        }

        if (_target != null)
        {
            _target.localScale = new Vector3(2.4f, 2.4f, 0.6f);
        }

        if (_turretPivot != null)
        {
            _turretPivot.localRotation = Quaternion.identity;
        }

        if (_barrelPivot != null)
        {
            _barrelPivot.localRotation = Quaternion.identity;
        }
    }

    private void AnimateCamera(float t)
    {
        Vector3 start = tankPosition + new Vector3(-7f, 4.2f, -10f);
        Vector3 end = tankPosition + new Vector3(2f, 3.2f, -7f);
        _cutsceneCamera.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
        _cutsceneCamera.transform.LookAt(targetPosition + new Vector3(0f, 0.5f, 0f));
    }

    private void AnimateTurret(float t)
    {
        if (_turretPivot != null)
        {
            _turretPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-8f, 6f, t), 0f);
        }

        if (_barrelPivot != null)
        {
            _barrelPivot.localRotation = Quaternion.Euler(Mathf.Lerp(4f, 18f, t), 0f, 0f);
        }
    }

    private void TryFire(float t)
    {
        if (_activeShell != null || _firePoint == null)
        {
            return;
        }

        if (t < 0.58f || t > 0.62f)
        {
            return;
        }

        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "CutsceneShell";
        shell.transform.SetParent(_cutsceneRoot, false);
        shell.transform.position = _firePoint.position;
        shell.transform.localScale = Vector3.one * 0.55f;
        ApplyColor(shell, new Color(0.22f, 0.2f, 0.18f, 1f));
        Rigidbody body = shell.AddComponent<Rigidbody>();
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Vector3 toTarget = (targetPosition - _firePoint.position).normalized;
        body.linearVelocity = toTarget * 78f + Vector3.up * 16f;
        _activeShell = shell;
    }

    private void LateUpdate()
    {
        if (_activeShell == null || _target == null)
        {
            return;
        }

        if (Vector3.Distance(_activeShell.transform.position, _target.position) < 2.2f)
        {
            _target.localScale = new Vector3(3.2f, 3.2f, 0.2f);
            ApplyColor(_target.gameObject, new Color(0.15f, 0.15f, 0.15f, 1f));
            Destroy(_activeShell);
            _activeShell = null;
        }
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = new Material(Shader.Find("Unlit/Color"));
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
        }

        material.color = color;
        renderer.sharedMaterial = material;
    }
}
