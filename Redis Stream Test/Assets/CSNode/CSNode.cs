using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Author: Sam Boyer
/// Sam.James.Boyer@gmail.com 
/// 
/// Things to change:
///     - sdoes not catch SIGINT for a graceful shutdown. catching SIGINT seems very complicated and not worth
/// </summary>

public class CSNode
{
    //indicates the status of the current node. This concept is not yet implemented anywhere in BRAND, 
    //to my knowledge, and is largely irrelevant 
    protected enum Status
    {
        NODE_STARTED,
        NODE_READY,
        NODE_SHUTDOWN,
        NODE_FATAL_ERROR,
        NODE_WARNING,
        NODE_INFO
    }


    protected string _serverSocket; //the socket (path) from command line. currently unimplemented
    protected string _nickname; //nickname of this node recieved from command line 
    protected string _serverIP; //redis IP from command line
    protected string _serverPort; //redis port from command line
    protected Status _currentState;
    protected ConnectionMultiplexer _redis; //redis multiplexer. do not dispose, this object is expensive
    protected IDatabase _database; //data base for reading and writing to redis.
    protected Dictionary<string, string> _parameters; //this nodes parameters, as read from the supergraph
    protected string _stateKeyString; //string indicating this nodes key for itself in the graph_status. will soon have lowered access 

    //if run open, declare CSNode with an override string to manually connect the redis stream synchronously
    public CSNode(string overrideString = null)
    {
        _currentState = Status.NODE_READY;
        _parameters = new Dictionary<string, string>();
        if (overrideString != null) //command string is inputed, and this node is being run from unity editor 
        {
            ConnectFromUnity(overrideString); //connect syncronously. this only works if run open
        }
        else //if no override input, and this node is being run from BRAND and is concidered to be "running closed" 
        {
            string[] args = Environment.GetCommandLineArgs();
            HandleArgs(args); //parses the args into the flags 
            if (_serverSocket != null || (_serverIP != null && _serverPort != null)) {
                ConnectFromBRAND(_serverIP, _serverPort).Wait(); //connect async. currently doesnt handle sockets 
                _stateKeyString = _nickname + "_state";
                _database.StreamAdd(_stateKeyString, new NameValueEntry[] { new NameValueEntry("status", _currentState.ToString()) });
            } else {
                Debug.LogWarning("insufficient arguments have been passed. node must shut down");
            }
        }
    }

    //interprets the incoming command line arguments by their flags
    private void HandleArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-s":
                    _serverSocket = args[i + 1];
                    break;
                case "-n":
                    _nickname = args[i + 1];
                    break;
                case "i":
                    _serverIP = args[i + 1];
                    break;
                case "p":
                    _serverPort = args[i + 1];
                    break;
            }
        }
    }

    //gets a dictionary of paramters from the supergraph stream 
    private async void ParseGraphParameters()
    {
        _database = _redis.GetDatabase();
        var streamRangeTask = Task.Run(async () =>
        {
            var key = "supergraph_stream"; //at risk due to hardcode 
            var result = await _database.StreamRangeAsync(key, "-", "+", 1,
            Order.Descending);
            if (result.Any())
            {
                //painful extraction of the parameters from the SUPER jagged json string 
                string masterJsonString = result[0].Values[0].Value.ToString();
                JObject jobject = JObject.Parse(masterJsonString);
                Dictionary<string, object> dict = jobject.ToObject<Dictionary<string, object>>();
                var nodesString = dict["nodes"].ToString();
                var nodesDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(nodesString);
                if (nodesDict.ContainsKey(_nickname))
                {
                    var graphDict = nodesDict[_nickname];
                    _parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(graphDict["parameters"].ToString());
                }
                else
                {
                    Debug.LogError("could not find this nodes parameters in the super graph");
                }
            }
        });
        await Task.WhenAll(streamRangeTask);
    }


    private async Task ConnectFromBRAND(string serverIP, string serverPort, string serverSocket = null)
    {
        string connectionString = serverSocket != null ? serverSocket : $"{serverIP}:{serverPort}"; //prioritize server socket 
        Debug.Log(connectionString);

        var options = new ConfigurationOptions //can add more custom options as the need arises 
        {
            EndPoints = { connectionString }
        };

        try
        {
            Debug.Log($"attempting connection from BRAND to {connectionString}");
            _redis = await ConnectionMultiplexer.ConnectAsync(options);
            _database = _redis.GetDatabase();
            ParseGraphParameters();
            _currentState = Status.NODE_STARTED;
        }
        catch (Exception ex)
        {
            Debug.LogError($"A connection error occurred: {ex.Message}");
            _currentState = Status.NODE_FATAL_ERROR;
        }
    }

    //used to connect to redis synchronously. used when running the node from editor
    private void ConnectFromUnity(string connectionString)
    {
        //connect using connection string 
        try
        {
            Debug.Log($"attempting open connection from unity to {connectionString}");
            ConnectionMultiplexer _redis = ConnectionMultiplexer.Connect(connectionString);
            _database = _redis.GetDatabase();
            _currentState = Status.NODE_STARTED;
        }
        catch (Exception ex)
        {
            Debug.LogError($"A connection error occured: {ex}");
            _currentState = Status.NODE_FATAL_ERROR;
        }
    }

    protected void Run()
    {
        Work();
        UpdateParameters();
    }

    protected virtual void UpdateParameters() { }

    protected virtual void Work() { }


    public string State
    {
        get { return _currentState.ToString(); }
    }

    //return database. this will 
    public IDatabase GetDatabase()
    {
        return _database;
    }
}
