using UnityEngine;

[DisallowMultipleComponent]
public class DrivingPracticeProbe : MonoBehaviour
{
    public TankController playerTank;
    public float fallThroughY = -8f;
    public bool logStatus = true;

    private float _lastLogTime;
    private bool _reportedFall;

    private void Update()
    {
        if (playerTank == null)
        {
            playerTank = Object.FindAnyObjectByType<TankController>();
        }

        if (playerTank == null)
        {
            return;
        }

        float y = playerTank.transform.position.y;
        if (!_reportedFall && y < fallThroughY)
        {
            _reportedFall = true;
            Debug.LogError("[DrivingPracticeProbe] FALL_THROUGH_MAP playerY=" + y.ToString("0.00"));
        }

        if (!logStatus || Time.time - _lastLogTime < 2f)
        {
            return;
        }

        _lastLogTime = Time.time;
        Debug.Log("[DrivingPracticeProbe] y=" + y.ToString("0.00")
            + " speed=" + playerTank.CurrentGroundSpeed.ToString("0.00")
            + " grounded=" + playerTank.IsGrounded
            + " wheels=" + playerTank.GroundedWheelCount);
    }
}
