using UnityEngine;

[DisallowMultipleComponent]
public class TankPlayableDebugGizmos : MonoBehaviour
{
    public TankController tank;
    public Camera aimCamera;
    public bool drawCannonForward = true;
    public bool drawCameraForward = true;
    public bool drawPivotMarkers = true;
    public bool drawSuspensionPoints = true;
    public float cannonRayLength = 8f;
    public float cameraRayLength = 5f;

    private void OnDrawGizmos()
    {
        if (tank == null)
        {
            tank = GetComponent<TankController>();
        }

        if (tank == null)
        {
            return;
        }

        if (drawPivotMarkers)
        {
            DrawMarker(tank.turretYawPivot, Color.cyan, 0.18f);
            DrawMarker(tank.barrelPitchPivot, Color.yellow, 0.14f);
        }

        if (drawCannonForward && tank.cannonFirePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(tank.cannonFirePoint.position, tank.cannonFirePoint.forward * Mathf.Max(0.1f, cannonRayLength));
        }

        if (drawCameraForward)
        {
            Camera camera = aimCamera != null ? aimCamera : tank.gameplayCamera;
            if (camera != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(camera.transform.position, camera.transform.forward * Mathf.Max(0.1f, cameraRayLength));
            }
        }

        if (drawSuspensionPoints)
        {
            WheeledSuspensionController suspension = GetComponent<WheeledSuspensionController>();
            if (suspension == null)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.85f);
            for (int i = 0; i < suspension.WheelCount; i++)
            {
                Vector3 mount = transform.TransformPoint(suspension.GetLocalWheelMount(i));
                Gizmos.DrawWireSphere(mount, Mathf.Max(0.05f, suspension.wheelRadius));
                Gizmos.DrawLine(mount + transform.up * suspension.probeHeight, mount - transform.up * suspension.suspensionTravel);
            }
        }
    }

    private static void DrawMarker(Transform target, Color color, float size)
    {
        if (target == null)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawWireSphere(target.position, size);
        Gizmos.DrawRay(target.position, target.forward * size * 2f);
    }
}
