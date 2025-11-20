using UnityEngine;

public class BoatSway : MonoBehaviour
{
    public float bobHeight = 0.25f;   
    public float bobSpeed = 1.0f;   

    public float rollAngle = 5f;     
    public float rollSpeed = 0.8f;

    public float pitchAngle = 2f;   
    public float pitchSpeed = 0.6f;

    Vector3 startLocalPos;
    Quaternion startLocalRot;
    float seed;

    void Start()
    {
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;
        seed = Random.value * 10f;   
    }

    void Update()
    {
        float t = Time.time + seed;

        float bob   = Mathf.Sin(t * bobSpeed)   * bobHeight;
        float roll  = Mathf.Sin(t * rollSpeed)  * rollAngle;
        float pitch = Mathf.Cos(t * pitchSpeed) * pitchAngle;

        transform.localPosition = startLocalPos + Vector3.up * bob;
        transform.localRotation = startLocalRot * Quaternion.Euler(pitch, 0f, roll);
    }
}
