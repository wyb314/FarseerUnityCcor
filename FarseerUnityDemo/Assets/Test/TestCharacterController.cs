using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCharacterController : MonoBehaviour {

	// Use this for initialization
	void Start ()
	{
	    CharacterController cc = this.GetComponent<CharacterController>();

        Rigidbody rigidbody = this.GetComponent<Rigidbody>();

        Debug.LogError("rigidbody is null -> " + (rigidbody == null));
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
