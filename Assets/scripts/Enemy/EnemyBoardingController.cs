using UnityEngine;
using UnityEngine.Animations;

public class EnemyBoardingController : MonoBehaviour
{
    [Header("boat + constraint")]
    public BoatFollower enemyBoat;          // small boat with BoatFollower
    public ParentConstraint parentConstraint;
    public int woodBoatSourceIndex = 0;     // not really used now, but kept for clarity
    public int shipPivotSourceIndex = 1;    // index of ShipPivot in the ParentConstraint

    [Header("climb settings")]
    public Transform climbEndOnDeck;        // point ON THE DECK where he should end up
    public float climbHeightOffset = 2f;    // extra arc height
    public float climbDuration = 1.5f;

    [Header("anim (optional)")]
    public Animator animator;
    public string climbTriggerName = "Climb";

    [Header("next phase")]
    public EnemyDeckWalker deckWalker;

    bool startedBoarding;

    void Update()
    {
        if (startedBoarding) return;
        if (enemyBoat == null) return;

        // wait until the follower boat is actually docked
        if (!enemyBoat.IsFullyDocked) return;

        startedBoarding = true;
        StartCoroutine(BoardRoutine());
    }

    System.Collections.IEnumerator BoardRoutine()
    {
        if (animator && !string.IsNullOrEmpty(climbTriggerName))
            animator.SetTrigger(climbTriggerName);

        // turn off the constraint so it stops yanking him around
        if (parentConstraint != null)
            parentConstraint.constraintActive = false;

        if (climbEndOnDeck == null)
        {
            Debug.LogWarning("EnemyBoardingController: climbEndOnDeck is not assigned.");
            yield break;
        }

        Vector3 startPos = transform.position;
        Vector3 endPos   = climbEndOnDeck.position;

        float t   = 0f;
        float dur = Mathf.Max(0.01f, climbDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            // move horizontally towards deck point
            Vector3 horiz = Vector3.Lerp(
                new Vector3(startPos.x, 0f, startPos.z),
                new Vector3(endPos.x,   0f, endPos.z),
                eased
            );

            // make a simple "climb" arc upwards
            float y = Mathf.Lerp(startPos.y, endPos.y + climbHeightOffset, eased);

            transform.position = new Vector3(horiz.x, y, horiz.z);

            // face the deck point
            Vector3 dir = endPos - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                var look = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
            }

            yield return null;
        }

        // snap cleanly onto the deck point
        transform.position = endPos;

        // === IMPORTANT: attach him to the player boat so he moves with it ===
        Transform shipPivot = null;

        if (parentConstraint != null &&
            shipPivotSourceIndex >= 0 &&
            shipPivotSourceIndex < parentConstraint.sourceCount)
        {
            var src = parentConstraint.GetSource(shipPivotSourceIndex);
            shipPivot = src.sourceTransform;
        }

        if (shipPivot != null)
        {
            // parent while keeping world position -> no teleport
            transform.SetParent(shipPivot, true);
        }

        // now he’s a child of the player boat, so if the boat moves, he stays on it
        if (deckWalker != null)
            deckWalker.BeginWalk();
    }
}
