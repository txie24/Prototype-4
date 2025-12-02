using System.Collections;
using UnityEngine;

public class EnemyBoardingAttack : MonoBehaviour
{
    public ShipHealth targetShip;
    public float damagePerHit = 5f;
    public float minDelay = 0.5f;
    public float maxDelay = 1.5f;

    public Transform[] slashPoints;

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
            Transform hit = null;
            if (slashPoints != null && slashPoints.Length > 0)
                hit = slashPoints[Random.Range(0, slashPoints.Length)];

            if (hit != null)
            {
                Vector3 up = transform.parent != null ? transform.parent.up : Vector3.up;

                Vector3 toTarget = hit.position - transform.position;
                Vector3 flatDir = Vector3.ProjectOnPlane(toTarget, up);

                if (flatDir.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(flatDir.normalized, up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
                }
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
