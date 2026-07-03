using UnityEngine;

[DisallowMultipleComponent]
public class MainMenuActionCutscene : MonoBehaviour
{
    public float loopDuration = 15f;
    public Vector3 tankPosition = new Vector3(0f, 0.35f, 6f);
    public float tankYawDegrees = 180f;
    public Vector3 targetPosition = new Vector3(0f, 1.4f, -30f);

    private Transform _cutsceneRoot;
    private Transform _turretPivot;
    private Transform _barrelPivot;
    private Transform _firePoint;
    private Transform _target;
    private Transform _tankRoot;
    private Camera _cutsceneCamera;
    private float _loopTimer;
    private GameObject _activeShell;
    private bool _firedPrimary;
    private bool _firedSecondary;

    private void Start()
    {
        HideLegacyMenuEnvironment();
        BuildCutscene();
    }

    private void Update()
    {
        if (_cutsceneRoot == null || _cutsceneCamera == null || _tankRoot == null)
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
        if (menuCamera != null && menuCamera.GetComponent<MainMenuActionCutscene>() == null)
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
        ground.transform.position = new Vector3(0f, -0.12f, -8f);
        ground.transform.localScale = new Vector3(80f, 0.2f, 90f);
        ApplyColor(ground, new Color(0.36f, 0.34f, 0.3f, 1f));
        Collider groundCollider = ground.GetComponent<Collider>();
        if (groundCollider != null)
        {
            groundCollider.enabled = false;
        }

        GameObject tankRoot = new GameObject("CutsceneTank");
        tankRoot.transform.SetParent(_cutsceneRoot, false);
        tankRoot.transform.position = tankPosition;
        tankRoot.transform.rotation = Quaternion.Euler(0f, tankYawDegrees, 0f);
        SillyModelInstaller.Ensure(tankRoot, "Models/Tanks/SillyPlayerTank", 3f, false);
        _tankRoot = tankRoot.transform;

        // Never animate the Blender mesh Turret/Barrel (wrong pivot space after -90 import).
        // Build a dedicated cutscene gun rig parented to the tank root.
        HideVisualGunMeshes(tankRoot.transform);
        BuildCutsceneGunRig(tankRoot.transform);

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "CutsceneTarget";
        target.transform.SetParent(_cutsceneRoot, false);
        target.transform.position = targetPosition;
        target.transform.localScale = new Vector3(2.4f, 2.4f, 0.6f);
        ApplyColor(target, new Color(0.72f, 0.18f, 0.14f, 1f));
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            targetCollider.enabled = false;
        }

        _target = target.transform;

        GameObject cameraGo = new GameObject("CutsceneCamera", typeof(Camera), typeof(AudioListener));
        cameraGo.transform.SetParent(_cutsceneRoot, false);
        _cutsceneCamera = cameraGo.GetComponent<Camera>();
        _cutsceneCamera.tag = "MainCamera";
        _cutsceneCamera.clearFlags = CameraClearFlags.SolidColor;
        _cutsceneCamera.backgroundColor = new Color(0.1f, 0.14f, 0.18f, 1f);
        _cutsceneCamera.fieldOfView = 42f;
        _cutsceneCamera.nearClipPlane = 0.2f;
    }

    private void HideVisualGunMeshes(Transform tankRoot)
    {
        // Keep the model turret; only hide the mesh barrel so our cutscene barrel is the visible gun.
        string[] hideNames = { "Barrel", "BarrelDamaged" };
        Transform[] parts = tankRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            Transform part = parts[i];
            if (part == null || part == tankRoot)
            {
                continue;
            }

            for (int n = 0; n < hideNames.Length; n++)
            {
                if (part.name == hideNames[n])
                {
                    part.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    private void BuildCutsceneGunRig(Transform tankRoot)
    {
        // Dedicated aim rig in tank-local space (visual model uses -90 import rotation).
        GameObject turret = new GameObject("CutsceneTurretPivot");
        turret.transform.SetParent(tankRoot, false);
        turret.transform.localPosition = new Vector3(0f, 2.05f, 0.2f);
        turret.transform.localRotation = Quaternion.identity;
        _turretPivot = turret.transform;

        GameObject barrelPivot = new GameObject("CutsceneBarrelPivot");
        barrelPivot.transform.SetParent(_turretPivot, false);
        barrelPivot.transform.localPosition = new Vector3(0f, 0.15f, 0.9f);
        barrelPivot.transform.localRotation = Quaternion.identity;
        _barrelPivot = barrelPivot.transform;

        // Long dark cube barrel — reliable render, obvious silhouette.
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrel.name = "CutsceneBarrel";
        barrel.transform.SetParent(_barrelPivot, false);
        barrel.transform.localPosition = new Vector3(0f, 0f, 2.4f);
        barrel.transform.localRotation = Quaternion.identity;
        barrel.transform.localScale = new Vector3(0.38f, 0.38f, 4.8f);
        ApplyColor(barrel, new Color(0.12f, 0.12f, 0.12f, 1f));
        DisableCollider(barrel);

        GameObject muzzleBrake = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muzzleBrake.name = "CutsceneMuzzleBrake";
        muzzleBrake.transform.SetParent(_barrelPivot, false);
        muzzleBrake.transform.localPosition = new Vector3(0f, 0f, 4.85f);
        muzzleBrake.transform.localScale = new Vector3(0.55f, 0.55f, 0.45f);
        ApplyColor(muzzleBrake, new Color(0.08f, 0.08f, 0.08f, 1f));
        DisableCollider(muzzleBrake);

        GameObject muzzle = new GameObject("CutsceneMuzzle");
        muzzle.transform.SetParent(_barrelPivot, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 5.2f);
        muzzle.transform.localRotation = Quaternion.identity;
        _firePoint = muzzle.transform;
    }

    private void ResetLoop()
    {
        _loopTimer = 0f;
        _firedPrimary = false;
        _firedSecondary = false;
        if (_activeShell != null)
        {
            Destroy(_activeShell);
            _activeShell = null;
        }

        if (_target != null)
        {
            _target.localScale = new Vector3(2.4f, 2.4f, 0.6f);
            ApplyColor(_target.gameObject, new Color(0.72f, 0.18f, 0.14f, 1f));
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
        Vector3 tankForward = _tankRoot.forward;
        Vector3 tankRight = _tankRoot.right;
        Vector3 tankUp = Vector3.up;
        Vector3 lookFocus = _tankRoot.position + tankUp * 1.6f + tankForward * 1.2f;

        // 2x stand-off from the tank for a wider cinematic frame.
        Vector3 frontClose = _tankRoot.position + tankForward * 28f + tankUp * 6.4f + tankRight * -3f;
        Vector3 frontWide = _tankRoot.position + tankForward * 44f + tankUp * 11f + tankRight * 16f;
        Vector3 frontHero = _tankRoot.position + tankForward * 36f + tankUp * 8.4f + tankRight * -12f;
        Vector3 impact = targetPosition + tankRight * -12f + tankUp * 9f - tankForward * 16f;

        Vector3 position;
        Vector3 lookAt;
        if (t < 0.32f)
        {
            float local = t / 0.32f;
            position = Vector3.Lerp(frontClose, frontWide, Mathf.SmoothStep(0f, 1f, local));
            lookAt = lookFocus;
        }
        else if (t < 0.58f)
        {
            float local = (t - 0.32f) / 0.26f;
            position = Vector3.Lerp(frontWide, frontHero, Mathf.SmoothStep(0f, 1f, local));
            lookAt = Vector3.Lerp(lookFocus, _firePoint != null ? _firePoint.position : lookFocus, local);
        }
        else if (t < 0.82f)
        {
            float local = (t - 0.58f) / 0.24f;
            position = Vector3.Lerp(frontHero, impact, Mathf.SmoothStep(0f, 1f, local));
            lookAt = targetPosition + tankUp * 0.8f;
        }
        else
        {
            float local = (t - 0.82f) / 0.18f;
            position = Vector3.Lerp(impact, frontClose, Mathf.SmoothStep(0f, 1f, local));
            lookAt = lookFocus;
        }

        _cutsceneCamera.transform.position = position;
        _cutsceneCamera.transform.rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
    }

    private void AnimateTurret(float t)
    {
        if (_turretPivot == null || _barrelPivot == null || _target == null)
        {
            return;
        }

        Vector3 toTarget = _target.position - _turretPivot.position;
        if (toTarget.sqrMagnitude < 0.001f)
        {
            return;
        }

        // Aim the dedicated gun rig at the target in tank-local space.
        Vector3 localAim = _tankRoot.InverseTransformDirection(toTarget.normalized);
        float yaw = Mathf.Atan2(localAim.x, localAim.z) * Mathf.Rad2Deg;
        float planar = Mathf.Sqrt(localAim.x * localAim.x + localAim.z * localAim.z);
        float pitch = -Mathf.Atan2(localAim.y, Mathf.Max(0.01f, planar)) * Mathf.Rad2Deg;

        float aimBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.5f));
        float idleYaw = Mathf.Lerp(-6f, 4f, t);
        float idlePitch = Mathf.Lerp(2f, 8f, Mathf.Clamp01(t / 0.55f));

        _turretPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(idleYaw, yaw, aimBlend), 0f);
        _barrelPivot.localRotation = Quaternion.Euler(Mathf.Lerp(idlePitch, pitch, aimBlend), 0f, 0f);
    }

    private void TryFire(float t)
    {
        if (_firePoint == null)
        {
            return;
        }

        if (!_firedPrimary && t >= 0.56f && t <= 0.58f)
        {
            SpawnShell();
            _firedPrimary = true;
        }

        if (!_firedSecondary && t >= 0.72f && t <= 0.74f)
        {
            SpawnShell();
            _firedSecondary = true;
        }
    }

    private void SpawnShell()
    {
        if (_activeShell != null)
        {
            Destroy(_activeShell);
        }

        // Fire along the barrel muzzle forward so the shell leaves the gun tip.
        Vector3 muzzlePosition = _firePoint.position;
        Vector3 aimDirection = _firePoint.forward;
        if (_target != null)
        {
            Vector3 toTarget = (_target.position - muzzlePosition).normalized;
            aimDirection = Vector3.Slerp(aimDirection, toTarget, 0.35f).normalized;
        }

        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "CutsceneShell";
        shell.transform.SetParent(_cutsceneRoot, false);
        shell.transform.position = muzzlePosition + aimDirection * 0.5f;
        shell.transform.localScale = Vector3.one * 0.55f;
        ApplyColor(shell, new Color(0.22f, 0.2f, 0.18f, 1f));
        DisableCollider(shell);

        Rigidbody body = shell.AddComponent<Rigidbody>();
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = aimDirection * 82f + Vector3.up * 8f;
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

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        material.color = color;
        renderer.sharedMaterial = material;
    }
}
