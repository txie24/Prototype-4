using UnityEngine;
using System.Collections;

public class BoosterPivot : MonoBehaviour
{
    [Header("Ship root (same object that has ShipController)")]
    public Transform shipRoot;

    [Header("Arm + Poses")]
    public Transform arm;
    public Transform stowedPose;
    public Transform deployedPose;
    public float moveTime = 0.75f;

    [Header("Booster Settings")]
    public int boosterLevel = 2;
    public float baseThrust = 500f;
    public Rigidbody boatRb;
    public ParticleSystem boosterFx;

    [HideInInspector]
    public bool canThrust = false;

    bool isDeployed;
    bool isMoving;
    Coroutine moveRoutine;

    Vector3 stowedLocalPos;
    Quaternion stowedLocalRot;
    Vector3 deployedLocalPos;
    Quaternion deployedLocalRot;

    void Awake()
    {
        if (shipRoot == null)
            shipRoot = transform.root;

        if (arm == null)
            arm = transform;

        stowedLocalPos = shipRoot.InverseTransformPoint(stowedPose.position);
        stowedLocalRot = Quaternion.Inverse(shipRoot.rotation) * stowedPose.rotation;

        deployedLocalPos = shipRoot.InverseTransformPoint(deployedPose.position);
        deployedLocalRot = Quaternion.Inverse(shipRoot.rotation) * deployedPose.rotation;
    }

    public void ToggleBooster()
    {
        if (isMoving) return;

        isDeployed = !isDeployed;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveArm(isDeployed));
    }

    IEnumerator MoveArm(bool deploy)
    {
        isMoving = true;

        Vector3 startPos = deploy ? stowedLocalPos : deployedLocalPos;
        Quaternion startRot = deploy ? stowedLocalRot : deployedLocalRot;
        Vector3 endPos = deploy ? deployedLocalPos : stowedLocalPos;
        Quaternion endRot = deploy ? deployedLocalRot : stowedLocalRot;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveTime;
            float k = Mathf.SmoothStep(0f, 1f, t);

            Vector3 interpLocalPos = Vector3.Lerp(startPos, endPos, k);
            Quaternion interpLocalRot = Quaternion.Slerp(startRot, endRot, k);

            arm.position = shipRoot.TransformPoint(interpLocalPos);
            arm.rotation = shipRoot.rotation * interpLocalRot;

            yield return null;
        }

        arm.position = shipRoot.TransformPoint(endPos);
        arm.rotation = shipRoot.rotation * endRot;

        isMoving = false;

        if (deploy) ActivateBooster();
        else DeactivateBooster();
    }

    void ActivateBooster()
    {
        if (boosterFx != null)
            boosterFx.Play();
    }

    void DeactivateBooster()
    {
        if (boosterFx != null)
            boosterFx.Stop();

        canThrust = false;
    }

    void FixedUpdate()
    {
        if (!isDeployed || boatRb == null || !canThrust) return;

        float thrust = baseThrust * boosterLevel;
        boatRb.AddForce(arm.forward * thrust, ForceMode.Force);
    }
}