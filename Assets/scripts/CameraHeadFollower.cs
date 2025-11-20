
using UnityEngine;

public class CameraAnimFollow : MonoBehaviour
{
    [Header("Bones & Rig")]
    [Tooltip("Animated head (or upper spine) bone to follow.")]
    public Transform headBone;

    [Tooltip("The pivot that PlayerMove rotates (yaw+pitch).")]
    public Transform pitchPivot;

    [Tooltip("Optional: a child under pitchPivot that we rotate for subtle sway. If empty, we create one and parent the camera under it.")]
    public Transform swayRig;

    [Header("Position Follow")]
    [Tooltip("Head-space local offset for the view origin.")]
    public Vector3 headLocalOffset = new Vector3(0f, 0.04f, -0.02f);

    [Tooltip("Seconds to smooth position. 0 = snap.")]
    [Min(0)] public float positionSmoothTime = 0.08f;

    [Tooltip("Clamp vertical bob amplitude (meters). 0 = no clamp.")]
    [Min(0)] public float maxVerticalBob = 0.15f;

    [Header("Rotation Sway (small & comfy)")]
    [Tooltip("Enable a little rotation from the head animation.")]
    public bool enableSway = true;

    [Tooltip("How quickly the sway catches up. Larger = snappier.")]
    [Min(0)] public float swayResponsiveness = 8f;

    [Tooltip("Max degrees we allow from animation.")]
    [Range(0f, 20f)] public float maxYawSway = 4f;
    [Range(0f, 20f)] public float maxPitchSway = 2f;
    [Range(0f, 20f)] public float maxRollSway = 3f;

    Vector3 _vel;
    float _baseHeadY;
    bool  _initialized;
    Quaternion _currentSway = Quaternion.identity;

    void Start()
    {
        if (!pitchPivot)
        {
            var t = transform.Find("PitchPivot");
            if (t) pitchPivot = t;
        }

        if (pitchPivot && !swayRig)
        {
            var existing = pitchPivot.Find("ViewRig");
            swayRig = existing ? existing : new GameObject("ViewRig").transform;

            if (!existing) swayRig.SetParent(pitchPivot, false);
            if (Camera.main && Camera.main.transform.IsChildOf(pitchPivot) && Camera.main.transform.parent != swayRig)
                Camera.main.transform.SetParent(swayRig, true);
        }

        if (headBone) _baseHeadY = headBone.position.y;
    }

    void LateUpdate()
    {
        if (!headBone || !pitchPivot) return;

        if (!_initialized)
        {
            Vector3 snap = headBone.position + headBone.TransformVector(headLocalOffset);
            pitchPivot.position = snap;
            _initialized = true;
        }

        Vector3 targetPos = headBone.position + headBone.TransformVector(headLocalOffset);
        if (maxVerticalBob > 0f && _baseHeadY > 0f)
        {
            float dy = Mathf.Clamp(targetPos.y - _baseHeadY, -maxVerticalBob, maxVerticalBob);
            targetPos.y = _baseHeadY + dy;
        }
        pitchPivot.position = Vector3.SmoothDamp(pitchPivot.position, targetPos, ref _vel, positionSmoothTime);

        if (enableSway && swayRig)
        {
            Quaternion headRel = Quaternion.Inverse(pitchPivot.rotation) * headBone.rotation;
            Vector3 e = NormalizeEuler(headRel.eulerAngles);

            e.y = Mathf.Clamp(e.y, -maxYawSway,   maxYawSway);
            e.x = Mathf.Clamp(e.x, -maxPitchSway, maxPitchSway);
            e.z = Mathf.Clamp(e.z, -maxRollSway,  maxRollSway);

            Quaternion desired = Quaternion.Euler(e.x, e.y, e.z);

            float t = 1f - Mathf.Exp(-swayResponsiveness * Time.deltaTime);
            _currentSway = Quaternion.Slerp(_currentSway, desired, t);

            swayRig.localRotation = _currentSway;
        }
        else if (swayRig)
        {
            swayRig.localRotation = Quaternion.identity;
        }
    }

    static Vector3 NormalizeEuler(Vector3 e)
    {
        e.x = Mathf.DeltaAngle(0f, e.x);
        e.y = Mathf.DeltaAngle(0f, e.y);
        e.z = Mathf.DeltaAngle(0f, e.z);
        return e;
    }
}
