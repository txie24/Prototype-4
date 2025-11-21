using UnityEngine;

public class hook_object : MonoBehaviour
{
    [SerializeField] private hook_behaviour HB;
    void Start()
    {
        HB = transform.GetComponentInParent<hook_behaviour>();
    }

    void OnTriggerEnter(Collider other)
    {
        if(HB.hooked_object_transform == null && HB.isLerping)
        {
            //Debug.Log("trying to hook object");
            HB.hooked_object_transform = other.transform;
            HB.hooked_object_transform.parent = HB.hook_object;
            other.attachedRigidbody.isKinematic = true;
            HB.rb_cache = other.attachedRigidbody;
        }
    }
}
