using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LocalGravityBody : MonoBehaviour
{
    [Tooltip("Transform whose up-axis defines 'up' (use the enemy boat root).")]
    public Transform gravitySource;
    public float gravity = 9.81f;

    public bool alignToSurface = true;
    public float alignSpeed = 5f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        if (gravitySource == null)
            gravitySource = transform;
    }

    void FixedUpdate()
    {
        if (gravitySource == null) return;

        Vector3 down = -gravitySource.up;
        rb.AddForce(down * gravity, ForceMode.Acceleration);

        if (alignToSurface)
        {
            Quaternion current = rb.rotation;
            Quaternion target = Quaternion.FromToRotation(transform.up, gravitySource.up) * current;
            rb.MoveRotation(Quaternion.Slerp(
                current,
                target,
                alignSpeed * Time.fixedDeltaTime
            ));
        }
    }
}
