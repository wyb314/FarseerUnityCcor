using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInputService
{

    bool GetMouseButtenDown(int code);

    bool GetKeyDown(KeyCode keyCode);

}
