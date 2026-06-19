using UnityEngine;

public class ProjectileCameraDirector : MonoBehaviour
{
    public enum CameraMode
    {
        Tank,
        Projectile
    }

    public CameraMode CurrentMode => ProjectileCameraController.ActivePlayerProjectile != null
        ? CameraMode.Projectile
        : CameraMode.Tank;

    public void CancelProjectileCamera()
    {
        ProjectileCameraController.ActivePlayerProjectile?.CancelProjectileCamera();
    }
}
