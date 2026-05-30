using UnityEngine;

/// <summary>
/// Simple script to make a placeholder plane fly across the scene and clean itself up afterwards.
/// Attach this to a GameObject to make it move continuously forward and destroy itself after a set time.
/// This adds a bit of spectacle for the air drop effect.
/// </summary>
public class PlaneFlyby : MonoBehaviour
{
    // Speed at which the plane moves forward.
    public float speed = 50f;

    // Time in seconds before the plane is destroyed to avoid clutter.
    public float lifeTime = 30f;

    private void Start()
    {
        // Schedule self‑destruction
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Move the plane forward each frame in local space (its facing direction)
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
    }
}