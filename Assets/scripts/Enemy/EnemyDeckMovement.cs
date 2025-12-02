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

    [Header("ground + upright")]
    public LayerMask groundMask = ~0;     // what counts as ground (Default etc)
    public float groundCheckDistance = 0.3f;
    public float groundCheckRadius = 0.25f;
    public float uprightLerpSpeed = 12f;

    public bool IsGrounded { get; private set; }

    Rigidbody rb;
    bool walking;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void BeginWalk()
    {
        walking = true;
    }

    void FixedUpdate()
    {
        Vector3 up = transform.parent != null ? transform.parent.up : Vector3.up;

        // --- ground check ---
        Vector3 checkOrigin = transform.position + up * 0.1f;
        float rayLength = groundCheckDistance + 0.1f;
        IsGrounded = Physics.SphereCast(
            checkOrigin,
            groundCheckRadius,
            -up,
            out _,
            rayLength,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        // --- movement towards target ---
        Vector3 moveDir = Vector3.zero;

        if (walking && walkTarget != null && IsGrounded)
        {
            Vector3 toTarget = walkTarget.position - transform.position;

            // flatten onto deck plane (so we don't walk "up" into the air)
            Vector3 toTargetFlat = Vector3.ProjectOnPlane(toTarget, up);
            float dist = toTargetFlat.magnitude;

            if (dist <= stopDistance)
            {
                walking = false;

                if (animator && !string.IsNullOrEmpty(speedParam))
                    animator.SetFloat(speedParam, 0f);

                if (attack != null)
                    attack.BeginAttack();
            }
            else
            {
                moveDir = toTargetFlat.normalized;

                Vector3 newPos = rb != null
                    ? rb.position + moveDir * moveSpeed * Time.fixedDeltaTime
                    : transform.position + moveDir * moveSpeed * Time.fixedDeltaTime;

                if (rb != null)
                    rb.MovePosition(newPos);
                else
                    transform.position = newPos;
            }
        }

        // --- rotation: keep upright + face move direction if there is one ---
        Vector3 desiredForward;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            desiredForward = moveDir;
        }
        else
        {
            // no movement? just keep whatever forward we have, but flattened on the deck
            desiredForward = Vector3.ProjectOnPlane(transform.forward, up);
            if (desiredForward.sqrMagnitude < 0.0001f)
                desiredForward = Vector3.ProjectOnPlane(Vector3.forward, up);
        }

        desiredForward.Normalize();
        Quaternion targetRot = Quaternion.LookRotation(desiredForward, up);

        if (rb != null)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, uprightLerpSpeed * Time.fixedDeltaTime));
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, uprightLerpSpeed * Time.deltaTime);

        // --- anim speed ---
        if (animator && !string.IsNullOrEmpty(speedParam))
        {
            float animSpeed = (walking && IsGrounded) ? moveSpeed : 0f;
            animator.SetFloat(speedParam, animSpeed);
        }
    }
}
