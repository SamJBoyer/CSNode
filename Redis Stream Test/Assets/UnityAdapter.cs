using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UnityAdapter : MonoBehaviour
{
    
    public string Channel1Name;
    public string Channel2Name; 

    void Start()
    {
        Debug.Log("starting adapter");

        new Hardpoint(new string[]{Channel1Name, Channel2Name}, "127.0.0.1:6379");
    }

    void Update(){
        var dict = Hardpoint.InstChannelData[Channel1Name];
        foreach (var kvp in dict){
            print(kvp.Value);
        }
    }
}
