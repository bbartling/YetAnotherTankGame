using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EffectPool : MonoBehaviour
{
    public int maxActiveEffects = 32;

    private readonly Queue<GameObject> _activeEffects = new Queue<GameObject>();
    public int ActiveCount => _activeEffects.Count;

    public void Register(GameObject effect)
    {
        if (effect == null)
        {
            return;
        }

        effect.SetActive(true);
        _activeEffects.Enqueue(effect);
        while (_activeEffects.Count > Mathf.Max(1, maxActiveEffects))
        {
            GameObject oldest = _activeEffects.Dequeue();
            if (oldest != null)
            {
                oldest.SetActive(false);
            }
        }
    }

    public void Clear()
    {
        while (_activeEffects.Count > 0)
        {
            GameObject effect = _activeEffects.Dequeue();
            if (effect != null)
            {
                effect.SetActive(false);
            }
        }
    }
}
