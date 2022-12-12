using RiptideNetworking;
using RiptideNetworking.Utils;
using UnityEngine;
using System;
using Microsoft.MixedReality.Toolkit.UI;
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
        EventManager.Singleton.OnCreateAtom += atomCreated;
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
    /// Sends a message with the device name and the device type to the server
    /// </summary>
    public void sendName()
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.deviceNameAndType);
        message.AddString(SystemInfo.deviceName);
        message.AddUShort(getDeviceType());
        Client.Send(message);
    }

    /// <summary>
    /// Returns the type of this device
    /// </summary>
    public ushort getDeviceType()
    {
        // TODO implement
        return (ushort)DeviceType.HoloLens;
    }

    public void atomCreated(int id, string abbre, Vector3 pos)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.atomCreated);
        message.AddInt(id);
        message.AddString(abbre);
        message.AddVector3(pos);
        Client.Send(message);
    }
}