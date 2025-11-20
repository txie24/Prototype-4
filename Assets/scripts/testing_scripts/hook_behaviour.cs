using UnityEngine;

using System.Collections;


public class hook_behaviour : MonoBehaviour
{
    [SerializeField] public Transform hook_object;
    [SerializeField] public Transform hooked_object_transform;
    [SerializeField] private LineRenderer hookLine;
    [SerializeField] private Transform return_point;

    public Rigidbody rb_cache;
    public bool isLerping = false;

    void Start()
    {
        hookLine.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if(hooked_object_transform == null)
        {
            Debug.Log("trying to hook object");
            hooked_object_transform = other.transform;
            hooked_object_transform.parent = hook_object;
            other.attachedRigidbody.isKinematic = true;
            rb_cache = other.attachedRigidbody;
        }
    }

    public IEnumerator Hook_Shoot(Vector3 Destination, float hook_shot_duration)
    {
        hookLine.enabled = true;

        isLerping = true;
        float shot_time = 0;
        Vector3 hook_start_position = hook_object.position;
        while (shot_time < hook_shot_duration)
        {
            hookLine.SetPosition(0, return_point.position);
            hookLine.SetPosition(1, hook_object.position);
            hook_object.position = Vector3.Lerp(hook_start_position, Destination, shot_time / hook_shot_duration);
            if (hooked_object_transform != null)
            {
                break;
            }
            yield return new WaitForEndOfFrame();
            shot_time += Time.deltaTime;
        }
        StartCoroutine(Return_Hook(hook_shot_duration));
        yield return null;
    }

    public IEnumerator Return_Hook(float hook_shot_duration)
    {
        float shot_time = 0;
        Vector3 hook_start_position = hook_object.position;
        while (shot_time < hook_shot_duration)
        {
            hookLine.SetPosition(0, return_point.position);
            hookLine.SetPosition(1, hook_object.position);
            hook_object.position = Vector3.Lerp(hook_start_position, return_point.position, shot_time / hook_shot_duration);
            yield return new WaitForEndOfFrame();
            shot_time += Time.deltaTime;
        }
        
        isLerping = false;
        hook_object.position = return_point.position;
        hookLine.enabled = false;

        if(hooked_object_transform != null)
        {
            rb_cache.isKinematic = false;
            rb_cache = null;
            hooked_object_transform.parent = null;
            hooked_object_transform = null;
        }
        
        yield return null;
    }
}
