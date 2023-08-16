using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ClientToServerID : ushort
{
    deviceNameAndType = 1,
    positionAndRotation,
    atomCreated,
    moleculeMoved,
    atomMoved,
    moleculeMerged,
    sendAtomWorld,
    moleculeLoaded,
    deleteEverything,
    deleteAtom,
    deleteBond,
    deleteMolecule,
    selectAtom,
    selectMolecule,
    selectBond,
    changeAtom,
    syncMe,
    undo,
    enableForceField,
    changeBondTerm,
    changeAngleTerm,
    changeTorsionTerm,
    markTerm,
    modifyHyb,
    keepConfig,
    replaceDummies
}

public enum ServerToClientID : ushort
{
    userSpawned = 1,
    bcastPositionAndRotation,
    bcastAtomCreated,
    bcastMoleculeMoved,
    bcastAtomMoved,
    sendAtomWorld,
    bcastMoleculeMerged,
    bcastMoleculeLoad,
    bcastDeleteEverything,
    bcastDeleteAtom,
    bcastDeleteBond,
    bcastDeleteMolecule,
    bcastSelectAtom,
    bcastSelectMolecule,
    bcastSelectBond,
    bcastChangeAtom,
    bcastEnableForceField,
    bcastChangeBondTerm,
    bcastChangeAngleTerm,
    bcastChangeTorsionTerm,
    bcastMarkTerm,
    bcastModifyHyb,
    bcastKeepConfig,
    bcastReplaceDummies
}

public enum myDeviceType : ushort
{
    HoloLens = 1,
    Mobile = 2,
    PC = 3
}