using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;


public class TestTextSpeech : MonoBehaviour {

    public TextToSpeechManager myTTS;
    public GameObject cube;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
            

    }

    public void OnClicTest()
    {
        myTTS.SpeakText("Hello i am the robot i speak english. Je parle pas la france.");
        cube.GetComponent<Renderer>().material.color = Color.yellow;
    }
}
