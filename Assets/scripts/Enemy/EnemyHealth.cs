using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Death Settings")]
    public float destroyDelay = 0f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        // Debug.Log($"{name} took {amount} damage. Current HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died!");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Disable the enemy AI script here (not sure which one it is tinyu)

        // Remove from the world
        Destroy(gameObject, destroyDelay);
    }
}