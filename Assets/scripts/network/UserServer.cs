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
    private GameObject head;


    public static void spawn(ushort id_, string deviceName_, myDeviceType deviceType_, Vector3 offset_pos, Quaternion offset_rot)
    {
        foreach (UserServer otherUser in list.Values)
        {
            otherUser.sendSpawned(id_);
        }

        var cubeUser = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeUser.AddComponent<Camera>();
        cubeUser.transform.localScale = Vector3.one * 0.2f;

        var anchorPrefab = (GameObject)Resources.Load("prefabs/QR/QRAnchorNoScript");
        var anchor = Instantiate(anchorPrefab);
        anchor.transform.position = offset_pos;
        anchor.transform.rotation = offset_rot;
        UserServer user = anchor.AddComponent<UserServer>();

        user.deviceName = string.IsNullOrEmpty(deviceName_) ? $"Unknown{id_}" : deviceName_;
        user.ID = id_;
        user.deviceType = deviceType_;


        cubeUser.transform.parent = anchor.transform;
        user.head = cubeUser;

        user.sendSpawned();
        list.Add(id_, user);
        if (list.Count == 1)
        {
            LoginData.offsetPos = offset_pos;
            LoginData.offsetRot = offset_rot;
            var aw = GameObject.Find("AtomWorld");
            if (aw != null)
            {
                aw.transform.position = LoginData.offsetPos;
                aw.transform.rotation = LoginData.offsetRot;
            }
        }
    }

    private void OnDestroy()
    {
        list.Remove(ID);
    }

    private void applyPositionAndRotation(Vector3 pos, Vector3 forward)
    {
        // TODO: Check if we have to apply offsetPos
        head.transform.localPosition = pos;
        head.GetComponent<Camera>().transform.forward = forward;
    }

    #region Messages

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
