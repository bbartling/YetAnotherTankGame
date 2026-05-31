using UnityEngine;

[DisallowMultipleComponent]
public class DestructibleGround : MonoBehaviour
{
    private CraterTerrain _craterTerrain;

    private void Awake()
    {
        _craterTerrain = GetComponent<CraterTerrain>();
    }

    public void ApplyImpact(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        if (_craterTerrain == null)
        {
            _craterTerrain = GetComponent<CraterTerrain>();
        }

        if (_craterTerrain != null)
        {
            _craterTerrain.ApplyImpact(worldPoint, worldNormal, force);
        }
    }
}
