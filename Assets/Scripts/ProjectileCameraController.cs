using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ProjectileCameraController : MonoBehaviour
{
    // These will be set by the CannonManager when the ball is fired
    public Camera mainCamera;
    public float launchElevation;
    public CannonManager cannonManager;

    [Header("Sound Effects")]
    public AudioClip flyingShellSound;
    public AudioClip explosionSound;

    [Header("Cinematic Settings")]
    [Tooltip("How long the camera stays on the explosion before returning to the cannon.")]
    public float explosionLingerTime = 5.0f;

    [Header("Explosion Settings")]
    public GameObject explosionVFX; // The particle effect is still used!

    private Camera projectileCamera;
    private AudioSource audioSource;
    private bool isDestroying = false;
    private const float selfDestructTime = 20f;
    private AudioListener mainCameraListener;
    private Camera mainCameraComponent;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (mainCamera != null)
        {
            mainCameraListener = mainCamera.GetComponent<AudioListener>();
            if (mainCameraListener != null)
            {
                mainCameraListener.enabled = false;
            }

            mainCameraComponent = mainCamera.GetComponent<Camera>();
            if (mainCameraComponent != null)
            {
                mainCameraComponent.enabled = false;
            }
        }

        if (flyingShellSound != null)
        {
            audioSource.loop = false;
            audioSource.clip = flyingShellSound;
            audioSource.Play();
        }

        projectileCamera = gameObject.AddComponent<Camera>();
        projectileCamera.fieldOfView = 75;
        projectileCamera.rect = new Rect(0, 0, 0.5f, 1);
        gameObject.AddComponent<AudioListener>();

        float currentYaw = transform.eulerAngles.y;
        float newPitch = -(launchElevation - 25f);
        transform.rotation = Quaternion.Euler(newPitch, currentYaw, 0);

        Invoke("SelfDestruct", selfDestructTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // When we hit something, the physics engine will handle the impact.
        // We just need to start the visual/audio sequence.
        if (!isDestroying)
        {
            StartCoroutine(ExplosionSequence());
        }
    }

    void SelfDestruct()
    {
        if (!isDestroying)
        {
            StartCoroutine(ExplosionSequence());
        }
    }

    // --- THIS METHOD IS NOW MUCH SIMPLER ---
    IEnumerator ExplosionSequence()
    {
        isDestroying = true;
        CancelInvoke("SelfDestruct");

        // We stop the flying sound
        audioSource.Stop();

        // Play the explosion sound at the point of impact
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // Spawn the visual particle effect
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        // Immediately hide the cannonball so it looks like it exploded on impact
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        // Stop the cannonball from colliding with more things after the first hit
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }


        // Wait for the cinematic camera linger
        yield return new WaitForSeconds(explosionLingerTime);

        if (cannonManager != null)
        {
            cannonManager.ShowUI();
        }

        if (mainCameraComponent != null)
        {
            mainCameraComponent.enabled = true;
        }

        if (mainCameraListener != null)
        {
            mainCameraListener.enabled = true;
        }

        Destroy(gameObject);
    }
}