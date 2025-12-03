using UnityEngine;

public class EnemyDeckWalker : MonoBehaviour
{
    [Header("movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.4f;

    [Header("anim")]
    public Animator animator;
    public string speedParam = "Speed";

    [Header("attack")]
    public EnemyBoardingAttack attack;

    [Header("ground snapping")]
    public LayerMask groundMask = ~0;
    public float groundSnapDistance = 5f;
    public float footHeight = 0.02f;

    Rigidbody rb;
    Collider col;

    Transform currentTarget;
    bool walking;
    bool lockedIn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void BeginWalk()
    {
        lockedIn = false;

        if (attack == null || attack.slashPoints == null || attack.slashPoints.Length == 0)
        {
            Debug.LogWarning("EnemyDeckWalker: no slash points set.");
            return;
        }

        currentTarget = attack.slashPoints[Random.Range(0, attack.slashPoints.Length)];
        walking = true;
    }

    void Update()
    {
        Vector3 up = transform.parent != null ? transform.parent.up : Vector3.up;

        // --- CONVEYOR BELT LOGIC (Stick to Ship) ---
        // Get the ship's movement for this frame
        Vector3 shipMoveDelta = Vector3.zero;
        Quaternion shipRotDelta = Quaternion.identity;
        Vector3 shipPos = Vector3.zero;

        if (ShipController.Instance != null)
        {
            shipMoveDelta = ShipController.Instance.positionDelta;
            shipRotDelta = ShipController.Instance.rotationDelta;
            shipPos = ShipController.Instance.transform.position; // Assuming pivot is at transform.position
            // Actually, we need the Rigidbody position of the ship, which is likely on the same object
            if (ShipController.Instance.GetComponent<Rigidbody>() != null)
                shipPos = ShipController.Instance.GetComponent<Rigidbody>().position;
        }

        // Apply Ship Rotation Leverage (Swing)
        // If the ship rotates, we need to rotate around the ship's center
        Vector3 currentPos = transform.position;
        if (ShipController.Instance != null)
        {
            Vector3 offset = currentPos - shipPos;
            Vector3 rotatedOffset = shipRotDelta * offset;
            shipMoveDelta += (rotatedOffset - offset);
        }

        // Apply Ship Movement immediately
        currentPos += shipMoveDelta;

        // Update our position so subsequent calculations use the "moved" position
        transform.position = currentPos;

        // --- WALKING LOGIC ---
        // 1. LOCKED IN (Arrived)
        if (lockedIn)
        {
            SnapToGround(ref currentPos, up);
            transform.position = currentPos;
            KeepUpright(up);
            if (animator) animator.SetFloat(speedParam, 0f);
            return;
        }

        // 2. IDLE / WAITING
        if (!walking || currentTarget == null)
        {
            SnapToGround(ref currentPos, up);
            transform.position = currentPos;
            KeepUpright(up);
            if (animator) animator.SetFloat(speedParam, 0f);
            return;
        }

        // 3. MOVING TO TARGET
        Vector3 toTarget = currentTarget.position - transform.position; // transform.position includes ship move now
        Vector3 flat = Vector3.ProjectOnPlane(toTarget, up);
        float dist = flat.magnitude;

        if (dist <= stopDistance)
        {
            walking = false;
            lockedIn = true;

            // Snap to target
            currentPos = currentTarget.position;
            SnapToGround(ref currentPos, up);
            transform.position = currentPos;

            KeepUpright(up);
            if (animator) animator.SetFloat(speedParam, 0f);
            if (attack != null) attack.BeginAttack();
            return;
        }

        // Move towards target
        Vector3 dir = flat.normalized;
        currentPos += dir * moveSpeed * Time.fixedDeltaTime;

        // Final Snap
        SnapToGround(ref currentPos, up);
        transform.position = currentPos;

        // Rotation
        Quaternion lookRot = Quaternion.LookRotation(dir, up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.fixedDeltaTime);

        if (animator) animator.SetFloat(speedParam, moveSpeed);
    }

    void KeepUpright(Vector3 up)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(Vector3.forward, up);
        Quaternion uprightRot = Quaternion.LookRotation(fwd.normalized, up);
        transform.rotation = Quaternion.Slerp(transform.rotation, uprightRot, 10f * Time.fixedDeltaTime);
    }

    void SnapToGround(ref Vector3 position, Vector3 up)
    {
        Vector3 origin = position + up * 1f;
        float remaining = groundSnapDistance;
        RaycastHit hit;

        while (remaining > 0f && Physics.Raycast(origin, -up, out hit, remaining, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider != col && !hit.collider.transform.IsChildOf(transform))
            {
                position = hit.point + up * footHeight;
                return;
            }
            float travelled = hit.distance + 0.01f;
            origin = hit.point - up * 0.01f;
            remaining -= travelled;
        }
    }
}