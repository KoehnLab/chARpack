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
    sendAtomWorld = 7,
    moleculeLoaded = 8,
    deleteEverything = 9,
    deleteAtom = 10,
    deleteBond = 11,
    deleteMolecule = 12,
    selectAtom = 13,
    selectMolecule = 14,
    selectBond = 15,
    changeAtom = 16,
    syncMe = 17
}

public enum ServerToClientID : ushort
{
    userSpawned = 1,
    bcastPositionAndRotation = 2,
    bcastAtomCreated = 3,
    bcastMoleculeMoved = 4,
    bcastAtomMoved = 5,
    sendAtomWorld = 6,
    bcastMoleculeMerged = 7,
    bcastMoleculeLoad = 8,
    bcastDeleteEverything = 9,
    bcastDeleteAtom = 10,
    bcastDeleteBond = 11,
    bcastDeleteMolecule = 12,
    bcastSelectAtom = 13,
    bcastSelectMolecule = 14,
    bcastSelectBond = 15,
    bcastChangeAtom = 16
}

public enum myDeviceType : ushort
{
    HoloLens = 1,
    Mobile = 2,
    PC = 3
}