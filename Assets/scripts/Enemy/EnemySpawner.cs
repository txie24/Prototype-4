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
    public float boatLifetime = 20f;

    [Header("Global References")]
    public Transform playerShipPivot;

    private float _timer;
    private List<GameObject> _activeEnemies = new List<GameObject>();

    void Start()
    {
        if (playerShipPivot == null && ShipController.Instance != null)
        {
            playerShipPivot = ShipController.Instance.transform;
        }
    }

    void Update()
    {
        if (playerShipPivot == null) return;
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
        // safety checks
        if (woodBoatPrefab == null || piratePrefab == null)
        {
            Debug.LogWarning("EnemySpawner: prefabs missing.");
            return;
        }

        // position around player ship
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        if (randomDir.sqrMagnitude < 0.001f) randomDir = Vector2.right;

        float radius = Random.Range(spawnRadiusMin, spawnRadiusMax);
        Vector3 spawnOffset = new Vector3(randomDir.x, 0, randomDir.y) * radius;
        Vector3 spawnPos = playerShipPivot.position + spawnOffset;
        spawnPos.y = 0;

        // small boat faces toward the ship
        Quaternion boatRot = Quaternion.LookRotation(-spawnOffset, Vector3.up);

        // boat
        GameObject newBoat = Instantiate(woodBoatPrefab, spawnPos, boatRot);

        BoatFollower boatFollower = newBoat.GetComponent<BoatFollower>();
        if (boatFollower != null)
        {
            boatFollower.playerBoat = playerShipPivot;
        }

        // auto cleanup boat after lifetime
        if (boatLifetime > 0f)
            Destroy(newBoat, boatLifetime);

        // pirate
        Vector3 piratePos = spawnPos + Vector3.up * 0.5f;
        GameObject newPirate = Instantiate(piratePrefab, piratePos, boatRot);

        // boarding controller
        EnemyBoardingController boardingCtrl = newPirate.GetComponent<EnemyBoardingController>();
        if (boardingCtrl != null)
        {
            boardingCtrl.enemyBoat = newBoat.GetComponent<BoatFollower>();
        }

        // attack
        EnemyBoardingAttack attackCtrl = newPirate.GetComponent<EnemyBoardingAttack>();
        if (attackCtrl != null)
        {
            ShipHealth shipHealth = playerShipPivot.GetComponent<ShipHealth>();
            if (shipHealth != null)
                attackCtrl.targetShip = shipHealth;
        }

        // constraint setup
        SetupPirateConstraint(newPirate, newBoat.transform);

        _activeEnemies.Add(newBoat);
    }

    void SetupPirateConstraint(GameObject pirate, Transform boatTransform)
    {
        ParentConstraint constraint = pirate.GetComponent<ParentConstraint>();
        if (constraint == null) constraint = pirate.AddComponent<ParentConstraint>();

        List<ConstraintSource> sources = new List<ConstraintSource>();

        // source 0 = small boat
        ConstraintSource boatSource = new ConstraintSource();
        boatSource.sourceTransform = boatTransform;
        boatSource.weight = 1.0f;
        sources.Add(boatSource);

        // source 1 = player ship pivot
        if (playerShipPivot != null)
        {
            ConstraintSource shipSource = new ConstraintSource();
            shipSource.sourceTransform = playerShipPivot;
            shipSource.weight = 0.0f;
            sources.Add(shipSource);
        }

        constraint.SetSources(sources);
        constraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;
        constraint.translationAxis = Axis.X | Axis.Y | Axis.Z;

        for (int i = 0; i < constraint.sourceCount; i++)
        {
            constraint.SetTranslationOffset(i, Vector3.zero);
            constraint.SetRotationOffset(i, Vector3.zero);
        }

        constraint.constraintActive = true;
    }
}