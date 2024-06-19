using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StackExchange.Redis;
using System.Threading.Tasks; // required for running the async xrange
using System.Linq;
using TMPro;

public class Test : MonoBehaviour
{

    public TextMeshProUGUI connectionText;

    public TextMeshProUGUI stateText;

    public TextMeshProUGUI dbText;


    public TextMeshProUGUI text;
    public TextMeshProUGUI allText;

    public TextMeshProUGUI[] argText;

    Dictionary<string, string> ParseResult(StreamEntry entry) =>
    entry.Values.ToDictionary(x => x.Name.ToString(), x =>
    x.Value.ToString());

    IDatabase db;

    private void Start()
    {
        connectionText.text = "trying to connect";

        //CSNode thisNode = new CSNode();
        CSNode thisNode = new CSNode("127.0.0.1:6379");
        db = thisNode.GetDatabase();

        try {
            connectionText.text = thisNode.ConnectionString;

        } catch {
            connectionText.text = "error";
        }

        try {
            stateText.text = thisNode.State.ToString();
        } catch {
            stateText.text = "error";
        }

        try {
            dbText.text = db.ToString();
        } catch {
            dbText.text = "error";
        }

        string[] args = thisNode.Args;
        if (args != null)
        {
            if (args.Length > 0)
            {
                argText[0].text = args[0];
                argText[1].text = args[1];
                argText[2].text = args[2];
                argText[3].text = args[3];
            }
        }

        string allArgs = string.Empty;
        int i = 0;
        try
        {
            foreach (string ar in args)
            {
                allArgs += i.ToString() + " " + ar + " ||| ";
                i++;
            }
        }
        catch
        {
            Debug.LogWarning("no args");
        }

        allText.text = allArgs;
        text.text = "string started";
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
            Debug.Log("made it through");
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
        /*text.text = ReadFromBuffer();

        var streamRangeTask = Task.Run(async () =>
        {
            string key = "test_stream";
            var result = await db.StreamRangeAsync(key, "-", "+", 1,
            Order.Descending);
            Debug.Log("made it through");
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
        await Task.WhenAll(streamRangeTask);
        text.text = ReadFromBuffer();*/
    }

    private string ReadFromBuffer()
    {
        return stringBuffer.Count > 0 ? stringBuffer.Dequeue() : string.Empty;
    }

}
