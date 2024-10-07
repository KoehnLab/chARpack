namespace chARpack
{
    public enum ClientToServerID : ushort
    {
        deviceNameAndType = 1000,
        positionAndRotation = 1001,
        atomCreated = 1002,
        moleculeMoved = 1003,
        atomMoved = 1004,
        moleculeMerged = 1005,
        sendAtomWorld = 1006,
        moleculeLoaded = 1007,
        deleteEverything = 1008,
        deleteAtom = 1009,
        deleteBond = 1010,
        deleteMolecule = 1011,
        selectAtom = 1012,
        selectMolecule = 1013,
        selectBond = 1014,
        changeAtom = 1015,
        syncMe = 1016,
        undo = 1017,
        enableForceField = 1018,
        changeBondTerm = 1019,
        changeAngleTerm = 1020,
        changeTorsionTerm = 1021,
        markTerm = 1022,
        modifyHyb = 1023,
        keepConfig = 1024,
        replaceDummies = 1025,
        focusHighlight = 1026,
        scaleMolecule = 1027,
        freezeAtom = 1028,
        freezeMolecule = 1029,
        snapMolecules = 1030,
        stopMoveAtom = 1031,
        createMeasurement = 1032,
        clearMeasurements = 1033,
        eyeCalibrationState = 1034,
        batteryState = 1035,
        grabAtom = 1036,
        transitionGrabOnScreen = 1037,
        releaseTransitionGrabOnScreen = 1038,
        hoverOverScreen = 1039,
        transitionMolecule = 1040,
        transitionGenericObject = 1041,
        sendSpawnGhostObject = 1042,
        sendObjectToTrack = 1043,
        sendResults = 1044,
        transitionUnsuccessful = 1045,
        grabOnScreen = 1046,
        releaseGrabOnScreen = 1047,
        handPose = 1048
    }

    public enum ServerToClientID : ushort
    {
        userSpawned = 2000,
        bcastPositionAndRotation = 2001,
        bcastAtomCreated = 2002,
        bcastMoleculeMoved = 2003,
        bcastAtomMoved = 2004,
        sendAtomWorld = 2005,
        bcastMoleculeMerged = 2006,
        bcastMoleculeLoad = 2007,
        bcastDeleteEverything = 2008,
        bcastDeleteAtom = 2009,
        bcastDeleteBond = 2010,
        bcastDeleteMolecule = 2011,
        bcastSelectAtom = 2012,
        bcastSelectMolecule = 2013,
        bcastSelectBond = 2014,
        bcastChangeAtom = 2015,
        bcastEnableForceField = 2016,
        bcastChangeBondTerm = 2017,
        bcastChangeAngleTerm = 2018,
        bcastChangeTorsionTerm = 2019,
        bcastMarkTerm = 2020,
        bcastModifyHyb = 2021,
        bcastKeepConfig = 2022,
        bcastReplaceDummies = 2023,
        bcastSettings = 2024,
        bcastFocusHighlight = 2025,
        bcastScaleMolecule = 2026,
        bcastFreezeAtom = 2027,
        bcastFreezeMolecule = 2028,
        bcastStopMoveAtom = 2029,
        bcastCreateMeasurement = 2030,
        bcastClearMeasurements = 2031,
        MRCapture = 2032,
        bcastSnapMolecules = 2033,
        requestEyeCalibrationState = 2034,
        requestBatteryState = 2035,
        bcastServerFocusHighlight = 2036,
        bcastNumOutlines = 2037,
        bcastServerViewport = 2038,
        bcastSyncMode = 2039,
        transitionMolecule = 2040,
        transitionGenericObject = 2041,
        requestTransition = 2042,
        bcastRequestDeleteMarked = 2043,
        bcastMousePosition = 2044,
        sendSpawnGhostObject = 2045,
        sendObjectToTrack = 2046,
        sendSpawnObjectCollection = 2047,
        requestResults = 2048,
        bcastScreenSizeChanged = 2049
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
}
