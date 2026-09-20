using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class CannonManager : MonoBehaviour
{
    [Header("Cannon Parts")]
    public GameObject cannonBallPrefab;
    public Transform firePoint;
    public LineRenderer lineRenderer;

    [Header("UI Controls")]
    public Slider elevationSlider;
    public Slider angleSlider;
    public Slider powerSlider;
    public Slider massSlider;
    public TextMeshProUGUI elevationText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI massText;

    [Header("Target Tracking")]
    public Transform baseForCamera;
    [Tooltip("Used when a shot clears the cannon — typically the goal-post / target tracking point.")]
    public Vector3 goalCameraOffset = new Vector3(0f, 2f, 0f);
    [Tooltip("Fixed camera offset from the cannon when it blows apart.")]
    public Vector3 destroyedCameraOffset = new Vector3(0f, 5.5f, -9f);

    [Header("Sound Effects")]
    public AudioClip cannonFireSound;
    public AudioClip cannonDestroyedSound;

    [Header("Muzzle Clearance")]
    [Tooltip("Ball must travel this far from the muzzle without hitting the cannon.")]
    public float muzzleClearanceDistance = 3.25f;
    [Tooltip("If the ball is still inside the cannon volume after this many seconds, it counts as a failed clear.")]
    public float muzzleClearanceSeconds = 0.55f;

    private const int N_TRAJECTORY_POINTS = 20;
    private Camera _mainCam;
    private AudioSource _audio;
    private bool _destroyed;
    private bool _lockDestroyedCamera;
    private GameObject _failPanel;
    private Material _burstMaterial;
    private Vector3 _destroyedFocus;

    // Private variables to hold slider values
    private float _elevationDeg;
    private float _traverseDeg;
    private float _powerImpulse;
    private float _mass;

    void Awake()
    {
        _mainCam = Camera.main;
        _audio = GetComponent<AudioSource>();
        if (_audio != null)
        {
            _audio.playOnAwake = false;
            _audio.loop = false;
            _audio.Stop();
        }

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = N_TRAJECTORY_POINTS;
            lineRenderer.enabled = true;
        }

        SetElevation();
        SetAngle();
        SetPower();
        SetMass();
    }

    void Start()
    {
        if (GetComponent<CannonControlsTutorial>() == null)
        {
            gameObject.AddComponent<CannonControlsTutorial>();
        }
    }

    void OnDestroy()
    {
        if (_burstMaterial != null)
        {
            Destroy(_burstMaterial);
        }
    }

    void Update()
    {
        if (_destroyed)
        {
            return;
        }

        UpdateTrajectoryPreview();
    }

    void LateUpdate()
    {
        if (!_lockDestroyedCamera || _mainCam == null)
        {
            return;
        }

        // Keep a fixed cinematic on the wrecked cannon (not the goal-post camera).
        Vector3 camPos = transform.TransformPoint(destroyedCameraOffset);
        _mainCam.transform.position = camPos;
        _mainCam.transform.LookAt(_destroyedFocus);
    }

    public void SetElevation()
    {
        if (elevationSlider == null) return;
        _elevationDeg = elevationSlider.value;
        if (elevationText != null) elevationText.text = $"{_elevationDeg:F0}°";
        ApplyAim();
    }

    public void SetAngle()
    {
        if (angleSlider == null) return;
        _traverseDeg = angleSlider.value;
        if (angleText != null) angleText.text = $"{_traverseDeg:F0}°";
        ApplyAim();
    }

    public void SetPower()
    {
        if (powerSlider == null) return;
        _powerImpulse = powerSlider.value;
        if (powerText != null) powerText.text = $"{_powerImpulse:F0}";
    }

    public void SetMass()
    {
        if (massSlider == null) return;
        _mass = massSlider.value;
        if (massText != null) massText.text = $"{_mass:F0} kg";
    }

    public void Fire()
    {
        if (_destroyed)
        {
            return;
        }

        if (cannonFireSound != null && _audio != null)
        {
            _audio.PlayOneShot(cannonFireSound);
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRot = firePoint != null ? firePoint.rotation : transform.rotation;
        GameObject ball = Instantiate(cannonBallPrefab, spawnPos, spawnRot);

        var pcc = ball.GetComponent<ProjectileCameraController>();
        if (pcc != null)
        {
            pcc.mainCamera = _mainCam;
            // Successful clear ? follow the goal-post / target tracking point.
            pcc.trackingBase = baseForCamera;
            pcc.cameraPositionOffset = goalCameraOffset;
            pcc.ConfigureMuzzleClearance(this, spawnPos, muzzleClearanceDistance, muzzleClearanceSeconds);
        }

        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = _mass;
            Vector3 impulse = transform.forward * _powerImpulse;
            rb.AddForce(impulse, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Called by the projectile when it strikes the cannon or never leaves the muzzle volume.
    /// </summary>
    public void NotifyFailedMuzzleClearance(ProjectileCameraController ball)
    {
        if (_destroyed)
        {
            return;
        }

        if (ball != null)
        {
            ball.AbortForCannonDestruction();
        }

        StartCoroutine(BlowUpCannon(
            "The shot never cleared the cannon. It cooked off in the breech and blew the gun apart."));
    }

    public void RestartGame()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.buildIndex >= 0)
        {
            SceneManager.LoadScene(active.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(active.name);
        }
    }

    public bool IsPartOfCannon(Transform other)
    {
        return other != null && (other == transform || other.IsChildOf(transform));
    }

    private IEnumerator BlowUpCannon(string reason)
    {
        _destroyed = true;
        _lockDestroyedCamera = true;
        SetControlsInteractable(false);

        _destroyedFocus = firePoint != null
            ? firePoint.position
            : transform.position + Vector3.up * 1.5f;

        if (_mainCam != null)
        {
            Vector3 camPos = transform.TransformPoint(destroyedCameraOffset);
            _mainCam.transform.position = camPos;
            _mainCam.transform.LookAt(_destroyedFocus);
        }

        CannonControlsTutorial tutorial = GetComponent<CannonControlsTutorial>();
        if (tutorial != null)
        {
            tutorial.enabled = false;
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform overlay = canvas.transform.Find("CannonControlsTutorialOverlay");
                if (overlay != null)
                {
                    overlay.gameObject.SetActive(false);
                }
            }
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        if (cannonDestroyedSound != null && _audio != null)
        {
            _audio.PlayOneShot(cannonDestroyedSound);
        }
        else if (cannonFireSound != null && _audio != null)
        {
            _audio.PlayOneShot(cannonFireSound);
        }

        SpawnCannonExplosion(_destroyedFocus);

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null && !(renderer is LineRenderer))
            {
                renderer.enabled = false;
            }
        }

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        ShowFailPanel(reason);
        yield break;
    }

    private void SetControlsInteractable(bool enabled)
    {
        if (elevationSlider != null) elevationSlider.interactable = enabled;
        if (angleSlider != null) angleSlider.interactable = enabled;
        if (powerSlider != null) powerSlider.interactable = enabled;
        if (massSlider != null) massSlider.interactable = enabled;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == "FireButton")
            {
                buttons[i].interactable = enabled;
            }
        }
    }

    private void ShowFailPanel(string reason)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (_failPanel != null)
        {
            Destroy(_failPanel);
        }

        _failPanel = new GameObject("CannonDestroyedPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform root = _failPanel.GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.SetAsLastSibling();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        Image dim = _failPanel.GetComponent<Image>();
        dim.color = new Color(0.15f, 0.02f, 0.02f, 0.55f);
        dim.raycastTarget = true;

        GameObject cardObject = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform card = cardObject.GetComponent<RectTransform>();
        card.SetParent(root, false);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(520f, 220f);
        card.anchoredPosition = Vector2.zero;
        cardObject.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.08f, 0.96f);

        TextMeshProUGUI title = CreateTmp(card, "Title", 34f, FontStyles.Bold, new Vector2(0f, 58f), new Vector2(480f, 44f));
        title.text = "You just blew the cannon!";
        title.color = new Color(1f, 0.45f, 0.2f, 1f);

        TextMeshProUGUI body = CreateTmp(card, "Body", 18f, FontStyles.Normal, new Vector2(0f, 8f), new Vector2(460f, 70f));
        body.text = reason;
        body.color = Color.white;

        GameObject buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(card, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 22f);
        buttonRect.sizeDelta = new Vector2(180f, 44f);
        buttonObject.GetComponent<Image>().color = new Color(0.75f, 0.25f, 0.15f, 1f);
        Button restart = buttonObject.GetComponent<Button>();
        restart.onClick.AddListener(RestartGame);

        TextMeshProUGUI label = CreateTmp(buttonRect, "Label", 20f, FontStyles.Bold, Vector2.zero, new Vector2(170f, 36f));
        label.text = "Restart Game";
        label.color = Color.white;
    }

    private static TextMeshProUGUI CreateTmp(
        RectTransform parent,
        string name,
        float fontSize,
        FontStyles style,
        Vector2 anchoredPos,
        Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.text = "";
        return tmp;
    }

    private void ApplyAim()
    {
        transform.localRotation = Quaternion.Euler(-_elevationDeg, _traverseDeg, 0f);
    }

    private void UpdateTrajectoryPreview()
    {
        if (lineRenderer == null || firePoint == null) return;

        Vector3 v0 = (transform.forward * _powerImpulse) / Mathf.Max(_mass, 0.0001f);
        Vector3 p0 = firePoint.position;

        for (int i = 0; i < N_TRAJECTORY_POINTS; i++)
        {
            float t = i * 0.1f;
            Vector3 p = p0 + v0 * t + 0.5f * Physics.gravity * (t * t);
            lineRenderer.SetPosition(i, p);
        }
    }

    private void SpawnCannonExplosion(Vector3 position)
    {
        GameObject burstObject = new GameObject("CannonDestroyedBurst");
        burstObject.transform.position = position;
        ParticleSystem burst = burstObject.AddComponent<ParticleSystem>();
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = burst.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 2.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 18f);
        main.startColor = new Color(1f, 0.4f, 0.05f, 1f);
        main.gravityModifier = 0.35f;
        main.maxParticles = 96;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = burst.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 48, 72) });

        ParticleSystem.ShapeModule shape = burst.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;

        ParticleSystemRenderer renderer = burst.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetBurstMaterial();
        }

        burst.Play(true);
        Destroy(burstObject, 3f);
    }

    private Material GetBurstMaterial()
    {
        if (_burstMaterial != null)
        {
            return _burstMaterial;
        }

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        _burstMaterial = new Material(shader)
        {
            name = "CannonDestroyedBurstMaterial",
            mainTexture = Texture2D.whiteTexture,
            color = new Color(1f, 0.45f, 0.1f, 1f)
        };
        return _burstMaterial;
    }
}
