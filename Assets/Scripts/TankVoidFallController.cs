using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TankVoidFallController : MonoBehaviour
{
    public TankController tank;
    public float fallYThreshold = -12f;
    public bool useHorizontalBounds;
    public float minX = -46f;
    public float maxX = 46f;
    public float minZ = -70f;
    public float maxZ = 550f;
    public float boundsGraceSeconds = 0.35f;
    public float spawnGraceSeconds = 4f;
    public float fallScreamDelaySeconds = 1.1f;
    public float voidFallScreenDelaySeconds = 1.0f;
    public float voidFallReturnDelaySeconds = 4.4f;
    public float voidFallDownwardSpeed = 32f;
    public float voidFallSpinSpeed = 10f;
    public float voidCameraHeightAboveMap = 5.5f;
    public float voidCameraBackDistance = 16f;
    public AudioClip fallScreamClip;

    private float _outsideBoundsSeconds;
    private float _spawnGraceRemaining;
    private float _lastSafeMapY;
    private bool _triggered;

    public static TankVoidFallController Ensure(TankController targetTank, bool useRangeBounds)
    {
        if (targetTank == null)
        {
            return null;
        }

        TankVoidFallController existing = targetTank.GetComponent<TankVoidFallController>();
        if (existing != null)
        {
            existing.tank = targetTank;
            existing.useHorizontalBounds = useRangeBounds;
            return existing;
        }

        TankVoidFallController controller = targetTank.gameObject.AddComponent<TankVoidFallController>();
        controller.tank = targetTank;
        controller.useHorizontalBounds = useRangeBounds;
        controller.EnsureFallScreamClip();
        return controller;
    }

    private void Awake()
    {
        if (tank == null)
        {
            tank = GetComponent<TankController>();
        }

        _spawnGraceRemaining = spawnGraceSeconds;
        _lastSafeMapY = transform.position.y;
        EnsureFallScreamClip();
    }

    private void Update()
    {
        if (_triggered || tank == null || tank.IsDestroyed)
        {
            return;
        }

        if (_spawnGraceRemaining > 0f)
        {
            _spawnGraceRemaining -= Time.deltaTime;
            return;
        }

        Vector3 position = tank.transform.position;
        bool belowVoid = position.y <= fallYThreshold;
        bool outsideBounds = useHorizontalBounds && IsOutsideHorizontalBounds(position);

        if (!belowVoid && !outsideBounds)
        {
            _lastSafeMapY = position.y;
        }

        if (outsideBounds)
        {
            _outsideBoundsSeconds += Time.deltaTime;
        }
        else
        {
            _outsideBoundsSeconds = 0f;
        }

        if (!belowVoid && _outsideBoundsSeconds < boundsGraceSeconds)
        {
            return;
        }

        TriggerVoidFall();
    }

    private bool IsOutsideHorizontalBounds(Vector3 position)
    {
        return position.x < minX || position.x > maxX || position.z < minZ || position.z > maxZ;
    }

    private void TriggerVoidFall()
    {
        _triggered = true;
        PinVoidFallCamera();
        AccelerateVoidFall();
        StartCoroutine(PlayFallScreamAfterDelay());

        PracticeReturnController practiceReturn = Object.FindAnyObjectByType<PracticeReturnController>();
        if (practiceReturn != null)
        {
            StartCoroutine(TriggerPracticeReturnAfterDelay(practiceReturn));
            return;
        }

        if (BattlefieldDirector.Instance != null)
        {
            BattlefieldDirector.Instance.ForceDefeat("Tank fell off the map");
            return;
        }

        tank.enabled = false;
    }

    private IEnumerator TriggerPracticeReturnAfterDelay(PracticeReturnController practiceReturn)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, voidFallScreenDelaySeconds));
        if (practiceReturn != null)
        {
            practiceReturn.TriggerVoidFallReturn(voidFallReturnDelaySeconds);
        }
    }

    private IEnumerator PlayFallScreamAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, fallScreamDelaySeconds));
        PlayFallScream();
    }

    private void AccelerateVoidFall()
    {
        if (tank == null)
        {
            return;
        }

        Rigidbody body = tank.GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        body.maxAngularVelocity = Mathf.Max(body.maxAngularVelocity, voidFallSpinSpeed);
#if UNITY_6000_0_OR_NEWER
        Vector3 velocity = body.linearVelocity;
#else
        Vector3 velocity = body.velocity;
#endif
        velocity.y = Mathf.Min(velocity.y, -Mathf.Abs(voidFallDownwardSpeed));
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = velocity;
#else
        body.velocity = velocity;
#endif
        Vector3 spinAxis = Vector3.Cross(Vector3.up, tank.transform.forward);
        if (spinAxis.sqrMagnitude < 0.0001f)
        {
            spinAxis = Vector3.right;
        }

        body.angularVelocity = (spinAxis.normalized + Vector3.up * 0.45f).normalized * Mathf.Abs(voidFallSpinSpeed);
        body.WakeUp();
    }

    private void PinVoidFallCamera()
    {
        Camera camera = tank != null && tank.gameplayCamera != null ? tank.gameplayCamera : Camera.main;
        if (camera == null || tank == null)
        {
            return;
        }

        TankOrbitCamera orbitCamera = camera.GetComponent<TankOrbitCamera>();
        if (orbitCamera != null)
        {
            orbitCamera.enabled = false;
        }

        TankBarrelScopeCamera scopeCamera = camera.GetComponent<TankBarrelScopeCamera>();
        if (scopeCamera != null)
        {
            scopeCamera.allowScope = false;
        }

        Vector3 flatBack = Vector3.ProjectOnPlane(-tank.transform.forward, Vector3.up);
        if (flatBack.sqrMagnitude < 0.0001f)
        {
            flatBack = Vector3.ProjectOnPlane(-camera.transform.forward, Vector3.up);
        }

        if (flatBack.sqrMagnitude < 0.0001f)
        {
            flatBack = Vector3.back;
        }

        flatBack.Normalize();
        float mapY = ResolveMapHeight(tank.transform.position);
        Vector3 pinnedPosition = tank.transform.position + flatBack * voidCameraBackDistance;
        pinnedPosition.y = mapY + voidCameraHeightAboveMap;
        camera.transform.position = pinnedPosition;

        VoidFallPinnedCamera pinned = camera.GetComponent<VoidFallPinnedCamera>();
        if (pinned == null)
        {
            pinned = camera.gameObject.AddComponent<VoidFallPinnedCamera>();
        }

        pinned.target = tank.transform;
        pinned.pinnedPosition = pinnedPosition;
    }

    private float ResolveMapHeight(Vector3 nearPosition)
    {
        Vector3 origin = new Vector3(nearPosition.x, Mathf.Max(nearPosition.y + 200f, 200f), nearPosition.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider != null
            && !hit.collider.transform.IsChildOf(transform))
        {
            return hit.point.y;
        }

        return _lastSafeMapY;
    }

    private void PlayFallScream()
    {
        EnsureFallScreamClip();
        if (fallScreamClip == null)
        {
            return;
        }

        GameObject audioHost = new GameObject("TankVoidFallScream");
        AudioSource source = audioHost.AddComponent<AudioSource>();
        source.clip = fallScreamClip;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.Play();
        Object.Destroy(audioHost, fallScreamClip.length + 0.25f);
    }

    private void EnsureFallScreamClip()
    {
        if (fallScreamClip != null)
        {
            return;
        }

        fallScreamClip = Resources.Load<AudioClip>("Audio/HoneyFallScream");
#if UNITY_EDITOR
        if (fallScreamClip == null)
        {
            fallScreamClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/HoneyFallScream.wav");
        }
#endif
    }
}

public class VoidFallPinnedCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 pinnedPosition;

    private void LateUpdate()
    {
        ApplyNow();
    }

    public void ApplyNow()
    {
        transform.position = pinnedPosition;
        if (target == null)
        {
            return;
        }

        Vector3 lookVector = target.position - pinnedPosition;
        if (lookVector.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookVector.normalized, Vector3.up);
        }
    }
}
