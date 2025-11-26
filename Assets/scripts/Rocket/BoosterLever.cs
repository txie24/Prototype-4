using UnityEngine;
using System.Collections;

public class RocketLever : MonoBehaviour
{
    [Header("Lever movement")]
    public Transform leverRoot;          // pivot that rotates (the parent of your cubes)
    public float offAngle = -20f;        // angle when lever is "off"
    public float onAngle  =  20f;        // angle when lever is "on"
    public float moveTime = 0.15f;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.F;
    public string playerTag = "Player";

    [Header("Rocket boosters")]
    public BoosterPivot leftBooster;     // left rocket arm
    public BoosterPivot rightBooster;    // right rocket arm

    public bool IsPlayerInRange { get; private set; }  // UI can read this

    bool isOn = false;
    bool isMoving = false;
    Coroutine moveRoutine;

    void Start()
    {
        if (leverRoot != null)
            SetLeverAngle(offAngle);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        IsPlayerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        IsPlayerInRange = false;
    }

    void Update()
    {
        if (!IsPlayerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            ToggleLever();
        }
    }

    void ToggleLever()
    {
        if (isMoving || leverRoot == null) return;

        isOn = !isOn;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(AnimateLever(isOn));

        // toggle both boosters
        if (leftBooster != null)
            leftBooster.ToggleBooster();
        if (rightBooster != null)
            rightBooster.ToggleBooster();
    }

    IEnumerator AnimateLever(bool turnOn)
    {
        isMoving = true;

        float start = leverRoot.localEulerAngles.x;
        if (start > 180f) start -= 360f;

        float target = turnOn ? onAngle : offAngle;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveTime;
            float k = Mathf.SmoothStep(0f, 1f, t);

            float angle = Mathf.Lerp(start, target, k);
            Vector3 e = leverRoot.localEulerAngles;
            e.x = angle;                       // change to y/z if needed
            leverRoot.localEulerAngles = e;

            yield return null;
        }

        SetLeverAngle(target);
        isMoving = false;
    }

    void SetLeverAngle(float angle)
    {
        Vector3 e = leverRoot.localEulerAngles;
        e.x = angle;                           // same axis as above
        leverRoot.localEulerAngles = e;
    }
}
