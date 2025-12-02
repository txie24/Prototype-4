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
    public float boatLifetime = 30f;

    [Header("Global References (Scene Objects)")]
    public Transform playerShipPivot;

    public Transform dockLeft;
    public Transform dockRight;

    [Header("Pirate Destinations")]
    public Transform climbEndPoint;   // where they climb onto deck

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

        // Cleanup destroyed enemies from list (Destroy(boat) will set them null)
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
        // Calculate Position around player ship
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 spawnOffset = new Vector3(randomDir.x, 0, randomDir.y) * Random.Range(spawnRadiusMin, spawnRadiusMax);
        Vector3 spawnPos = playerShipPivot.position + spawnOffset;
        spawnPos.y = 0;

        // Spawn the Boat
        GameObject newBoat = Instantiate(woodBoatPrefab, spawnPos, Quaternion.LookRotation(-spawnOffset));

        // This clears the boat after boatLifetime, allowing list count to drop
        Destroy(newBoat, boatLifetime);

        // BoatFollower setup
        BoatFollower boatScript = newBoat.GetComponent<BoatFollower>();
        if (boatScript != null)
        {
            boatScript.playerBoat = playerShipPivot;

            // Pass the dock points to the boat's array
            boatScript.dockingPoints = new Transform[] { dockLeft, dockRight };
        }

        // Spawn the Pirate
        Vector3 piratePos = spawnPos + Vector3.up * 0.5f;
        GameObject newPirate = Instantiate(piratePrefab, piratePos, Quaternion.LookRotation(-spawnOffset));

        // A. Boarding controller setup
        EnemyBoardingController boardingCtrl = newPirate.GetComponent<EnemyBoardingController>();
        if (boardingCtrl != null)
        {
            boardingCtrl.enemyBoat = newBoat.GetComponent<BoatFollower>();
            boardingCtrl.climbEndOnDeck = climbEndPoint;
        }

        // B. Boarding Attack
        EnemyBoardingAttack attackCtrl = newPirate.GetComponent<EnemyBoardingAttack>();
        if (attackCtrl != null)
        {
            // only set targetShip; slashPoints come from prefab/inspector
            ShipHealth shipHealth = playerShipPivot.GetComponent<ShipHealth>();
            if (shipHealth != null)
                attackCtrl.targetShip = shipHealth;
        }

        // C. Deck Walker
        EnemyDeckWalker walkerCtrl = newPirate.GetComponent<EnemyDeckWalker>();
        if (walkerCtrl != null)
        {
            // nothing to assign here; it will pick a random slashPoint itself in BeginWalk()
        }

        // Setup Parent Constraint (Sticking to boat)
        SetupPirateConstraint(newPirate, newBoat.transform);

        // Track boat so we can limit active count
        _activeEnemies.Add(newBoat);
    }

    void SetupPirateConstraint(GameObject pirate, Transform boatTransform)
    {
        ParentConstraint constraint = pirate.GetComponent<ParentConstraint>();

        if (constraint == null) constraint = pirate.AddComponent<ParentConstraint>();

        List<ConstraintSource> sources = new List<ConstraintSource>();

        ConstraintSource boatSource = new ConstraintSource();
        boatSource.sourceTransform = boatTransform;
        boatSource.weight = 1.0f;
        sources.Add(boatSource);

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
