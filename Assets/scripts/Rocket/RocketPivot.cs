using UnityEngine;
using System.Collections;

public class BoosterPivot : MonoBehaviour
{
    [Header("Ship root (same object that has ShipController)")]
    public Transform shipRoot;   // e.g. WaterBlock_50m

    [Header("Arm + Poses")]
    public Transform arm;           // LeftPivot / RightPivot (the moving arm)
    public Transform stowedPose;    // LeftRocketStow / RightRocketStow
    public Transform deployedPose;  // LeftRocketDeploy / RightRocketDeploy
    public float moveTime = 0.75f;

    [Header("Booster Settings")]
    public int boosterLevel = 2;
    public float baseThrust = 500f;
    public Rigidbody boatRb;        // ship rigidbody
    public ParticleSystem boosterFx;

    bool isDeployed;
    bool isMoving;
    Coroutine moveRoutine;

    // cached poses in SHIP-LOCAL space
    Vector3 stowedLocalPos;
    Quaternion stowedLocalRot;
    Vector3 deployedLocalPos;
    Quaternion deployedLocalRot;

    void Awake()
    {
        if (shipRoot == null)
            shipRoot = transform.root;   // fallback

        if (arm == null)
            arm = transform;

        // cache stow/deploy in shipRoot local space
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

        Vector3 startPos   = deploy ? stowedLocalPos   : deployedLocalPos;
        Quaternion startRot = deploy ? stowedLocalRot  : deployedLocalRot;
        Vector3 endPos     = deploy ? deployedLocalPos : stowedLocalPos;
        Quaternion endRot   = deploy ? deployedLocalRot: stowedLocalRot;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveTime;
            float k = Mathf.SmoothStep(0f, 1f, t);

            // interpolate in ship-local space
            Vector3 interpLocalPos = Vector3.Lerp(startPos, endPos, k);
            Quaternion interpLocalRot = Quaternion.Slerp(startRot, endRot, k);

            // then convert back to world, using the CURRENT ship transform
            arm.position = shipRoot.TransformPoint(interpLocalPos);
            arm.rotation = shipRoot.rotation * interpLocalRot;

            yield return null;
        }

        // snap to final
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
    }

    void FixedUpdate()
    {
        if (!isDeployed || boatRb == null) return;

        float thrust = baseThrust * boosterLevel;
        boatRb.AddForce(arm.forward * thrust, ForceMode.Force);
    }
}
