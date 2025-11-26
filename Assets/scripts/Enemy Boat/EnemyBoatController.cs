using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class BoatFollower : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform playerBoat;
    public Transform[] dockingPoints;        // Assign empty GameObjects parented to PlayerBoat here
    public float followDistance = 10f;       // Distance to start following
    public float dockDistance = 15f;         // Distance to trigger docking mode
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 2f;
    public float dockSpeed = 3f;
    
    // Removed fixed offset, we now use the Transform positions of dockingPoints
    // public Vector3 dockedOffset = new Vector3(4f, 0f, 0f); 

    [Header("Physics & Gravity")]
    public float gravityForce = 20f;         // Downward force for NPCs
    public LayerMask npcLayer;               // Layer or Logic to detect NPCs

    [Header("Model Correction")]
    // Based on your image: Red (X) is along the length, Blue (Z) is to the side.
    // usually Forward is Z. If your Forward is negative X, we need a -90 or +90 offset.
    // Try 90, -90, or 180 if it flies sideways.
    public float rotationOffset = 90f; 

    private Rigidbody rb;
    private bool isDocked = false;
    private Transform activeDockPoint; // The specific point we are currently docking to
    private List<Rigidbody> affectedNPCs = new List<Rigidbody>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true; // The boat itself needs normal gravity
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (playerBoat == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerBoat.position);

        // --- STATE MACHINE ---
        if (distanceToPlayer < dockDistance)
        {
            HandleDocking();
        }
        else
        {
            // Reset the active dock point when we move away so we can pick a new best one later
            activeDockPoint = null; 
            HandleFollowing(distanceToPlayer);
        }

        // --- LOCAL GRAVITY LOGIC ---
        ApplyLocalGravity();
    }

    void HandleFollowing(float distance)
    {
        isDocked = false;

        // 1. Calculate direction to player
        Vector3 directionToPlayer = (playerBoat.position - transform.position).normalized;

        // 2. Move only if outside follow distance
        if (distance > followDistance)
        {
            // Move forward relative to WORLD space toward target
            // We use MovePosition for smoother physics interaction than AddForce for boats usually
            Vector3 targetPos = Vector3.MoveTowards(transform.position, playerBoat.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(targetPos);
        }

        // 3. Rotate towards player with Compensation
        // We ignore Y height difference for rotation so it doesn't tilt up/down weirdly
        Vector3 lookDir = new Vector3(directionToPlayer.x, 0, directionToPlayer.z);
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            
            // COMPENSATION: Apply the offset to fix the model's wrong axis
            // If your boat flies sideways, change rotationOffset in Inspector
            targetRotation *= Quaternion.Euler(0, rotationOffset, 0);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void HandleDocking()
    {
        isDocked = true;

        // 1. Pick the best docking point if we haven't already
        if (activeDockPoint == null)
        {
            activeDockPoint = GetClosestDockPoint();
        }

        // If no points assigned, do nothing (or fallback to old behavior if you prefer)
        if (activeDockPoint == null) return;

        // 2. Smoothly move to the specific dock point position
        Vector3 newPos = Vector3.Lerp(transform.position, activeDockPoint.position, dockSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // 3. Match the Dock Point's Rotation
        // This allows you to rotate the empty GameObject in the editor to define which way the boat faces when docked
        Quaternion targetRotation = activeDockPoint.rotation;
        
        // COMPENSATION: Still need to fix the model axis even when matching rotation
        targetRotation *= Quaternion.Euler(0, rotationOffset, 0);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    Transform GetClosestDockPoint()
    {
        if (dockingPoints == null || dockingPoints.Length == 0) return null;

        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (Transform potentialTarget in dockingPoints)
        {
            if (potentialTarget == null) continue;

            Vector3 directionToTarget = potentialTarget.position - currentPos;
            float dSqrToTarget = directionToTarget.sqrMagnitude;

            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget;
            }
        }

        return bestTarget;
    }

    // --- LOCAL GRAVITY SYSTEM ---
    // This pulls NPCs down relative to the Boat's "Up", allowing them to walk on walls/decks if the boat rocks.
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("NPC"))
        {
            Rigidbody npcRb = other.GetComponent<Rigidbody>();
            if (npcRb != null && !affectedNPCs.Contains(npcRb))
            {
                affectedNPCs.Add(npcRb);
                // Optional: Disable global gravity on NPC so they don't slide off due to world "down"
                npcRb.useGravity = false; 
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("NPC"))
        {
            Rigidbody npcRb = other.GetComponent<Rigidbody>();
            if (npcRb != null && affectedNPCs.Contains(npcRb))
            {
                affectedNPCs.Remove(npcRb);
                // Re-enable global gravity when they leave the boat
                npcRb.useGravity = true;
            }
        }
    }

    void ApplyLocalGravity()
    {
        foreach (Rigidbody npc in affectedNPCs)
        {
            if (npc == null) continue;

            Vector3 localDown = -transform.up;

            npc.AddForce(localDown * gravityForce, ForceMode.Acceleration);
            

        }
    }
}