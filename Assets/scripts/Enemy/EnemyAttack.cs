using System.Collections;
using UnityEngine;

public class EnemyBoardingAttack : MonoBehaviour
{
    [Header("Target")]
    public ShipHealth targetShip;
    public float damagePerHit = 5f;
    public float minDelay = 0.5f;
    public float maxDelay = 1.5f;

    [Header("Slash Points")]
    [Tooltip("Points on the player ship the pirate will aim at. Optional, only used for facing/aim.")]
    public Transform[] slashPoints;

    [Header("Animation")]
    public Animator animator;
    public string attackTriggerName = "Slash";

    bool attacking;

    void OnDisable()
    {
        // stop coroutine if this object is disabled/destroyed
        attacking = false;
    }

    public void BeginAttack()
    {
        if (attacking) return;

        // make sure we actually have a ShipHealth to damage
        if (targetShip == null)
        {
            // preferred: use the ShipController's reference if it exists
            if (ShipController.Instance != null && ShipController.Instance.shipHealth != null)
            {
                targetShip = ShipController.Instance.shipHealth;
            }
            else
            {
                // fallback: just grab any ShipHealth in the scene
                targetShip = FindObjectOfType<ShipHealth>();
            }

            if (targetShip == null)
            {
                Debug.LogWarning($"EnemyBoardingAttack on {name}: no ShipHealth found, attacks will do nothing.");
            }
        }

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
            // optional: rotate towards a random slash point so they look like they’re swinging at the ship
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

            // play attack animation
            if (animator && !string.IsNullOrEmpty(attackTriggerName))
                animator.SetTrigger(attackTriggerName);

            // actually damage the ship
            if (targetShip != null)
            {
                targetShip.TakeDamage(damagePerHit);
            }

            float wait = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(wait);
        }
    }
}
