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
        RedisChannel channel1 = new RedisChannel(Channel1Name, false);
        RedisChannel channel2 = new RedisChannel(Channel2Name, false);
        new Hardpoint(new RedisChannel[]{channel1, channel2}, "127.0.0.1:6379");
    }

    void Update(){
        var dict = Hardpoint.InstChannelData[Channel1Name];
        foreach (var kvp in dict){
            print(kvp.Value);
        }


    }

}
