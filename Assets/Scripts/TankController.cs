using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    [Header("Required Tank References")]
    [Tooltip("Best setup: an EMPTY child object located at the exact center of the turret ring on top of the tank.")]
    public Transform turretYawPivot;

    [Tooltip("Best setup: an EMPTY child object located at the barrel hinge/trunnion.")]
    public Transform barrelPitchPivot;

    [Tooltip("Muzzle point for the cannon. Its blue Z/forward arrow should point out of the barrel.")]
    public Transform cannonFirePoint;

    [Tooltip("Optional separate muzzle point for the machine gun. If empty, the cannon fire point is used.")]
    public Transform machineGunFirePoint;

    [Tooltip("Optional camera used by projectile camera switching.")]
    public Camera gameplayCamera;

    [Header("Legacy References Kept So Existing Scene Assignments Still Work")]
    public Transform turret;
    public Transform barrel;
    public Transform firePoint;
    public LineRenderer lineRenderer;

    [Header("Track Drive / WASD")]
    public float forwardAcceleration = 32f;
    public float reverseAcceleration = 20f;
    public float maxForwardSpeed = 12f;
    public float maxReverseSpeed = 7f;
    public float turnAcceleration = 85f;
    public float pivotTurnAcceleration = 125f;
    public float lateralGrip = 8f;
    public float groundCheckDistance = 1.4f;
    public LayerMask groundMask = ~0;

    [Header("Tank Stability")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.55f, 0f);
    public float uprightAssist = 12f;
    public float angularDamping = 3f;
    public float linearDamping = 0.65f;

    [Header("Mouse Turret")]
    [Tooltip("Mouse X yaws the turret left/right. This is intentionally local to turretYawPivot so the turret does not orbit the map.")]
    public float mouseYawDegreesPerSecond = 210f;
    public float turretYawSpeed = 720f;

    [Tooltip("Mouse wheel plus PageUp/PageDown pitch the barrel.")]
    public float barrelPitchSpeed = 55f;
    public float mouseWheelPitchSensitivity = 18f;
    public float keyboardPitchSpeed = 35f;
    public float minBarrelElevation = -8f;
    public float maxBarrelElevation = 35f;

    [Header("Cannon")]
    public GameObject shellPrefab;
    public float maxPower = 75f;
    [Range(0f, 100f)] public float powerPercentage = 65f;
    public float cannonCooldown = 0.45f;
    public KeyCode cannonKey = KeyCode.Space;
    public bool leftClickFiresCannon = true;
    public AudioClip playerFireSound;

    [Header("Optional UI")]
    public TextMeshProUGUI elevationText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI powerText;

    [Header("Health / Damage API")]
    public float maxHealth = 100f;
    public float projectileDirectDamage = 55f;
    public float projectileBlastDamage = 42f;
    public float tankCollisionDamage = 24f;
    public float fatalImpactThreshold = 0.55f;

    private Rigidbody _rb;
    private AudioSource _audio;
    private float _turretYaw;
    private float _barrelElevation;
    private float _nextCannonTime;
    private float _health;
    private bool _dead;

    public Rigidbody RigidbodyComponent => _rb;
    public Vector3 CurrentVelocity => GetVelocity();
    public float TurretYawDegrees => Mathf.Repeat(_turretYaw, 360f);
    public bool IsDestroyed => _dead;
    public float CurrentHealth => Mathf.Max(0f, _health);
    public float HealthPercent => maxHealth > 0f ? Mathf.Clamp01(_health / maxHealth) * 100f : 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();
        _health = maxHealth;

        if (_rb != null)
        {
            _rb.centerOfMass += centerOfMassOffset;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.angularDamping = angularDamping;
            _rb.linearDamping = linearDamping;
            _rb.maxAngularVelocity = 12f;
        }

        AutoWireReferences();
        CacheStartingAngles();
        ValidatePivotSetup();
    }

    private void AutoWireReferences()
    {
        if (turretYawPivot == null)
        {
            Transform found = transform.Find("TurretYawPivot");
            if (found != null) turretYawPivot = found;
        }

        if (barrelPitchPivot == null && turretYawPivot != null)
        {
            Transform found = turretYawPivot.Find("BarrelPitchPivot");
            if (found == null) found = turretYawPivot.Find("BarrelPivot");
            if (found != null) barrelPitchPivot = found;
        }

        if (cannonFirePoint == null && barrelPitchPivot != null)
        {
            Transform found = barrelPitchPivot.Find("FirePoint");
            if (found == null) found = barrelPitchPivot.Find("CannonFirePoint");
            if (found != null) cannonFirePoint = found;
        }

        // Legacy fallback from your older script fields.
        if (turretYawPivot == null && turret != null) turretYawPivot = turret;
        if (barrelPitchPivot == null && barrel != null) barrelPitchPivot = barrel;
        if (cannonFirePoint == null && firePoint != null) cannonFirePoint = firePoint;
        if (machineGunFirePoint == null) machineGunFirePoint = cannonFirePoint;

        if (gameplayCamera == null)
        {
            gameplayCamera = GetComponentInChildren<Camera>(true);
            if (gameplayCamera == null) gameplayCamera = Camera.main;
        }

        if (firePoint == null) firePoint = cannonFirePoint;
        if (turret == null) turret = turretYawPivot;
        if (barrel == null) barrel = barrelPitchPivot;
    }

    private void CacheStartingAngles()
    {
        if (turretYawPivot != null)
        {
            _turretYaw = NormalizeSignedAngle(turretYawPivot.localEulerAngles.y);
        }

        if (barrelPitchPivot != null)
        {
            _barrelElevation = -NormalizeSignedAngle(barrelPitchPivot.localEulerAngles.x);
            _barrelElevation = Mathf.Clamp(_barrelElevation, minBarrelElevation, maxBarrelElevation);
        }
    }

    private void ValidatePivotSetup()
    {
        if (turretYawPivot == null)
        {
            Debug.LogError("[TankController] Missing turretYawPivot. Create an EMPTY child named TurretYawPivot on the tank roof and assign it.");
            return;
        }

        if (turret != null && turretYawPivot == turret)
        {
            Debug.LogWarning("[TankController] turretYawPivot is using the visible turret transform. If the turret orbits, create an EMPTY TurretYawPivot at the turret ring, parent the visible turret mesh under it, and assign that empty object instead.");
        }

        if (cannonFirePoint == null)
        {
            Debug.LogWarning("[TankController] Missing cannonFirePoint. Cannon will not fire until assigned.");
        }
    }

    private void Update()
    {
        if (_dead) return;

        HandleTurretAndBarrelInput();
        HandleCannonInput();
        UpdateOptionalUi();
    }

    private void FixedUpdate()
    {
        if (_dead || _rb == null) return;

        HandleTrackDrive();
        ApplyLateralGrip();
        ApplyUprightAssist();
        ClampForwardSpeed();
    }

    private void HandleTrackDrive()
    {
        float throttle = 0f;
        if (Input.GetKey(KeyCode.W)) throttle += 1f;
        if (Input.GetKey(KeyCode.S)) throttle -= 1f;

        float steer = 0f;
        if (Input.GetKey(KeyCode.D)) steer += 1f;
        if (Input.GetKey(KeyCode.A)) steer -= 1f;

        bool grounded = IsGrounded();

        if (grounded && Mathf.Abs(throttle) > 0.01f)
        {
            float accel = throttle > 0f ? forwardAcceleration : reverseAcceleration;
            _rb.AddForce(transform.forward * (throttle * accel), ForceMode.Acceleration);
        }

        if (grounded && Mathf.Abs(steer) > 0.01f)
        {
            // A/D alone pivots like tank tracks. A/D while moving gives a smoother moving turn.
            float torque = Mathf.Abs(throttle) > 0.01f ? turnAcceleration : pivotTurnAcceleration;
            _rb.AddTorque(Vector3.up * (steer * torque), ForceMode.Acceleration);
        }
    }

    private bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.35f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void ApplyLateralGrip()
    {
        Vector3 velocity = GetVelocity();
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        Vector3 lateralVelocity = transform.right * localVelocity.x;
        _rb.AddForce(-lateralVelocity * lateralGrip, ForceMode.Acceleration);
    }

    private void ApplyUprightAssist()
    {
        Vector3 tiltAxis = Vector3.Cross(transform.up, Vector3.up);
        if (tiltAxis.sqrMagnitude > 0.0001f)
        {
            _rb.AddTorque(tiltAxis * uprightAssist, ForceMode.Acceleration);
        }
    }

    private void ClampForwardSpeed()
    {
        Vector3 velocity = GetVelocity();
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);

        localVelocity.z = Mathf.Clamp(localVelocity.z, -maxReverseSpeed, maxForwardSpeed);

        // Do not hard clamp Y; gravity needs to work.
        Vector3 clamped = transform.TransformDirection(localVelocity);
        clamped.y = velocity.y;
        SetVelocity(clamped);
    }

    private void HandleTurretAndBarrelInput()
    {
        if (turretYawPivot != null)
        {
            float mouseX = Input.GetAxisRaw("Mouse X");
            _turretYaw += mouseX * mouseYawDegreesPerSecond * Time.deltaTime;

            Quaternion targetLocalYaw = Quaternion.Euler(0f, _turretYaw, 0f);
            turretYawPivot.localRotation = Quaternion.RotateTowards(
                turretYawPivot.localRotation,
                targetLocalYaw,
                turretYawSpeed * Time.deltaTime
            );
        }

        if (barrelPitchPivot != null)
        {
            float pitchInput = 0f;
            pitchInput += Input.GetAxisRaw("Mouse ScrollWheel") * mouseWheelPitchSensitivity;

            if (Input.GetKey(KeyCode.PageUp)) pitchInput += keyboardPitchSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.PageDown)) pitchInput -= keyboardPitchSpeed * Time.deltaTime;

            _barrelElevation = Mathf.Clamp(
                _barrelElevation + pitchInput,
                minBarrelElevation,
                maxBarrelElevation
            );

            barrelPitchPivot.localRotation = Quaternion.Euler(-_barrelElevation, 0f, 0f);
        }
    }

    private void HandleCannonInput()
    {
        bool pressedSpace = Input.GetKeyDown(cannonKey);
        bool pressedMouse = leftClickFiresCannon && Input.GetMouseButtonDown(0);
        if (!pressedSpace && !pressedMouse) return;
        if (Time.time < _nextCannonTime) return;

        // Your existing projectile camera script uses this static guard.
        if (ProjectileCameraController.ActivePlayerProjectile != null) return;

        FireCannon();
        _nextCannonTime = Time.time + cannonCooldown;
    }

    public void FireCannon()
    {
        if (shellPrefab == null || cannonFirePoint == null)
        {
            Debug.LogWarning("[TankController] Cannot fire cannon. shellPrefab or cannonFirePoint is missing.");
            return;
        }

        if (_audio != null && playerFireSound != null)
        {
            _audio.PlayOneShot(playerFireSound);
        }

        GameObject shell = Instantiate(shellPrefab, cannonFirePoint.position, cannonFirePoint.rotation);

        ProjectileCameraController projectileCamera = shell.GetComponent<ProjectileCameraController>();
        if (projectileCamera != null)
        {
            projectileCamera.trackingBase = transform;
            projectileCamera.launchPowerPercentage = powerPercentage;
        }

        Rigidbody shellBody = shell.GetComponent<Rigidbody>();
        if (shellBody != null)
        {
            shellBody.interpolation = RigidbodyInterpolation.Interpolate;
            shellBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            float launchSpeed = maxPower * Mathf.Clamp01(powerPercentage / 100f);
            Vector3 launchVelocity = cannonFirePoint.forward * launchSpeed + GetVelocity();
            SetVelocity(shellBody, launchVelocity);
        }
    }

    private void UpdateOptionalUi()
    {
        if (elevationText != null) elevationText.text = "Elev: " + _barrelElevation.ToString("F0") + "°";
        if (angleText != null) angleText.text = "Turret: " + TurretYawDegrees.ToString("F0") + "°";
        if (powerText != null) powerText.text = "Power: " + powerPercentage.ToString("F0") + "%";
    }

    public void ApplyBulletDamage(float damage, Vector3 worldPoint, Vector3 worldNormal)
    {
        ApplyDamage(Mathf.Max(0.05f, damage), worldPoint, worldNormal, false);
    }

    public void ApplyProjectileDamage(float impactForce, Vector3 worldPoint, Vector3 worldNormal)
    {
        float damage = Mathf.Max(projectileDirectDamage, impactForce);
        ApplyDamage(damage, worldPoint, worldNormal, true);
    }

    public void ApplyExplosionDamage(float explosionForce, Vector3 explosionPoint, Vector3 worldNormal, float distanceFactor = 1f)
    {
        float damage = Mathf.Max(projectileBlastDamage * Mathf.Clamp01(distanceFactor), explosionForce * Mathf.Clamp01(distanceFactor));
        ApplyDamage(damage, explosionPoint, worldNormal, true);
    }

    public void ApplyCollisionDamage(float impactForce, Vector3 worldPoint, Vector3 worldNormal)
    {
        ApplyDamage(Mathf.Max(tankCollisionDamage, impactForce), worldPoint, worldNormal, false);
    }

    private void ApplyDamage(float amount, Vector3 worldPoint, Vector3 worldNormal, bool fatalImpactAllowed)
    {
        if (_dead) return;

        _health -= Mathf.Max(0.01f, amount);

        if (fatalImpactAllowed && amount >= maxHealth * fatalImpactThreshold)
        {
            _health = 0f;
        }

        if (_health <= 0f)
        {
            Die(worldPoint, worldNormal, amount);
        }
    }

    private void Die(Vector3 worldPoint, Vector3 worldNormal, float force)
    {
        if (_dead) return;

        _dead = true;
        Debug.Log("[TankController] Player tank destroyed.");

        if (_rb != null)
        {
            _rb.AddExplosionForce(Mathf.Max(200f, force * 8f), worldPoint, 8f, 1f, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_dead || collision == null || collision.contactCount == 0) return;

        EnemyTankAI enemy = collision.collider.GetComponentInParent<EnemyTankAI>();
        if (enemy == null) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact > 4f)
        {
            ContactPoint contact = collision.GetContact(0);
            ApplyCollisionDamage(impact * 4f, contact.point, contact.normal);
        }
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }

    private Vector3 GetVelocity()
    {
        if (_rb == null) return Vector3.zero;
#if UNITY_6000_0_OR_NEWER
        return _rb.linearVelocity;
#else
        return _rb.velocity;
#endif
    }

    private void SetVelocity(Vector3 value)
    {
        if (_rb == null) return;
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = value;
#else
        _rb.velocity = value;
#endif
    }

    private static void SetVelocity(Rigidbody body, Vector3 value)
    {
        if (body == null) return;
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = value;
#else
        body.velocity = value;
#endif
    }
}
