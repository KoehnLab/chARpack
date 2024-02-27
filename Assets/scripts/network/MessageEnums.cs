using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ClientToServerID : ushort
{
    deviceNameAndType = 1000,
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
    replaceDummies,
    focusHighlight,
    scaleMolecule,
    freezeAtom,
    freezeMolecule,
    stopMoveAtom,
    createMeasurement,
    clearMeasurements,
    eyeCalibrationState,
    batteryState
}

public enum ServerToClientID : ushort
{
    userSpawned = 2000,
    bcastPositionAndRotation,
    bcastAtomCreated,
    bcastMoleculeMoved,
    bcastAtomMoved,
    sendAtomWorld = 2005,
    bcastMoleculeMerged,
    bcastMoleculeLoad = 2007,
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
    bcastReplaceDummies,
    bcastSettings = 2024,
    bcastFocusHighlight,
    bcastScaleMolecule,
    bcastFreezeAtom,
    bcastFreezeMolecule,
    bcastStopMoveAtom,
    bcastCreateMeasurement,
    bcastClearMeasurements,
    MRCapture,
    requestEyeCalibrationState,
    requestBatteryState
}

public enum SimToServerID : ushort
{
    sendInit = 3000,
    sendMolecule = 3001,
    sendMoleculeUpdate = 3002,
}

public enum ServerToSimID : ushort
{
    pauseSim = 4000,
    stopSim
}

public enum myDeviceType : ushort
{
    Unknown = 0,
    AR = 1,
    Mobile = 2,
    PC = 3,
    VR = 4,
    XR = 5
}