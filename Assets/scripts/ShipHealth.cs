using UnityEngine;
using UnityEngine.UI;

public class ShipHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Reference")]
    [Tooltip("Drag a UI Slider here to visualize health.")]
    public Slider healthBar;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        UpdateUI();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (healthBar != null)
        {
            // Update slider value (0 to 1)
            healthBar.value = currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        Debug.Log("The Ship has sunk!");
        // Add game over logic or sinking animation here
        enabled = false; // Disable health logic
    }
}