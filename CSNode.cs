using System.Collections.Generic;
using UnityEngine;
using System;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.Experimental.GraphView;

/// <summary>
/// Author: Sam Boyer
/// Sam.James.Boyer@gmail.com 
/// 
/// This code is a c# implementation of a BRAND node described in 
/// https://github.com/brandbci/brand
/// 
/// Things to change:
///     - sdoes not catch SIGINT for a graceful shutdown. catching SIGINT seems very complicated and not worth
///     - does not allow access through socket 
/// </summary>

public class CSNode
{
    //indicates the status of the current node. This concept is not yet implemented anywhere in BRAND, 
    //to my knowledge, and is largely irrelevant 
    protected enum Status
    {
        NODE_STARTED, //state upon initialization
        NODE_READY, //state after successful connection
        NODE_SHUTDOWN, //never used due to no handling of sigint
        NODE_FATAL_ERROR, //used when connection failure occurs 
        NODE_WARNING, //unused and optional
        NODE_INFO //unused and optional
    }


    protected string _serverSocket; //the socket (path) from command line. currently unimplemented
    protected string _nickname; //nickname of this node recieved from command line 
    protected string _serverIP; //redis IP from command line
    protected string _serverPort; //redis port from command line
    protected Status _currentState;
    protected ConnectionMultiplexer _redis; //redis multiplexer. do not dispose, this object is expensive
    protected IDatabase _database; //data base for reading and writing to redis.
    protected Dictionary<string, object> _parameters; //this nodes parameters, as read from the supergraph
    protected string _stateKeyString; //string indicating this nodes key for itself in the graph_status. will soon have lowered access 

    public CSNode(string overrideString = null)
    {
        _currentState = Status.NODE_STARTED;
        _parameters = new Dictionary<string, object>();
        if (overrideString != null) //command string is inputed, and this node is being run from unity editor 
        {
            ConnectFromUnity(overrideString); //connect syncronously. this only works if run open
        }
        else //if no override input, then this node is being run from BRAND
        {
            string[] args = Environment.GetCommandLineArgs();
            HandleArgs(args); //parses the args into the flags 
            //Debug.Log($"socket {_serverSocket} ip {_serverIP} port {_serverPort}");
            if (_serverSocket != null || (_serverIP != null && _serverPort != null))
            {
                ConnectFromBRANDAsync(_serverIP, _serverPort).Wait(); //connect async. currently doesnt handle sockets v
                _stateKeyString = _nickname + "_state";
                _database.StreamAdd(_stateKeyString, new NameValueEntry[] { new NameValueEntry("status", _currentState.ToString()) });
            }
            else
            {
                _currentState = Status.NODE_FATAL_ERROR;
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
                case "-i":
                    _serverIP = args[i + 1];
                    break;
                case "-p":
                    _serverPort = args[i + 1];
                    break;
            }
        }
    }

    //connect using socket or IP address and call parse paramters 
    private async Task ConnectFromBRANDAsync(string serverIP, string serverPort, string serverSocket = null)
    {
        string connectionString = serverSocket != null ? serverSocket : $"{serverIP}:{serverPort}"; //prioritize server socket 
        var options = new ConfigurationOptions //can add more custom options as the need arises 
        {
            EndPoints = { connectionString }
        };

        try
        {
            Debug.Log($"attempting connection from BRAND to {connectionString}");
            _redis = await ConnectionMultiplexer.ConnectAsync(options);
            _database = _redis.GetDatabase();
            _parameters = ParseGraphParameters();
            _currentState = Status.NODE_READY;
        }
        catch (Exception ex)
        {
            Debug.LogError($"A connection error occurred: {ex.Message}");
            _currentState = Status.NODE_FATAL_ERROR;
        }
    }

    //gets a dictionary of paramters from the supergraph stream 
    private Dictionary<string, object> ParseGraphParameters()
    {
        _database = _redis.GetDatabase();
        var values = new Dictionary<string, object>();
        var key = "supergraph_stream"; //at risk due to hardcode 
        var result =  _database.StreamRange(key, "-", "+", 1, Order.Descending);

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
                values = JsonConvert.DeserializeObject<Dictionary<string, object>>(graphDict["parameters"].ToString());
            }
            else
            {
                Debug.LogError("could not find this nodes parameters in the super graph");
            }
        }
        return values;
    }

    //used to connect to redis synchronously. used when running the node from editor and is called if node
    //is instantiated with an override string as an argument 
    private void ConnectFromUnity(string overrideString)
    {
        //connect using connection string 
        try
        {
            Debug.Log($"attempting open connection from unity to {overrideString}");
            ConnectionMultiplexer _redis = ConnectionMultiplexer.Connect(overrideString);
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

    protected virtual void UpdateParameters(){}
    protected virtual void Work(){}

    public string GetState()
    {
        return _currentState.ToString();
    }

    public IDatabase GetDatabase()
    {
        return _database;
    }
}
