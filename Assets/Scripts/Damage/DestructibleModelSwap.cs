using UnityEngine;

[DisallowMultipleComponent]
public class DestructibleModelSwap : MonoBehaviour
{
    public GameObject intactModel;
    public GameObject damagedModel;
    public GameObject destroyedModel;

    public void ApplyState(DamageStateController.State state)
    {
        SetActive(intactModel, state == DamageStateController.State.Intact || state == DamageStateController.State.Smoking);
        SetActive(damagedModel, state == DamageStateController.State.BurningDisabled);
        SetActive(destroyedModel, state == DamageStateController.State.Wrecked);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
