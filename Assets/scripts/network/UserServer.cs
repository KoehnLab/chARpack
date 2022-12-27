using RiptideNetworking;
using RiptideNetworking.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserServer : MonoBehaviour
{
    public static Dictionary<ushort, UserServer> list = new Dictionary<ushort, UserServer>();
    public ushort ID;
    public string deviceName;
    public myDeviceType deviceType;
    public Vector3 offsetPos = Vector3.one;

    public static void spawn(ushort id_, string deviceName_, myDeviceType deviceType_, Vector3 offset_pos)
    {
        foreach (UserServer otherUser in list.Values)
        {
            otherUser.sendSpawned(id_);
        }

        var cubeUser = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeUser.AddComponent<Camera>();
        cubeUser.transform.localScale = Vector3.one * 0.2f;
        UserServer user = cubeUser.AddComponent<UserServer>();

        user.deviceName = string.IsNullOrEmpty(deviceName_) ? $"Unknown{id_}" : deviceName_;
        user.ID = id_;
        user.deviceType = deviceType_;
        user.offsetPos = offset_pos;

        user.sendSpawned();
        list.Add(id_, user);
    }

    private void OnDestroy()
    {
        list.Remove(ID);
    }

    private void applyPositionAndRotation(Vector3 pos, Vector3 forward)
    {
        // TODO: Check if we have to apply offsetPos
        gameObject.transform.position = GlobalCtrl.Singleton.atomWorld.transform.position + pos;
        GetComponent<Camera>().transform.forward = forward;
        Debug.DrawRay(pos, forward);
    }

    #region Messages

    [MessageHandler((ushort)ClientToServerID.deviceNameAndType)]
    private static void getName(ushort fromClientId, Message message)
    {
        var name = message.GetString();
        myDeviceType type = (myDeviceType)message.GetUShort();
        var offset_pos = message.GetVector3();
        Debug.Log($"[UserServer] Got name {name}, and device type {type} from client {fromClientId}");
        spawn(fromClientId, name, type, offset_pos);
    }

    private void sendSpawned()
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientID.userSpawned);

        NetworkManagerServer.Singleton.Server.SendToAll(addSpawnData(message));
    }

    private void sendSpawned(ushort toClientID)
    {
        Message message = Message.Create(MessageSendMode.reliable, ServerToClientID.userSpawned);

        NetworkManagerServer.Singleton.Server.Send(addSpawnData(message),toClientID);
    }

    private Message addSpawnData(Message message)
    {
        message.AddUShort(ID);
        message.AddString(deviceName);
        message.AddUShort((ushort)deviceType);
        message.AddVector3(transform.position);

        return message;
    }

    [MessageHandler((ushort)ClientToServerID.positionAndRotation)]
    private static void getPositionAndRotation(ushort fromClientId, Message message)
    {
        var pos = message.GetVector3();
        var forward = message.GetVector3();
        if (list.TryGetValue(fromClientId, out UserServer user))
        {
            user.applyPositionAndRotation(pos, forward);
        }
        // only send message to other users
        foreach (var otherUser in list.Values)
        {
            if (otherUser.ID != fromClientId)
            {
                Message bcastMessage = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastPositionAndRotation);
                bcastMessage.AddUShort(fromClientId);
                bcastMessage.AddVector3(pos);
                bcastMessage.AddVector3(forward);
                NetworkManagerServer.Singleton.Server.Send(bcastMessage, otherUser.ID);
            }
        }
    }

    #endregion
}
