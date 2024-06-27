using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Channels;

/// <summary>
/// Author: Sam Boyer
/// Email: Sam.James.Boyer@gmail.com
/// 
/// This script extends the CSNode with additional features to make reading and writing to redis streams easier
/// 
/// </summary>

public class Hardpoint : CSNode
{

    public static Dictionary<string, Dictionary<string, string>> ChannelDataDict; //dictionary of all the channel data 
    private List<Task> _readingTasks;

    //adding an override string as an argument means the node will connect syncronously and should only be used when
    //debugging from the unity editor 
    public Hardpoint(string[] channels, string overrideString = null) : base(overrideString)
    {
        ChannelDataDict = new Dictionary<string, Dictionary<string, string>>();
        _readingTasks = new List<Task>();
        foreach (string channel in channels)
        {
            var channelDict = new Dictionary<string, string>();
            ChannelDataDict.Add(channel, channelDict);
            _readingTasks.Add(ReadFromStreamAsync(channel));
        }
        Debug.Log("starting hardpoint");
        base.Run();
    }

    //if no channels are given as an argument, automatically read from input_streams 
    public Hardpoint(string overrideString = null) : base(overrideString)
    {
        ChannelDataDict = new Dictionary<string, Dictionary<string, string>>();
        StartListenersFromGraph();
        Debug.Log("starting hardpoint");
        base.Run();
    }

    //add values from input_stream property of the parameters to the list of tasks to listen to 
    private void StartListenersFromGraph()
    {
        var tasks = new List<Task>();
        foreach (KeyValuePair<string, object> kvp in _parameters)
        {
            Debug.Log($"param key: {kvp.Key} | param value: {kvp.Value}");
        }

        if (_parameters.ContainsKey("input_streams"))
        {
            var inputStreamObj = _parameters["input_streams"];
            try
            {
                var inputStreamArray = JsonConvert.DeserializeObject<string[]>(inputStreamObj.ToString());
                foreach (string inputStream in inputStreamArray)
                {
                    Debug.Log($"adding {inputStream} to reading task list");
                    tasks.Add(ReadFromStreamAsync(inputStream));
                }
            }
            catch (Exception ex)
            {
                //Debug.LogWarning("could not load streams from graph. please declare input streams manually");
                Debug.LogWarning(ex);
            }
        }
        else
        {
            Debug.Log("no input streams declared in graph");
        }
        //return tasks;
        _readingTasks = tasks;
    }

    protected override async void Work()
    {
        try
        {
            await Task.WhenAll(_readingTasks);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
        }
    }

    //returns a task of writing a string to a stream 
    public async Task WriteToStream(string key, string entryName, string entry)
    {
        NameValueEntry newEntry = new NameValueEntry(entryName, entry);
        await _database.StreamAddAsync(key, new NameValueEntry[] { newEntry });
    }

    //returns the task of reading an entry from a channel in the database
    private async Task ReadFromStreamAsync(string channelName)
    {
        while (Application.isPlaying && _currentState.Equals(Status.NODE_READY))
        { // Application is playing is basically a replacement for SIGINT
            await Task.Run(async () =>
            {
                var result = await _database.StreamRangeAsync(channelName, "-", "+", 1,
                Order.Descending);

                if (result.Any()) //this will always be 1, unless 
                {
                    foreach (var entry in result)
                    {
                        ChannelDataDict[channelName] = ParseResult(entry);
                    }
                }
            });
        }
        Debug.LogWarning("hardpoint is stopping reading tasks");
    }


    Dictionary<string, string> ParseResult(StreamEntry entry) =>
entry.Values.ToDictionary(x => x.Name.ToString(), x =>
x.Value.ToString());

}
