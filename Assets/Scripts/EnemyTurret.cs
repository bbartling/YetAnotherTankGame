using UnityEngine;

public class EnemyTurret : MonoBehaviour
{
    public Transform turretBase;
    public Transform barrel;
    public Transform firePoint;
    public GameObject shellPrefab;
    
    public float detectRange = 200f;
    public float fireRate = 3f;
    public float shellPower = 50f;
    public AudioClip fireSound;
    public float accuracy = 0.5f; // 1.0 is perfect, 0.0 is very bad

    private Transform _player;
    private float _nextFireTime;
    private AudioSource _audio;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        
        GameObject p = GameObject.Find("PlayerTank");
        if (p != null) _player = p.transform;
        
        gameObject.tag = "EnemyTurret";
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
        if (turretBase != null)
        {
            Vector3 direction = _player.position - turretBase.position;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                turretBase.rotation = Quaternion.Slerp(turretBase.rotation, targetRot, Time.deltaTime * (2f * accuracy));
            }
        }

        if (barrel != null)
        {
            // Simple elevation (look at player)
            Vector3 targetDir = _player.position - barrel.position;
            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            barrel.rotation = Quaternion.Slerp(barrel.rotation, targetRot, Time.deltaTime * (2f * accuracy));
        }
    }

    void Fire()
    {
        if (fireSound != null) _audio.PlayOneShot(fireSound);

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        
        var pcc = shell.GetComponent<ProjectileCameraController>();
        if (pcc != null) pcc.enableCameraSwitching = false; 

        var rb = shell.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Add some inaccuracy
            Vector3 forceDir = firePoint.forward + Random.insideUnitSphere * (1f - accuracy) * 0.1f;
            rb.AddForce(forceDir.normalized * shellPower, ForceMode.Impulse);
        }
    }
}
