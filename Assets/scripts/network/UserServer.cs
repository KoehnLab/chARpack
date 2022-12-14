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
    public GameObject head;
    public GameObject leftHand;
    public GameObject rightHand;

    public static void spawn(ushort id_, string deviceName_, myDeviceType deviceType_)
    {
        foreach (UserServer otherUser in list.Values)
        {
            otherUser.sendSpawned(id_);
        }

        UserServer user;
        if (deviceType_ == myDeviceType.HoloLens)
        {
            var holoLensUserPrefab = (GameObject)Resources.Load("prefabs/HoloLensUser");
            user = Instantiate(holoLensUserPrefab).GetComponent<UserServer>();
        } 
        else
        {
            var cubeUser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeUser.AddComponent<Camera>();
            cubeUser.transform.localScale = Vector3.one * 0.2f;
            user = cubeUser.AddComponent<UserServer>();
        }
        user.deviceName = string.IsNullOrEmpty(deviceName_) ? $"Unknown{id_}" : deviceName_;
        user.ID = id_;
        user.deviceType = deviceType_;

        user.sendSpawned();
        list.Add(id_, user);
    }

    private void OnDestroy()
    {
        list.Remove(ID);
    }

    public void Start()
    {
        if (deviceType == myDeviceType.HoloLens)
        {
            if (head == null)
            {
                head = gameObject.transform.Find("Head").gameObject;
            }
            if (leftHand == null)
            {
                leftHand = gameObject.transform.Find("LeftHand").gameObject;
            }
            if (rightHand == null)
            {
                rightHand = gameObject.transform.Find("RightHand").gameObject;
            }
        } else
        {
            head = gameObject;
        }
    }

    private void applyPositionAndRotation(Vector3 pos, Vector3 forward)
    {
        head.transform.position = pos;
        head.GetComponent<Camera>().transform.forward = forward;
    }

    #region Messages

    [MessageHandler((ushort)ClientToServerID.deviceNameAndType)]
    private static void getName(ushort fromClientId, Message message)
    {
        var name = message.GetString();
        myDeviceType type = (myDeviceType)message.GetUShort();
        spawn(fromClientId, name, type);
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
    private static void getPsitionAndRotation(ushort fromClientId, Message message)
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
