using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedisChannel
{
    private string _channelName;
    private bool _buffer; 
    private Type _type; 

    public RedisChannel(string channelName, bool buffer){
        _channelName = channelName;
        _buffer = buffer;
    }

    public string Name {
        get {return _channelName; }
    }

    public bool IsBuffer{
        get {return _buffer; }
    }

    public Type Type {
        get {return _type; } 
    }
}
