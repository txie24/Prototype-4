using UnityEngine;
using System.Collections.Generic;

public class ShipController : MonoBehaviour
{
    // Singleton Instance
    public static ShipController Instance;

    [Header("Movement Settings")]
    public float forwardSpeed = 5f;

    [Header("Steering Settings")]
    public float steeringSensitivity = 10f;
    public float wheelRotateSpeed = 100f;
    public float maxWheelAngle = 180f;

    [Header("Sway Settings")]
    public float bobHeight = 0.25f;
    public float bobSpeed = 1.0f;
    public float rollAngle = 5f;
    public float rollSpeed = 0.8f;
    public float pitchAngle = 2f;
    public float pitchSpeed = 0.6f;

    [Header("Collision & Health")]
    [Tooltip("Layers the ship should collide with (e.g. Default). Uncheck Player/Water.")]
    public LayerMask obstacleLayers = 1;
    [Tooltip("Drag the ShipHealth script here.")]
    public ShipHealth shipHealth;
    public float damageInterval = 1.0f; // How often to take damage (in seconds)

    [Header("Passengers")]
    public float localGravityForce = 20f;

    [Header("Player Interaction")]
    public Transform playerTransform;
    public float interactionDistance = 3f;

    private Transform wheelTransform;
    private float currentWheelRotation = 0f;
    private bool isPlayerInRange = false;

    // Movement State
    Vector3 startLocalPos;
    float seed;
    float currentYaw = 0f;

    private Rigidbody rb;

    // Damage Timer
    private float _nextDamageTime = 0f;

    // History
    private Vector3 _prevPosition;
    private Quaternion _prevRotation;
    public Vector3 positionDelta;
    public Quaternion rotationDelta;


    // Passenger Lists
    private List<CharacterController> _passengerControllers = new List<CharacterController>();
    private List<Rigidbody> _passengerRigidbodies = new List<Rigidbody>();

    private const KeyCode Key_TurnLeft = KeyCode.Q;
    private const KeyCode Key_TurnRight = KeyCode.E;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("ShipController: Rigidbody missing. Please add one to this object.");
            enabled = false;
            return;
        }

        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.interpolation = RigidbodyInterpolation.None;

        wheelTransform = transform.Find("StylShip_Unity/Wheel");

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (shipHealth == null)
        {
            shipHealth = GetComponent<ShipHealth>();
        }

        if (playerTransform != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) AddPassenger(cc);
        }

        startLocalPos = transform.localPosition;
        seed = Random.value * 10f;
        currentYaw = transform.localEulerAngles.y;

        // Init history
        _prevPosition = rb.position;
        _prevRotation = rb.rotation;
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

        ApplyWheelVisualRotation();
    
        ApplyCombinedMovementAndRotation();

        // Move passengers AFTER the ship moves to capture the exact delta
        MovePassengers();

        // Update history
        _prevPosition = rb.position;
        _prevRotation = rb.rotation;
    }

    private void ApplyCombinedMovementAndRotation()
    {
        float t = Time.time + seed;
        float roll = Mathf.Sin(t * rollSpeed) * rollAngle;
        float pitch = Mathf.Cos(t * pitchSpeed) * pitchAngle;

        // apply roll/pitch/yaw
        Quaternion finalRotation = Quaternion.Euler(pitch, currentYaw, roll);
        rb.MoveRotation(finalRotation);

        // Forward movement this frame
        Vector3 forwardStep = transform.forward * forwardSpeed * Time.deltaTime;

        // Collision Check
        if (rb.SweepTest(forwardStep.normalized, out RaycastHit hit, forwardStep.magnitude + 0.1f, QueryTriggerInteraction.Ignore))
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleLayers) != 0)
            {
                // Damage Logic
                if (shipHealth != null && Time.time >= _nextDamageTime)
                {
                    float damageAmount = shipHealth.maxHealth * 0.30f;
                    shipHealth.TakeDamage(damageAmount);
                    _nextDamageTime = Time.time + damageInterval;
                    Debug.Log($"Ship hit iceberg! Took {damageAmount} damage.");
                }

                // Slide Logic
                forwardStep = Vector3.ProjectOnPlane(forwardStep, hit.normal);
            }
        }

        // ----- bobbing -----
        float bob = Mathf.Sin(t * bobSpeed) * bobHeight;
        Vector3 targetLocalPos = rb.transform.localPosition;
        targetLocalPos.y = startLocalPos.y + bob;

        Vector3 bobbingOffsetWorld =
            transform.parent != null
                ? transform.parent.TransformPoint(targetLocalPos)
                : targetLocalPos;

        // ----- final position -----
        Vector3 finalMoveTarget = new Vector3(
            rb.position.x + forwardStep.x,
            bobbingOffsetWorld.y,
            rb.position.z + forwardStep.z
        );

        positionDelta = finalMoveTarget - rb.position;
        // Rotation delta: diff between current and next rotation
        rotationDelta = finalRotation * Quaternion.Inverse(rb.rotation);

        rb.MovePosition(finalMoveTarget);
    }


    private void MovePassengers()
    {
        positionDelta = rb.position - _prevPosition;
        rotationDelta = rb.rotation * Quaternion.Inverse(_prevRotation);

        for (int i = _passengerControllers.Count - 1; i >= 0; i--)
        {
            CharacterController cc = _passengerControllers[i];
            if (cc == null) { _passengerControllers.RemoveAt(i); continue; }

            Vector3 finalMove = positionDelta;

            // Rotation leverage
            Vector3 offset = cc.transform.position - rb.position;
            Vector3 rotatedOffset = rotationDelta * offset;
            finalMove += (rotatedOffset - offset);

            cc.Move(finalMove);
        }

        // Move Rigidbodies
        for (int i = _passengerRigidbodies.Count - 1; i >= 0; i--)
        {
            Rigidbody pRb = _passengerRigidbodies[i];
            if (pRb == null) { _passengerRigidbodies.RemoveAt(i); continue; }

            if (pRb.isKinematic)
            {
                // Kinematic bodies ignore AddForce, so we must MovePosition manually
                Vector3 finalPos = pRb.position + positionDelta;

                // Rotation leverage
                Vector3 offset = pRb.position - rb.position;
                Vector3 rotatedOffset = rotationDelta * offset;
                finalPos += (rotatedOffset - offset);

                pRb.MovePosition(finalPos);
            }
            else
            {
                // Dynamic bodies get gravity
                pRb.AddForce(-transform.up * localGravityForce, ForceMode.Acceleration);
            }
        }
    }

    public void AddPassenger(CharacterController cc)
    {
        if (cc != null && !_passengerControllers.Contains(cc))
            _passengerControllers.Add(cc);
    }

    public void RemovePassenger(CharacterController cc)
    {
        if (cc != null && _passengerControllers.Contains(cc))
            _passengerControllers.Remove(cc);
    }

    public void AddPassengerRigidbody(Rigidbody rb)
    {
        if (rb != null && !_passengerRigidbodies.Contains(rb))
        {
            _passengerRigidbodies.Add(rb);
            rb.useGravity = false;
        }
    }

    private void CheckPlayerDistance()
    {
        if (playerTransform != null && wheelTransform != null)
        {
            Vector3 wheelPos = wheelTransform.position;
            Vector3 wheelPosFlat = new Vector3(wheelPos.x, 0, wheelPos.z);
            Vector3 playerPosFlat = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
            isPlayerInRange = Vector3.Distance(wheelPosFlat, playerPosFlat) <= interactionDistance;
        }
    }

    private void HandleSteeringInput()
    {
        float turnInput = 0f;
        if (Input.GetKey(Key_TurnLeft)) turnInput = -1f;
        else if (Input.GetKey(Key_TurnRight)) turnInput = 1f;

        float wheelTargetRotation = currentWheelRotation + (turnInput * wheelRotateSpeed * Time.deltaTime);
        currentWheelRotation = Mathf.Clamp(wheelTargetRotation, -maxWheelAngle, maxWheelAngle);

        if (turnInput == 0f) currentWheelRotation = Mathf.MoveTowards(currentWheelRotation, 0f, wheelRotateSpeed * Time.deltaTime * 0.5f);

        float normalizedSteering = currentWheelRotation / maxWheelAngle;
        currentYaw += normalizedSteering * steeringSensitivity * Time.deltaTime;
    }

    private void ApplyWheelVisualRotation()
    {
        if (wheelTransform != null)
        {
            wheelTransform.localRotation = Quaternion.AngleAxis(-currentWheelRotation, Vector3.forward);
        }
    }
}