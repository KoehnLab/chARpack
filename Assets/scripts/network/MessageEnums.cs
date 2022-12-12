using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ClientToServerID : ushort
{
    deviceNameAndType = 1,
    positionAndRotation = 2,
}

public enum ServerToClientID : ushort
{
    userSpawned = 1,
    bcastPositionAndRotation = 2,
}

public enum DeviceType : ushort
{
    HoloLens = 1,
    Mobile = 2,
    PC = 3
}