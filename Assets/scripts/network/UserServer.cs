using Riptide;
using Riptide.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using chARpackColorPalette;

/// <summary>
/// This class provides functionalities for a server device.
/// </summary>
public class UserServer : MonoBehaviour
{
    public static Dictionary<ushort, UserServer> list = new Dictionary<ushort, UserServer>();

    public static ushort simID = 0;
    public static ushort structureID = 0;

    public ushort ID;
    public string deviceName;
    private GameObject head;
    private Vector3 offsetPos;
    private Quaternion offsetRot;
    private bool _eyeCalibrationState = false;
    private int _highlightFocusID = -1;
    public int highlightFocusID { get => _highlightFocusID; set
        {
            _highlightFocusID = value;
            if (CameraSwitcher.Singleton && CameraSwitcher.Singleton.panel.ContainsKey(ID))
            {
                CameraSwitcher.Singleton.panel[ID].GetComponent<UserPanelEntry>().setFocusColor(FocusColors.getColor(value));
            }
        }
    }
    public bool eyeCalibrationState { get => _eyeCalibrationState; set
        {
            _eyeCalibrationState = value;
            if (CameraSwitcher.Singleton && CameraSwitcher.Singleton.panel.ContainsKey(ID))
            {
                CameraSwitcher.Singleton.panel[ID].GetComponent<UserPanelEntry>().updateEyeCalibrationState(value);
            }
        } 
    }
    private BatteryStatus _batteryStatus = BatteryStatus.Unknown;
    public BatteryStatus batteryStatus { get => _batteryStatus; set { 
            _batteryStatus = value;
            if (CameraSwitcher.Singleton && CameraSwitcher.Singleton.panel.ContainsKey(ID))
            {
                CameraSwitcher.Singleton.panel[ID].GetComponent<UserPanelEntry>().updateBatteryStaus(value);
            }
        } }
    private float _batteryLevel = -1.0f;
    public float batteryLevel { get => _batteryLevel; set
        {
            _batteryLevel = value;
            if (CameraSwitcher.Singleton)
            {
                if (CameraSwitcher.Singleton.panel.ContainsKey(ID) && CameraSwitcher.Singleton.panel.ContainsKey(ID))
                {
                    CameraSwitcher.Singleton.panel[ID].GetComponent<UserPanelEntry>().updateBatteryLevel(value);
                }
            }
        } }
    private myDeviceType _deviceType;
    public myDeviceType deviceType { get => _deviceType; set { 
            _deviceType = value;
            if (CameraSwitcher.Singleton && CameraSwitcher.Singleton.panel.ContainsKey(ID))
            {
                CameraSwitcher.Singleton.panel[ID].GetComponent<UserPanelEntry>().updateDeviceType(value);
            }
        } }

    public static void spawn(ushort id_, string deviceName_, myDeviceType deviceType_, Vector3 offset_pos, Quaternion offset_rot)
    {
        foreach (UserServer otherUser in list.Values)
        {
            otherUser.sendSpawned(id_);
        }

        var anchorPrefab = (GameObject)Resources.Load("prefabs/QR/QRAnchorNoScript");
        var anchor = Instantiate(anchorPrefab);
        //anchor.transform.position = offset_pos;
        //anchor.transform.rotation = offset_rot;
        anchor.transform.parent = NetworkManagerServer.Singleton.UserWorld.transform;
        anchor.transform.localPosition = Vector3.zero;
        anchor.transform.localRotation = Quaternion.identity;

        UserServer user = anchor.AddComponent<UserServer>();

        user.deviceName = string.IsNullOrEmpty(deviceName_) ? $"Unknown{id_}" : deviceName_;
        user.ID = id_;
        user.offsetPos = offset_pos;
        user.offsetRot = offset_rot;
        user.deviceType = deviceType_;
        var focus_id = FocusManager.addClient(id_);

        anchor.name = user.deviceName;

        // head
        var cubeUser = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeUser.GetComponent<Renderer>().material = (Material)Resources.Load("materials/UserMaterial");
        cubeUser.GetComponent<Renderer>().material.color = FocusColors.getColor(focus_id);
        cubeUser.transform.localScale = Vector3.one * 0.2f;
        cubeUser.AddComponent<Camera>();

        // view ray
        var lineRenderer = cubeUser.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        var line_material = (Material)Resources.Load("prefabs/QR/yellow");
        lineRenderer.material = line_material;
        lineRenderer.material.color = FocusColors.getColor(focus_id);

        cubeUser.transform.parent = anchor.transform;
        user.head = cubeUser;

        user.sendSpawned();
        list.Add(id_, user);


        // add user to panel
        CameraSwitcher.Singleton.addCamera(id_, cubeUser.GetComponent<Camera>());
        // have to add device again to update visual
        user.deviceType = deviceType_;
        user.highlightFocusID = focus_id;

        // perodically request status from devices
        if (id_ > 0)
        {
            if (deviceType_ != myDeviceType.PC)
            {
                user.requestEyeCalibrationState();
                user.periodicStatusRequests();
            }
        }

        // TODO: Probably not necessary
        if (list.Count == 1)
        {
            GlobalCtrl.Singleton.atomWorld.transform.position = offset_pos;
            GlobalCtrl.Singleton.atomWorld.transform.rotation = offset_rot;
            NetworkManagerServer.Singleton.UserWorld.transform.position = offset_pos;
            NetworkManagerServer.Singleton.UserWorld.transform.rotation = offset_rot;
        }
    }

    private void periodicStatusRequests()
    {
        InvokeRepeating("requestBatteryState", 1.0f, 30.0f);
    }

    private void OnDestroy()
    {
        FocusManager.removeClient(ID);
        CameraSwitcher.Singleton.removeCamera(ID);
        list.Remove(ID);
    }

    private void applyPositionAndRotation(Vector3 pos, Quaternion quat)
    {
        var new_pos = GlobalCtrl.Singleton.atomWorld.transform.TransformPoint(pos);
        head.transform.position = new_pos;
        var new_quat = GlobalCtrl.Singleton.atomWorld.transform.rotation * quat;
        head.transform.rotation = new_quat;
        head.GetComponent<LineRenderer>().SetPosition(0, head.transform.position);
        head.GetComponent<LineRenderer>().SetPosition(1, head.transform.forward * 0.8f + head.transform.position);
    }

    #region Messages

    public void requestEyeCalibrationState()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientID.requestEyeCalibrationState);
        NetworkManagerServer.Singleton.Server.Send(message, ID);
    }

    [MessageHandler((ushort)ClientToServerID.eyeCalibrationState)]
    private static void getEyeCalibrationState(ushort fromClientId, Message message)
    {
        var state = message.GetBool();

        list[fromClientId].eyeCalibrationState = state;
    }

    public void requestBatteryState()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientID.requestBatteryState);
        NetworkManagerServer.Singleton.Server.Send(message, ID);
    }

    [MessageHandler((ushort)ClientToServerID.batteryState)]
    private static void getBatteryState(ushort fromClientId, Message message)
    {
        var status = (BatteryStatus)message.GetUShort();
        var level = message.GetFloat();

        list[fromClientId].batteryStatus = status;
        list[fromClientId].batteryLevel = level;

        //Debug.Log($"Battery status: {status}, level: {level}");

    }

    [MessageHandler((ushort)ClientToServerID.deviceNameAndType)]
    private static void getName(ushort fromClientId, Message message)
    {
        var name = message.GetString();
        myDeviceType type = (myDeviceType)message.GetUShort();
        var offset_pos = message.GetVector3();
        var offset_rot = message.GetQuaternion();
        Debug.Log($"[UserServer] Got name {name}, and device type {type} from client {fromClientId}");
        spawn(fromClientId, name, type, offset_pos, offset_rot);
    }

    private void sendSpawned()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientID.userSpawned);

        NetworkManagerServer.Singleton.Server.SendToAll(addSpawnData(message));
    }

    private void sendSpawned(ushort toClientID)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientID.userSpawned);

        NetworkManagerServer.Singleton.Server.Send(addSpawnData(message), toClientID);
    }

    private Message addSpawnData(Message message)
    {
        message.AddUShort(ID);
        message.AddString(deviceName);
        message.AddUShort((ushort)deviceType);
        message.AddInt(highlightFocusID);

        return message;
    }

    [MessageHandler((ushort)ClientToServerID.positionAndRotation)]
    private static void getPositionAndRotation(ushort fromClientId, Message message)
    {
        var pos = message.GetVector3();
        var quat = message.GetQuaternion();
        if (list.TryGetValue(fromClientId, out UserServer user))
        {
            user.applyPositionAndRotation(pos, quat);
        }
        // only send message to other users
        foreach (var otherUser in list.Values)
        {
            if (otherUser.ID != fromClientId)
            {
                Message bcastMessage = Message.Create(MessageSendMode.Unreliable, ServerToClientID.bcastPositionAndRotation);
                bcastMessage.AddUShort(fromClientId);
                bcastMessage.AddVector3(pos);
                bcastMessage.AddQuaternion(quat);
                NetworkManagerServer.Singleton.Server.Send(bcastMessage, otherUser.ID);
            }
        }
    }

    #endregion
}
