using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class PracticeFinishLine : MonoBehaviour
{
    public bool logCrossings = true;
    public string finishMessage = "FINISH_LINE_CROSSED";

    private bool _crossed;

    public bool HasCrossed => _crossed;

    public static bool CourseCompleted { get; private set; }

    public static void ResetCourseCompletion()
    {
        CourseCompleted = false;
    }

    private void Start()
    {
        CheckerFlagVisual.EnsureAtFinish(this);
    }

    private void Reset()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_crossed)
        {
            return;
        }

        TankController tank = other.GetComponentInParent<TankController>();
        if (tank == null)
        {
            return;
        }

        _crossed = true;
        CourseCompleted = true;
        if (logCrossings)
        {
            Debug.Log(finishMessage);
        }
    }

    public void ResetCrossing()
    {
        _crossed = false;
    }
}
