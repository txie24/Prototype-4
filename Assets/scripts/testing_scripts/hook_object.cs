using UnityEngine;

public class hook_object : MonoBehaviour
{
    [SerializeField] private hook_behaviour HB;
    [SerializeField] private LayerMask hookableItems;

    void Start()
    {
        HB = transform.GetComponentInParent<hook_behaviour>();
    }

    void OnTriggerEnter(Collider other)
    {
        if(HB.hooked_object_transform == null && HB.isLerping)
        {

            if ((hookableItems.value & (1 << other.gameObject.layer)) != 0)
            {
                //Debug.Log("trying to hook object");
                HB.hooked_object_transform = other.transform;
                HB.hooked_object_transform.parent = HB.hook_object;
                other.attachedRigidbody.isKinematic = true;
                HB.rb_cache = other.attachedRigidbody;
            }
            else
            {
                HB.StopAllCoroutines();
                HB.StartCoroutine(
                    HB.Return_Hook(0.5f*Vector3.Distance(HB.transform.position, transform.position)/10)
                    );
            }
        }
    }
}
