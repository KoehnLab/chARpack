using Microsoft.MixedReality.Toolkit.UI;
using Riptide;
using Riptide.Utils;
using chARpackStructs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerClient : MonoBehaviour
{
    private static NetworkManagerClient _singleton;

    public static NetworkManagerClient Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(NetworkManagerClient)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    [HideInInspector] public GameObject showErrorPrefab;
    private static byte[] cmlTotalBytes;
    private static List<cmlData> cmlWorld;
    private static ushort chunkSize = 255;
    public Client Client { get; private set; }
    [HideInInspector] public GameObject userWorld;
    [HideInInspector] public bool controlledExit = false;

    private void Awake()
    {
        if (LoginData.normal_mode)
        {
            Debug.Log($"[{nameof(NetworkManagerClient)}] No network connection reqested - shutting down.");
            Destroy(gameObject);
            return;
        }
        Singleton = this;
        // create user world
        userWorld = new GameObject("UserWorld");
        userWorld.transform.position = LoginData.offsetPos;
        userWorld.transform.rotation = LoginData.offsetRot;
    }

    private void Start()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
        showErrorPrefab = (GameObject)Resources.Load("prefabs/confirmDialog");

        Client = new Client();
        Client.Connected += DidConnect;
        Client.ConnectionFailed += FailedToConnect;
        Client.ClientDisconnected += ClientDisconnnected;
        Client.Disconnected += DidDisconnect;

        Connect();

        // subscribe to event manager events
        EventManager.Singleton.OnCreateAtom += sendAtomCreated;
        EventManager.Singleton.OnMoveMolecule += sendMoleculeMoved;
        EventManager.Singleton.OnMoveAtom += sendAtomMoved;
        EventManager.Singleton.OnStopMoveAtom += sendStopMoveAtom;
        EventManager.Singleton.OnMergeMolecule += sendMoleculeMerged;
        EventManager.Singleton.OnDeviceLoadMolecule += sendDeviceMoleculeLoaded;
        EventManager.Singleton.OnDeleteEverything += sendDeleteEverything;
        EventManager.Singleton.OnDeleteAtom += sendDeleteAtom;
        EventManager.Singleton.OnDeleteBond += sendDeleteBond;
        EventManager.Singleton.OnDeleteMolecule += sendDeleteMolecule;
        EventManager.Singleton.OnSelectAtom += sendSelectAtom;
        EventManager.Singleton.OnSelectMolecule += sendSelectMolecule;
        EventManager.Singleton.OnSelectBond += sendSelectBond;
        EventManager.Singleton.OnChangeAtom += sendChangeAtom;
        EventManager.Singleton.OnUndo += sendUndo;
        EventManager.Singleton.OnEnableForceField += sendEnableForceField;
        EventManager.Singleton.OnChangeBondTerm += sendChangeBondTerm;
        EventManager.Singleton.OnChangeAngleTerm += sendChangeAngleTerm;
        EventManager.Singleton.OnChangeTorsionTerm += sendChangeTorsionTerm;
        EventManager.Singleton.OnMarkTerm += sendMarkTerm;
        EventManager.Singleton.OnModifyHyb += sendModifyHyb;
        EventManager.Singleton.OnSetKeepConfig += sendKeepConfig;
        EventManager.Singleton.OnReplaceDummies += sendReplaceDummies;
        EventManager.Singleton.OnFocusHighlight += sendFocusHighlight;
        EventManager.Singleton.OnChangeMoleculeScale += sendScaleMolecue;
        EventManager.Singleton.OnFreezeAtom += sendFreezeAtom;
        EventManager.Singleton.OnFreezeMolecule += sendFreezeMolecule;
        EventManager.Singleton.OnSetSnapColors += sendSetSnapColor;
        EventManager.Singleton.OnCreateMeasurement += sendCreateMeasurement;
        EventManager.Singleton.OnClearMeasurements += sendClearMeasurements;
        EventManager.Singleton.OnGrabAtom += sendGrabAtom;
    }

    private void FixedUpdate()
    {
        Client.Update();
    }

    private void OnDestroy()
    {
        if (Client != null)
        {
            if (Client.IsConnected)
            {
                Client.Disconnect();
            }
        }
    }

    /// <summary>
    /// Lets this client try to connect to the riptide server
    /// </summary>
    public void Connect()
    {
        Client.Connect($"{LoginData.ip}:{LoginData.port}");
    }

    /// <summary>
    /// Callback on successfull connection to the riptide server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DidConnect(object sender, EventArgs e)
    {
        sendName();
    }

    /// <summary>
    /// Callback on failed connection attempt to the riptide server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FailedToConnect(object sender, ConnectionFailedEventArgs e)
    {
        Debug.LogError($"[NetworkmanagerClient:FailedToConnect] Reason {e.Reason}; Message {e.Message}");
        var myDialog = Dialog.Open(showErrorPrefab, DialogButtonType.OK, "Connection Failed", $"Connection to {LoginData.ip}:{LoginData.port} failed\nGoing back to Login Screen.", true);
        //make sure the dialog is rotated to the camera
        myDialog.transform.forward = -GlobalCtrl.Singleton.mainCamera.transform.forward;

        if (myDialog != null)
        {
            myDialog.OnClosed += OnClosedDialogEvent;
        }
    }

    private void OnClosedDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.OK)
        {
            SceneManager.LoadScene("LoginScreenScene");
        }
    }

    /// <summary>
    /// Callback when another connected client left the riptide network
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ClientDisconnnected(object sender, ClientDisconnectedEventArgs e)
    {
        Destroy(UserClient.list[e.Id].gameObject);
    }

    /// <summary>
    /// Callback on disconnection invoked by the riptide server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DidDisconnect(object sender, EventArgs e)
    {
        if (!controlledExit)
        {
            controlledExit = false;
            MainActionMenu.Singleton.gameObject.SetActive(false);
            var myDialog = Dialog.Open(showErrorPrefab, DialogButtonType.OK, "Connection Failed", $"Connection to {LoginData.ip}:{LoginData.port} failed\nGoing back to Login Screen.", true);
            //make sure the dialog is rotated to the camera
            myDialog.transform.forward = -GlobalCtrl.Singleton.mainCamera.transform.forward;
            myDialog.transform.position = GlobalCtrl.Singleton.mainCamera.transform.position + 0.01f * myDialog.transform.forward;

            if (myDialog != null)
            {
                myDialog.OnClosed += OnClosedDialogEvent;
            }
        }
    }

    /// <summary>
    /// Returns the type of this device
    /// </summary>
    public ushort getDeviceType()
    {
        if (SystemInfo.deviceModel.ToLower().Contains("hololens"))
        {
            return (ushort)myDeviceType.AR;
        }
        if (SystemInfo.deviceModel.ToLower().Contains("quest"))
        {
            return (ushort)myDeviceType.XR;
        }
        if (SystemInfo.deviceType == DeviceType.Desktop)
        {
            return (ushort)myDeviceType.PC;
        }
        return (ushort)myDeviceType.Unknown;
    }

    #region Sends
    /// <summary>
    /// Sends a message with the device name and the device type to the server
    /// </summary>
    public void sendName()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.deviceNameAndType);
        message.AddString(SystemInfo.deviceName);
        message.AddUShort(getDeviceType());
        message.AddVector3(LoginData.offsetPos);
        message.AddQuaternion(LoginData.offsetRot);
        Client.Send(message);
    }

    public void sendSyncRequest()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.syncMe);
        Client.Send(message);
    }

    public void sendAtomCreated(Guid mol_id, string abbre, Vector3 pos, ushort hyb)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.atomCreated);
        message.AddGuid(mol_id);
        message.AddString(abbre);
        message.AddVector3(pos);
        message.AddUShort(hyb);
        Client.Send(message);
    }

    public void sendMoleculeMoved(Guid mol_id, Vector3 pos, Quaternion quat)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerID.moleculeMoved);
        message.AddGuid(mol_id);
        message.AddVector3(pos);
        message.AddQuaternion(quat);
        Client.Send(message);
    }

    public void sendAtomMoved(Guid mol_id, ushort atom_id, Vector3 pos)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerID.atomMoved);
        message.AddGuid(mol_id);
        message.AddUShort(atom_id);
        message.AddVector3(pos);
        Client.Send(message);
    }

    public void sendStopMoveAtom(Guid mol_id, ushort atom_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.stopMoveAtom);
        message.AddGuid(mol_id);
        message.AddUShort(atom_id);
        Client.Send(message);
    }

    public void sendMoleculeMerged(Guid mol1ID, ushort atom1ID, Guid mol2ID, ushort atom2ID)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.moleculeMerged);
        message.AddGuid(mol1ID);
        message.AddUShort(atom1ID);
        message.AddGuid(mol2ID);
        message.AddUShort(atom2ID);
        Client.Send(message);
    }

    public void sendDeviceMoleculeLoaded(string name)
    {
        var molData = GlobalCtrl.Singleton.getMoleculeData(name);
        NetworkUtils.serializeCmlData((ushort)ClientToServerID.moleculeLoaded, molData, chunkSize, true);
    }

    public void sendDeleteEverything()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.deleteEverything);
        Client.Send(message);
    }
    
    public void sendSelectAtom(Guid mol_id, ushort atom_id, bool selected)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.selectAtom);
        message.AddGuid(mol_id);
        message.AddUShort(atom_id);
        message.AddBool(selected);
        Client.Send(message);
    }

    public void sendSelectMolecule(Guid mol_id, bool selected)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.selectMolecule);
        message.AddGuid(mol_id);
        message.AddBool(selected);
        Client.Send(message);
    }

    public void sendSelectBond(ushort bond_id, Guid mol_id, bool selected)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.selectBond);
        message.AddUShort(bond_id);
        message.AddGuid(mol_id);
        message.AddBool(selected);
        Client.Send(message);
    }

    public void sendDeleteAtom(Guid mol_id, ushort atom_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.deleteAtom);
        message.AddGuid(mol_id);
        message.AddUShort(atom_id);
        Client.Send(message);
        UnityEngine.Debug.Log("[NetworkManagerClient] Sent delete atom");
    }

    public void sendDeleteMolecule(Guid mol_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.deleteMolecule);
        message.AddGuid(mol_id);
        Client.Send(message);
        UnityEngine.Debug.Log("[NetworkManagerClient] Sent delete molecule");
    }

    public void sendDeleteBond(ushort bond_id, Guid mol_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.deleteBond);
        message.AddUShort(bond_id);
        message.AddGuid(mol_id);
        Client.Send(message);
        UnityEngine.Debug.Log("[NetworkManagerClient] Sent delete bond");
    }

    public void sendChangeAtom(Guid mol_id, ushort atom_id, string chemAbbre)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.changeAtom);
        message.AddGuid(mol_id);
        message.AddUShort(atom_id);
        message.AddString(chemAbbre);
        Client.Send(message);
        UnityEngine.Debug.Log("[NetworkManagerClient] Sent change atom");
    }

    public void sendUndo()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.undo);
        Client.Send(message);
    }

    public void sendEnableForceField(bool enableForceField)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.enableForceField);
        message.AddBool(enableForceField);
        Client.Send(message);
    }


    public void sendChangeBondTerm(ForceField.BondTerm term, Guid mol_id, ushort term_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.changeBondTerm);
        message.AddGuid(mol_id);
        message.AddUShort(term_id);
        message.AddBondTerm(term);
        Client.Send(message);
    }

    public void sendChangeAngleTerm(ForceField.AngleTerm term, Guid mol_id, ushort term_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.changeAngleTerm);
        message.AddGuid(mol_id);
        message.AddUShort(term_id);
        message.AddAngleTerm(term);
        Client.Send(message);
    }

    public void sendChangeTorsionTerm(ForceField.TorsionTerm term, Guid mol_id, ushort term_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.changeTorsionTerm);
        message.AddGuid(mol_id);
        message.AddUShort(term_id);
        message.AddTorsionTerm(term);
        Client.Send(message);
    }

    public void sendMarkTerm(ushort term_type, Guid mol_id, ushort term_id, bool marked)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.markTerm);
        message.AddUShort(term_type);
        message.AddGuid(mol_id);
        message.AddUShort(term_id);
        message.AddBool(marked);
        Client.Send(message);
    }

    public void sendModifyHyb(Guid mol_id, ushort atom_id, ushort hyb)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.modifyHyb);
        message.AddGuid(mol_id);
        message.AddUShort(atom_id);
        message.AddUShort(hyb);
        Client.Send(message);
    }

    public void sendKeepConfig(Guid mol_id, bool keep_config)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.keepConfig);
        message.AddGuid(mol_id);
        message.AddBool(keep_config);
        Client.Send(message);
    }


    public void sendReplaceDummies(Guid mol_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.replaceDummies);
        message.AddGuid(mol_id);
        Client.Send(message);
    }

    public void sendFocusHighlight(Guid mol_id, ushort atom_id, bool active)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.focusHighlight);
        message.AddGuid(mol_id);
        message.AddUShort(atom_id);
        message.AddBool(active);
        Client.Send(message);
    }

    public void sendScaleMolecue(Guid mol_id, float scale)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.scaleMolecule);
        message.AddGuid(mol_id);
        message.AddFloat(scale);
        Client.Send(message);
    }

    public void sendFreezeAtom(Guid mol_id, ushort atom_id, bool value)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.freezeAtom);
        message.AddGuid(mol_id);
        message.AddUShort(atom_id);
        message.AddBool(value);
        Client.Send(message);
    }

    public void sendFreezeMolecule(Guid mol_id, bool value)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.freezeMolecule);
        message.AddGuid(mol_id);
        message.AddBool(value);
        Client.Send(message);
    }

    public void sendSetSnapColor(Guid mol1_id, Guid mol2_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.snapMolecules);
        message.AddGuid(mol1_id);
        message.AddGuid(mol2_id);
        Client.Send(message);
    }

    public void sendCreateMeasurement(Guid mol1_id, ushort atom1_id, Guid mol2_id, ushort atom2_id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.createMeasurement);
        message.AddGuid(mol1_id);
        message.AddUShort(atom1_id);
        message.AddGuid(mol2_id);
        message.AddUShort(atom2_id);
        Client.Send(message);
    }

    public void sendClearMeasurements()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.clearMeasurements);
        Client.Send(message);
    }

    public void sendGrabAtom(Atom a, bool value)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerID.grabAtom);
        message.AddGuid(a.m_molecule.m_id);
        message.AddUShort(a.m_id);
        message.AddBool(value);
        Client.Send(message);
    }
    #endregion

    #region Listen

    [MessageHandler((ushort)ServerToClientID.sendAtomWorld)]
    private static void listenForAtomWorld(Message message)
    {
        NetworkUtils.deserializeCmlData(message, ref cmlTotalBytes, ref cmlWorld, chunkSize);
    }

    [MessageHandler((ushort)ServerToClientID.MRCapture)]
    private static void listenForMRCapture(Message message)
    {
        var client_id = message.GetUShort();
        var rec = message.GetBool();

        // start/stop recording
        if (MRCaptureManager.Singleton)
        {
            MRCaptureManager.Singleton.setRecording(rec);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastAtomCreated)]
    private static void getAtomCreated(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var abbre = message.GetString();
        var pos = message.GetVector3();
        var hyb = message.GetUShort();

        // do the create
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id))
            {
                Debug.LogError($"[NetworkManagerClient:getAtomCreated] Molecule with new creation id {mol_id} already exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.CreateAtom(mol_id, abbre, pos, hyb, true);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastMoleculeMoved)]
    private static void getMoleculeMoved(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var pos = message.GetVector3();
        var quat = message.GetQuaternion();

        // do the move
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.moveMolecule(mol_id, pos, quat))
            {
                Debug.LogError($"[NetworkManagerClient:getMoleculeMoved] Molecule with id {mol_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }

    }

    [MessageHandler((ushort)ServerToClientID.bcastAtomMoved)]
    private static void getAtomMoved(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var atom_id = message.GetUShort();
        var pos = message.GetVector3();

        // do the move
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.moveAtom(mol_id, atom_id, pos))
            {
                Debug.LogError($"[NetworkManagerClient:getAtomMoved] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastStopMoveAtom)]
    private static void getStopMoveAtom(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var atom_id = message.GetUShort();

        // do the move
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.stopMoveAtom(mol_id, atom_id))
            {
                Debug.LogError($"[NetworkManagerClient:getStopMoveAtom] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastMoleculeMerged)]
    private static void getMoleculeMerged(Message message)
    {
        var client_id = message.GetUShort();
        var mol1ID = message.GetGuid();
        var atom1ID = message.GetUShort();
        var mol2ID = message.GetGuid();
        var atom2ID = message.GetUShort();

        // do the merge
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol1 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol1ID);
            var atom1 = mol1?.atomList.ElementAtOrNull(atom1ID, null);
            var mol2 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol2ID);
            var atom2 = mol2?.atomList.ElementAtOrNull(atom2ID, null);
            if (mol1 == null || atom1 == null || mol2 == null || atom2 == null )
            {
                Debug.LogError($"[NetworkManagerClient] Merging operation cannot be executed. Atom IDs do not exist (Atom1: {atom1ID}, Atom2 {atom2ID}).\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.MergeMolecule(mol1ID, atom1ID, mol2ID, atom2ID);
        }

    }

    [MessageHandler((ushort)ServerToClientID.bcastMoleculeLoad)]
    private static void getMoleculeLoaded(Message message)
    {
        NetworkUtils.deserializeCmlData(message, ref cmlTotalBytes, ref cmlWorld, chunkSize, false);
    }

    [MessageHandler((ushort)ServerToClientID.bcastDeleteEverything)]
    private static void getDeleteEverything(Message message)
    {
        var client_id = message.GetUShort();
        // do the delete
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            GlobalCtrl.Singleton.DeleteAll();
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastSelectAtom)]
    private static void getAtomSelected(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var atom_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            var atom = mol?.atomList.ElementAtOrNull(atom_id, null);
            if (mol == null || atom == null)
            {
                Debug.LogError($"[NetworkManagerClient:getAtomSelected] Atom with id {atom_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            if (atom.m_molecule.isMarked)
            {
                atom.m_molecule.markMolecule(false);
            }
            atom.advancedMarkAtom(selected, true);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastSelectMolecule)]
    private static void getMoleculeSelected(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var selected = message.GetBool();
        // do the select
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            if (mol == null)
            {
                Debug.LogError($"[NetworkManagerClient:getMoleculeSelected] Molecule with id {mol_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.List_curMolecules[mol_id].markMolecule(selected, true);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastSelectBond)]
    private static void getBondSelected(Message message)
    {
        var client_id = message.GetUShort();
        var bond_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var selected = message.GetBool();
        // do the select
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            var bond = mol?.bondList.ElementAtOrNull(bond_id, null);
            if (mol == null || bond == null)
            {
                Debug.LogError($"[NetworkManagerClient:getBondSelected] Bond with id {bond_id} or molecule with id {mol_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            bond.markBond(selected);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastDeleteAtom)]
    private static void getAtomDeleted(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var atom_id = message.GetUShort();
        // do the delete
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            var atom = mol?.atomList.ElementAtOrNull(atom_id, null);
            if (mol == null || atom == null)
            {
                Debug.LogError($"[NetworkManagerClient:getAtomDeleted] Atom with id {atom_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.deleteAtom(atom);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastDeleteMolecule)]
    private static void getMoleculeDeleted(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        // do the delete
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            if (mol == null)
            {
                Debug.LogError($"[NetworkManagerClient:getMoleculeSelected] Molecule with id {mol_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.deleteMolecule(GlobalCtrl.Singleton.List_curMolecules[mol_id]);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastDeleteBond)]
    private static void getBondDeleted(Message message)
    {
        var client_id = message.GetUShort();
        var bond_id = message.GetUShort();
        var mol_id = message.GetGuid();
        // do the delete
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            var bond = mol.bondList.ElementAtOrNull(bond_id, null);
            if (mol == null || bond == null)
            {
                Debug.LogError($"[NetworkManagerClient:getBondSelected] Bond with id {bond_id} or molecule with id {mol_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.deleteBond(bond);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastChangeAtom)]
    private static void getAtomChanged(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var atom_id = message.GetUShort();
        var chemAbbre = message.GetString();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.changeAtom(mol_id, atom_id, chemAbbre))
            {
                Debug.LogError($"[NetworkManagerClient:getAtomChanged] Atom with id {atom_id} of Molecule {mol_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastEnableForceField)]
    private static void getEnabelForceField(Message message)
    {
        var client_id = message.GetUShort();
        var enabled = message.GetBool();

        // do the enable/disable
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            ForceField.Singleton.enableForceFieldMethod(enabled);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastChangeBondTerm)]
    private static void getBondTermChanged(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var term_id = message.GetUShort();
        var term = message.GetBondTerm();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.changeBondTerm(mol_id, term_id, term))
            {
                Debug.LogError($"[NetworkManagerClient:getAtomChanged] Bond term with id {term_id} of Molecule {mol_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastChangeAngleTerm)]
    private static void getAngleTermChanged(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var term_id = message.GetUShort();
        var term = message.GetAngleTerm();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.changeAngleTerm(mol_id, term_id, term))
            {
                Debug.LogError($"[NetworkManagerClient:getAtomChanged] Angle term with id {term_id} of Molecule {mol_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastChangeTorsionTerm)]
    private static void getTorsionTermChanged(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var term_id = message.GetUShort();
        var term = message.GetTorsionTerm();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.changeTorsionTerm(mol_id, term_id, term))
            {
                Debug.LogError($"[NetworkManagerClient:getAtomChanged] Angle term with id {term_id} of Molecule {mol_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastMarkTerm)]
    private static void getMarkTerm(Message message)
    {
        var client_id = message.GetUShort();
        // process message
        var term_type = message.GetUShort();
        var mol_id = message.GetGuid();
        var term_id = message.GetUShort();
        var marked = message.GetBool();

        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {

            // do the change
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            if (mol == null)
            {
                Debug.LogError($"[NetworkManagerClient:getMarkTerm] Molecule with id {mol_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }

            if (term_type == 0)
            {
                var term = mol.bondTerms.ElementAtOrDefault(term_id);
                mol.markBondTerm(term, marked);
            }
            else if (term_type == 1)
            {
                var term = mol.angleTerms.ElementAtOrDefault(term_id);
                mol.markAngleTerm(term, marked);
            }
            else if (term_type == 2)
            {
                var term = mol.torsionTerms.ElementAtOrDefault(term_id);
                mol.markTorsionTerm(term, marked);
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastModifyHyb)]
    private static void getModifyHyb(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var atom_id = message.GetUShort();
        var hyb = message.GetUShort();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.modifyHybrid(mol_id, atom_id, hyb))
            {
                Debug.LogError($"[NetworkManagerClient:getModifyHyb] Atom with id {atom_id} of Molecule {mol_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastKeepConfig)]
    private static void getKeepConfig(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var keep_config = message.GetBool();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.setKeepConfig(mol_id, keep_config))
            {
                Debug.LogError($"[NetworkManagerClient:getKeepConfig] Could not set keep config.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastReplaceDummies)]
    private static void getReplaceDummies(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            if (mol == null)
            {
                Debug.LogError($"[NetworkManagerClient:getReplaceDummies] Molecule {mol_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.List_curMolecules[mol_id].toggleDummies();
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastSettings)]
    private static void getSettings(Message message)
    {
        var client_id = message.GetUShort();
        var bondStiffness = message.GetUShort();
        var repulsionScale = message.GetFloat();
        var forceField = message.GetBool();
        var spatialMesh = message.GetBool();
        var handMesh = message.GetBool();
        var handJoints = message.GetBool();
        var handRay = message.GetBool();
        var handMenu = message.GetBool();
        var language = message.GetString();
        var gazeHighlighting = message.GetBool();
        var pointerHighlighting = message.GetBool();
        var showAllHighlightsOnClients = message.GetBool();
        var integrationMethodString = message.GetString();
        var timeFactors = message.GetFloats();
        var interactionModeString = message.GetString();
        var coop = message.GetBools();
        var networkMeasurements = message.GetBool();
        var interpolateColors = message.GetBool();
        var licoriceRendering = message.GetBool();
        var useAngstrom = message.GetBool();

        // Get enum entries from strings
        Enum.TryParse(integrationMethodString, ignoreCase: true, out ForceField.Method integrationMethod);
        Enum.TryParse(interactionModeString, ignoreCase: true, out GlobalCtrl.InteractionModes interactionMode);

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            SettingsData.bondStiffness = bondStiffness;
            SettingsData.repulsionScale = repulsionScale;
            SettingsData.forceField = forceField;
            SettingsData.spatialMesh = spatialMesh;
            SettingsData.handMesh = handMesh;
            SettingsData.handJoints = handJoints;
            SettingsData.handRay = handRay;
            SettingsData.handMenu = handMenu;
            SettingsData.language = language;
            SettingsData.gazeHighlighting = gazeHighlighting;
            SettingsData.pointerHighlighting = pointerHighlighting;
            SettingsData.showAllHighlightsOnClients = showAllHighlightsOnClients;
            SettingsData.integrationMethod = integrationMethod;
            SettingsData.timeFactors = timeFactors;
            SettingsData.interactionMode = interactionMode;
            SettingsData.coop = coop;
            SettingsData.networkMeasurements = networkMeasurements;
            SettingsData.interpolateColors = interpolateColors;
            SettingsData.licoriceRendering = licoriceRendering;
            SettingsData.useAngstrom = useAngstrom;
            settingsControl.Singleton.updateSettings();
            if (appSettings.Singleton != null)
            {
                appSettings.Singleton.updateVisuals();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastFocusHighlight)]
    private static void getFocusHighlight(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var atom_id = message.GetUShort();
        var active = message.GetBool();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            var atom = mol.atomList.ElementAtOrNull(atom_id, null);
            if (mol == null || atom == null)
            {
                Debug.LogError($"[NetworkManagerClient:getFocusHighlight] Molecule {mol_id} or atom {atom_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            atom.networkSetFocus(active, UserClient.list[client_id].highlightFocusID);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastScaleMolecule)]
    private static void getScaleMolecule(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var scale = message.GetFloat();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            if (mol == null)
            {
                Debug.LogError($"[NetworkManagerClient:getScaleMolecule] Molecule {mol_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.List_curMolecules[mol_id].transform.localScale = scale * Vector3.one;
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastFreezeAtom)]
    private static void getFreezeAtom(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var atom_id = message.GetUShort();
        var freeze = message.GetBool();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            var atom = mol.atomList.ElementAtOrNull(atom_id, null);
            if (mol == null || atom == null)
            {
                Debug.LogError($"[NetworkManagerClient:getFreezeAtom] Molecule {mol_id} or Atom {atom_id} does not exists.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            atom.freeze(freeze);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastFreezeMolecule)]
    private static void getFreezeMolecule(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetGuid();
        var freeze = message.GetBool();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
            if (mol == null)
            {
                Debug.LogError($"[NetworkManagerClient:getFreezeMolecule] Molecule {mol_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.List_curMolecules[mol_id].freeze(freeze);
        }
    }


    [MessageHandler((ushort)ServerToClientID.bcastSnapMolecules)]
    private static void getSnapMolecules(Message message)
    {
        var client_id = message.GetUShort();
        var mol1_id = message.GetGuid();
        var mol2_id = message.GetGuid();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol1 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol1_id);
            var mol2 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol2_id);
            if (mol1 == null || mol2 == null)
            {
                Debug.LogError($"[NetworkManagerClient:getSnapMolecules] Molecule {mol1_id} or {mol2_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            mol1.setSnapColors(mol2);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastCreateMeasurement)]
    private static void getCreateMeasurement(Message message)
    {
        var client_id = message.GetUShort();
        var mol1ID = message.GetGuid();
        var atom1ID = message.GetUShort();
        var mol2ID = message.GetGuid();
        var atom2ID = message.GetUShort();

        // do the merge
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!DistanceMeasurement.Create(mol1ID, atom1ID, mol2ID, atom2ID))
            {
                Debug.LogError($"[NetworkManagerClient] Create measuurement cannot be executed. Atom IDs do not exist (Atom1: {atom1ID}, Atom2 {atom2ID}).\nRequesing world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastClearMeasurements)]
    private static void getClearMeasurements(Message message)
    {
        var client_id = message.GetUShort();

        // do the merge
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            GlobalCtrl.Singleton.deleteAllMeasurements();
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastServerFocusHighlight)]
    private static void getSeverFocusHighlight(Message message)
    {
        var mol_id = message.GetGuid();
        var atom_id = message.GetUShort();
        var value = message.GetBool();

        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
        var atom = mol.atomList.ElementAtOrNull(atom_id, null);
        if (mol == null || atom == null)
        {
            Debug.LogError($"[NetworkManagerClient:getSeverFocusHighlight] Molecule {mol_id} or Atom {atom_id} does not exist.\nRequesting world sync.");
            NetworkManagerClient.Singleton.sendSyncRequest();
            return;
        }
        atom.serverFocusHighlight(value);
    }

    [MessageHandler((ushort)ServerToClientID.bcastNumOutlines)]
    private static void getNumOutlines(Message message)
    {
        var num_outlines = message.GetInt();
        Debug.Log($"[NetworkManagerClient] Got num_outlines {num_outlines}");
        GlobalCtrl.Singleton.changeNumOutlines(num_outlines);
    }
    
    #endregion

    }
