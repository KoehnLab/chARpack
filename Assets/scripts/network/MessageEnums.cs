using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ClientToServerID : ushort
{
    deviceNameAndType = 1,
    positionAndRotation = 2,
    atomCreated = 3,
    moleculeMoved = 4,
    atomMoved = 5,
    moleculeMerged = 6,
    sendAtomWorld = 7
}

public enum ServerToClientID : ushort
{
    userSpawned = 1,
    bcastPositionAndRotation = 2,
    bcastAtomCreated = 3,
    bcastMoleculeMoved = 4,
    bcastAtomMoved = 5,
    sendAtomWorld = 6,
    bcastMoleculeMerged = 7
}

public enum myDeviceType : ushort
{
    HoloLens = 1,
    Mobile = 2,
    PC = 3
}