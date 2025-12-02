using System.Collections;
using UnityEngine;
using UnityEngine.Animations;

public class BoardingEnemy : MonoBehaviour
{
    [Header("Boat / Constraint")]
    public BoatFollower enemyBoat;              // small enemy boat
    public ParentConstraint parentConstraint;   // parent constraint on this npc
    public int smallBoatSourceIndex = 0;        // index of small boat source
    public int bigShipSourceIndex = 1;          // index of big ship source

    [Header("Hull / Climb Targets")]
    public Transform hullFollowTarget;          // StylShip_Body (must have a collider)
    public Transform climbTopPoint;             // point on deck (only Y used)
    public float moveToHullSpeed = 3f;
    public float climbSpeed = 3f;

    [Header("Hull Contact Settings")]
    public float hullRayHeightOffset = 1f;      // ray start height above npc
    public float hullSurfaceOffset = 0.05f;     // how far off the hull to sit
    public float hullRayMaxDistance = 30f;

    [Header("On-Deck Movement & Attacks")]
    public Transform[] slashPoints;
    public float walkSpeed = 2f;
    public float timeBetweenSlashes = 1.0f;

    [Header("Animations")]
    public Animator anim;
    public string walkBool = "IsWalking";
    public string climbTrigger = "Climb";
    public string slashTrigger = "Slash";

    private bool hasStartedBoarding = false;

    Rigidbody rb;
    bool storedUseGravity;
    bool storedKinematic;

    Collider hullCollider;
    Vector3 climbContactPoint;
    Vector3 climbContactNormal;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (hullFollowTarget != null)
            hullCollider = hullFollowTarget.GetComponent<Collider>();
    }

    void Update()
    {
        // start only when the small boat is actually docked / locked in
        if (!hasStartedBoarding &&
            enemyBoat != null &&
            enemyBoat.IsFullyDocked)
        {
            hasStartedBoarding = true;
            StartCoroutine(BoardAndAttackRoutine());
        }
    }

    IEnumerator BoardAndAttackRoutine()
    {
        // turn off constraint while we move manually
        if (parentConstraint != null)
            parentConstraint.constraintActive = false;

        // kill physics + gravity while climbing
        if (rb != null)
        {
            storedUseGravity = rb.useGravity;
            storedKinematic = rb.isKinematic;

            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (anim != null && !string.IsNullOrEmpty(climbTrigger))
            anim.SetTrigger(climbTrigger);

        // ------------------------------------------------
        // 1) Find the hull surface point (side of the ship)
        // ------------------------------------------------
        if (hullFollowTarget != null)
        {
            if (hullCollider == null)
                hullCollider = hullFollowTarget.GetComponent<Collider>();

            Vector3 rayOrigin = transform.position + Vector3.up * hullRayHeightOffset;
            Vector3 rayDir = hullFollowTarget.position - transform.position;
            if (rayDir.sqrMagnitude < 0.001f)
                rayDir = hullFollowTarget.forward;   // fallback
            rayDir.Normalize();

            Ray ray = new Ray(rayOrigin, rayDir);
            RaycastHit hit;

            bool hitHull = false;

            // try to hit JUST StylShip_Body first
            if (hullCollider != null && hullCollider.Raycast(ray, out hit, hullRayMaxDistance))
            {
                hitHull = true;
            }
            else if (Physics.Raycast(ray, out hit, hullRayMaxDistance))
            {
                // fallback: hit whatever is in front (hopefully the ship hull)
                hitHull = true;
            }

            if (hitHull)
            {
                climbContactPoint  = hit.point + hit.normal * hullSurfaceOffset;
                climbContactNormal = hit.normal;
            }
            else
            {
                // last-resort fallback: approximate using hull center
                climbContactPoint  = hullFollowTarget.position;
                climbContactNormal = -rayDir;
            }

            // ------------------------------------------------
            // 2) Move sideways until we're touching the hull side
            //    (follow outline, keep current Y)
            // ------------------------------------------------
            Vector3 sideTarget = new Vector3(
                climbContactPoint.x,
                transform.position.y,
                climbContactPoint.z
            );

            while (Vector3.Distance(transform.position, sideTarget) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    sideTarget,
                    moveToHullSpeed * Time.deltaTime
                );

                Vector3 flatDir = sideTarget - transform.position;
                flatDir.y = 0f;
                if (flatDir.sqrMagnitude > 0.001f)
                    transform.forward = flatDir.normalized;   // face along motion

                yield return null;
            }

            // ------------------------------------------------
            // 3) Climb straight up the side of the ship
            //    x/z fixed at hull surface, only Y moves up
            // ------------------------------------------------
            Vector3 topContact = new Vector3(
                climbContactPoint.x,
                climbTopPoint.position.y,   // deck height
                climbContactPoint.z
            );

            while (Vector3.Distance(transform.position, topContact) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    topContact,
                    climbSpeed * Time.deltaTime
                );

                // during climb, face the hull (or opposite, pick what looks better)
                Vector3 alongHull = Vector3.ProjectOnPlane(-climbContactNormal, Vector3.up);
                if (alongHull.sqrMagnitude > 0.001f)
                    transform.forward = alongHull.normalized;

                yield return null;
            }

            transform.position = topContact;  // snap onto deck edge
        }
        else
        {
            // no hullFollowTarget assigned – just go straight to top point
            while (Vector3.Distance(transform.position, climbTopPoint.position) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    climbTopPoint.position,
                    climbSpeed * Time.deltaTime
                );
                transform.forward = (climbTopPoint.position - transform.position).normalized;
                yield return null;
            }
        }

        // ------------------------------------------------
        // 4) Now on big ship → switch constraint
        // ------------------------------------------------
        if (parentConstraint != null)
        {
            ConstraintSource smallSrc = parentConstraint.GetSource(smallBoatSourceIndex);
            ConstraintSource bigSrc   = parentConstraint.GetSource(bigShipSourceIndex);

            smallSrc.weight = 0f;
            bigSrc.weight   = 1f;

            parentConstraint.SetSource(smallBoatSourceIndex, smallSrc);
            parentConstraint.SetSource(bigShipSourceIndex, bigSrc);

            parentConstraint.constraintActive = true;
        }

        // re-enable physics / gravity
        if (rb != null)
        {
            rb.isKinematic = storedKinematic;
            rb.useGravity  = storedUseGravity;
        }

        // no slash points = done
        if (slashPoints == null || slashPoints.Length == 0)
            yield break;

        // ------------------------------------------------
        // 5) Walk around and slash at random points
        // ------------------------------------------------
        while (true)
        {
            Transform target = slashPoints[Random.Range(0, slashPoints.Length)];
            Vector3 targetPos = new Vector3(
                target.position.x,
                transform.position.y,
                target.position.z
            );

            if (anim != null && !string.IsNullOrEmpty(walkBool))
                anim.SetBool(walkBool, true);

            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    walkSpeed * Time.deltaTime
                );
                transform.forward = (targetPos - transform.position).normalized;
                yield return null;
            }

            if (anim != null && !string.IsNullOrEmpty(walkBool))
                anim.SetBool(walkBool, false);

            if (anim != null && !string.IsNullOrEmpty(slashTrigger))
                anim.SetTrigger(slashTrigger);

            yield return new WaitForSeconds(timeBetweenSlashes);
        }
    }
}
