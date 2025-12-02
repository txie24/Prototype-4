using System.Collections;
using UnityEngine;

public class EnemyBoardingAttack : MonoBehaviour
{
    public ShipHealth targetShip;        // the big ship's health script
    public float damagePerHit = 5f;
    public float minDelay = 0.5f;
    public float maxDelay = 1.5f;

    public Transform[] slashPoints;      // empties on the boarded ship

    public Animator animator;
    public string attackTriggerName = "Slash";

    bool attacking;

    public void BeginAttack()
    {
        if (attacking) return;
        attacking = true;
        StartCoroutine(AttackLoop());
    }

    public void StopAttack()
    {
        attacking = false;
    }

    IEnumerator AttackLoop()
    {
        while (attacking)
        {
            // pick a random point on the boarded boat
            Transform hit = null;
            if (slashPoints != null && slashPoints.Length > 0)
            {
                hit = slashPoints[Random.Range(0, slashPoints.Length)];
            }

            if (hit != null)
            {
                Vector3 targetPos = hit.position;
                targetPos.y = transform.position.y;

                Vector3 dir = targetPos - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            if (animator && !string.IsNullOrEmpty(attackTriggerName))
                animator.SetTrigger(attackTriggerName);

            if (targetShip != null)
                targetShip.TakeDamage(damagePerHit);

            float wait = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(wait);
        }
    }
}
