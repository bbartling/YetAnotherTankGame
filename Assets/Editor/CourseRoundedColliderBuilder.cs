#if UNITY_EDITOR
using UnityEngine;

public static class CourseRoundedColliderBuilder
{
    public static void ApplyRoundedCollider(GameObject obstacle, float edgeRadiusNormalized = 0.1f)
    {
        if (obstacle == null)
        {
            return;
        }

        Collider existing = obstacle.GetComponent<Collider>();
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        float radius = Mathf.Clamp(edgeRadiusNormalized, 0.05f, 0.2f);
        BuildCompoundRoundedBox(obstacle.transform, radius);
    }

    private static void BuildCompoundRoundedBox(Transform parent, float radius)
    {
        float half = 0.5f;
        float r = Mathf.Min(radius, half * 0.4f);
        float inner = half - r;

        if (inner > 0.01f)
        {
            CreateBoxCollider(parent, "RoundedCore", Vector3.zero, new Vector3(inner * 2f, inner * 2f, inner * 2f));
        }

        CreateBoxCollider(parent, "RoundedFaceXPos", new Vector3(half - r * 0.5f, 0f, 0f), new Vector3(r, inner * 2f, inner * 2f));
        CreateBoxCollider(parent, "RoundedFaceXNeg", new Vector3(-half + r * 0.5f, 0f, 0f), new Vector3(r, inner * 2f, inner * 2f));
        CreateBoxCollider(parent, "RoundedFaceYPos", new Vector3(0f, half - r * 0.5f, 0f), new Vector3(inner * 2f, r, inner * 2f));
        CreateBoxCollider(parent, "RoundedFaceYNeg", new Vector3(0f, -half + r * 0.5f, 0f), new Vector3(inner * 2f, r, inner * 2f));
        CreateBoxCollider(parent, "RoundedFaceZPos", new Vector3(0f, 0f, half - r * 0.5f), new Vector3(inner * 2f, inner * 2f, r));
        CreateBoxCollider(parent, "RoundedFaceZNeg", new Vector3(0f, 0f, -half + r * 0.5f), new Vector3(inner * 2f, inner * 2f, r));

        CreateCapsuleCollider(parent, "RoundedEdgeXFrontTop", new Vector3(half - r, half - r, half - r), r, inner * 2f, 2);
        CreateCapsuleCollider(parent, "RoundedEdgeXFrontBot", new Vector3(half - r, -half + r, half - r), r, inner * 2f, 2);
        CreateCapsuleCollider(parent, "RoundedEdgeXBackTop", new Vector3(half - r, half - r, -half + r), r, inner * 2f, 2);
        CreateCapsuleCollider(parent, "RoundedEdgeXBackBot", new Vector3(half - r, -half + r, -half + r), r, inner * 2f, 2);
        CreateCapsuleCollider(parent, "RoundedEdgeXFrontTopNeg", new Vector3(-half + r, half - r, half - r), r, inner * 2f, 2);
        CreateCapsuleCollider(parent, "RoundedEdgeXFrontBotNeg", new Vector3(-half + r, -half + r, half - r), r, inner * 2f, 2);
        CreateCapsuleCollider(parent, "RoundedEdgeXBackTopNeg", new Vector3(-half + r, half - r, -half + r), r, inner * 2f, 2);
        CreateCapsuleCollider(parent, "RoundedEdgeXBackBotNeg", new Vector3(-half + r, -half + r, -half + r), r, inner * 2f, 2);

        CreateCapsuleCollider(parent, "RoundedEdgeZRightTop", new Vector3(half - r, half - r, 0f), r, inner * 2f, 1);
        CreateCapsuleCollider(parent, "RoundedEdgeZRightBot", new Vector3(half - r, -half + r, 0f), r, inner * 2f, 1);
        CreateCapsuleCollider(parent, "RoundedEdgeZLeftTop", new Vector3(-half + r, half - r, 0f), r, inner * 2f, 1);
        CreateCapsuleCollider(parent, "RoundedEdgeZLeftBot", new Vector3(-half + r, -half + r, 0f), r, inner * 2f, 1);

        CreateCapsuleCollider(parent, "RoundedEdgeYFrontTop", new Vector3(0f, half - r, half - r), r, inner * 2f, 0);
        CreateCapsuleCollider(parent, "RoundedEdgeYFrontBot", new Vector3(0f, -half + r, half - r), r, inner * 2f, 0);
        CreateCapsuleCollider(parent, "RoundedEdgeYBackTop", new Vector3(0f, half - r, -half + r), r, inner * 2f, 0);
        CreateCapsuleCollider(parent, "RoundedEdgeYBackBot", new Vector3(0f, -half + r, -half + r), r, inner * 2f, 0);

        CreateSphereCollider(parent, "RoundedCornerPPP", new Vector3(half - r, half - r, half - r), r);
        CreateSphereCollider(parent, "RoundedCornerPPN", new Vector3(half - r, half - r, -half + r), r);
        CreateSphereCollider(parent, "RoundedCornerPNP", new Vector3(half - r, -half + r, half - r), r);
        CreateSphereCollider(parent, "RoundedCornerPNN", new Vector3(half - r, -half + r, -half + r), r);
        CreateSphereCollider(parent, "RoundedCornerNPP", new Vector3(-half + r, half - r, half - r), r);
        CreateSphereCollider(parent, "RoundedCornerNPN", new Vector3(-half + r, half - r, -half + r), r);
        CreateSphereCollider(parent, "RoundedCornerNNP", new Vector3(-half + r, -half + r, half - r), r);
        CreateSphereCollider(parent, "RoundedCornerNNN", new Vector3(-half + r, -half + r, -half + r), r);
    }

    private static void CreateBoxCollider(Transform parent, string name, Vector3 localCenter, Vector3 localSize)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localCenter;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        BoxCollider collider = child.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = localSize;
    }

    private static void CreateSphereCollider(Transform parent, string name, Vector3 localCenter, float radius)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localCenter;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        SphereCollider collider = child.AddComponent<SphereCollider>();
        collider.center = Vector3.zero;
        collider.radius = radius;
    }

    private static void CreateCapsuleCollider(Transform parent, string name, Vector3 localCenter, float radius, float height, int direction)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localCenter;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        CapsuleCollider collider = child.AddComponent<CapsuleCollider>();
        collider.center = Vector3.zero;
        collider.radius = radius;
        collider.height = Mathf.Max(height, radius * 2f);
        collider.direction = direction;
    }
}
#endif
