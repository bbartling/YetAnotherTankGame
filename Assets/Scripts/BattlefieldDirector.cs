using UnityEngine;

[DisallowMultipleComponent]
public class BattlefieldDirector : MonoBehaviour
{
    public static BattlefieldDirector Instance { get; private set; }

    [Header("References")]
    public Transform playerTank;
    public Transform castle;

    [Header("Difficulty")]
    public int enemyCountThisRun = 0;
    public float enemyCountDifficultyStep = 0.08f;
    public float timeDifficultyStepPerMinute = 0.06f;

    private float _runStartTime;

    private void Awake()
    {
        Instance = this;
        _runStartTime = Time.time;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        ResolveReferences();
    }

    public void SetEnemyCount(int count)
    {
        enemyCountThisRun = Mathf.Max(0, count);
    }

    public float GetDifficultyMultiplier()
    {
        float enemyPressure = enemyCountThisRun * enemyCountDifficultyStep;
        float timePressure = Mathf.Max(0f, Time.time - _runStartTime) / 60f * timeDifficultyStepPerMinute;
        return Mathf.Max(1f, 1f + enemyPressure + timePressure);
    }

    public Vector3 GetSharedTurretAimPoint(Vector3 sourcePosition, float leadSeconds = 0.35f)
    {
        if (TryGetPlayerTank(out Transform target))
        {
            Rigidbody body = target.GetComponent<Rigidbody>();
            Vector3 predicted = target.position + Vector3.up * 1.1f;
            if (body != null)
            {
                Vector3 velocity = GetVelocity(body);
                float distance = Vector3.Distance(sourcePosition, target.position);
                float lead = Mathf.Clamp(distance / 240f, 0.12f, 0.8f) * leadSeconds;
                predicted += velocity * lead;
            }

            return predicted;
        }

        return sourcePosition + Vector3.forward * 25f;
    }

    public bool TryGetPlayerTank(out Transform target)
    {
        if (playerTank != null)
        {
            target = playerTank;
            return true;
        }

        GameObject player = GameObject.Find("PlayerTank");
        if (player != null)
        {
            playerTank = player.transform;
            target = playerTank;
            return true;
        }

        target = null;
        return false;
    }

    public bool TryGetCastle(out Transform target)
    {
        if (castle != null)
        {
            target = castle;
            return true;
        }

        GameObject castleObject = GameObject.Find("Castle");
        if (castleObject == null)
        {
            castleObject = GameObject.Find("Castle(Clone)");
        }

        if (castleObject != null)
        {
            castle = castleObject.transform;
            target = castle;
            return true;
        }

        target = null;
        return false;
    }

    private void ResolveReferences()
    {
        if (playerTank == null)
        {
            TryGetPlayerTank(out _);
        }

        if (castle == null)
        {
            TryGetCastle(out _);
        }
    }

    private Vector3 GetVelocity(Rigidbody body)
    {
#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }
}
