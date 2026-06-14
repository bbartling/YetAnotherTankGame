using UnityEngine;

[DisallowMultipleComponent]
public class SillyModelInstaller : MonoBehaviour
{
    public string resourcePath;
    public float visualScale = 1f;
    public bool hideLegacyRenderers = true;

    public GameObject InstalledVisual { get; private set; }

    public static SillyModelInstaller Ensure(GameObject root, string requiredResourcePath, float scale, bool hideLegacy)
    {
        SillyModelInstaller installer = root.GetComponent<SillyModelInstaller>();
        if (installer == null)
        {
            installer = root.AddComponent<SillyModelInstaller>();
        }

        installer.resourcePath = requiredResourcePath;
        installer.visualScale = scale;
        installer.hideLegacyRenderers = hideLegacy;
        installer.Install();
        return installer;
    }

    public void Install()
    {
        if (InstalledVisual != null)
        {
            return;
        }

        GameObject model = Resources.Load<GameObject>(resourcePath);
        if (model == null)
        {
            Debug.LogError($"[SillyModelInstaller] Required model missing at Resources/{resourcePath} for {name}.");
            return;
        }

        if (hideLegacyRenderers)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer is LineRenderer || renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                renderer.enabled = false;
            }
        }

        InstalledVisual = Instantiate(model, transform);
        InstalledVisual.name = "SillyModelVisual";
        InstalledVisual.transform.localPosition = Vector3.zero;
        InstalledVisual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        InstalledVisual.transform.localScale = Vector3.one * visualScale;
        ConfigureInitialVariants();
        BindTankVisualAnimator();
    }

    public void ReplaceResource(string requiredResourcePath, float scale)
    {
        resourcePath = requiredResourcePath;
        visualScale = scale;
        if (InstalledVisual != null)
        {
            if (Application.isPlaying)
            {
                Destroy(InstalledVisual);
            }
            else
            {
                DestroyImmediate(InstalledVisual);
            }
            InstalledVisual = null;
        }

        Install();
    }

    private void ConfigureInitialVariants()
    {
        Transform[] parts = InstalledVisual.GetComponentsInChildren<Transform>(true);
        foreach (Transform part in parts)
        {
            string partName = part.name;
            bool isDamageVariant = partName.Contains("Damaged") ||
                                   partName.Contains("Cracked") ||
                                   partName.Contains("Collapsed") ||
                                   partName.Contains("Fallen") ||
                                   partName == "Stump";
            if (isDamageVariant)
            {
                part.gameObject.SetActive(false);
            }
        }
    }

    public void ApplyDamageState(DamageStateController.State state)
    {
        if (InstalledVisual == null)
        {
            return;
        }

        bool damaged = state == DamageStateController.State.BurningDisabled || state == DamageStateController.State.Wrecked;
        Transform[] parts = InstalledVisual.GetComponentsInChildren<Transform>(true);
        foreach (Transform part in parts)
        {
            if (part.name.Contains("Damaged"))
            {
                part.gameObject.SetActive(damaged);
            }
            else if (part.name == "Hull" || part.name == "Turret" || part.name == "Barrel" ||
                     part.name == "LeftTrack" || part.name == "RightTrack")
            {
                part.gameObject.SetActive(!damaged);
            }
        }
    }

    private void BindTankVisualAnimator()
    {
        Transform barrel = InstalledVisual.transform.Find("Barrel");
        Transform antenna = InstalledVisual.transform.Find("Antenna");
        if (barrel == null && antenna == null)
        {
            return;
        }

        TankVisualAnimator animator = GetComponent<TankVisualAnimator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<TankVisualAnimator>();
        }
        animator.Bind(barrel, antenna);
        animator.leftTrack = InstalledVisual.transform.Find("LeftTrack");
        animator.rightTrack = InstalledVisual.transform.Find("RightTrack");
        animator.wheels = new Transform[8];
        for (int i = 0; i < 4; i++)
        {
            animator.wheels[i] = InstalledVisual.transform.Find($"LeftWheel_{i}");
            animator.wheels[i + 4] = InstalledVisual.transform.Find($"RightWheel_{i}");
        }

        BindVisualPivot(InstalledVisual.transform.Find("Turret"), GetComponent<TankController>()?.turretYawPivot);
        BindVisualPivot(InstalledVisual.transform.Find("Turret_Damaged"), GetComponent<TankController>()?.turretYawPivot);
        BindVisualPivot(InstalledVisual.transform.Find("GunMantlet"), GetComponent<TankController>()?.turretYawPivot);
        BindVisualPivot(InstalledVisual.transform.Find("CommanderCupola"), GetComponent<TankController>()?.turretYawPivot);
        BindVisualPivot(InstalledVisual.transform.Find("CommanderHelmet"), GetComponent<TankController>()?.turretYawPivot);
        BindVisualPivot(InstalledVisual.transform.Find("Hatch"), GetComponent<TankController>()?.turretYawPivot);
        BindVisualPivot(InstalledVisual.transform.Find("Antenna"), GetComponent<TankController>()?.turretYawPivot);
        BindVisualPivot(InstalledVisual.transform.Find("Barrel"), GetComponent<TankController>()?.barrelPitchPivot);
        BindVisualPivot(InstalledVisual.transform.Find("Barrel_Damaged"), GetComponent<TankController>()?.barrelPitchPivot);
    }

    private static void BindVisualPivot(Transform visual, Transform pivot)
    {
        if (visual == null || pivot == null) return;
        VisualPivotFollower follower = visual.GetComponent<VisualPivotFollower>();
        if (follower == null) follower = visual.gameObject.AddComponent<VisualPivotFollower>();
        follower.Bind(pivot);
    }
}
