using UnityEngine;

[DisallowMultipleComponent]
public class CheckerFlagVisual : MonoBehaviour
{
    public float poleHeight = 5.5f;
    public float flagWidth = 2.4f;
    public float flagHeight = 1.6f;
    public int checkerColumns = 4;
    public int checkerRows = 3;

    public static void EnsureAtFinish(PracticeFinishLine finishLine)
    {
        if (finishLine == null)
        {
            return;
        }

        Transform root = finishLine.transform;
        if (root.Find("CheckerFlag") != null)
        {
            return;
        }

        GameObject flagRoot = new GameObject("CheckerFlag");
        flagRoot.transform.SetParent(root, false);
        flagRoot.transform.localPosition = new Vector3(8f, 0f, 0f);
        CheckerFlagVisual visual = flagRoot.AddComponent<CheckerFlagVisual>();
        visual.Build();
    }

    public void Build()
    {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "FlagPole";
        pole.transform.SetParent(transform, false);
        pole.transform.localPosition = new Vector3(0f, poleHeight * 0.5f, 0f);
        pole.transform.localScale = new Vector3(0.18f, poleHeight * 0.5f, 0.18f);
        ApplyColor(pole, new Color(0.82f, 0.82f, 0.84f, 1f));

        GameObject flagPanel = new GameObject("FlagPanel");
        flagPanel.transform.SetParent(transform, false);
        flagPanel.transform.localPosition = new Vector3(flagWidth * 0.5f, poleHeight - flagHeight * 0.35f, 0f);

        float cellWidth = flagWidth / checkerColumns;
        float cellHeight = flagHeight / checkerRows;
        for (int row = 0; row < checkerRows; row++)
        {
            for (int col = 0; col < checkerColumns; col++)
            {
                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cell.name = "Checker_" + row + "_" + col;
                cell.transform.SetParent(flagPanel.transform, false);
                cell.transform.localPosition = new Vector3(
                    col * cellWidth - flagWidth * 0.5f + cellWidth * 0.5f,
                    -row * cellHeight + flagHeight * 0.5f - cellHeight * 0.5f,
                    0f);
                cell.transform.localScale = new Vector3(cellWidth * 0.96f, cellHeight * 0.96f, 0.08f);
                Collider cellCollider = cell.GetComponent<Collider>();
                if (cellCollider != null)
                {
                    Object.Destroy(cellCollider);
                }

                bool white = (row + col) % 2 == 0;
                ApplyColor(cell, white ? Color.white : Color.black);
            }
        }
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
