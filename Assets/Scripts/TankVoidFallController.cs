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
    public AudioClip fallScreamClip;

    private float _outsideBoundsSeconds;
    private float _spawnGraceRemaining;
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
        PlayFallScream();

        PracticeReturnController practiceReturn = Object.FindAnyObjectByType<PracticeReturnController>();
        if (practiceReturn != null)
        {
            practiceReturn.TriggerVoidFallReturn();
            return;
        }

        if (BattlefieldDirector.Instance != null)
        {
            BattlefieldDirector.Instance.ForceDefeat("Tank fell off the map");
            return;
        }

        tank.enabled = false;
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
