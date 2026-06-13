using System.Linq;
using UnityEngine;

public static class ModelImportValidation
{
    private static readonly string[] RequiredTankParts =
    {
        "Hull",
        "Turret",
        "Barrel",
        "LeftWheel_0",
        "LeftWheel_1",
        "LeftWheel_2",
        "LeftWheel_3",
        "RightWheel_0",
        "RightWheel_1",
        "RightWheel_2",
        "RightWheel_3"
    };

    public static bool HasVisibleRendererBounds(GameObject model)
    {
        if (model == null)
        {
            return false;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        return renderers.Length > 0 && renderers.Any(renderer => renderer != null && renderer.bounds.size.sqrMagnitude > 0.0001f);
    }

    public static bool HasRequiredTankParts(GameObject model)
    {
        if (model == null)
        {
            return false;
        }

        Transform[] transforms = model.GetComponentsInChildren<Transform>(true);
        return RequiredTankParts.All(required => transforms.Any(item => item.name == required));
    }
}
