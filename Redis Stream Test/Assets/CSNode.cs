using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CSNode
{
    private enum Status
    {
        NODE_STARTED,
        NODE_READY,
        NODE_SHUTDOWN,
        NODE_FATAL_ERROR,
        NODE_WARNING,
        NODE_INFO
    }

    private string[] _args;

    protected string _serverSocket;
    protected string _nickname;
    protected string _serverIP;
    protected string _serverPort;
    private Status _currentState;
    protected ConnectionMultiplexer _redis;
    protected IDatabase _database;
    protected string _connectionString;
    protected Dictionary<string, string> _parameters;
    protected string _stateKeyString; //string indicating this nodes key for itself in the graph_status 


    public CSNode(string overrideString = null)
    {
        _currentState = Status.NODE_READY; 
        _parameters = new Dictionary<string, string>();
        if (overrideString != null) //command string is inputed, and this node is being run from unity editor 
        {
            ConnectFromUnity(overrideString); //connect syncronously
            //_database.StreamAdd("RedisTestSender_state", new NameValueEntry[]{new NameValueEntry("status","hello redis")});

        }
        else //no override input, and this node is being run from BRAND
        {
            string[] args = Environment.GetCommandLineArgs();
            _args = args;
            //this is so chalked 
            _serverSocket = args[0];
            _nickname = args[2];
            _serverIP = args[4];
            _serverPort = args[6];
            ConnectFromBRAND(_serverIP, _serverPort).Wait(); //connect async
            _stateKeyString = _nickname + "_state";
            //_stateKeyString = "RedisTestSender_state";
            _database.StreamAdd(_stateKeyString, new NameValueEntry[]{new NameValueEntry("status",_currentState.ToString())});
        }
    }


    private async void ParseGraphParameters()
    {
        _database = _redis.GetDatabase();
        var streamRangeTask = Task.Run(async () =>
        {
            var key = "supergraph_stream";
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
                _nickname = "RedisTestSender"; //placeholder because being run as open 
                var graphDict = nodesDict[_nickname];
                _parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(graphDict["parameters"].ToString());
            }
        });
        await Task.WhenAll(streamRangeTask);
    }


    private async Task ConnectFromBRAND(string serverIP, string serverPort, string serverSocket = null)
    {
        string connectionString = serverSocket != null ? serverSocket : $"{serverIP}:{serverPort}";
        Debug.Log(connectionString);
        var options = new ConfigurationOptions //can add more custom options as the need arises 
        {
            EndPoints = { connectionString }
        };
        try
        {
            _redis = await ConnectionMultiplexer.ConnectAsync(options);
            _database = _redis.GetDatabase();
            ParseGraphParameters();
            _currentState = Status.NODE_STARTED;
        }
        catch (RedisConnectionException ex)
        {
            Console.WriteLine($"Failed to connect to Redis server: {ex.Message}");
            _currentState = Status.NODE_FATAL_ERROR;
        }
        catch (RedisTimeoutException ex)
        {
            Console.WriteLine($"Connection attempt timed out: {ex.Message}");
            _currentState = Status.NODE_FATAL_ERROR;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            _currentState = Status.NODE_FATAL_ERROR;
        }
    }
    
    //used to connect to redis synchronously. used when running the node from editor
    private void ConnectFromUnity(string connectionString)
    {
        //connect using connection string 
        try
        {
            _connectionString = connectionString;
            ConnectionMultiplexer _redis = ConnectionMultiplexer.Connect(connectionString);
            _database = _redis.GetDatabase();
            _currentState = Status.NODE_STARTED;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            _currentState = Status.NODE_FATAL_ERROR;
        }
    }

    public IDatabase GetDatabase()
    {
        return _database;
    }

    protected void Run(){
        Work();
        UpdateParameters();
    }

    protected virtual void UpdateParameters(){}

    protected virtual void Work(){}

    //this method is used to ensure the right arguments are being passed by displaying them to in game text 
    public string[] Args
    {
        get { return _args; }
    }

    public string ConnectionString
    {
        get { return _connectionString; }
    }

    public string State
    {
        get { return _currentState.ToString(); }
    }
}
