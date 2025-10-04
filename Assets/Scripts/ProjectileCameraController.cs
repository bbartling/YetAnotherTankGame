using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ProjectileCameraController : MonoBehaviour
{
    public enum OwnerType { Player, Enemy }

    [Header("Ownership")]
    public OwnerType owner = OwnerType.Player;
    public Camera mainCamera;

    [Header("Audio")]
    public AudioClip flyingShellSound;
    public AudioClip enemyFlyingShellSound;
    public AudioClip explosionSound;

    [Header("Cinematics (Player Only)")]
    public float explosionLingerTime = 2.0f;
    public float cameraPitchOffset = 15.0f; // ADDED: Adjustable downward angle

    [Header("Explosion")]
    public GameObject explosionVFX;
    public float explosionRadius = 10f;
    public float explosionForce = .7f;
    public float upwardsModifier = 1.0f;

    private Camera _projCam;
    private AudioSource _audio;
    private bool _isDestroying = false;
    private const float SELF_DESTRUCT_S = 8f;

    private AudioListener _mainCamListener;
    private Camera _mainCamComponent;

    void Start()
    {
        _audio = GetComponent<AudioSource>();

        if (owner == OwnerType.Player)
        {
            if (mainCamera != null)
            {
                _mainCamListener = mainCamera.GetComponent<AudioListener>();
                if (_mainCamListener != null) _mainCamListener.enabled = false;

                _mainCamComponent = mainCamera.GetComponent<Camera>();
                if (_mainCamComponent != null) _mainCamComponent.enabled = false;
            }

            if (flyingShellSound != null)
            {
                _audio.clip = flyingShellSound;
                _audio.Play();
            }

            _projCam = gameObject.AddComponent<Camera>();
            if (_mainCamComponent != null) _projCam.rect = _mainCamComponent.rect;

            // ADDED: This line tilts the camera downwards
            transform.Rotate(cameraPitchOffset, 0, 0, Space.Self);

            gameObject.AddComponent<AudioListener>();
        }
        else
        {
            if (enemyFlyingShellSound != null)
            {
                _audio.clip = enemyFlyingShellSound;
                _audio.Play();
            }
        }

        Invoke(nameof(SelfDestruct), SELF_DESTRUCT_S);
    }

    // ... The rest of the script is unchanged ...
    void OnCollisionEnter(Collision collision)
    {
        if (!_isDestroying)
        {
            if (collision.gameObject.CompareTag("Enemy"))
                Destroy(collision.gameObject);

            StartCoroutine(ExplosionSequence());
        }
    }

    void SelfDestruct()
    {
        if (!_isDestroying) StartCoroutine(ExplosionSequence());
    }

    private IEnumerator ExplosionSequence()
    {
        _isDestroying = true;
        CancelInvoke(nameof(SelfDestruct));

        if (_audio != null) _audio.Stop();

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var h in hits)
        {
            var hrb = h.attachedRigidbody;
            if (hrb != null)
                hrb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
        }

        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (explosionSound != null && _audio != null && _audio.enabled)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        if (owner == OwnerType.Player)
            yield return new WaitForSeconds(explosionLingerTime);

        if (owner == OwnerType.Player)
        {
            if (_mainCamComponent != null) _mainCamComponent.enabled = true;
            if (_mainCamListener != null) _mainCamListener.enabled = true;
        }

        Destroy(gameObject);
    }
}