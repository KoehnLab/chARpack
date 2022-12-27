using RiptideNetworking;
using RiptideNetworking.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserClient : MonoBehaviour
{
    public static Dictionary<ushort, UserClient> list = new Dictionary<ushort, UserClient>();
    public ushort ID;
    public string deviceName { get; private set; }
    public myDeviceType deviceType { get; private set; }
    public bool isLocal;

    private void OnDestroy()
    {
        list.Remove(ID);
    }

    public static void spawn(ushort id_, string deviceName_, myDeviceType deviceType_, Vector3 pos)
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
            user = cubeUser.AddComponent<UserClient>();

            user.isLocal = false;
        }

        user.deviceName = string.IsNullOrEmpty(deviceName_) ? $"Unknown{id_}" : deviceName_;
        user.ID = id_;
        user.deviceType = deviceType_;

        list.Add(id_, user);
    }

    private void FixedUpdate()
    {
        sendPositionAndRotation();
    }

    private void applyPositionAndRotation(Vector3 pos, Vector3 forward)
    {
        gameObject.transform.position = GlobalCtrl.Singleton.atomWorld.transform.position + pos;
        gameObject.transform.forward = forward;
        Debug.DrawRay(GlobalCtrl.Singleton.atomWorld.transform.position + pos, forward);
    }


    #region Messages

    [MessageHandler((ushort)ServerToClientID.userSpawned)]
    private static void spawnUser(Message message)
    {
        var id = message.GetUShort();
        var name = message.GetString();
        var type = (myDeviceType)message.GetUShort();
        var pos = message.GetVector3();

        spawn(id,name,type,pos);
    }

    private void sendPositionAndRotation()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ClientToServerID.positionAndRotation);
        message.AddVector3(Camera.main.transform.position - GlobalCtrl.Singleton.atomWorld.transform.position);
        message.AddVector3(Camera.main.transform.forward);
        NetworkManagerClient.Singleton.Client.Send(message);
    }

    [MessageHandler((ushort)ServerToClientID.bcastPositionAndRotation)]
    private static void getPositionAndRotation(Message message)
    {
        var id = message.GetUShort();
        var pos = message.GetVector3();
        var forward = message.GetVector3();
        // if (list.TryGetValue(id, out UserClient user) && !user.isLocal)
        if (list.TryGetValue(id, out UserClient user))
        {
            user.applyPositionAndRotation(pos, forward);
        }
    }

    #endregion

}
