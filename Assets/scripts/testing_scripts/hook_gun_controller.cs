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
        if (fire_key.down && !HB.isLerping)
        {
            //Debug.Log("attempting to fire");
            StartCoroutine(HB.Hook_Shoot(mainCam.transform.forward*range+mainCam.transform.position , 0.5f));
        }
    }
}
