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

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        StartServer();
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

    #region Messages

    [MessageHandler((ushort)ClientToServerID.atomCreated)]
    private static void getAtomCreated(ushort fromClientId, Message message)
    {
        var atom_id = message.GetUShort();
        var abbre = message.GetString();
        var pos = message.GetVector3();
        // do the create on the server
        GlobalCtrl.Singleton.CreateAtom(atom_id, abbre, pos);

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
        // prepare clients for the messages'
        Message startMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.sendAtomWorld);
        startMessage.AddString("start");
        NetworkManagerServer.Singleton.Server.Send(startMessage, toClientID);

        // we need all meta data so we do the splitting of the world first
        for (ushort i = 0; i < world.Count; i++)
        {
            var currentCml = world[i];
            var totalBytes = Serializer.Serialize(currentCml);
            uint totalLength = (uint)totalBytes.Length; // first
            ushort rest = (ushort)(totalBytes.Length % chunkSize);
            ushort numPieces = rest == 0 ? (ushort)(totalBytes.Length / chunkSize) : (ushort)((totalBytes.Length / chunkSize) + 1); // second
            //
            List<ushort> bytesPerPiece = new List<ushort>();
            for (ushort j = 0; j < (numPieces - 1); j++)
            {
                bytesPerPiece.Add(chunkSize);
            }
            if (rest != 0)
            {
                bytesPerPiece.Add(rest);
            } else
            {
                bytesPerPiece.Add(chunkSize);
            }
            
            // create pieces and messages
            for (ushort j = 0; j < numPieces; j++)
            {
                var currentPieceID = j; // third
                var piece = totalBytes[..bytesPerPiece[j]]; // forth
                totalBytes = totalBytes[bytesPerPiece[j]..];
                Message message = Message.Create(MessageSendMode.reliable, ServerToClientID.sendAtomWorld);
                message.AddString("data");
                message.AddUInt(totalLength);
                message.AddUShort(numPieces);
                message.AddUShort(currentPieceID);
                message.AddBytes(piece);
                NetworkManagerServer.Singleton.Server.Send(message, toClientID);
            }
        }
        Message endMessage = Message.Create(MessageSendMode.reliable, ServerToClientID.sendAtomWorld);
        endMessage.AddString("end");
        NetworkManagerServer.Singleton.Server.Send(endMessage, toClientID);
    }


    [MessageHandler((ushort)ClientToServerID.sendAtomWorld)]
    private static void listenForAtomWorld(ushort fromClientId, Message message)
    {
        var state = message.GetString();
        if (state == "start")
        {
            Debug.Log("[NetworkManagerServer] Receiving atom world");
            cmlWorld = new List<cmlData>();
        }
        else if (state == "end")
        {
            GlobalCtrl.Singleton.DeleteAll();
            GlobalCtrl.Singleton.rebuildAtomWorld(cmlWorld);
        }
        else
        {
            // get rest of message
            var totalLength = message.GetUInt();
            var numPieces = message.GetUShort();
            var currentPieceID = message.GetUShort();
            var currentPiece = message.GetBytes();

            if (currentPieceID == 0)
            {
                cmlTotalBytes = new byte[totalLength];
                currentPiece.CopyTo(cmlTotalBytes, 0);
            } else if (currentPieceID == numPieces -1)
            {
                currentPiece.CopyTo(cmlTotalBytes, currentPieceID * chunkSize);
                cmlWorld.Add(Serializer.Deserialize<cmlData>(cmlTotalBytes));
            } else
            {
                currentPiece.CopyTo(cmlTotalBytes, currentPieceID * chunkSize);
            }
        }
    }

    #endregion
}
