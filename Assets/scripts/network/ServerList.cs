using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using RiptideNetworking;
using RiptideNetworking.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;

public class ServerList : MonoBehaviour
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
    public GameObject clippingBox;
    public GameObject gridObjectCollection;
    public GameObject scrollingObjectCollection;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        serverEntryPrefab = (GameObject)Resources.Load("prefabs/ServerEntry");
        manualAddServerPrefab = (GameObject)Resources.Load("prefabs/ManualAddServer");

        // Check for open servers
        // get servers and generate entries
        generateServerEntries();

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

    public void connectLocal()
    {
        LoginData.ip = "127.0.0.1";
        LoginData.normal_mode = false;
        SceneManager.LoadScene("MainScene");
    }

    public void generateServerEntries()
    {
        var myServer = new FindServer.ServerData();
        myServer.ip = IPAddress.Parse("192.168.188.22");
        myServer.port = LoginData.port;

        // get old scale
        var oldScale = scrollingObjectCollection.transform.parent.localScale;
        //reset scale 
        scrollingObjectCollection.transform.parent.localScale = Vector3.one;


        generateSingleEntry(myServer);

        // first manually added servers
        if (FindServer.Singleton.manualServerList.Count > 0)
        {
            foreach (var server in FindServer.Singleton.manualServerList)
            {
                generateSingleEntry(server);
            }
        }
        // servers scanned on FindServers script every 5 sec
        if (FindServer.Singleton.serverList.Count > 0)
        {
            // then found servers by bcast
            foreach (var server in FindServer.Singleton.serverList)
            {
                generateSingleEntry(server);
            }


        } else
        {
            Debug.Log("[ServerList] No Servers found.");
        }
        // update on collection places all items in order
        gridObjectCollection.GetComponent<GridObjectCollection>().UpdateCollection();
        // update on scoll content makes the list scrollable
        scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
        // update clipping for out of sight entries
        updateClipping();
        // scale after setting everything up
        scrollingObjectCollection.transform.parent.localScale = oldScale;
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

    public void updateClipping()
    {
        if (gameObject.activeSelf)
        {
            var cb = clippingBox.GetComponent<ClippingBox>();
            foreach (Transform child in gridObjectCollection.transform)
            {
                var renderers = child.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    cb.AddRenderer(renderer);
                }
            }
        }
    }

    public void manualAddServer()
    {
        var manualAddInstance = Instantiate(manualAddServerPrefab);
        manualAddInstance.transform.position += 0.5f * Camera.main.transform.forward;
        manualAddInstance.GetComponent<ManualAddServer>().serverListInstance = gameObject;
        gameObject.SetActive(false);
    }

}
