using UnityEngine;

public class EnemyBoatController : MonoBehaviour
{
    public enum EnemyBoatState
    {
        Chasing,
        Docking,
        Docked
    }

    [Header("Target (player boat)")]
    public Transform playerBoat;      // root of player ship
    public Transform leftDockPoint;   // side points on player ship
    public Transform rightDockPoint;

    [Header("Chase behaviour")]
    [Tooltip("Where the enemy tries to sit relative to the player (player local space).")]
    public Vector3 chaseOffset = new Vector3(0f, 0f, -30f);
    public float maxChaseSpeed = 10f;
    public float chaseTurnRateDeg = 60f;     // how fast boat can yaw while chasing
    public float accel = 4f;
    public float decel = 6f;
    public float slowRadius = 10f;           // start slowing when closer than this
    public float stopDistance = 4f;          // don't ram the exact point

    [Header("Docking behaviour")]
    [Tooltip("Start docking when within this distance of the chosen dock point.")]
    public float startDockDistance = 20f;

    [Tooltip("How far out from the side the boat lines up before sliding in.")]
    public float dockApproachOffset = 6f;

    [Tooltip("Snap + parent when closer than this to the dock point.")]
    public float dockSnapDistance = 0.8f;

    public float maxDockSpeed = 6f;
    public float dockTurnRateDeg = 80f;

    public EnemyBoatState state = EnemyBoatState.Chasing;

    float currentSpeed = 0f;
    Transform currentDockPoint;

    void Update()
    {
        if (playerBoat == null) return;

        switch (state)
        {
            case EnemyBoatState.Chasing:
                UpdateChasing();
                break;

            case EnemyBoatState.Docking:
                UpdateDocking();
                break;

            case EnemyBoatState.Docked:
                // locked on, do nothing for now
                break;
        }
    }

    // ---------- CHASING ----------

    void UpdateChasing()
    {
        // smooth boat-like chase: turn toward a point behind the player and move forward only
        Vector3 chaseTarget = playerBoat.TransformPoint(chaseOffset);
        BoatSteerTowards(chaseTarget,
                         maxChaseSpeed,
                         chaseTurnRateDeg,
                         slowRadius,
                         stopDistance);

        // choose a dock point and switch to docking when we're close enough
        Transform bestDock = GetBestDockPoint();
        if (bestDock == null) return;

        float distToDock = Vector3.Distance(transform.position, bestDock.position);
        if (distToDock <= startDockDistance)
        {
            currentDockPoint = bestDock;
            state = EnemyBoatState.Docking;
        }
    }

    // ---------- DOCKING ----------

    void UpdateDocking()
    {
        if (currentDockPoint == null)
        {
            state = EnemyBoatState.Chasing;
            return;
        }

        // we approach from OUTSIDE the side of the ship, parallel to it,
        // then slide inwards for the last meter.
        Vector3 sideOut = currentDockPoint.right; // red axis should point out away from player hull
        Vector3 approachPos = currentDockPoint.position + sideOut * dockApproachOffset;

        float distToApproach = Vector3.Distance(transform.position, approachPos);
        float distToDock = Vector3.Distance(transform.position, currentDockPoint.position);

        if (distToApproach > 1.5f)
        {
            // phase 1: get to the approach lane, align with dockPoint forward (parallel to player)
            BoatSteerTowards(
                approachPos,
                maxDockSpeed,
                dockTurnRateDeg,
                slowRadius: 5f,
                stopDistance: 1.5f,
                lockHeadingTo: currentDockPoint.forward
            );
        }
        else if (distToDock > dockSnapDistance)
        {
            // phase 2: slide in slowly while staying parallel
            BoatSteerTowards(
                currentDockPoint.position,
                maxDockSpeed * 0.6f,
                dockTurnRateDeg,
                slowRadius: 3f,
                stopDistance: dockSnapDistance,
                lockHeadingTo: currentDockPoint.forward
            );
        }
        else
        {
            // phase 3: hard dock
            transform.position = currentDockPoint.position;
            transform.rotation = currentDockPoint.rotation;
            transform.SetParent(playerBoat);
            currentSpeed = 0f;
            state = EnemyBoatState.Docked;
        }
    }

    // ---------- COMMON BOAT STEERING ----------

    /// <summary>
    /// Steers like a boat: rotate gradually, move only along forward, with accel/decel.
    /// If lockHeadingTo is non-zero, the boat tries to keep that forward direction instead
    /// of pointing directly at the target.
    /// </summary>
    void BoatSteerTowards(Vector3 worldTarget,
                          float maxSpeed,
                          float turnRateDeg,
                          float slowRadius,
                          float stopDistance,
                          Vector3? lockHeadingTo = null)
    {
        float dt = Time.deltaTime;

        // flatten target on water plane
        Vector3 targetPos = worldTarget;
        targetPos.y = transform.position.y;

        Vector3 toTarget = targetPos - transform.position;
        float dist = toTarget.magnitude;

        // figure out desired heading
        Vector3 desiredForward;
        if (lockHeadingTo.HasValue)
        {
            desiredForward = lockHeadingTo.Value.normalized;
            desiredForward.y = 0f;
            if (desiredForward.sqrMagnitude < 0.001f && dist > 0.001f)
                desiredForward = toTarget.normalized;
        }
        else
        {
            if (dist < 0.001f) return;
            desiredForward = toTarget.normalized;
        }

        Vector3 currentForward = transform.forward;
        currentForward.y = 0f;

        // rotate towards desired heading
        if (desiredForward.sqrMagnitude > 0.0001f && currentForward.sqrMagnitude > 0.0001f)
        {
            Vector3 newForward = Vector3.RotateTowards(
                currentForward.normalized,
                desiredForward.normalized,
                turnRateDeg * Mathf.Deg2Rad * dt,
                0f
            );
            transform.rotation = Quaternion.LookRotation(newForward, Vector3.up);
        }

        // choose desired speed based on distance (arrival behaviour)
        float desiredSpeed;
        if (dist > slowRadius)
        {
            desiredSpeed = maxSpeed;
        }
        else if (dist > stopDistance)
        {
            float t = Mathf.InverseLerp(stopDistance, slowRadius, dist);
            desiredSpeed = maxSpeed * t;
        }
        else
        {
            desiredSpeed = 0f;
        }

        // smooth speed change
        float rate = (desiredSpeed > currentSpeed) ? accel : decel;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * dt);

        // move forward only (no strafing)
        transform.position += transform.forward * currentSpeed * dt;
    }

    Transform GetBestDockPoint()
    {
        if (leftDockPoint == null && rightDockPoint == null) return null;
        if (leftDockPoint != null && rightDockPoint == null) return leftDockPoint;
        if (rightDockPoint != null && leftDockPoint == null) return rightDockPoint;

        float leftDist = Vector3.Distance(transform.position, leftDockPoint.position);
        float rightDist = Vector3.Distance(transform.position, rightDockPoint.position);
        return leftDist <= rightDist ? leftDockPoint : rightDockPoint;
    }

    public void Undock()
    {
        if (state != EnemyBoatState.Docked) return;
        transform.SetParent(null);
        state = EnemyBoatState.Chasing;
    }
}
