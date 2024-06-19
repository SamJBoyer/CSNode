using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BasicTesting : MonoBehaviour
{
    public GameObject TestObj;
    public Material TestMat; 
    public TextMeshProUGUI AllArgs;
    public TextMeshProUGUI[] SplitFields; 

    private string[] _args; 

    private void Awake(){
        Console.WriteLine("starting game from script");
        CSNode csNode = new CSNode();
        _args = csNode.Args;
        //_args = new string[]{"a", "b", "c", "d"};
        //string outString = string.Empty;

        for (int i = 0; i < 4; i++) {
            SplitFields[i].text = _args[i];
        }

        //AllArgs.text = outString;
        
    }

    private void Update(){
        //Debug.Log("logging game through Debu");
        Console.WriteLine("logging game through console");

        if (Input.GetKeyDown(KeyCode.Escape )){
            Console.WriteLine("exiting application from script");
            Application.Quit();
        }
    }
}
