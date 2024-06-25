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
/// </summary>

public class Hardpoint : CSNode
{

    public static Dictionary<string, Dictionary<string, string>> InstChannelData;
    private List<Task> _readingTasks;

    public Hardpoint(string[] channels, string overrideString = null) : base(overrideString)
    {
        InstChannelData = new Dictionary<string, Dictionary<string, string>>();
        _readingTasks = new List<Task>();
        foreach (string channel in channels)
        {
            var channelDict = new Dictionary<string, string>();
            InstChannelData.Add(channel, channelDict);
            _readingTasks.Add(ReadStreamToValue(channel));
        }
        base.Run();
    }

    public Hardpoint(string overrideString = null) : base(overrideString)
    {
        InstChannelData = new Dictionary<string, Dictionary<string, string>>();
        _readingTasks = StartListenersFromGraph();
        base.Run();
    }

    private List<Task> StartListenersFromGraph()
    {
        var tasks = new List<Task>();
        if (_parameters.ContainsKey("input_streams"))
        {
            var inputStreamString = _parameters["input_streams"];
            try
            {
                var inputStreamArray = JsonConvert.DeserializeObject<string[]>(inputStreamString);
                foreach (string inputStream in inputStreamArray)
                {
                    tasks.Add(ReadStreamToValue(inputStream));
                }
            }
            catch
            {
                Debug.LogWarning("could not load streams from graph. please declare input streams manual");
            }
        }
        else
        {
            Debug.Log("no input streams declared in graph");
        }
        return tasks;
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

    public async Task WriteToStream(string key, string entryName, string entry)
    {
        NameValueEntry newEntry = new NameValueEntry(entryName, entry);
        await _database.StreamAddAsync(key, new NameValueEntry[] { newEntry });
    }


    private async Task ReadStreamToValue(string key)
    {
        while (Application.isPlaying && _currentState.Equals(Status.NODE_STARTED))
        { // Application is playing is basically a replacement for SIGINT
            await Task.Run(async () =>
            {
                var result = await _database.StreamRangeAsync(key, "-", "+", 1,
                Order.Descending);

                if (result.Any()) //this will always be 1, unless 
                {
                    foreach (var entry in result)
                    {
                        InstChannelData[key] = ParseResult(entry);
                    }
                }
            });
        }
    }


    Dictionary<string, string> ParseResult(StreamEntry entry) =>
entry.Values.ToDictionary(x => x.Name.ToString(), x =>
x.Value.ToString());

}
