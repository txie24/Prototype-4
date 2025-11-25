using UnityEngine;

public class MouseCamera : MonoBehaviour
{
    public Camera cam;
    public static MouseCamera instance;
    [SerializeField] private float sens = 200.0f;
    [SerializeField] private float x;
    [SerializeField] private float y;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }

    void Start()
    {
        cam = GetComponent<Camera>();

        Vector3 euler = transform.rotation.eulerAngles;
        x = euler.x;
        y = euler.y;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        const float yMin = -89.9f;
        const float yMax = 89.9f;

        x += Input.GetAxis("Mouse X") * (sens * Time.deltaTime);
        y -= Input.GetAxis("Mouse Y") * (sens * Time.deltaTime);
        y = Mathf.Clamp(y, yMin, yMax);

        transform.rotation = Quaternion.Euler(y, x, 0.0f);
    }
}
