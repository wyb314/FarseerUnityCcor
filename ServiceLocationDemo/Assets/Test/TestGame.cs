using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGame : MonoBehaviour
{


    private SimpleGame _game;
	// Use this for initialization
	void Start ()
    {
		this._game = new SimpleGame();
	}
	
	// Update is called once per frame
	void Update ()
    {
        this._game.Update();
		
	}
}
