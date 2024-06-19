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

    public static Dictionary<string, Dictionary<string, object>> InstChannelData;
    public static Dictionary<string, Queue<Dictionary<string, object>>> BufferedChannelData;
    private List<Task> _tasks;

    public Hardpoint(RedisChannel[] channels, string overrideString = null) : base(overrideString)
    {
        InstChannelData = new Dictionary<string, Dictionary<string, object>>();
        BufferedChannelData = new Dictionary<string, Queue<Dictionary<string, object>>>();
        _tasks = new List<Task>();

        foreach (RedisChannel channel in channels)
        {
            if (channel.IsBuffer)
            {
                var channelQueue = new Queue<Dictionary<string, object>>();
                BufferedChannelData.Add(channel.Name, channelQueue);
                _tasks.Add(ReadStreamToBuffer(channel.Name));
            }
            else
            {
                var channelDict = new Dictionary<string, object>();
                InstChannelData.Add(channel.Name, channelDict);
                _tasks.Add(ReadStreamToValue(channel.Name));
            }
        }
        base.Run();
    }

    protected override async void Work()
    {
        await Task.WhenAll(_tasks);
    }

    private async Task WriteToStream(string key, string entryName, string entry){
        NameValueEntry newEntry = new NameValueEntry(entryName, entry);

        await _database.StreamAddAsync(key, new NameValueEntry[]{newEntry});
    }

    private async Task ReadStreamToBuffer(string key)
    {
        while (Application.isPlaying){ //should also check if redis is connected to anything 
            await Task.Run(async () =>
            {
                var result = await _database.StreamRangeAsync(key, "-", "+", 1,
                Order.Descending);

                if (result.Any()) //this will always be 1, unless 
                {
                    foreach (var entry in result)
                    {
                        BufferedChannelData[key].Enqueue(ParseToDict(entry));
                    }
                }
            });
        }
    }

    private async Task ReadStreamToValue(string key)
    {
        while (Application.isPlaying){ //might not have access to Application in the future. should parse command line args 
            await Task.Run(async () =>
            {
                var result = await _database.StreamRangeAsync(key, "-", "+", 1,
                Order.Descending);

                if (result.Any()) //this will always be 1, unless 
                {
                    foreach (var entry in result)
                    {
                        InstChannelData[key] = ParseToDict(entry);
                    }
                }
            });
        }
    }

Dictionary<string, object> ParseToDict (StreamEntry entry) => //hopefully will be able to parse to the respective type 
entry.Values.ToDictionary(x => x.Name.ToString(), x => (object) x.Value);

    Dictionary<string, string> ParseResult(StreamEntry entry) =>
entry.Values.ToDictionary(x => x.Name.ToString(), x =>
x.Value.ToString());

}
