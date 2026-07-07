using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class BallDriveTestController : MonoBehaviour
{
    public float rollForce = 38f;
    public float rollTorque = 22f;
    public float maxSpeed = 18f;
    public float maxAngularVelocity = 35f;
    public float gravityMultiplier = 1f;
    public float brakeDrag = 2.5f;
    public float airControlMultiplier = 0.25f;
    public float groundCheckDistance = 0.75f;
    public LayerMask groundMask = ~0;
    public Camera followCamera;
    public float cameraDistance = 26f;
    public float cameraHeight = 7.5f;
    public float cameraPitch = 16f;
    public float minCameraPitch = 4f;
    public float maxCameraPitch = 42f;
    public float mouseYawSensitivity = 2.4f;
    public float mousePitchSensitivity = 1.2f;
    public float cameraFollowSpeed = 8f;
    public float cameraLookHeight = 0.75f;
    public bool lockCursorOnPlay = true;
    public float tankTurnDegreesPerSecond = 95f;

    private Rigidbody _rb;
    private float _cameraYaw;
    private float _tankYaw;
    private Vector3 _cameraVelocity;

    public float CurrentTankYaw => _tankYaw;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.linearDamping = 0.28f;
        _rb.angularDamping = 0.12f;
        _rb.maxAngularVelocity = maxAngularVelocity;

        if (followCamera == null)
        {
            followCamera = Camera.main;
        }

        _cameraYaw = transform.eulerAngles.y;
        _tankYaw = transform.eulerAngles.y;
        cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);
    }

    private void OnEnable()
    {
        if (Application.isPlaying && lockCursorOnPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying && Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        UpdateCameraInput();
    }

    private void FixedUpdate()
    {
        ApplyExtraGravity();

        Vector2 rawInput = ReadInput();
        _tankYaw = CalculateTankYaw(_tankYaw, rawInput.x, tankTurnDegreesPerSecond, Time.fixedDeltaTime);
        Vector3 input = BuildTankDriveDirection(rawInput.y, _tankYaw);
        bool grounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        float control = grounded ? 1f : airControlMultiplier;

        if (input.sqrMagnitude > 0.001f)
        {
            Vector3 planarVelocity = Vector3.ProjectOnPlane(_rb.linearVelocity, Vector3.up);
            if (planarVelocity.magnitude < maxSpeed)
            {
                _rb.AddForce(input * rollForce * control, ForceMode.Acceleration);
            }

            Vector3 torqueAxis = Vector3.Cross(Vector3.up, input).normalized;
            _rb.AddTorque(torqueAxis * rollTorque * control, ForceMode.Acceleration);
        }
        else if (grounded)
        {
            Vector3 planarVelocity = Vector3.ProjectOnPlane(_rb.linearVelocity, Vector3.up);
            _rb.AddForce(-planarVelocity * brakeDrag, ForceMode.Acceleration);
        }
    }

    private void ApplyExtraGravity()
    {
        if (gravityMultiplier <= 1f)
        {
            return;
        }

        _rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
    }

    private void LateUpdate()
    {
        if (followCamera == null)
        {
            return;
        }

        Vector3 focus = transform.position + Vector3.up * cameraLookHeight;
        Vector3 yawForward = Quaternion.Euler(0f, _cameraYaw, 0f) * Vector3.forward;
        float clampedPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);
        float pitchRadians = clampedPitch * Mathf.Deg2Rad;
        float horizontalDistance = cameraDistance * Mathf.Cos(pitchRadians);
        float verticalOffset = cameraHeight + cameraDistance * Mathf.Sin(pitchRadians);
        Vector3 targetPosition = focus - yawForward * horizontalDistance + Vector3.up * verticalOffset;

        followCamera.transform.position = Vector3.SmoothDamp(
            followCamera.transform.position,
            targetPosition,
            ref _cameraVelocity,
            1f / Mathf.Max(0.01f, cameraFollowSpeed));
        followCamera.transform.rotation = Quaternion.LookRotation(focus - followCamera.transform.position, Vector3.up);
    }

    public static Vector3 BuildCameraRelativeInput(Vector2 input, float yawDegrees)
    {
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Quaternion yaw = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3 forward = yaw * Vector3.forward;
        Vector3 right = yaw * Vector3.right;
        Vector3 direction = forward * input.y + right * input.x;
        direction.y = 0f;
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }

    public static Vector3 BuildTankDriveDirection(float throttle, float yawDegrees)
    {
        float clampedThrottle = Mathf.Clamp(throttle, -1f, 1f);
        if (Mathf.Abs(clampedThrottle) < 0.001f)
        {
            return Vector3.zero;
        }

        Quaternion yaw = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3 direction = yaw * Vector3.forward * clampedThrottle;
        direction.y = 0f;
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }

    public static float CalculateTankYaw(
        float currentYawDegrees,
        float turnInput,
        float turnDegreesPerSecond,
        float deltaTime)
    {
        float next = currentYawDegrees + Mathf.Clamp(turnInput, -1f, 1f) * turnDegreesPerSecond * Mathf.Max(0f, deltaTime);
        return Mathf.Repeat(next, 360f);
    }

    private void UpdateCameraInput()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (lockCursorOnPlay && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        _cameraYaw += Input.GetAxisRaw("Mouse X") * mouseYawSensitivity;
        cameraPitch = Mathf.Clamp(
            cameraPitch - Input.GetAxisRaw("Mouse Y") * mousePitchSensitivity,
            minCameraPitch,
            maxCameraPitch);
    }

    private Vector2 ReadInput()
    {
        Vector2 input = Vector2.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input += Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input += Vector2.down;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input += Vector2.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input += Vector2.left;
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }
}
