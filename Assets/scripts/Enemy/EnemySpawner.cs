using UnityEngine;
using UnityEngine.Animations;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject woodBoatPrefab;
    public GameObject piratePrefab;

    [Header("Settings")]
    public float spawnInterval = 5f;
    public float spawnRadiusMin = 30f;
    public float spawnRadiusMax = 60f;
    public int maxEnemies = 5;
    [Tooltip("How long (seconds) before the boat sinks/destroys itself to make room for new ones.")]
    public float boatLifetime = 30f;

    [Header("Global References (Scene Objects)")]
    [Tooltip("The pivot of the player's ship. Auto-detected if null.")]
    public Transform playerShipPivot;

    [Tooltip("Drag the Left Docking Point (GameObject) from the Player Ship here.")]
    public Transform dockLeft;
    [Tooltip("Drag the Right Docking Point (GameObject) from the Player Ship here.")]
    public Transform dockRight;

    [Header("Pirate Destinations")]
    [Tooltip("The 'Climb End' point on the deck.")]
    public Transform climbEndPoint;

    [Tooltip("The 'Small Stairs' transform (used for Slash Points and Walk Target).")]
    public Transform targetStairs;

    private float _timer;
    private List<GameObject> _activeEnemies = new List<GameObject>();

    void Start()
    {
        if (playerShipPivot == null && ShipController.Instance != null)
        {
            playerShipPivot = ShipController.Instance.transform;
        }

        if (dockLeft == null || dockRight == null)
        {
            Debug.LogWarning("EnemySpawner: Please assign Dock Left and Dock Right in the Inspector!");
        }
    }

    void Update()
    {
        if (playerShipPivot == null) return;

        // Cleanup destroyed enemies from list (This handles the Destroy(boat) logic automatically)
        _activeEnemies.RemoveAll(item => item == null);

        if (_activeEnemies.Count < maxEnemies)
        {
            _timer += Time.deltaTime;
            if (_timer >= spawnInterval)
            {
                SpawnEnemyUnit();
                _timer = 0f;
            }
        }
    }

    void SpawnEnemyUnit()
    {
        // 1. Calculate Position
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnOffset = new Vector3(randomDir.x, 0, randomDir.y) * Random.Range(spawnRadiusMin, spawnRadiusMax);
        Vector3 spawnPos = playerShipPivot.position + spawnOffset;
        spawnPos.y = 0;

        // 2. Spawn the Boat
        GameObject newBoat = Instantiate(woodBoatPrefab, spawnPos, Quaternion.LookRotation(-spawnOffset));

        // --- AUTO-DESTROY BOAT ---
        // This clears the boat after 30s, allowing the list count to drop so new enemies can spawn.
        Destroy(newBoat, boatLifetime);

        // Fix Boat References
        BoatFollower boatScript = newBoat.GetComponent<BoatFollower>();
        if (boatScript != null)
        {
            boatScript.playerBoat = playerShipPivot;

            // Pass the Left and Right dock points to the boat's array
            boatScript.dockingPoints = new Transform[] { dockLeft, dockRight };
        }

        // 3. Spawn the Pirate
        Vector3 piratePos = spawnPos + Vector3.up * 0.5f;
        GameObject newPirate = Instantiate(piratePrefab, piratePos, Quaternion.LookRotation(-spawnOffset));

        // 4. --- INJECT MISSING REFERENCES INTO PIRATE ---

        // A. Boarding Controller
        EnemyBoardingController boardingCtrl = newPirate.GetComponent<EnemyBoardingController>();
        if (boardingCtrl != null)
        {
            // FIX: Get the COMPONENT from the boat, not just the transform
            boardingCtrl.enemyBoat = newBoat.GetComponent<BoatFollower>();
            boardingCtrl.climbEndOnDeck = climbEndPoint;
        }

        // B. Boarding Attack
        EnemyBoardingAttack attackCtrl = newPirate.GetComponent<EnemyBoardingAttack>();
        if (attackCtrl != null)
        {
            // FIX: Get the ShipHealth COMPONENT from the player ship pivot
            attackCtrl.targetShip = playerShipPivot.GetComponent<ShipHealth>();

            // FIX: Wrap the single stair transform in a new Array
            attackCtrl.slashPoints = new Transform[] { targetStairs };
        }

        // C. Deck Walker
        EnemyDeckWalker walkerCtrl = newPirate.GetComponent<EnemyDeckWalker>();
        if (walkerCtrl != null)
        {
            walkerCtrl.walkTarget = targetStairs;
        }

        // 5. Setup Parent Constraint (Sticking to boat)
        SetupPirateConstraint(newPirate, newBoat.transform);

        // Track them
        _activeEnemies.Add(newBoat);
    }

    void SetupPirateConstraint(GameObject pirate, Transform boatTransform)
    {
        ParentConstraint constraint = pirate.GetComponent<ParentConstraint>();

        if (constraint == null) constraint = pirate.AddComponent<ParentConstraint>();

        List<ConstraintSource> sources = new List<ConstraintSource>();

        // Boat Source (Active)
        ConstraintSource boatSource = new ConstraintSource();
        boatSource.sourceTransform = boatTransform;
        boatSource.weight = 1.0f;
        sources.Add(boatSource);

        // Ship Source (Inactive)
        ConstraintSource shipSource = new ConstraintSource();
        shipSource.sourceTransform = playerShipPivot;
        shipSource.weight = 0.0f;
        sources.Add(shipSource);

        constraint.SetSources(sources);
        constraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;
        constraint.translationAxis = Axis.X | Axis.Y | Axis.Z;
        constraint.SetTranslationOffset(0, Vector3.zero);
        constraint.SetRotationOffset(0, Vector3.zero);
        constraint.constraintActive = true;
    }
}