using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using RiptideNetworking;
using RiptideNetworking.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                Debug.Log($"{nameof(ServerList)} instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }


    [HideInInspector] public GameObject serverEntryPrefab;
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

    private void generateServerEntries()
    {
        // servers scanned on FindServers script every 5 sec
        if (FindServer.Singleton.serverList.Count > 0)
        {
            foreach (var server in FindServer.Singleton.serverList)
            {
                // generate server entries
                var serverEntry = Instantiate(serverEntryPrefab);
                serverEntry.GetComponent<ButtonConfigHelper>().MainLabelText = $"Server: {server.ip.ToString()}";
                serverEntry.GetComponent<showConnectConfirm>().ip = server.ip.ToString();
                // add entries to collection
                serverEntry.transform.parent = gridObjectCollection.transform;
            }

            // update on collection places all items in order
            gridObjectCollection.GetComponent<GridObjectCollection>().UpdateCollection();
            // update on scoll content makes the list scrollable
            scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
            // update clipping for out of sight entries
            updateClipping();
        } else
        {
            Debug.Log("[ServerList] No Servers found.");
        }
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
}
