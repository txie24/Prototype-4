using UnityEngine;

public class EnemyDeckWalker : MonoBehaviour
{
    [Header("movement")]
    public Transform walkTarget;
    public float moveSpeed = 2f;
    public float stopDistance = 0.5f;

    [Header("anim")]
    public Animator animator;
    public string speedParam = "Speed";

    [Header("attack")]
    public EnemyBoardingAttack attack;

    [Header("ground snapping")]
    public LayerMask groundMask = ~0;          // layers considered as deck/ground
    public float groundSnapDistance = 2f;      // max distance to search downwards
    public float footHeight = 0.05f;           // small offset above deck

    Rigidbody rb;
    Collider col;
    bool walking;
    bool atTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rb != null)
        {
            // we handle vertical + upright ourselves
            rb.useGravity = false;

            // prevent tipping over: only allow yaw
            rb.freezeRotation = false;
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    public void BeginWalk()
    {
        walking = true;
        atTarget = false;

        if (rb != null)
        {
            // make sure position isn't frozen from a previous state
            rb.constraints &= ~(RigidbodyConstraints.FreezePositionX |
                                RigidbodyConstraints.FreezePositionY |
                                RigidbodyConstraints.FreezePositionZ);
        }
    }

    void FixedUpdate()
    {
        Vector3 up = transform.parent != null ? transform.parent.up : Vector3.up;

        if (!walking || walkTarget == null)
        {
            KeepUpright(up);
            return;
        }

        // direction to target, flattened on deck plane
        Vector3 toTarget = walkTarget.position - transform.position;
        Vector3 flat = Vector3.ProjectOnPlane(toTarget, up);
        float dist = flat.magnitude;

        if (dist <= stopDistance)
        {
            // === ARRIVED AT ATTACK LOCATION ===
            walking = false;
            atTarget = true;

            Vector3 pos = transform.position;
            SnapToGround(ref pos, up);

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.MovePosition(pos);

                // completely freeze at attack spot
                rb.constraints |= RigidbodyConstraints.FreezePositionX |
                                  RigidbodyConstraints.FreezePositionY |
                                  RigidbodyConstraints.FreezePositionZ |
                                  RigidbodyConstraints.FreezeRotationX |
                                  RigidbodyConstraints.FreezeRotationY |
                                  RigidbodyConstraints.FreezeRotationZ;
            }
            else
            {
                transform.position = pos;
            }

            if (animator && !string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, 0f);

            if (attack != null)
                attack.BeginAttack();

            return;
        }

        Vector3 dir = flat.normalized;

        // move towards target
        Vector3 newPos = transform.position + dir * moveSpeed * Time.fixedDeltaTime;
        SnapToGround(ref newPos, up);   // stick feet to deck

        if (rb != null)
            rb.MovePosition(newPos);
        else
            transform.position = newPos;

        // rotate to face movement direction, upright
        Quaternion lookRot = Quaternion.LookRotation(dir, up);

        if (rb != null)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, 10f * Time.fixedDeltaTime));
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);

        if (animator && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, moveSpeed);
    }

    void KeepUpright(Vector3 up)
    {
        // keep forward flattened on deck
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up);
        if (fwd.sqrMagnitude < 0.0001f)
            fwd = Vector3.ProjectOnPlane(Vector3.forward, up);

        Quaternion uprightRot = Quaternion.LookRotation(fwd.normalized, up);

        if (rb != null)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, uprightRot, 10f * Time.fixedDeltaTime));
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, uprightRot, 10f * Time.deltaTime);
    }

    void SnapToGround(ref Vector3 position, Vector3 up)
    {
        // cast down, but skip hits on our own collider
        Vector3 origin = position + up * 1f;
        float remaining = groundSnapDistance;
        RaycastHit hit;

        while (remaining > 0f &&
               Physics.Raycast(origin, -up, out hit, remaining, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null &&
                hit.collider != col &&
                !hit.collider.transform.IsChildOf(transform))
            {
                // this is real ground
                position = hit.point + up * footHeight;
                return;
            }

            // we hit ourselves or a child – move origin past that and keep going
            float travelled = hit.distance + 0.01f;
            origin = hit.point - up * 0.01f;
            remaining -= travelled;
        }
    }
}
