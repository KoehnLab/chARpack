using Microsoft.MixedReality.Toolkit.UI;
using RiptideNetworking;
using RiptideNetworking.Utils;
using StructClass;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerClient : MonoBehaviour
{
    private static NetworkManagerClient _singleton;

    public static NetworkManagerClient Singleton
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
                Debug.Log($"[{nameof(NetworkManagerClient)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    [HideInInspector] public GameObject showErrorPrefab;
    public Client Client { get; private set; }

    private void Awake()
    {
        if (LoginData.normal_mode)
        {
            Debug.Log($"[{nameof(NetworkManagerClient)}] No network connection reqested - shutting down.");
            Destroy(gameObject);
            return;
        }
        Singleton = this;
    }

    private void Start()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
        showErrorPrefab = (GameObject)Resources.Load("prefabs/confirmLoadDialog");

        Client = new Client();
        Client.Connected += DidConnect;
        Client.ConnectionFailed += FailedToConnect;
        Client.ClientDisconnected += ClientDisconnnected;
        Client.Disconnected += DidDisconnect;

        Connect();

        // subscribe to event manager events
        EventManager.Singleton.OnCreateAtom += sendAtomCreated;
        EventManager.Singleton.OnMoveMolecule += sendMoleculeMoved;
        EventManager.Singleton.OnMoveAtom += sendAtomMoved;

    }

    private void FixedUpdate()
    {
        Client.Tick();
    }

    private void OnApplicationQuit()
    {
        Client.Disconnect();
    }

    /// <summary>
    /// Lets this client try to connect to the riptide server
    /// </summary>
    public void Connect()
    {
        Client.Connect($"{LoginData.ip}:{LoginData.port}");
    }

    /// <summary>
    /// Callback on successfull connection to the riptide server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DidConnect(object sender, EventArgs e)
    {
        sendName();
    }

    /// <summary>
    /// Callback on failed connection attempt to the riptide server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FailedToConnect(object sender, EventArgs e)
    {
        var myDialog = Dialog.Open(showErrorPrefab, DialogButtonType.OK, "Connection Failed", $"Connection to {LoginData.ip}:{LoginData.port} failed\nGoing back to Login Screen.", true);
        if (myDialog != null)
        {
            myDialog.OnClosed += OnClosedDialogEvent;
        }
    }

    private void OnClosedDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.OK)
        {
            SceneManager.LoadScene("LoginScreenScene");
        }
    }

    /// <summary>
    /// Callback when another connected client left the riptide network
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ClientDisconnnected(object sender, ClientDisconnectedEventArgs e)
    {
        Destroy(UserClient.list[e.Id].gameObject);
    }

    /// <summary>
    /// Callback on disconnection invoked by the riptide server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DidDisconnect(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// Returns the type of this device
    /// </summary>
    public ushort getDeviceType()
    {
        myDeviceType currentDeviceType;
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            currentDeviceType = myDeviceType.Mobile;
        }
        else if (SystemInfo.deviceType == DeviceType.Desktop)
        {
            currentDeviceType = myDeviceType.PC;
        }
        else
        {
            currentDeviceType = myDeviceType.HoloLens;
        }

        return (ushort)currentDeviceType;
    }

    #region Messages
    /// <summary>
    /// Sends a message with the device name and the device type to the server
    /// </summary>
    public void sendName()
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.deviceNameAndType);
        message.AddString(SystemInfo.deviceName);
        message.AddUShort(getDeviceType());
        Client.Send(message);
    }

    public void sendAtomCreated(ushort id, string abbre, Vector3 pos)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.atomCreated);
        message.AddUShort(id);
        message.AddString(abbre);
        message.AddVector3(pos);
        Client.Send(message);
    }

    public void sendMoleculeMoved(ushort id, Vector3 pos, Quaternion quat)
    {
        Message message = Message.Create(MessageSendMode.unreliable, ClientToServerID.moleculeMoved);
        message.AddUShort(id);
        message.AddVector3(pos);
        message.AddQuaternion(quat);
        Client.Send(message);
    }

    public void sendAtomMoved(ushort id, Vector3 pos)
    {
        Message message = Message.Create(MessageSendMode.unreliable, ClientToServerID.atomMoved);
        message.AddUShort(id);
        message.AddVector3(pos);
        Client.Send(message);
    }

    [MessageHandler((ushort)ServerToClientID.sendAtomWorld)]
    private static void getAtomWorld(Message message)
    {
        var count = message.GetUShort();
        List<cmlData> atomWorld = new List<cmlData>(); 
        for (ushort i = 0; i < count; i++)
        {
            atomWorld.Add(message.GetCmlData());
        }
        // do the recreation
        if (atomWorld.Count > 0)
        {
            GlobalCtrl.Singleton.rebuildAtomWorld(atomWorld);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastAtomCreated)]
    private static void getAtomCreated(Message message)
    {
        var client_id = message.GetUShort();
        var atom_id = message.GetUShort();
        var abbre = message.GetString();
        var pos = message.GetVector3();

        // do the create
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            GlobalCtrl.Singleton.CreateAtom(atom_id, abbre, pos);
        }

    }

    [MessageHandler((ushort)ServerToClientID.bcastMoleculeMoved)]
    private static void getMoleculeMoved(Message message)
    {
        var client_id = message.GetUShort();
        var molecule_id = message.GetUShort();
        var pos = message.GetVector3();
        var quat = message.GetQuaternion();

        // do the move
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            GlobalCtrl.Singleton.moveMolecule(molecule_id, pos, quat);
        }

    }

    [MessageHandler((ushort)ServerToClientID.bcastAtomMoved)]
    private static void getAtomMoved(Message message)
    {
        var client_id = message.GetUShort();
        var atom_id = message.GetUShort();
        var pos = message.GetVector3();

        // do the move
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            GlobalCtrl.Singleton.moveAtom(atom_id, pos);
        }
    }

    [MessageHandler((ushort)ServerToClientID.sendAtomWorld)]
    private static void getAtomWorld(Message message)
    {
        
    }

    #endregion
}
