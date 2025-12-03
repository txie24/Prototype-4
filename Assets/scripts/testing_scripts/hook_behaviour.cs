using UnityEngine;
using System.Collections;
using Unity.Mathematics;

public class hook_behaviour : MonoBehaviour
{
    [SerializeField] public Transform hook_object;
    [SerializeField] public Transform hooked_object_transform;
    [SerializeField] private LineRenderer hookLine;
    [SerializeField] private Transform return_point;
    [SerializeField] private Vector3 spin_direction_hook;

    [Header("Gameplay")]
    public float healAmount = 20f;
    public float destroyDelay = 0.5f;

    public Vector2 xRotationLimits = new Vector2(-45f, 45f);
    public Vector2 yRotationLimits = new Vector2(-30f, 30f);

    public Rigidbody rb_cache;
    public bool isLerping = false;

    void Start()
    {
        hookLine.enabled = false;
    }

    public IEnumerator Hook_Shoot(Vector3 Destination, float hook_shot_duration)
    {
        hookLine.enabled = true;
        isLerping = true;
        float shot_time = 0;
        Vector3 hook_start_position = hook_object.position;
        while (shot_time < hook_shot_duration)
        {
            transform.LookAt(hook_object);
            hook_object.localEulerAngles += spin_direction_hook * Time.deltaTime;
            
            hookLine.SetPosition(0, return_point.position);
            hookLine.SetPosition(1, hook_object.position);
            hook_object.position = Vector3.Lerp(hook_start_position, Destination, shot_time / hook_shot_duration);
            if (hooked_object_transform != null) break;
            yield return new WaitForEndOfFrame();
            shot_time += Time.deltaTime;
        }
        hook_object.localEulerAngles = Vector3.zero;
        StartCoroutine(Return_Hook(hook_shot_duration));
        yield return null;
    }

    public IEnumerator Return_Hook(float hook_shot_duration)
    {
        float shot_time = 0;
        Vector3 hook_start_position = hook_object.position;
        while (shot_time < hook_shot_duration)
        {


            transform.LookAt(hook_object);
            
            hookLine.SetPosition(0, return_point.position);
            hookLine.SetPosition(1, hook_object.position);
            hook_object.position = Vector3.Lerp(hook_start_position, return_point.position, shot_time / hook_shot_duration);
            yield return new WaitForEndOfFrame();
            shot_time += Time.deltaTime;
        }
        transform.localEulerAngles = Vector3.zero;
        isLerping = false;
        hook_object.position = return_point.position;
        hookLine.enabled = false;

        if (hooked_object_transform != null)
        {

            if (hooked_object_transform.CompareTag("Fuel"))
            {
                hooked_object_transform.localPosition = Vector3.zero;
                Debug.Log("Holding Fuel. Press Fire (Right Click) to Drop.");
            }
            else
            {
                if (ShipController.Instance != null)
                {
                    ShipHealth health = ShipController.Instance.GetComponent<ShipHealth>();
                    if (health != null) health.Heal(healAmount);
                    Debug.Log("Item retrieved! Ship repaired.");
                }

                hooked_object_transform.parent = null;
                Destroy(hooked_object_transform.gameObject, destroyDelay);
                hooked_object_transform = null;
            }
        }
        yield return null;
    }

    public void DropHeldItem()
    {
        if (hooked_object_transform != null)
        {
            hooked_object_transform.parent = null;

            if (rb_cache != null)
            {
                rb_cache.isKinematic = false;
                rb_cache.useGravity = true;
                rb_cache = null;
            }
            else
            {
                Rigidbody rb = hooked_object_transform.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }
            Rigidbody finalRb = hooked_object_transform.GetComponent<Rigidbody>();
            if (finalRb != null) finalRb.AddForce(transform.forward * 2f, ForceMode.Impulse);

            hooked_object_transform = null;
        }
    }
    
    
}