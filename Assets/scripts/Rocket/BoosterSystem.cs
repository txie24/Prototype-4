using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoosterSystem : MonoBehaviour
{
    [Header("Core Connection")]
    public RocketLever rocketLever;
    public ShipDeckInventory deckInventory;
    public BoosterPivot[] boosters;
    public ShipController shipController;

    [Header("Fuel Configuration")]
    public Transform playerTransform;
    public Transform[] refuelPoints;
    public float interactDistance = 10f;
    public float refuelDuration = 10f;

    [Header("Fuel Tank and Boost Speed")]
    public float maxFuel = 100f;
    public float currentFuel = 0f;
    public float burnRate = 10f;
    public float boostSpeed = 20f;

    [Tooltip("Power Deday for 1.5s")]
    public float activationDelay = 1.5f;

    private float _normalSpeed;
    private float _deploymentTimer = 0f; 

    [Header("UI Reference")]
    public Slider hudFuelSlider;
    public GameObject promptCanvas;
    public TextMeshProUGUI statusText;

    [Header("Audio")]
    public AudioClip boostSound;
    [Range(0f, 1f)] public float soundVolume = 1.0f;
    private AudioSource _audioSource;

    private float _refuelTimer = 0f;

    void Start()
    {
        if (promptCanvas) promptCanvas.SetActive(false);

        if (!playerTransform)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) playerTransform = p.transform;
        }

        if (!shipController) shipController = FindFirstObjectByType<ShipController>();
        if (shipController) _normalSpeed = shipController.forwardSpeed;

        if (hudFuelSlider)
        {
            hudFuelSlider.maxValue = 1f;
            hudFuelSlider.value = currentFuel / maxFuel;
        }

        // Setup Audio Source
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.clip = boostSound;
        _audioSource.loop = true; // Important: Loop the sound while engine is on
        _audioSource.playOnAwake = false;
        _audioSource.volume = soundVolume;
    }

    void Update()
    {
        HandleRefuelLogic();
        HandleEngineLogic();

        if (hudFuelSlider)
        {
            hudFuelSlider.value = currentFuel / maxFuel;
        }
    }

    void HandleRefuelLogic()
    {
        if (!playerTransform || refuelPoints == null) return;

        bool near = false;
        foreach (var p in refuelPoints)
        {
            if (p && Vector3.Distance(playerTransform.position, p.position) <= interactDistance)
            {
                near = true; break;
            }
        }

        if (rocketLever && rocketLever.isOn)
        {
            if (promptCanvas) promptCanvas.SetActive(false);
            _refuelTimer = 0f;
            return;
        }

        if (promptCanvas) promptCanvas.SetActive(near);
        if (!near) { _refuelTimer = 0f; return; }

        int deckCans = deckInventory.FuelCount;

        if (currentFuel < maxFuel && deckCans > 0)
        {
            if (Input.GetKey(KeyCode.F))
            {
                _refuelTimer += Time.deltaTime;

                float remaining = refuelDuration - _refuelTimer;
                int displaySeconds = Mathf.CeilToInt(remaining);
                if (displaySeconds < 1) displaySeconds = 1;

                if (statusText) statusText.text = $"Refueling... {displaySeconds}";

                if (_refuelTimer >= refuelDuration)
                {
                    deckInventory.ConsumeOneFuel();
                    currentFuel = maxFuel;
                    _refuelTimer = 0f;
                    if (statusText) statusText.text = "Tank Full";
                }
            }
            else
            {
                _refuelTimer = 0f;
                if (statusText) statusText.text = $"Hold F to Refuel   (Cans: {deckCans})";
            }
        }
        else if (currentFuel >= maxFuel)
        {
            if (statusText) statusText.text = "Tank Full";
        }
        else
        {
            if (statusText) statusText.text = "No Fuel Cans";
        }
    }

    void HandleEngineLogic()
    {
        if (!rocketLever) return;
        bool isOn = rocketLever.isOn;
        if (isOn && currentFuel > 0)
        {
            _deploymentTimer += Time.deltaTime;
            if (_deploymentTimer >= activationDelay)
            {
                currentFuel -= burnRate * Time.deltaTime;
                SetThrust(true); 
                if (shipController) shipController.forwardSpeed = boostSpeed; 
            }
            else
            {
                SetThrust(false);
                if (shipController) shipController.forwardSpeed = _normalSpeed;
            }
            if (currentFuel <= 0)
            {
                currentFuel = 0;
                rocketLever.ForceOff();
                _deploymentTimer = 0f;
            }
        }
        else
        {
            _deploymentTimer = 0f;

            SetThrust(false);
            if (shipController) shipController.forwardSpeed = _normalSpeed; 

            if (isOn && currentFuel <= 0) rocketLever.ForceOff();
        }
    }

    void SetThrust(bool state)
    {
        foreach (var b in boosters) if (b) b.canThrust = state;

        // Handle Audio
        if (_audioSource && boostSound)
        {
            if (state)
            {
                if (!_audioSource.isPlaying) _audioSource.Play();
            }
            else
            {
                if (_audioSource.isPlaying) _audioSource.Stop();
            }
        }
    }
}