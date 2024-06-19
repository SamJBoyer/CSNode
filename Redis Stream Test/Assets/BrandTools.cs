using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StackExchange.Redis;
using System.Threading.Tasks; // required for running the async xrange
using System.Linq;


//this script's purpose is to help decode values from redis streams using brand for easier novel use 
public class BrandTools
{
    public async Task GetStream(IDatabase db, string channelName)
    {
        Dictionary<string, string> ParseResult(StreamEntry entry) =>
        entry.Values.ToDictionary(x => x.Name.ToString(), x =>
        x.Value.ToString());

        var streamRangeTask = Task.Run(async () =>
        {
            var key = channelName;
            var result = await db.StreamRangeAsync(key, "-", "+", 1,
            Order.Descending);
            if (result.Any())
            {
                foreach (var entry in result)
                {
                    var dict = ParseResult(entry);
                    Debug.LogFormat("id: {0}", entry.Id);
                    foreach (var ele in dict)
                    {
                        Debug.LogFormat("key: {0}, value: {1}", ele.Key, ele.Value);
                    }
                }
            }
        });
        await Task.WhenAll(streamRangeTask);
    }
}