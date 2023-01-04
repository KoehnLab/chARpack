using RiptideNetworking;
using RiptideNetworking.Utils;
using StructClass;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkManagerServer : MonoBehaviour
{
    private static NetworkManagerServer _singleton;

    public static NetworkManagerServer Singleton
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
                Debug.Log($"[{nameof(NetworkManagerServer)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    public Server Server { get; private set; }

    private bool ServerStarted = false;

    private static byte[] cmlTotalBytes;
    private static List<cmlData> cmlWorld;
    private static ushort chunkSize = 255;
    private static bool receiveComplete = false;

    public GameObject userWorld;

    private void Awake()
    {
        Singleton = this;
        // create user world
        userWorld = new GameObject("UserWorld");
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        StartServer();

        EventManager.Singleton.OnCmlReceiveCompleted += flagReceiveComplete;
    }


    /// <summary>
    /// Starts the actual riptide server
    /// </summary>
    public static void StartServer()
    {
        Singleton.Server = new Server();
        Singleton.Server.Start(LoginData.port, LoginData.maxConnections);
        Singleton.Server.ClientDisconnected += Singleton.ClientDisconnected;
        Singleton.Server.ClientConnected += Singleton.ClientConnected; // client invokes sendName

        Debug.Log("[NetworkManagerServer] Server started.");

        Singleton.ServerStarted = true;

    }

    private void flagReceiveComplete()
    {
        receiveComplete = true;
    }

    private void FixedUpdate()
    {
        if (ServerStarted)
        {
            Server.Tick();
        }
    }

    private void OnApplicationQuit()
    {
        if (ServerStarted)
        {
            Server.Stop();
        }
    }

    /// <summary>
    /// Callback on client disconnection
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
    {
        Debug.Log($"[NetworkManagerServer] Client {e.Id} disconnected. Cleaning up.");
        // destroy user gameObject
        Destroy(UserServer.list[e.Id].gameObject);
    }

    private void ClientConnected(object sender, ServerClientConnectedEventArgs e)
    {
        // send current atom world
        Debug.Log($"[NetworkManagerServer] Client {e.Client.Id} connected. Sending current world.");
        var atomWorld = GlobalCtrl.Singleton.saveAtomWorld();
        sendAtomWorld(atomWorld, e.Client.Id);
    }

    #region MessageHandler

    [MessageHandler((ushort)ClientToServerID.atomCreated)]
    private static void getAtomCreated(ushort fromClientId, Message message)
    {
        var atom_id = message.GetUShort();
        var abbre = message.GetString();
        var pos = message.GetVector3();
        // do the create on the server
        GlobalCtrl.Singleton.CreateAtom(atom_id, abbre, pos, true);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastAtomCreated);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(atom_id);
        outMessage.AddString(abbre);
        outMessage.AddVector3(pos);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.moleculeMoved)]
    private static void getMoleculeMoved(ushort fromClientId, Message message)
    {
        var molecule_id = message.GetUShort();
        var pos = message.GetVector3();
        var quat = message.GetQuaternion();
        // do the move on the server
        GlobalCtrl.Singleton.moveMolecule(molecule_id, pos, quat);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastMoleculeMoved);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(molecule_id);
        outMessage.AddVector3(pos);
        outMessage.AddQuaternion(quat);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.atomMoved)]
    private static void getAtomMoved(ushort fromClientId, Message message)
    {
        var atom_id = message.GetUShort();
        var pos = message.GetVector3();
        // do the move on the server
        GlobalCtrl.Singleton.moveAtom(atom_id, pos);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.unreliable, ServerToClientID.bcastAtomMoved);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(atom_id);
        outMessage.AddVector3(pos);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.moleculeMerged)]
    private static void getMoleculeMerged(ushort fromClientId, Message message)
    {
        var atom1ID = message.GetUShort();
        var atom2ID = message.GetUShort();

        // do the merge on the server
        // fist check the existence of atoms with the correspoinding ids
        if (GlobalCtrl.Singleton.List_curAtoms.ElementAtOrDefault(atom1ID) == null || GlobalCtrl.Singleton.List_curAtoms.ElementAtOrDefault(atom2ID) == null)
        {
            Debug.LogError($"[NetworkManagerServer] Merging operation cannot be executed. Atom IDs do not exist (Atom1: {atom1ID}, Atom2 {atom2ID})");
            return;
        }
        GlobalCtrl.Singleton.MergeMolecule(GlobalCtrl.Singleton.List_curAtoms[atom1ID], GlobalCtrl.Singleton.List_curAtoms[atom2ID]);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastMoleculeMerged);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(atom1ID);
        outMessage.AddUShort(atom2ID);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }


    public void sendAtomWorld(List<cmlData> world, ushort toClientID)
    {
        if (world.Count < 1) return;
        NetworkUtils.serializeCmlData((ushort)ServerToClientID.sendAtomWorld, world, chunkSize, false, toClientID);
    }


    [MessageHandler((ushort)ClientToServerID.sendAtomWorld)]
    private static void listenForAtomWorld(ushort fromClientId, Message message)
    {
        NetworkUtils.deserializeCmlData(message, ref cmlTotalBytes, ref cmlWorld, chunkSize);

        // do bcast?
    }

    [MessageHandler((ushort)ClientToServerID.moleculeLoaded)]
    private static void bcastMoleculeLoad(ushort fromClientId, Message message)
    {
        receiveComplete = false;
        NetworkUtils.deserializeCmlData(message, ref cmlTotalBytes, ref cmlWorld, chunkSize, false);

        // do the bcast
        if (receiveComplete)
        {
            foreach (var client in UserServer.list.Values)
            {
                if (client.ID != fromClientId)
                {
                    NetworkUtils.serializeCmlData((ushort)ServerToClientID.bcastMoleculeLoad, cmlWorld, chunkSize, false, client.ID);
                }
            }
        }
    }

    [MessageHandler((ushort)ClientToServerID.deleteEverything)]
    private static void bcastDeleteEverything(ushort fromClientId, Message message)
    {
        GlobalCtrl.Singleton.DeleteAll();
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastDeleteEverything);
        outMessage.AddUShort(fromClientId);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.selectAtom)]
    private static void getAtomSelected(ushort fromClientId, Message message)
    {
        var atom_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select on the server
        // don't show the tooltip - may change later
        var atom = GlobalCtrl.Singleton.List_curAtoms.ElementAtOrDefault(atom_id);
        if (atom == default)
        {
            Debug.LogError($"[NetworkManagerServer:getAtomSelected] Atom with id {atom_id} does not exist.");
            return;
        }
        if (atom.m_molecule.isMarked)
        {
            atom.m_molecule.markMolecule(false);
        }
        atom.markAtom(selected);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastSelectAtom);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(atom_id);
        outMessage.AddBool(selected);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.selectMolecule)]
    private static void getMoleculeSelected(ushort fromClientId, Message message)
    {
        var mol_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select on the server
        // don't show the tooltip - may change later
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
        if (mol == default)
        {
            Debug.LogError($"[NetworkManagerClient:getMoleculeSelected] Molecule with id {mol_id} does not exist.");
            return;
        }
        mol.markMolecule(selected);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastSelectMolecule);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        outMessage.AddBool(selected);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.selectBond)]
    private static void getBondSelected(ushort fromClientId, Message message)
    {
        var bond_id = message.GetUShort();
        var mol_id = message.GetUShort();
        var selected = message.GetBool();
        // do the select on the server
        // don't show the tooltip - may change later
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
        var bond = mol.bondList.ElementAtOrDefault(bond_id);
        if (mol == default || bond == default)
        {
            Debug.LogError($"[NetworkManagerClient:getBondSelected] Bond with id {bond_id} or molecule with id {mol_id} does not exist.");
            return;
        }
        bond.markBond(selected);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastSelectBond);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(bond_id);
        outMessage.AddUShort(mol_id);
        outMessage.AddBool(selected);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.deleteAtom)]
    private static void getAtomDeleted(ushort fromClientId, Message message)
    {
        var atom_id = message.GetUShort();
        // do the select on the server
        // don't show the tooltip - may change later
        var atom = GlobalCtrl.Singleton.List_curAtoms.ElementAtOrDefault(atom_id);
        if (atom == default)
        {
            Debug.LogError($"[NetworkManagerServer:getAtomDeleted] Atom with id {atom_id} does not exist.");
            return;
        }
        GlobalCtrl.Singleton.deleteAtom(atom);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastDeleteAtom);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(atom_id);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.deleteMolecule)]
    private static void getMoleculeDeleted(ushort fromClientId, Message message)
    {
        var mol_id = message.GetUShort();
        // do the select on the server
        // don't show the tooltip - may change later
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
        if (mol == default)
        {
            Debug.LogError($"[NetworkManagerServer:getMoleculeDeleted] Molecule with id {mol_id} does not exist.");
            return;
        }
        GlobalCtrl.Singleton.deleteMolecule(mol);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastDeleteMolecule);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(mol_id);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    [MessageHandler((ushort)ClientToServerID.deleteBond)]
    private static void getBondDeleted(ushort fromClientId, Message message)
    {
        var bond_id = message.GetUShort();
        var mol_id = message.GetUShort();
        // do the select on the server
        // don't show the tooltip - may change later
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(mol_id);
        var bond = mol.bondList.ElementAtOrDefault(bond_id);
        if (mol == default || bond == default)
        {
            Debug.LogError($"[NetworkManagerServer:getBondSelected] Bond with id {bond_id} or molecule with id {mol_id} does not exist.");
            return;
        }
        GlobalCtrl.Singleton.deleteBond(bond);

        // Broadcast to other clients
        Message outMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.bcastDeleteBond);
        outMessage.AddUShort(fromClientId);
        outMessage.AddUShort(bond_id);
        outMessage.AddUShort(mol_id);
        NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
    }

    #endregion
}
