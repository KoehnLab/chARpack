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
    snapMolecules,
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
    bcastSnapMolecules,
    requestEyeCalibrationState,
    requestBatteryState
}

public enum SimToServerID : ushort
{
    sendInit = 3000,
    sendMolecule = 3001,
    sendMoleculeUpdate = 3002,
    sendStructureFormula = 3003,
}

public enum ServerToSimID : ushort
{
    pauseSim = 4000,
    stopSim = 4001,
    requestStrucutreFormula = 4002
}

public enum StructureToServerID : ushort
{
    sendInit = 5000,
    sendStructureFormula = 5001,
}

public enum ServerToStructureID : ushort
{
    requestStrucutreFormula = 6000
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