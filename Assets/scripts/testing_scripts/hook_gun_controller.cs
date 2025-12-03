using UnityEngine;

public class hook_gun_controller : MonoBehaviour
{
    [SerializeField] private Input_Manager IM;
    [SerializeField] private hook_behaviour HB;
    [SerializeField] private Camera mainCam;
    [SerializeField] private InputActionKey fire_key;
    [SerializeField] private float lerptime = 0.5f;
    [SerializeField] public float range = 10f;

    void Start()
    {
        HB = transform.GetComponentInChildren<hook_behaviour>();
        IM = transform.GetComponentInParent<Input_Manager>();
        mainCam = transform.GetComponent<Camera>();
    }

    void LateUpdate()
    {
        fire_key = IM.GetAction("fire");

        if (fire_key.down)
        {
            if (HB.hooked_object_transform != null)
            {
                HB.DropHeldItem();
            }
            else if (!HB.isLerping)
            {
                HB.StartCoroutine(HB.Hook_Shoot(mainCam.transform.forward * range + mainCam.transform.position, lerptime));
            }
        }
    }
}