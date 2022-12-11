using RiptideNetworking;
using RiptideNetworking.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserClient : MonoBehaviour
{
    public static Dictionary<ushort, UserClient> list = new Dictionary<ushort, UserClient>();
    public ushort ID { get; private set; }
    public string deviceName { get; private set; }
    public DeviceType deviceType { get; private set; }
    public static bool isLocal { get; private set; }
    private static GameObject holoLensUserPrefab;
    public GameObject head;
    public GameObject leftHand;
    public GameObject rightHand;

    public void Start()
    {
        holoLensUserPrefab = (GameObject)Resources.Load("prefabs/HoloLensUser");
    }

    private void OnDestroy()
    {
        list.Remove(ID);
    }

    public static void spawn(ushort id_, string deviceName_, DeviceType deviceType_, Vector3 pos)
    {
        UserClient user;
        if (id_ == NetworkManagerClient.Singleton.Client.Id)
        {
            user = new GameObject().AddComponent<UserClient>();

            isLocal = true;
        }
        else
        {
            if (deviceType_ == DeviceType.HoloLens)
            {
                user = Instantiate(holoLensUserPrefab).GetComponent<UserClient>();
            }
            else
            {
                var cubeUser = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubeUser.transform.localScale = Vector3.one * 0.2f;
                user = cubeUser.AddComponent<UserClient>();
            }
            isLocal = false;
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
        // TODO
    }


    #region Messages

    [MessageHandler((ushort)ServerToClientID.userSpawned)]
    private static void spawnUser(Message message)
    {
        var id = message.GetUShort();
        var name = message.GetString();
        var type = (DeviceType)message.GetUShort();
        var pos = message.GetVector3();

        spawn(id,name,type,pos);
    }

    private void sendPositionAndRotation()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ClientToServerID.positionAndRotation);
        message.AddVector3(Camera.main.transform.position);
        message.AddVector3(Camera.main.transform.forward);
        NetworkManagerClient.Singleton.Client.Send(message);
    }

    [MessageHandler((ushort)ServerToClientID.bcastPositionAndRotation)]
    private static void getPositionAndRotation(Message message)
    {
        var id = message.GetUShort();
        var pos = message.GetVector3();
        var forward = message.GetVector3();
        if (list.TryGetValue(id, out UserClient user))
        {
            user.applyPositionAndRotation(pos, forward);
        }
    }

    #endregion

}
