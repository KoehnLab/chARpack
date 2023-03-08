using Microsoft.MixedReality.Toolkit.UI;
using RiptideNetworking;
using RiptideNetworking.Utils;
using StructClass;
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
    //public GameObject playspace;
    //public GameObject sceneContent;

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
        //// put MRPlayspace in userWorld coordinates
        //playspace.transform.position = LoginData.offsetPos;
        //playspace.transform.rotation = LoginData.offsetRot;
        //// put MRSceneContent in userWorld coordinates
        //sceneContent.transform.position = LoginData.offsetPos;
        //sceneContent.transform.rotation = LoginData.offsetRot;

    }

    private void Start()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
        showErrorPrefab = (GameObject)Resources.Load("prefabs/confirmLoadDialog");

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
        EventManager.Singleton.OnMergeMolecule += sendMoleculeMerged;
        EventManager.Singleton.OnLoadMolecule += sendMoleculeLoaded;
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
    }

    private void FixedUpdate()
    {
        Client.Tick();
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
    private void FailedToConnect(object sender, EventArgs e)
    {
        var myDialog = Dialog.Open(showErrorPrefab, DialogButtonType.OK, "Connection Failed", $"Connection to {LoginData.ip}:{LoginData.port} failed\nGoing back to Login Screen.", true);
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
        var myDialog = Dialog.Open(showErrorPrefab, DialogButtonType.OK, "Connection Failed", $"Connection to {LoginData.ip}:{LoginData.port} failed\nGoing back to Login Screen.", true);
        if (myDialog != null)
        {
            myDialog.OnClosed += OnClosedDialogEvent;
        }
    }

    /// <summary>
    /// Returns the type of this device
    /// </summary>
    public ushort getDeviceType()
    {
        myDeviceType currentDeviceType;
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            currentDeviceType = myDeviceType.Mobile;
        }
        else if (SystemInfo.deviceType == DeviceType.Desktop)
        {
            currentDeviceType = myDeviceType.PC;
        }
        else
        {
            currentDeviceType = myDeviceType.HoloLens;
        }

        return (ushort)currentDeviceType;
    }

    #region Sends
    /// <summary>
    /// Sends a message with the device name and the device type to the server
    /// </summary>
    public void sendName()
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.deviceNameAndType);
        message.AddString(SystemInfo.deviceName);
        message.AddUShort(getDeviceType());
        message.AddVector3(LoginData.offsetPos);
        message.AddQuaternion(LoginData.offsetRot);
        Client.Send(message);
    }

    public void sendSyncRequest()
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.syncMe);
        Client.Send(message);
    }

    public void sendAtomCreated(ushort id, string abbre, Vector3 pos)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.atomCreated);
        message.AddUShort(id);
        message.AddString(abbre);
        message.AddVector3(pos);
        Client.Send(message);
    }

    public void sendMoleculeMoved(ushort id, Vector3 pos, Quaternion quat)
    {
        Message message = Message.Create(MessageSendMode.unreliable, ClientToServerID.moleculeMoved);
        message.AddUShort(id);
        message.AddVector3(pos);
        message.AddQuaternion(quat);
        Client.Send(message);
    }

    public void sendAtomMoved(ushort mol_id, ushort atom_id, Vector3 pos)
    {
        Message message = Message.Create(MessageSendMode.unreliable, ClientToServerID.atomMoved);
        message.AddUShort(mol_id);
        message.AddUShort(atom_id);
        message.AddVector3(pos);
        Client.Send(message);
    }

    public void sendMoleculeMerged(ushort mol1ID, ushort atom1ID, ushort mol2ID, ushort atom2ID)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.moleculeMerged);
        message.AddUShort(mol1ID);
        message.AddUShort(atom1ID);
        message.AddUShort(mol2ID);
        message.AddUShort(atom2ID);
        Client.Send(message);
    }

    public void sendMoleculeLoaded(string name)
    {
        var molData = GlobalCtrl.Singleton.getMoleculeData(name);
        NetworkUtils.serializeCmlData((ushort)ClientToServerID.moleculeLoaded, molData, chunkSize, true);
    }

    public void sendDeleteEverything()
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.deleteEverything);
        Client.Send(message);
    }
    
    public void sendSelectAtom(ushort mol_id, ushort atom_id, bool selected)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.selectAtom);
        message.AddUShort(mol_id);
        message.AddUShort(atom_id);
        message.AddBool(selected);
        Client.Send(message);
    }

    public void sendSelectMolecule(ushort id, bool selected)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.selectMolecule);
        message.AddUShort(id);
        message.AddBool(selected);
        Client.Send(message);
    }

    public void sendSelectBond(ushort bond_id, ushort mol_id, bool selected)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.selectBond);
        message.AddUShort(bond_id);
        message.AddUShort(mol_id);
        message.AddBool(selected);
        Client.Send(message);
    }

    public void sendDeleteAtom(ushort mol_id, ushort atom_id)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.deleteAtom);
        message.AddUShort(mol_id);
        message.AddUShort(atom_id);
        Client.Send(message);
    }

    public void sendDeleteMolecule(ushort id)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.deleteMolecule);
        message.AddUShort(id);
        Client.Send(message);
    }

    public void sendDeleteBond(ushort bond_id, ushort mol_id)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.deleteBond);
        message.AddUShort(bond_id);
        message.AddUShort(mol_id);
        Client.Send(message);
    }

    public void sendChangeAtom(ushort mol_id, ushort atom_id, string chemAbbre)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.changeAtom);
        message.AddUShort(mol_id);
        message.AddUShort(atom_id);
        message.AddString(chemAbbre);
        Client.Send(message);
    }

    public void sendUndo()
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.undo);
        Client.Send(message);
    }

    public void sendEnableForceField(bool enableForceField)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.enableForceField);
        message.AddBool(enableForceField);
        Client.Send(message);
    }


    public void sendChangeBondTerm(ForceField.BondTerm term, ushort mol_id, ushort term_id)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.changeBondTerm);
        message.AddUShort(mol_id);
        message.AddUShort(term_id);
        message.AddBondTerm(term);
        Client.Send(message);
    }

    public void sendChangeAngleTerm(ForceField.AngleTerm term, ushort mol_id, ushort term_id)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.changeAngleTerm);
        message.AddUShort(mol_id);
        message.AddUShort(term_id);
        message.AddAngleTerm(term);
        Client.Send(message);
    }

    public void sendChangeTorsionTerm(ForceField.TorsionTerm term, ushort mol_id, ushort term_id)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.changeTorsionTerm);
        message.AddUShort(mol_id);
        message.AddUShort(term_id);
        message.AddTorsionTerm(term);
        Client.Send(message);
    }

    public void sendMarkTerm(ushort term_type, ushort mol_id, ushort term_id, bool marked)
    {
        Message message = Message.Create(MessageSendMode.reliable, ClientToServerID.markTerm);
        message.AddUShort(term_type);
        message.AddUShort(mol_id);
        message.AddUShort(term_id);
        message.AddBool(marked);
        Client.Send(message);
    }

    #endregion

    #region Listen

    [MessageHandler((ushort)ServerToClientID.sendAtomWorld)]
    private static void listenForAtomWorld(Message message)
    {
        NetworkUtils.deserializeCmlData(message, ref cmlTotalBytes, ref cmlWorld, chunkSize);
    }

    [MessageHandler((ushort)ServerToClientID.bcastAtomCreated)]
    private static void getAtomCreated(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetUShort();
        var abbre = message.GetString();
        var pos = message.GetVector3();

        // do the create
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (GlobalCtrl.Singleton.List_curMolecules.Find(x => mol_id == x.m_id) != null)
            {
                Debug.LogError($"[NetworkManagerServer:getAtomCreated] Molecule with new creation id {mol_id} already exists.\nRequesing world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.CreateAtom(mol_id, abbre, pos, true);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastMoleculeMoved)]
    private static void getMoleculeMoved(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetUShort();
        var pos = message.GetVector3();
        var quat = message.GetQuaternion();

        // do the move
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.moveMolecule(mol_id, pos, quat))
            {
                Debug.LogError($"[NetworkManagerServer:getMoleculeMoved] Molecule with id {mol_id} does not exists.\nRequesing world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }

    }

    [MessageHandler((ushort)ServerToClientID.bcastAtomMoved)]
    private static void getAtomMoved(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetUShort();
        var atom_id = message.GetUShort();
        var pos = message.GetVector3();

        // do the move
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.moveAtom(mol_id, atom_id, pos))
            {
                Debug.LogError($"[NetworkManagerServer:getAtomMoved] Atom with id {atom_id} of Molecule {mol_id} does not exists.\nRequesing world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastMoleculeMerged)]
    private static void getMoleculeMerged(Message message)
    {
        var client_id = message.GetUShort();
        var mol1ID = message.GetUShort();
        var atom1ID = message.GetUShort();
        var mol2ID = message.GetUShort();
        var atom2ID = message.GetUShort();

        // do the merge
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol1ID).atomList.ElementAtOrDefault(atom1ID) == null ||
                GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol2ID).atomList.ElementAtOrDefault(atom2ID) == null)
            {
                Debug.LogError($"[NetworkManagerClient] Merging operation cannot be executed. Atom IDs do not exist (Atom1: {atom1ID}, Atom2 {atom2ID}).\nRequesing world sync.");
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
        var mol_id = message.GetUShort();
        var atom_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var atom = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id).atomList.ElementAtOrDefault(atom_id);
            if (atom == default)
            {
                Debug.LogError($"[NetworkManagerClient:getAtomSelected] Atom with id {atom_id} does not exist.\nRequesing world sync.");
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
        var mol_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
            if (mol == default)
            {
                Debug.LogError($"[NetworkManagerClient:getMoleculeSelected] Molecule with id {mol_id} does not exist.\nRequesing world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            mol.markMolecule(selected, true);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastSelectBond)]
    private static void getBondSelected(Message message)
    {
        var client_id = message.GetUShort();
        var bond_id = message.GetUShort();
        var mol_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
            var bond = mol.bondList.ElementAtOrDefault(bond_id);
            if (mol == default || bond == default)
            {
                Debug.LogError($"[NetworkManagerClient:getBondSelected] Bond with id {bond_id} or molecule with id {mol_id} does not exist.\nRequesing world sync.");
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
        var mol_id = message.GetUShort();
        var atom_id = message.GetUShort();
        // do the delete
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var atom = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id).atomList.ElementAtOrDefault(atom_id);
            if (atom == default)
            {
                Debug.LogError($"[NetworkManagerClient:getAtomDeleted] Atom with id {atom_id} does not exist.\nRequesing world sync.");
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
        var mol_id = message.GetUShort();
        // do the delete
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
            if (mol == default)
            {
                Debug.LogError($"[NetworkManagerClient:getMoleculeSelected] Molecule with id {mol_id} does not exist.\nRequesing world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            GlobalCtrl.Singleton.deleteMolecule(mol);
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastDeleteBond)]
    private static void getBondDeleted(Message message)
    {
        var client_id = message.GetUShort();
        var bond_id = message.GetUShort();
        var mol_id = message.GetUShort();
        // do the delete
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
            var bond = mol.bondList.ElementAtOrDefault(bond_id);
            if (mol == default || bond == default)
            {
                Debug.LogError($"[NetworkManagerClient:getBondSelected] Bond with id {bond_id} or molecule with id {mol_id} does not exist.\nRequesing world sync.");
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
        var mol_id = message.GetUShort();
        var atom_id = message.GetUShort();
        var chemAbbre = message.GetString();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.changeAtom(mol_id, atom_id, chemAbbre))
            {
                Debug.LogError($"[NetworkManagerServer:getAtomChanged] Atom with id {atom_id} of Molecule {mol_id} does not exists.\nRequesing world sync.");
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
        var mol_id = message.GetUShort();
        var term_id = message.GetUShort();
        var term = message.GetBondTerm();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.changeBondTerm(mol_id, term_id, term))
            {
                Debug.LogError($"[NetworkManagerServer:getAtomChanged] Bond term with id {term_id} of Molecule {mol_id} does not exists.\nRequesing world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastChangeAngleTerm)]
    private static void getAngleTermChanged(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetUShort();
        var term_id = message.GetUShort();
        var term = message.GetAngleTerm();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.changeAngleTerm(mol_id, term_id, term))
            {
                Debug.LogError($"[NetworkManagerServer:getAtomChanged] Angle term with id {term_id} of Molecule {mol_id} does not exists.\nRequesing world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
            }
        }
    }

    [MessageHandler((ushort)ServerToClientID.bcastChangeTorsionTerm)]
    private static void getTorsionTermChanged(Message message)
    {
        var client_id = message.GetUShort();
        var mol_id = message.GetUShort();
        var term_id = message.GetUShort();
        var term = message.GetTorsionTerm();

        // do the change
        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {
            if (!GlobalCtrl.Singleton.changeTorsionTerm(mol_id, term_id, term))
            {
                Debug.LogError($"[NetworkManagerServer:getAtomChanged] Angle term with id {term_id} of Molecule {mol_id} does not exists.\nRequesing world sync.");
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
        var mol_id = message.GetUShort();
        var term_id = message.GetUShort();
        var marked = message.GetBool();

        if (client_id != NetworkManagerClient.Singleton.Client.Id)
        {

            // do the change
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
            if (mol == default)
            {
                Debug.LogError($"[NetworkManagerServer:getMarkTerm] Molecule with id {mol_id} does not exists.\nRequesing world sync.");
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

    #endregion

}
