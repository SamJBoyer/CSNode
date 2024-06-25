using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StackExchange.Redis;
using System.Threading.Tasks; // required for running the async xrange
using System.Linq;
using TMPro;

public class Test : MonoBehaviour
{

    Dictionary<string, string> ParseResult(StreamEntry entry) =>
    entry.Values.ToDictionary(x => x.Name.ToString(), x =>
    x.Value.ToString());

    IDatabase db;

    private void Start()
    {
        //CSNode thisNode = new CSNode();
        CSNode thisNode = new CSNode("127.0.0.1:6379");
        db = thisNode.GetDatabase();
    }

    Queue<string> stringBuffer = new Queue<string>();

    async void Update()
    {
        //print("recieving");

        //print("recieving");
        await Task.Run(async () =>
        {
            string key = "test_stream";
            var result = await db.StreamRangeAsync(key, "-", "+", 1,
            Order.Descending);
            if (result.Any()) //this will always be 1, unless 
            {

                foreach (var entry in result)
                {
                    var dict = ParseResult(entry);
                    Debug.LogFormat("id: {0}", entry.Id);
                    //Debug.Log(dict.Count);

                    foreach (var ele in dict)
                    {
                        //Debug.LogFormat("key: {0}, value: {1}", ele.Key, ele.Value);
                        stringBuffer.Enqueue(ele.Value.ToString());
                        Debug.Log(ele.Value);
                    }
                }
            }
        });
    }

    private string ReadFromBuffer()
    {
        return stringBuffer.Count > 0 ? stringBuffer.Dequeue() : string.Empty;
    }

}
