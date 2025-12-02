using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]

    public float attackRadius = 1.0f;

    public float attackDamage = 100f;

    public LayerMask enemyLayers;

    [Header("References")]
    [Tooltip("Optional: Assign an empty GameObject in front of the player as the attack center. If empty, uses Player position.")]
    public Transform attackPoint;

    [Header("Animation")]
    public string attackAnimTrigger = "Attack";
    private Animator _animator;

    // Timer to prevent spamming
    public float attackRate = 1f;
    private float nextAttackTime = 0f;

    void Update()
    {
        if (Time.time >= nextAttackTime)
        {
            // Check for Left Click (Works with both New Input System and Old)
            if (WasAttackPressed())
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    private bool WasAttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        // Use New Input System if enabled
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif

        // Fallback to Legacy Input
        return Input.GetMouseButtonDown(0);
    }

    void Attack()
    {
        // Shell for what the animation code would look like
        //if (_animator != null)
        //{
        //    _animator.SetTrigger(attackAnimTrigger);
        //}

        // Determine where the attack circle is
        Vector3 origin = attackPoint == null ? transform.position + transform.forward : attackPoint.position;

        // Detect enemies in range
        Collider[] hitEnemies = Physics.OverlapSphere(origin, attackRadius, enemyLayers);

        // Damage them
        foreach (Collider enemy in hitEnemies)
        {
            // Find the health component on the object or its parent
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null) health = enemy.GetComponentInParent<EnemyHealth>();

            if (health != null)
            {
                health.TakeDamage(attackDamage);
                return; // Only attack one enemy per click (more of a fix bc pirates have many colliders)
            }
        }
    }

    // Visualize the attack range in the Editor
    void OnDrawGizmosSelected()
    {
        Vector3 origin = attackPoint == null ? transform.position + transform.forward : attackPoint.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRadius);
    }
}