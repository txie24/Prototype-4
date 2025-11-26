using UnityEngine;

public class InfiniteOcean : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTransform;

    [Header("Settings")]
    [Tooltip("Set this to the size of your water tiles if textures slide. Otherwise leave at 0.")]
    public float gridSize = 0f; 

    private float seaLevelY;

    void Start()
    {
        // Record the water's starting height (Y)
        seaLevelY = transform.position.y;

        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else Debug.LogWarning("InfiniteOcean: No Player found! Assign manually.");
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // Get player position
        float currentX = playerTransform.position.x;
        float currentZ = playerTransform.position.z;

        // Optional: Snap to grid to prevent texture sliding
        // (Useful if your water shader uses world coordinates)
        if (gridSize > 0)
        {
            currentX = Mathf.Round(currentX / gridSize) * gridSize;
            currentZ = Mathf.Round(currentZ / gridSize) * gridSize;
        }

        // Move the water to the player's X/Z, but keep strictly to Sea Level Y
        transform.position = new Vector3(currentX, seaLevelY, currentZ);
    }
}