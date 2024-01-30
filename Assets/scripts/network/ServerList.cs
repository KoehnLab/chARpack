using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Riptide;
using Riptide.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;

/// <summary>
/// This class provides the functionality of a scrollable list of available servers.
/// </summary>
public class ServerList : myScrollObject
{
    private static ServerList _singleton;
    public static ServerList Singleton
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
                Debug.Log($"[{nameof(ServerList)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public object IPaddress { get; private set; }

    [HideInInspector] public GameObject serverEntryPrefab;
    [HideInInspector] public GameObject manualAddServerPrefab;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        serverEntryPrefab = (GameObject)Resources.Load("prefabs/ServerEntry");
        manualAddServerPrefab = (GameObject)Resources.Load("prefabs/ManualAddServer");

        // add default servers for easy development
        // and quick connects even if LAN find fails
        //addDefaultServers()

        // Check for open servers
        // get servers and generate entries
        generateServerEntries();


        refresh();
    }

    private void addDefaultServers()
    {
        var myServer1 = new FindServer.ServerData();
        myServer1.ip = IPAddress.Parse("192.168.188.22");
        myServer1.port = LoginData.port;

        var myServer2 = new FindServer.ServerData();
        myServer2.ip = IPAddress.Parse("192.168.178.33");
        myServer2.port = LoginData.port;

        var myServer3 = new FindServer.ServerData();
        myServer3.ip = IPAddress.Parse("192.168.166.55");
        myServer3.port = LoginData.port;

        if (!FindServer.manualServerList.Contains(myServer1))
        {
            FindServer.manualServerList.Add(myServer1);
        }
        if (!FindServer.manualServerList.Contains(myServer2))
        {
            FindServer.manualServerList.Add(myServer2);
        }
        if (!FindServer.manualServerList.Contains(myServer3))
        {
            FindServer.manualServerList.Add(myServer3);
        }
    }

    public void refresh()
    {
        generateServerEntries();
    }

    public void close()
    {
        Login.Singleton.gameObject.SetActive(true);
        Destroy(gameObject);
    }

    /// <summary>
    /// Loads the main scene locally without connecting to a server.
    /// </summary>
    public void connectLocal()
    {
        LoginData.ip = "127.0.0.1";
        LoginData.normal_mode = false;
        SceneManager.LoadScene("MainScene");
    }

    /// <summary>
    /// Generates list entries for each automatically found or manually added server.
    /// </summary>
    public void generateServerEntries()
    {
        clearEntries();

        if (!(FindServer.serverList.Count > 0) && !(FindServer.manualServerList.Count > 0))
        {
            Debug.Log("[ServerList] No Servers found and no servers manually added.");
            return;
        }

        // get old scale
        var oldScale = scrollingObjectCollection.transform.parent.localScale;
        //reset scale 
        scrollingObjectCollection.transform.parent.localScale = Vector3.one;

        // first manually added servers
        if (FindServer.manualServerList.Count > 0)
        {
            foreach (var server in FindServer.manualServerList)
            {
                generateSingleEntry(server);
            }
        }
        // servers scanned on FindServers script every 5 sec
        if (FindServer.serverList.Count > 0)
        {
            // then found servers by bcast
            foreach (var server in FindServer.serverList)
            {
                generateSingleEntry(server);
            }
        }
        
        // update on collection places all items in order
        gridObjectCollection.GetComponent<GridObjectCollection>().UpdateCollection();
        // update on scoll content makes the list scrollable
        scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
        // update clipping for out of sight entries
        updateClipping();
        // scale after setting everything up
        scrollingObjectCollection.transform.parent.localScale = oldScale;
        // reset rotation
        resetRotation();
    }

    private void generateSingleEntry(FindServer.ServerData server)
    {
        // generate server entries
        var serverEntry = Instantiate(serverEntryPrefab);
        serverEntry.GetComponent<ButtonConfigHelper>().MainLabelText = $"Server: {server.ip.ToString()}";
        serverEntry.GetComponent<showConnectConfirm>().ip = server.ip.ToString();
        // add entries to collection
        serverEntry.transform.parent = gridObjectCollection.transform;
    }

    /// <summary>
    /// Opens a dialog for the user to manually enter the IP address they want to connect to.
    /// </summary>
    public void manualAddServer()
    {
        var manualAddInstance = Instantiate(manualAddServerPrefab);
        manualAddInstance.transform.position = Camera.main.transform.position + 0.5f * Camera.main.transform.forward;
        manualAddInstance.transform.forward = Camera.main.transform.forward;
        manualAddInstance.GetComponent<ManualAddServer>().serverListInstance = gameObject;
        gameObject.SetActive(false);
    }



}
