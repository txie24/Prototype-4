using UnityEngine;
public class hook_gun_controller : MonoBehaviour
{
    [SerializeField] private Input_Manager IM;
    [SerializeField] private hook_behaviour HB;
    [SerializeField] private Camera mainCam;
    [SerializeField] private InputActionKey fire_key;
    [SerializeField] private float lerptime = 0.5f;
    

    [SerializeField] public float range = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HB = transform.GetComponentInChildren<hook_behaviour>();
        IM = transform.GetComponentInParent<Input_Manager>();
        mainCam = transform.GetComponent<Camera>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        fire_key = IM.GetAction("fire");
        if (fire_key.down && !HB.isLerping && HB.hooked_object_transform == null)
        {
            //Debug.Log("attempting to fire");
            HB.StartCoroutine(HB.Hook_Shoot(mainCam.transform.forward*range+mainCam.transform.position , lerptime));
        }else if (fire_key.down && !HB.isLerping)
        {
            HB.rb_cache.isKinematic = false;
            HB.rb_cache = null;
            HB.hooked_object_transform.parent = null;
            HB.hooked_object_transform = null;
        }
    }
}
