using UnityEngine;

[DisallowMultipleComponent]
public class BattlefieldWind : MonoBehaviour
{
    public static BattlefieldWind Instance { get; private set; }

    [Header("Wind")]
    public bool randomizeOnAwake = true;
    public float minSpeed = 0f;
    public float maxSpeed = 12f;
    public float changeInterval = 8f;
    public float gustStrength = 3.5f;
    public float gustSpeed = 0.45f;
    public float smoothing = 2.2f;
    public float calmChance = 0.18f;

    private float _currentSpeed;
    private float _targetSpeed;
    private float _currentAngle;
    private float _targetAngle;
    private float _nextChangeTime;
    private float _gustSeed;

    public float CurrentSpeed => _currentSpeed;

    public Vector3 CurrentWindVector => GetWindVector();

    private void Awake()
    {
        Instance = this;
        _gustSeed = Random.Range(0f, 1000f);
        RandomizeTargets(forceImmediate: true);
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
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
        if (Time.time >= _nextChangeTime)
        {
            RandomizeTargets(forceImmediate: false);
        }

        float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothing) * Time.deltaTime);
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, blend);
        _currentAngle = Mathf.LerpAngle(_currentAngle, _targetAngle, blend * 0.55f);
    }

    public Vector3 GetWindVector()
    {
        float angleRad = _currentAngle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));

        float gust = Mathf.PerlinNoise(_gustSeed, Time.time * gustSpeed) - 0.5f;
        float gustBonus = gust * 2f * gustStrength;
        float speed = Mathf.Max(0f, _currentSpeed + gustBonus);

        return direction * speed;
    }

    public string GetArrowLabel()
    {
        float normalized = Mathf.Repeat(_currentAngle, 360f);
        if (normalized < 22.5f || normalized >= 337.5f) return "→";
        if (normalized < 67.5f) return "↗";
        if (normalized < 112.5f) return "↑";
        if (normalized < 157.5f) return "↖";
        if (normalized < 202.5f) return "←";
        if (normalized < 247.5f) return "↙";
        if (normalized < 292.5f) return "↓";
        return "↘";
    }

    public string GetWindReadout()
    {
        float speed = GetDisplaySpeed();
        if (speed <= 0.25f)
        {
            return "CALM";
        }

        return string.Format("{0:0.0} m/s {1}", speed, GetArrowLabel());
    }

    public float GetDisplaySpeed()
    {
        float gust = Mathf.PerlinNoise(_gustSeed, Time.time * gustSpeed) - 0.5f;
        float gustBonus = gust * 2f * gustStrength;
        return Mathf.Max(0f, _currentSpeed + gustBonus);
    }

    private void RandomizeTargets(bool forceImmediate)
    {
        float calmRoll = Random.value;
        _targetSpeed = calmRoll < calmChance ? Random.Range(0f, 1.2f) : Random.Range(minSpeed, maxSpeed);
        _targetAngle = Random.Range(0f, 360f);

        if (forceImmediate)
        {
            _currentSpeed = _targetSpeed;
            _currentAngle = _targetAngle;
        }

        float interval = Mathf.Max(2f, changeInterval);
        _nextChangeTime = Time.time + Random.Range(interval * 0.6f, interval * 1.25f);
    }
}
