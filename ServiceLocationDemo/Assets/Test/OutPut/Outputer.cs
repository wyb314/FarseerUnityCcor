using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Outputer : IOutputerService
{
    public void Print(string log)
    {
        Debug.Log(log);
    }

    public void Write(string log)
    {
        Debug.LogError(log);
    }
}
