using Microsoft.MixedReality.Toolkit;
using Riptide;
using Riptide.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using chARpackColorPalette;

/// <summary>
/// This class provides functions for a client device in the scene.
/// </summary>
public class UserClient : MonoBehaviour
{
    public static Dictionary<ushort, UserClient> list = new Dictionary<ushort, UserClient>();
    public ushort ID;
    public string deviceName { get; private set; }
    public myDeviceType deviceType { get; private set; }
    public bool isLocal;
    public int highlightFocusID;

    private void OnDestroy()
    {
        FocusManager.decreaseNumOutlines();
        list.Remove(ID);
    }

    /// <summary>
    /// Spawns a user client object in the user world.
    /// If the user is connected to a non-local server, also adds a box and a ray
    /// indicating the user's head and forward direction respectively.
    /// </summary>
    /// <param name="id_"></param>
    /// <param name="deviceName_"></param>
    /// <param name="deviceType_"></param>
    public static void spawn(ushort id_, string deviceName_, myDeviceType deviceType_, int focus_id)
    {
        Debug.Log($"[UserClient:spawn] Id from function call {id_}, id from NetworkManager {NetworkManagerClient.Singleton.Client.Id}");
        UserClient user;
        if (id_ == NetworkManagerClient.Singleton.Client.Id)
        {
            user = new GameObject().AddComponent<UserClient>();

            user.isLocal = true;
        }
        else
        {
            var cubeUser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeUser.transform.localScale = Vector3.one * 0.2f;
            cubeUser.GetComponent<Renderer>().material = (Material)Resources.Load("materials/UserMaterial");
            var focus_col = FocusColors.getColor(focus_id);
            cubeUser.GetComponent<Renderer>().material.color = new Color(focus_col.r, focus_col.g, focus_col.b, 0.5f);
            cubeUser.tag = "User Box";
            user = cubeUser.AddComponent<UserClient>();
            

            user.isLocal = false;

            // view ray
            var lineRenderer = cubeUser.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.005f;
            var line_material = (Material)Resources.Load("prefabs/QR/yellow");
            lineRenderer.material = line_material;
            lineRenderer.material.color = FocusColors.getColor(focus_id);
        }

        user.deviceName = string.IsNullOrEmpty(deviceName_) ? $"Unknown{id_}" : deviceName_;
        user.name = user.isLocal ? "Me" : user.deviceName;
        user.ID = id_;
        user.deviceType = deviceType_;
        user.highlightFocusID = focus_id;

        user.transform.parent = NetworkManagerClient.Singleton.userWorld.transform;

        list.Add(id_, user);
        FocusManager.increaseNumOutlines();
    }

    private void FixedUpdate()
    {
        sendPositionAndRotation();
    }

    private void applyPositionAndRotation(Vector3 pos, Quaternion quat)
    {
        var new_pos = GlobalCtrl.Singleton.atomWorld.transform.TransformPoint(pos);
        gameObject.transform.position = new_pos;
        var new_quat = GlobalCtrl.Singleton.atomWorld.transform.rotation * quat;
        gameObject.transform.rotation = new_quat;
        GetComponent<LineRenderer>().SetPosition(0, transform.position);
        GetComponent<LineRenderer>().SetPosition(1, transform.forward * 0.8f + transform.position);
    }


    #region Messages

    [MessageHandler((ushort)ServerToClientID.userSpawned)]
    private static void spawnUser(Message message)
    {
        var id = message.GetUShort();
        var name = message.GetString();
        var type = (myDeviceType)message.GetUShort();
        var focus_id = message.GetInt();

        spawn(id, name, type, focus_id);
    }

    private void sendPositionAndRotation()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerID.positionAndRotation);

        message.AddVector3(GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(GlobalCtrl.Singleton.mainCamera.transform.position));
        message.AddQuaternion(Quaternion.Inverse(GlobalCtrl.Singleton.atomWorld.transform.rotation) * GlobalCtrl.Singleton.mainCamera.transform.rotation);
        NetworkManagerClient.Singleton.Client.Send(message);
    }

    [MessageHandler((ushort)ServerToClientID.requestEyeCalibrationState)]
    private static void getEyeCalibrationRequest(Message message)
    {
        var calibrationStatus = CoreServices.InputSystem?.EyeGazeProvider?.IsEyeCalibrationValid;

        var out_message = Message.Create(MessageSendMode.Reliable, ClientToServerID.eyeCalibrationState);
        out_message.AddBool(calibrationStatus.Value);
        NetworkManagerClient.Singleton.Client.Send(out_message);
    }

    [MessageHandler((ushort)ServerToClientID.requestBatteryState)]
    private static void getBatteryStateRequest(Message message)
    {
        var status = (ushort)SystemInfo.batteryStatus;
        var level = SystemInfo.batteryLevel;

        var out_message = Message.Create(MessageSendMode.Reliable, ClientToServerID.batteryState);
        out_message.AddUShort(status);
        out_message.AddFloat(level);
        NetworkManagerClient.Singleton.Client.Send(out_message);
    }

    [MessageHandler((ushort)ServerToClientID.bcastPositionAndRotation)]
    private static void getPositionAndRotation(Message message)
    {
        var id = message.GetUShort();
        var pos = message.GetVector3();
        //var forward = message.GetVector3();
        var quat = message.GetQuaternion();
        // if (list.TryGetValue(id, out UserClient user) && !user.isLocal)
        if (list.TryGetValue(id, out UserClient user))
        {
            user.applyPositionAndRotation(pos, quat);
        }
    }

#endregion

}
