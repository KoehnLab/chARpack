using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ManualAddServer : MonoBehaviour
{

    [HideInInspector] public GameObject serverListInstance;

    public GameObject inputField;

    public void OnGUI()
    {
        if (Event.current.Equals(Event.KeyboardEvent("return")))
        {
            addServer();
        }
    }

    public void addServer()
    {
        var ip = inputField.GetComponent<MRTKTMPInputField>().text;
        var data = new FindServer.ServerData();
        data.ip = IPAddress.Parse(ip);
        data.port = LoginData.port;
        if (!FindServer.manualServerList.Contains(data) && !FindServer.serverList.Contains(data))
        {
            FindServer.manualServerList.Add(data);
        } 
        else
        {
            Debug.Log("[ManualAddServer] Server already listed");
        }

        serverListInstance.GetComponent<ServerList>().generateServerEntries();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        serverListInstance.GetComponent<ServerList>().gridObjectCollection.GetComponent<GridObjectCollection>().UpdateCollection();
        serverListInstance.SetActive(true);
    }

}
