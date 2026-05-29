using UnityEngine;

public class EnemyTankAI : MonoBehaviour
{
    public Transform turret;
    public Transform barrel;
    public Transform firePoint;
    public GameObject shellPrefab;
    
    public float detectRange = 100f;
    public float fireRate = 3f;
    public float shellPower = 30f;
    public AudioClip fireSound;

    private Transform _player;
    private float _nextFireTime;
    private AudioSource _audio;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        
        GameObject p = GameObject.Find("PlayerTank");
        if (p != null) _player = p.transform;
    }

    void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist < detectRange)
        {
            AimAtPlayer();
            
            if (Time.time > _nextFireTime)
            {
                Fire();
                _nextFireTime = Time.time + fireRate;
            }
        }
    }

    void AimAtPlayer()
    {
        if (turret != null)
        {
            Vector3 direction = _player.position - turret.position;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                turret.rotation = Quaternion.Slerp(turret.rotation, targetRot, Time.deltaTime * 2f);
            }
        }

        if (barrel != null)
        {
            // Simple elevation (look slightly up)
            barrel.localRotation = Quaternion.Euler(-15, 0, 0);
        }
    }

    void Fire()
    {
        if (fireSound != null) _audio.PlayOneShot(fireSound);

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        
        var pcc = shell.GetComponent<ProjectileCameraController>();
        if (pcc != null)
        {
            pcc.enableCameraSwitching = false; 
        }

        var rb = shell.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(firePoint.forward * shellPower, ForceMode.Impulse);
        }
    }
}
