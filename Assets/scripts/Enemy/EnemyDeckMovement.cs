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
    public EnemyBoardingAttack attack;   // holds the slashPoints

    [Header("ground snapping")]
    public LayerMask groundMask = ~0;    // layers that count as deck
    public float groundSnapDistance = 5f;
    public float footHeight = 0.02f;     // how high above the deck the feet sit

    Rigidbody rb;
    Collider col;

    Transform currentTarget;             // chosen slash point
    bool walking;
    bool lockedIn;                       // true once we've reached the attack spot

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // we don't want physics to move this guy on the deck
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    // called by EnemyBoardingController after climbing onto the ship
    public void BeginWalk()
    {
        lockedIn = false;

        if (attack == null || attack.slashPoints == null || attack.slashPoints.Length == 0)
        {
            Debug.LogWarning("EnemyDeckWalker.BeginWalk: no slash points set on EnemyBoardingAttack.");
            return;
        }

        // pick one random slash point as the move target for this boarding
        currentTarget = attack.slashPoints[Random.Range(0, attack.slashPoints.Length)];
        walking = true;
    }

    void Update()
    {
        Vector3 up = transform.parent != null ? transform.parent.up : Vector3.up;

        // always keep feet snapped to the deck
        Vector3 pos = transform.position;
        SnapToGround(ref pos, up);
        transform.position = pos;

        // once we are locked in, never try to move again – just keep the body upright
        if (lockedIn)
        {
            KeepUpright(up);

            if (animator && !string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, 0f);

            return;
        }

        if (!walking || currentTarget == null)
        {
            KeepUpright(up);
            if (animator && !string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, 0f);
            return;
        }

        // move towards chosen slash point on the deck plane
        Vector3 toTarget = currentTarget.position - transform.position;
        Vector3 flat = Vector3.ProjectOnPlane(toTarget, up);
        float dist = flat.magnitude;

        if (dist <= stopDistance)
        {
            // reached attack position: snap, lock, and tell the attack script to start hitting the ship
            walking = false;
            lockedIn = true;

            // small snap in case we stopped just short of the slash point
            Vector3 lockPos = currentTarget.position;
            SnapToGround(ref lockPos, up);
            transform.position = lockPos;

            KeepUpright(up);

            if (animator && !string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, 0f);

            if (attack != null)
                attack.BeginAttack();

            return;
        }

        Vector3 dir = flat.normalized;

        // smooth movement
        transform.position += dir * moveSpeed * Time.deltaTime;

        // smooth rotation: face movement direction, upright relative to deck
        Quaternion lookRot = Quaternion.LookRotation(dir, up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 12f * Time.deltaTime);

        if (animator && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, moveSpeed);
    }

    void KeepUpright(Vector3 up)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up);
        if (fwd.sqrMagnitude < 0.0001f)
            fwd = Vector3.ProjectOnPlane(Vector3.forward, up);

        Quaternion uprightRot = Quaternion.LookRotation(fwd.normalized, up);
        transform.rotation = Quaternion.Slerp(transform.rotation, uprightRot, 12f * Time.deltaTime);
    }

    void SnapToGround(ref Vector3 position, Vector3 up)
    {
        // raycast down, ignoring our own collider / children
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
                position = hit.point + up * footHeight;
                return;
            }

            float travelled = hit.distance + 0.01f;
            origin = hit.point - up * 0.01f;
            remaining -= travelled;
        }
    }
}
