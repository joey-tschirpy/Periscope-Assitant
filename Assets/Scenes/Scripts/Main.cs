using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour {

	// Use this for initialization
	void Start () {
        //Screen.orientation = ScreenOrientation.LandscapeLeft;
        //Screen.autorotateToPortrait = false;
        //Screen.autorotateToPortraitUpsideDown = false;
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
	}
}
