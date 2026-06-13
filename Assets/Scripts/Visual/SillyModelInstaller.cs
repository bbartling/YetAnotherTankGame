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
}
