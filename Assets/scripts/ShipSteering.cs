using UnityEngine;

public class ShipSteering : MonoBehaviour
{
    [Header("Steering Settings")]
    [Tooltip("How fast the ship rotates (degrees per second).")]
    public float shipTurnSpeed = 30f;
    [Tooltip("How fast the wheel rotates visually (degrees per second).")]
    public float wheelRotateSpeed = 200f;
    [Tooltip("Maximum angle the wheel can rotate visually (e.g., -90 to +90).")]
    public float maxWheelAngle = 90f;

    [Header("Player Interaction")]
    [Tooltip("The player object's transform.")]
    public Transform playerTransform;
    [Tooltip("The maximum distance for the player to interact with the wheel.")]
    public float interactionDistance = 3f;

    private Transform wheelTransform;
    private float currentWheelRotation = 0f;
    private bool isPlayerInRange = false;

    private const KeyCode Key_TurnLeft = KeyCode.Q;
    private const KeyCode Key_TurnRight = KeyCode.E;

    void Start()
    {
        wheelTransform = transform.Find("Wheel");

        if (wheelTransform == null)
        {
            Debug.LogError("Wheel transform not found! Make sure 'Wheel' is a direct child of this GameObject.");
            enabled = false; // Disable the script if the wheel isn't found
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("Player Transform not set. Steering will always be active. Consider setting the 'Player' tag on your player object.");
            }
        }
    }

    void Update()
    {
        CheckPlayerDistance();

        // Only allow steering if the player is in range
        if (isPlayerInRange || playerTransform == null)
        {
            HandleSteeringInput();
        }
        else
        {
            // If the player leaves, let the wheel snap back to center slowly
            currentWheelRotation = Mathf.MoveTowards(currentWheelRotation, 0f, wheelRotateSpeed * Time.deltaTime);
        }

        // Apply the visual rotation to the wheel
        ApplyWheelVisualRotation();
    }

    private void CheckPlayerDistance()
    {
        if (playerTransform != null && wheelTransform != null)
        {
            // Use the Wheel's position as the interaction point
            Vector3 wheelPos = wheelTransform.position;

            // 1. Flatten both positions (set Y to 0) to measure only horizontal distance.
            // This prevents the player's height from affecting the check.
            Vector3 wheelPosFlat = new Vector3(wheelPos.x, 0, wheelPos.z);
            Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);

            // 2. Calculate the distance between these two flattened points
            float distance = Vector3.Distance(wheelPosFlat, playerPosFlat);

            isPlayerInRange = distance <= interactionDistance;
        }
        else
        {
            // If the player or wheel is missing (e.g., debug mode), always allow steering.
            isPlayerInRange = true;
        }
    }

    private void HandleSteeringInput()
    {
        float turnInput = 0f;

        if (Input.GetKey(Key_TurnLeft))
        {
            turnInput = 1f; // Left
        }
        else if (Input.GetKey(Key_TurnRight)) 
        {
            turnInput = -1f; // Right
        }

        float shipRotationAmount = turnInput * shipTurnSpeed * Time.deltaTime;
        transform.Rotate(0, shipRotationAmount, 0);

        float wheelTargetRotation = currentWheelRotation + (turnInput * wheelRotateSpeed * Time.deltaTime);

        // Clamp the wheel's rotation to the maximum allowed angle
        currentWheelRotation = Mathf.Clamp(wheelTargetRotation, -maxWheelAngle, maxWheelAngle);

        // If the player isn't pressing Q or E, gently move the wheel back to center.
        if (turnInput == 0f)
        {
            currentWheelRotation = Mathf.MoveTowards(currentWheelRotation, 0f, wheelRotateSpeed * Time.deltaTime * 0.5f);
        }
    }

    private void ApplyWheelVisualRotation()
    {
        if (wheelTransform != null)
        {
            wheelTransform.localRotation = Quaternion.AngleAxis(currentWheelRotation, Vector3.forward);
        }
    }
}