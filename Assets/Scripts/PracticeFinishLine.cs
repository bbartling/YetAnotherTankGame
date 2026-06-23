using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class PracticeFinishLine : MonoBehaviour
{
    public bool logCrossings = true;
    public string finishMessage = "FINISH_LINE_CROSSED";

    private bool _crossed;

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