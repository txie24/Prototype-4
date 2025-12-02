using UnityEngine;
using System.Collections.Generic;

public class IcebergSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject[] icebergPrefabs;
    public Transform playerTransform;

    [Header("Spawning Settings")]
    public int icebergCount = 20;

    [Tooltip("The Y position (height) where icebergs float.")]
    public float oceanLevelY = 0f;

    [Header("Initial Spawn (Start)")]
    public float initialMinDistance = 40f;
    public float initialMaxDistance = 100f;

    [Header("Respawn (Runtime)")]
    public float respawnMinDistance = 120f;
    public float respawnMaxDistance = 160f;
    public bool spawnOppositeSide = true;

    public float despawnDistance = 180f;

    // Internal list to track our active icebergs
    private List<GameObject> _spawnedIcebergs = new List<GameObject>();

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else
            {
                Debug.LogError("IcebergSpawner: No Player found! Please assign in Inspector.");
                enabled = false;
                return;
            }
        }

        if (icebergPrefabs.Length == 0)
        {
            Debug.LogWarning("IcebergSpawner: No prefabs assigned! Drag 'Iceberg' and 'Iceberg2' into the list.");
            return;
        }

        for (int i = 0; i < icebergCount; i++)
        {
            SpawnIceberg(initialMinDistance, initialMaxDistance, false);
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        for (int i = 0; i < _spawnedIcebergs.Count; i++)
        {
            GameObject iceberg = _spawnedIcebergs[i];
            // Checking if iceberg should despawn
            float distance = Vector3.Distance(
                new Vector3(playerTransform.position.x, 0, playerTransform.position.z),
                new Vector3(iceberg.transform.position.x, 0, iceberg.transform.position.z)
            );

            if (distance > despawnDistance)
            {
                RepositionIceberg(iceberg, respawnMinDistance, respawnMaxDistance, spawnOppositeSide);
            }
        }
    }

    void SpawnIceberg(float min, float max, bool spawnOpposite)
    {
        GameObject prefabToSpawn = icebergPrefabs[Random.Range(0, icebergPrefabs.Length)];

        GameObject newIceberg = Instantiate(prefabToSpawn);

        RepositionIceberg(newIceberg, min, max, spawnOpposite);

        newIceberg.transform.parent = transform;

        _spawnedIcebergs.Add(newIceberg);
    }

    void RepositionIceberg(GameObject iceberg, float min, float max, bool useOppositeSide)
    {
        Vector3 direction;

        // Spawn on opposite side of where it despawned (usually farther infront)
        if (useOppositeSide)
        {
            Vector3 relativeDir = playerTransform.position - iceberg.transform.position;
            direction = new Vector3(relativeDir.x, 0, relativeDir.z).normalized;
        }
        else
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            direction = new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        float distance = Random.Range(min, max);

        Vector3 newPos = playerTransform.position + (direction * distance);
        newPos.y = oceanLevelY;

        iceberg.transform.position = newPos;

        iceberg.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

        float randomScale = Random.Range(15f, 30f);
        iceberg.transform.localScale = Vector3.one * randomScale;
    }
}