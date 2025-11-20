using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct InputActionKey
{
    public string actionName;
    public KeyCode code;
    public bool held;
    public bool down;
    public bool released;
    public InputActionKey(string Name, KeyCode keyCode)
    {
        this.actionName = Name;
        this.code = keyCode;
        this.held = false;
        this.down = false;
        this.released = false;
    }
}

public class Input_Manager : MonoBehaviour
{
    public Vector2 move_keys = Vector2.zero;
    public Vector2 player_mouse = Vector2.zero;
    [SerializeField] public List<InputActionKey> actions = new List<InputActionKey>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        move_keys.x = Input.GetAxisRaw("Horizontal");
        move_keys.y = Input.GetAxisRaw("Vertical");

        player_mouse.x = Input.GetAxisRaw("Mouse X");
        player_mouse.y = Input.GetAxisRaw("Mouse Y");

        for (int i = 0; i < actions.Count; i++)
        {
            InputActionKey iter = actions[i];
            iter.held = Input.GetKey(iter.code);
            iter.down = Input.GetKeyDown(iter.code);
            iter.released = Input.GetKeyUp(iter.code);
            actions[i] = iter;
            //Debug.Log(iter.actionName + " held: " + iter.held);
        }
    }
    
    public InputActionKey GetAction(string name)
    {
        return actions.Find(button => button.actionName == name);
    }
}
