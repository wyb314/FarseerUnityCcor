using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputService : IInputService
{
    public bool GetKeyDown(KeyCode keyCode)
    {
        return Input.GetKeyDown(keyCode);
    }

    public bool GetMouseButtenDown(int code)
    {
        return Input.GetMouseButtonDown(code);
    }
}
