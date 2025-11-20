using System;
using UnityEngine;

public class player : MonoBehaviour
{
    public static player instance;
    public CharacterController cc;
    private Vector3 moveVector;
    private Transform cameraTransform;
    private Vector3 horizontalVec;
    private Vector3 forwardVec;
    private float yAcceleration = 0.0f;
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float gravity = -10.0f;
    [SerializeField] private float jumpStrength = 10.0f;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Movement();
    }

    public void Movement()
    {
        // gravity stuff (jumping and falling)
        if (!cc.isGrounded)
        {
            yAcceleration += gravity * Time.deltaTime;
        }
        else
        {
            yAcceleration = 0;
        }

        if (cc.isGrounded)
        {
            if (Input.GetAxis("Jump") > 0)
            {
                yAcceleration = jumpStrength;
            }
        }

        cameraTransform = MouseCamera.instance.transform;
        horizontalVec = Input.GetAxis("Horizontal") * new Vector3(cameraTransform.right.x, 0, cameraTransform.right.z);
        forwardVec = Input.GetAxis("Vertical") * new Vector3(cameraTransform.forward.x + cameraTransform.up.x, 0, cameraTransform.forward.z + cameraTransform.up.z);
        moveVector = new Vector3(horizontalVec.x + forwardVec.x, 0, horizontalVec.z + forwardVec.z);
        if (moveVector.magnitude > 1)
        {
            moveVector = moveVector.normalized;
        }
        moveVector.y = yAcceleration;

        

        cc.Move(moveSpeed * Time.deltaTime * moveVector);
    }
}
