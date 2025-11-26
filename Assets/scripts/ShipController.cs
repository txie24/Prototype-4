using UnityEngine;
using System.Collections.Generic;

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

    [Header("Gravity & Passengers")]
    [Tooltip("Force pulling NPCs down towards the ship deck.")]
    public float localGravityForce = 20f;
    public List<string> tagsToCatch = new List<string> { "Player", "NPC", "Enemy" };

    [Header("Player Interaction")]
    public Transform playerTransform;
    public float interactionDistance = 3f;

    private Transform wheelTransform;
    private float currentWheelRotation = 0f;
    private bool isPlayerInRange = false;

    // Movement & Sway State
    Vector3 startLocalPos;
    float seed;
    float currentYaw = 0f;

    // Platform Logic Variables
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private List<CharacterController> _passengerControllers = new List<CharacterController>();
    private List<Rigidbody> _passengerRigidbodies = new List<Rigidbody>();

    private const KeyCode Key_TurnLeft = KeyCode.Q;
    private const KeyCode Key_TurnRight = KeyCode.E;

    void Start()
    {
        wheelTransform = transform.Find("StylShip_Unity/Wheel");
        if (wheelTransform == null)
        {
            Debug.LogError("Wheel transform not found");
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) { playerTransform = player.transform; }
        }

        // Initialize Sway Variables
        startLocalPos = transform.localPosition;
        seed = Random.value * 10f;
        currentYaw = transform.localEulerAngles.y;

        // Initialize Platform Physics tracking
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
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
            currentWheelRotation = Mathf.MoveTowards(currentWheelRotation, 0f, wheelRotateSpeed * Time.deltaTime);
        }

        // 1. Move the Ship
        ApplyMovementAndBob();
        ApplyCombinedRotation();
        ApplyWheelVisualRotation();

        // 2. Move Passengers (Platform Logic)
        // We do this immediately after the ship moves to keep them synced frame-perfectly
        MovePassengers();

        // Update tracking for next frame
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        // Apply Gravity in FixedUpdate for better physics stability with Rigidbodies
        ApplyLocalGravity();
    }

    // --- PLATFORM & GRAVITY LOGIC ---

    private void MovePassengers()
    {
        // Calculate how much the ship moved/rotated this single frame
        Vector3 positionDelta = transform.position - _lastPosition;
        Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(_lastRotation);

        // 1. Move Character Controllers (Player)
        // CharacterControllers don't respect physics friction well, so we manually shove them
        for (int i = _passengerControllers.Count - 1; i >= 0; i--)
        {
            CharacterController cc = _passengerControllers[i];
            if (cc == null) { _passengerControllers.RemoveAt(i); continue; }

            // Apply linear movement
            Vector3 passengerMove = positionDelta;

            // Apply rotational movement (swinging the player if they are far from center)
            Vector3 offsetFromCenter = cc.transform.position - transform.position;
            Vector3 rotatedOffset = rotationDelta * offsetFromCenter;
            passengerMove += (rotatedOffset - offsetFromCenter);

            // Execute Move
            cc.Move(passengerMove);

            // Optional: Rotate the player to face with the ship? 
            // Usually players prefer to control their own camera, so we often skip rotating their look direction.
            // But we CAN rotate their body transform if we want them to turn with the ship:
            // cc.transform.rotation = rotationDelta * cc.transform.rotation;
        }

        // 2. Rigidbodies (NPCs / Crates) usually handle friction themselves, 
        // but adding manual velocity helps prevent them from lagging behind.
        foreach(var rb in _passengerRigidbodies)
        {
            if(rb != null && !rb.isKinematic)
            {
               // We rely on friction + Local Gravity for them, 
               // but we could fudge their position here if they slide too much.
            }
        }
    }

    private void ApplyLocalGravity()
    {
        Vector3 localDown = -transform.up; // Down relative to the ship deck

        for (int i = _passengerRigidbodies.Count - 1; i >= 0; i--)
        {
            Rigidbody rb = _passengerRigidbodies[i];
            if (rb == null) { _passengerRigidbodies.RemoveAt(i); continue; }

            // Pull them towards the floor of the ship
            rb.AddForce(localDown * localGravityForce, ForceMode.Acceleration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (tagsToCatch.Contains(other.tag))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null && !_passengerControllers.Contains(cc))
            {
                _passengerControllers.Add(cc);
            }

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && !_passengerRigidbodies.Contains(rb))
            {
                _passengerRigidbodies.Add(rb);
                rb.useGravity = false; // Disable world gravity so they don't fight us
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (tagsToCatch.Contains(other.tag))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null && _passengerControllers.Contains(cc))
            {
                _passengerControllers.Remove(cc);
            }

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && _passengerRigidbodies.Contains(rb))
            {
                _passengerRigidbodies.Remove(rb);
                rb.useGravity = true; // Restore world gravity
            }
        }
    }

    // --- EXISTING SHIP LOGIC ---

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

        if (Input.GetKey(Key_TurnLeft)) turnInput = -1f;
        else if (Input.GetKey(Key_TurnRight)) turnInput = 1f;

        float wheelTargetRotation = currentWheelRotation + (turnInput * wheelRotateSpeed * Time.deltaTime);
        currentWheelRotation = Mathf.Clamp(wheelTargetRotation, -maxWheelAngle, maxWheelAngle);

        if (turnInput == 0f)
        {
            currentWheelRotation = Mathf.MoveTowards(currentWheelRotation, 0f, wheelRotateSpeed * Time.deltaTime * 0.5f);
        }

        float normalizedSteering = currentWheelRotation / maxWheelAngle;
        float yawAmount = normalizedSteering * steeringSensitivity * Time.deltaTime;
        currentYaw += yawAmount;
    }

    private void ApplyMovementAndBob()
    {
        Vector3 forwardStep = transform.forward * forwardSpeed * Time.deltaTime;
        transform.position += forwardStep;

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