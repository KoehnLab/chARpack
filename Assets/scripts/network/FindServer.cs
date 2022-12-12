using RiptideNetworking.Utils;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;

public class FindServer : MonoBehaviour
{
    public struct ServerData
    {
        public IPAddress ip;
        public ushort port;
    }
    [HideInInspector] public List<ServerData> serverList = new List<ServerData>();
    [HideInInspector] public List<ServerData> manualServerList = new List<ServerData>();
    public bool isServer = false;
    public GameObject connectButtonText;

    private static FindServer _singleton;
    public static FindServer Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(FindServer)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }
    private void Awake()
    {
        Singleton = this;
    }

    public LanDiscovery lanDiscovery;

    private void Start()
    {
        lanDiscovery = new LanDiscovery(LoginData.uniqueID, LoginData.discoveryPort);
        lanDiscovery.HostDiscovered += HostDiscovered;
        if (!isServer)
        {
            Debug.Log("[FindServer:Client] Started looking for server via repeated broadcast.");
            InvokeRepeating("HelloThere", 2.0f, 5.0f);
        } 
        else
        {
            Debug.Log("[FindServer:Server] Started listening for client brodcasts.");
            ImHere();
        }
    }

    //Client
    public void HelloThere()
    {
        // update connect button with number of servers
        if (connectButtonText != null)
        {
            connectButtonText.GetComponent<TextMeshPro>().text = $"Connect ({serverList.Count})";
        }
        serverList.Clear();
        lanDiscovery.SendBroadcast();
        Debug.Log("[FindServer:Client] Broadcast sent.");
    }

    //Server
    public void ImHere()
    {
        lanDiscovery.HostIP = lanDiscovery.GetLocalIPAddress();
        lanDiscovery.StartListening();
    }

    void HostDiscovered(object sender, HostDiscoveredEventArgs e)
    {
        Debug.Log($"[FindServer:Client] Lan Discovery got {e.HostIP.ToString()}:{e.HostPort}");
        var data = new ServerData();
        data.ip = e.HostIP;
        data.port = e.HostPort;
        serverList.Add(data);
    }

    private void FixedUpdate()
    {
        if (lanDiscovery != null)
        {
            lanDiscovery.Tick();
        }
    }

    private void OnApplicationQuit()
    {
        if (lanDiscovery != null)
        {
            lanDiscovery.Stop();
        }
    }

}