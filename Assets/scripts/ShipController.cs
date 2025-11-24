using UnityEngine;

public class ShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 5f;

    [Header("Steering Settings")]
    public float steeringSensitivity = 10f;
    [Tooltip("How fast the wheel rotates visually (degrees per second).")]
    public float wheelRotateSpeed = 100f;
    [Tooltip("Maximum angle the wheel can rotate visually (e.g., -90 to +90).")]
    public float maxWheelAngle = 180f;

    [Header("Sway Settings")]
    public float bobHeight = 0.25f;
    public float bobSpeed = 1.0f;
    public float rollAngle = 5f;
    public float rollSpeed = 0.8f;
    public float pitchAngle = 2f;
    public float pitchSpeed = 0.6f;

    [Header("Player Interaction")]
    public Transform playerTransform;
    public float interactionDistance = 3f;

    private Transform wheelTransform;
    private float currentWheelRotation = 0f;
    private bool isPlayerInRange = false;

    Vector3 startLocalPos;
    Quaternion startLocalRot;
    float seed;
    float currentYaw = 0f;

    private const KeyCode Key_TurnLeft = KeyCode.Q;
    private const KeyCode Key_TurnRight = KeyCode.E;

    void Start()
    {
        wheelTransform = transform.Find("StylShip_Unity/Wheel");
        if (wheelTransform == null)
        {
            Debug.LogError("Wheel transform not found");
            enabled = false;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) { playerTransform = player.transform; }
            else { Debug.LogWarning("Player Transform not set. Steering will always be active."); }
        }

        // Initialize Sway Variables
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;
        seed = Random.value * 10f;

        // Initialize current Yaw to the ship's starting Y rotation
        currentYaw = transform.localEulerAngles.y;
    }

    void Update()
    {
        CheckPlayerDistance();

        if (isPlayerInRange || playerTransform == null)
        {
            HandleSteeringInput();
        }
        else
        {
            // Player left the wheel, snap visual wheel back to center
            currentWheelRotation = Mathf.MoveTowards(currentWheelRotation, 0f, wheelRotateSpeed * Time.deltaTime);
        }

        // Apply all Movement, Bobbing, and Combined Rotation logic
        ApplyMovementAndBob();
        ApplyCombinedRotation();

        // Apply the visual rotation to the wheel
        ApplyWheelVisualRotation();
    }

    private void CheckPlayerDistance()
    {
        if (playerTransform != null && wheelTransform != null)
        {
            Vector3 wheelPos = wheelTransform.position;
            Vector3 wheelPosFlat = new Vector3(wheelPos.x, 0, wheelPos.z);
            Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            float distance = Vector3.Distance(wheelPosFlat, playerPosFlat);
            isPlayerInRange = distance <= interactionDistance;
        }
        else
        {
            isPlayerInRange = true;
        }
    }

    private void HandleSteeringInput()
    {
        float turnInput = 0f;

        if (Input.GetKey(Key_TurnLeft))
        {
            turnInput = -1f;
        }
        else if (Input.GetKey(Key_TurnRight))
        {
            turnInput = 1f;
        }

        // Wheel visual rotation update
        float wheelTargetRotation = currentWheelRotation + (turnInput * wheelRotateSpeed * Time.deltaTime);
        currentWheelRotation = Mathf.Clamp(wheelTargetRotation, -maxWheelAngle, maxWheelAngle);

        // Snap the wheel back to center if no input is present
        if (turnInput == 0f)
        {
            currentWheelRotation = Mathf.MoveTowards(currentWheelRotation, 0f, wheelRotateSpeed * Time.deltaTime * 0.5f);
        }

        // Steer the ship based on how far the wheel is currently rotated.
        float normalizedSteering = currentWheelRotation / maxWheelAngle;

        // Accumulate the yaw rotation to the total yaw angle
        float yawAmount = normalizedSteering * steeringSensitivity * Time.deltaTime;
        currentYaw += yawAmount;

        Debug.Log($"Steering Yaw Increment: {yawAmount:F4}. Wheel Rotation: {currentWheelRotation:F2}");
    }

    private void ApplyMovementAndBob()
    {
        Vector3 forwardStep = transform.forward * forwardSpeed * Time.deltaTime;
        transform.position += forwardStep;
        Debug.Log($"Position Delta: {forwardStep:F4}");

        float t = Time.time + seed;
        float bob = Mathf.Sin(t * bobSpeed) * bobHeight;

        Vector3 newLocalPos = transform.localPosition;
        newLocalPos.y = startLocalPos.y + bob;
        transform.localPosition = newLocalPos;
    }

    private void ApplyCombinedRotation()
    {
        float t = Time.time + seed;

        float roll = Mathf.Sin(t * rollSpeed) * rollAngle;
        float pitch = Mathf.Cos(t * pitchSpeed) * pitchAngle;

        // Combine steering (Yaw), Pitch, and Roll
        Quaternion finalRotation = Quaternion.Euler(pitch, currentYaw, roll);
        transform.localRotation = finalRotation;
    }

    private void ApplyWheelVisualRotation()
    {
        if (wheelTransform != null)
        {
            wheelTransform.localRotation = Quaternion.AngleAxis(currentWheelRotation, Vector3.forward);
        }
    }
}