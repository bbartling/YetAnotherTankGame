using UnityEngine;
using System.Collections; // Required for Coroutines

[RequireComponent(typeof(AudioSource))] // Ensures this object always has an AudioSource
public class ProjectileCameraController : MonoBehaviour
{
    // These will be set by the CannonManager when the ball is fired
    public Camera mainCamera;
    public float launchElevation;

    [Header("Sound Effects")]
    public AudioClip flyingShellSound;
    public AudioClip explosionSound;

    [Header("Cinematic Settings")]
    [Tooltip("How long the camera stays on the explosion before returning to the cannon.")]
    public float explosionLingerTime = 2.0f; // Public variable for the delay

    private Camera projectileCamera;
    private AudioSource audioSource;
    private bool isDestroying = false;
    private const float selfDestructTime = 20f;
    private AudioListener mainCameraListener; // To fix the warnings

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Find and disable the main camera's listener to prevent warnings
        if (mainCamera != null)
        {
            mainCameraListener = mainCamera.GetComponent<AudioListener>();
            if (mainCameraListener != null)
            {
                mainCameraListener.enabled = false;
            }
        }

        if (flyingShellSound != null)
        {
            audioSource.loop = false;
            audioSource.clip = flyingShellSound;
            audioSource.Play();
        }

        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }

        projectileCamera = gameObject.AddComponent<Camera>();
        projectileCamera.fieldOfView = 75;
        gameObject.AddComponent<AudioListener>();

        float currentYaw = transform.eulerAngles.y;
        float newPitch = -(launchElevation - 25f);
        transform.rotation = Quaternion.Euler(newPitch, currentYaw, 0);

        // Use a wrapper to call the coroutine after the timeout
        Invoke("SelfDestruct", selfDestructTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // When we hit something, start the explosion sequence.
        if (!isDestroying)
        {
            StartCoroutine(ExplosionSequence());
        }
    }

    // Wrapper function for Invoke
    void SelfDestruct()
    {
        if (!isDestroying)
        {
            StartCoroutine(ExplosionSequence());
        }
    }

    IEnumerator ExplosionSequence()
    {
        isDestroying = true;
        CancelInvoke("SelfDestruct"); // Stop the self-destruct timer if we hit something first

        // Stop the flying sound and physics
        audioSource.Stop();
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Hide the projectile so it looks like it exploded
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Play the explosion sound at the point of impact
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // --- THE DELAY ---
        // Wait for the specified linger time
        yield return new WaitForSeconds(explosionLingerTime);

        // --- AFTER THE DELAY ---
        // Switch back to the main camera
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            // Re-enable the main camera's listener
            if (mainCameraListener != null)
            {
                mainCameraListener.enabled = true;
            }
        }

        // Finally, destroy this cannonball GameObject
        Destroy(gameObject);
    }
}

