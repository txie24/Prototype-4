using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Death Settings")]
    public float destroyDelay = 0f;

    [Header("boarding")]
    [Tooltip("if true, and this enemy has an EnemyBoardingController, its wooden boat is destroyed when this enemy dies so the spawner can make a new wave")]
    public bool destroyBoatOnDeath = true;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died!");

        // if this is a boarding pirate, kill its boat so the spawner can spawn again
        if (destroyBoatOnDeath)
        {
            var boarding = GetComponent<EnemyBoardingController>();
            if (boarding != null && boarding.enemyBoat != null)
            {
                Destroy(boarding.enemyBoat.gameObject);
            }
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // disable ai scripts here if you have any

        // remove from the world
        Destroy(gameObject, destroyDelay);
    }
}
