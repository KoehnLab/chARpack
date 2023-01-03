using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ManualAddServer : MonoBehaviour
{

    [HideInInspector] public GameObject serverListInstance;

    public GameObject inputField;

    public void addServer()
    {
        var ip = inputField.GetComponent<MRTKTMPInputField>().text;
        var data = new FindServer.ServerData();
        data.ip = IPAddress.Parse(ip);
        data.port = LoginData.port;
        FindServer.manualServerList.Add(data);

        serverListInstance.GetComponent<ServerList>().generateServerEntries();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        serverListInstance.SetActive(true);
    }

}
