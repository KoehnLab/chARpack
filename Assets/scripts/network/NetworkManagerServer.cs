using Riptide;
using Riptide.Utils;
using Riptide.Transports.Tcp;
using chARpack.Structs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;


namespace chARpack
{
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

        // CML deserialize memory
        private static byte[] cmlTotalBytes;
        private static List<cmlData> cmlWorld;
        private static sGenericObject sGO;
        private static ushort chunkSize = 255;
        private static bool receiveComplete = false;

        // Structure deserialize memory
        private static string svg_content;
        private static List<Vector2> svg_coords;

        private GameObject _userWorld;
        public GameObject UserWorld { get => _userWorld; private set => _userWorld = value; }
        private TransitionManager.SyncMode currentSyncMode = SettingsData.syncMode;

        public delegate void OnScreenSizeChangedAction();
        public event OnScreenSizeChangedAction OnScreenSizeChanged;
        public void ScreenSizeChanged()
        {
            OnScreenSizeChanged?.Invoke();
        }

        private void Awake()
        {
            LoginData.isServer = true;
            Singleton = this;
            // create user world
            UserWorld = new GameObject("UserWorld");
        }

        private void Start()
        {
            Application.targetFrameRate = 60;

            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            StartServer();

            EventManager.Singleton.OnStructureReceiveCompleted += structureReceiveComplete;
            EventManager.Singleton.OnUpdateSettings += bcastSettings;
            EventManager.Singleton.OnMRCapture += sendMRCapture;
            EventManager.Singleton.OnSetNumOutlines += bcastNumOutlines;
            EventManager.Singleton.OnSyncModeChanged += bcastSyncMode;
            EventManager.Singleton.OnForwardDeleteMarkedRequest += bcastDeleteMarkedRequest;
            EventManager.Singleton.OnDeleteEverything += bcastDeleteEverything;
            OnScreenSizeChanged += bcastScreenSizeChanged;
            if (currentSyncMode == TransitionManager.SyncMode.Sync)
            {
                activateSync();
            }
            else
            {
                activateAsync();
            }
            // set actual server viewport
            //SettingsData.serverViewport = new Vector2(Camera.main.pixelRect.width, Camera.main.pixelRect.height);
            SettingsData.serverViewport = new Vector2(Screen.width, Screen.height);
            Debug.Log($"[NetworkManagerServer] Server viewport set to: {SettingsData.serverViewport}");
        }

        public void changeSyncMode(TransitionManager.SyncMode mode)
        {
            if (currentSyncMode != mode)
            {
                if (mode == TransitionManager.SyncMode.Sync)
                {
                    deactivateAsync();
                    activateSync();
                    // TODO Send scene content
                }
                else
                {
                    deactivateSync();
                    activateAsync();
                }
                currentSyncMode = mode;
            }
        }

        private void activateAsync()
        {
            EventManager.Singleton.OnTransitionMolecule += transitionMol;
            EventManager.Singleton.OnReceiveMoleculeTransition += TransitionManager.Singleton.getMoleculeTransitionServer;
            EventManager.Singleton.OnTransitionGenericObject += transitionGenericObject;
            EventManager.Singleton.OnReceiveGenericObjectTransition += TransitionManager.Singleton.getGenericObjectTransitionServer;
            EventManager.Singleton.OnRequestTransition += requestTransition;
            EventManager.Singleton.OnSpawnGhostObject += sendSpawnGhostObject;
            EventManager.Singleton.OnObjectToTrack += sendObjectToTrack;
            EventManager.Singleton.OnSpawnObjectCollection += sendSpawnObjectCollection;
            EventManager.Singleton.OnRequestResults += requestResults;
            EventManager.Singleton.OnSetInterpolationState += sendInterpolationState;
        }

        private void deactivateAsync()
        {
            EventManager.Singleton.OnTransitionMolecule -= transitionMol;
            EventManager.Singleton.OnReceiveMoleculeTransition -= TransitionManager.Singleton.getMoleculeTransitionServer;
            EventManager.Singleton.OnTransitionGenericObject -= transitionGenericObject;
            EventManager.Singleton.OnReceiveGenericObjectTransition -= TransitionManager.Singleton.getGenericObjectTransitionServer;
            EventManager.Singleton.OnRequestTransition -= requestTransition;
            EventManager.Singleton.OnSpawnGhostObject -= sendSpawnGhostObject;
            EventManager.Singleton.OnObjectToTrack -= sendObjectToTrack;
            EventManager.Singleton.OnSpawnObjectCollection -= sendSpawnObjectCollection;
            EventManager.Singleton.OnRequestResults -= requestResults;
            EventManager.Singleton.OnSetInterpolationState -= sendInterpolationState;
        }


        private void activateSync()
        {
            Debug.Log($"[NetworkManagerServer] Sync mode Activated");

            EventManager.Singleton.OnCmlReceiveCompleted += flagReceiveComplete;
            EventManager.Singleton.OnMoveAtom += bcastMoveAtom;
            EventManager.Singleton.OnStopMoveAtom += bcastStopMoveAtom;
            EventManager.Singleton.OnMergeMolecule += bcastMergeMolecule;
            EventManager.Singleton.OnSelectAtom += bcastSelectAtom;
            EventManager.Singleton.OnCreateAtom += bcastCreateAtom;
            EventManager.Singleton.OnDeleteAtom += bcastDeleteAtom;
            EventManager.Singleton.OnDeleteMolecule += bcastDeleteMolecule;
            EventManager.Singleton.OnReplaceDummies += bcastReplaceDummies;
            EventManager.Singleton.OnSelectMolecule += bcastSelectMolecule;
            EventManager.Singleton.OnSelectBond += bcastSelectBond;
            EventManager.Singleton.OnMarkTerm += bcastMarkTerm;
            EventManager.Singleton.OnMoveMolecule += bcastMoveMolecule;
            EventManager.Singleton.OnChangeBondTerm += bcastChangeBondTerm;
            EventManager.Singleton.OnChangeAngleTerm += bcastChangeAngleTerm;
            EventManager.Singleton.OnChangeTorsionTerm += bcastChangeTorsionTerm;
            EventManager.Singleton.OnModifyHyb += bcastModifyHyb;
            EventManager.Singleton.OnChangeAtom += bcastChangeAtom;
            EventManager.Singleton.OnDeleteBond += bcastDeleteBond;
            EventManager.Singleton.OnChangeMoleculeScale += bcastScaleMolecule;
            EventManager.Singleton.OnCreateMeasurement += bcastCreateMeasurement;
            EventManager.Singleton.OnClearMeasurements += bcastClearMeasurements;
            EventManager.Singleton.OnFreezeAtom += bcastFreezeAtom;
            EventManager.Singleton.OnFreezeMolecule += bcastFreezeMolecule;
            EventManager.Singleton.OnSetSnapColors += bcastSetSnapColors;
            EventManager.Singleton.OnServerFocusHighlight += bcastServerFocusHighlight;
            EventManager.Singleton.OnGenerate3DFormula += bcastFormula;
        }

        private void deactivateSync()
        {
            Debug.Log($"[NetworkManagerServer] Sync mode Deactivated");

            EventManager.Singleton.OnCmlReceiveCompleted -= flagReceiveComplete;
            EventManager.Singleton.OnMoveAtom -= bcastMoveAtom;
            EventManager.Singleton.OnStopMoveAtom -= bcastStopMoveAtom;
            EventManager.Singleton.OnMergeMolecule -= bcastMergeMolecule;
            EventManager.Singleton.OnSelectAtom -= bcastSelectAtom;
            EventManager.Singleton.OnCreateAtom -= bcastCreateAtom;
            EventManager.Singleton.OnDeleteAtom -= bcastDeleteAtom;
            EventManager.Singleton.OnDeleteMolecule -= bcastDeleteMolecule;
            EventManager.Singleton.OnReplaceDummies -= bcastReplaceDummies;
            EventManager.Singleton.OnSelectMolecule -= bcastSelectMolecule;
            EventManager.Singleton.OnSelectBond -= bcastSelectBond;
            EventManager.Singleton.OnMarkTerm -= bcastMarkTerm;
            EventManager.Singleton.OnMoveMolecule -= bcastMoveMolecule;
            EventManager.Singleton.OnChangeBondTerm -= bcastChangeBondTerm;
            EventManager.Singleton.OnChangeAngleTerm -= bcastChangeAngleTerm;
            EventManager.Singleton.OnChangeTorsionTerm -= bcastChangeTorsionTerm;
            EventManager.Singleton.OnModifyHyb -= bcastModifyHyb;
            EventManager.Singleton.OnChangeAtom -= bcastChangeAtom;
            EventManager.Singleton.OnDeleteBond -= bcastDeleteBond;
            EventManager.Singleton.OnChangeMoleculeScale -= bcastScaleMolecule;
            EventManager.Singleton.OnCreateMeasurement -= bcastCreateMeasurement;
            EventManager.Singleton.OnClearMeasurements -= bcastClearMeasurements;
            EventManager.Singleton.OnFreezeAtom -= bcastFreezeAtom;
            EventManager.Singleton.OnFreezeMolecule -= bcastFreezeMolecule;
            EventManager.Singleton.OnSetSnapColors -= bcastSetSnapColors;
            EventManager.Singleton.OnServerFocusHighlight -= bcastServerFocusHighlight;
            EventManager.Singleton.OnGenerate3DFormula -= bcastFormula;
        }


        /// <summary>
        /// Starts the actual riptide server
        /// </summary>
        public static void StartServer()
        {
            if (SettingsData.currentNetworkingProtocol == SettingsData.Protocol.TCP)
            {
                Singleton.Server = new Server(new TcpServer());
            }
            else
            {
                Singleton.Server = new Server();
            }

            Singleton.Server.TimeoutTime = 20000;
            Singleton.Server.Start(LoginData.port, LoginData.maxConnections);
            Singleton.Server.ClientDisconnected += Singleton.ClientDisconnected;
            Singleton.Server.ClientConnected += Singleton.ClientConnected; // client invokes sendName

            // set IP address indicator
            List<string> ips = new List<string>();
            IPHostEntry HostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in HostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(ip.ToString());
                }
            }
            ServerIPindicator.Singleton.setIP(string.Join("\n", ips));

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
                if (OnScreenSizeChanged == null) { return; }
                if (SettingsData.serverViewport.x != Screen.width || SettingsData.serverViewport.y != Screen.height)
                {
                    SettingsData.serverViewport.x = Screen.width;
                    SettingsData.serverViewport.y = Screen.height;
                    ScreenSizeChanged();
                }
                bcastMousePosition();
                Server.Update();
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
        private void ClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            Debug.Log($"[NetworkManagerServer] Client {e.Client.Id} disconnected. Cleaning up.");
            // destroy user gameObject and panel entry
            if (UserServer.list.ContainsKey(e.Client.Id))
            {
                Destroy(UserServer.list[e.Client.Id].gameObject);
            }
        }

        private void ClientConnected(object sender, ServerConnectedEventArgs e)
        {
            // send current atom world
            Debug.Log($"[NetworkManagerServer] Client {e.Client.Id} connected. Sending current world and settings.");
            if (currentSyncMode == TransitionManager.SyncMode.Sync)
            {
                var atomWorld = GlobalCtrl.Singleton.saveAtomWorld();
                sendAtomWorld(atomWorld, e.Client.Id);
            }
            bcastSettings();
        }

        #region Messages
        public void pushLoadMolecule(List<cmlData> molecule)
        {
            NetworkUtils.serializeCmlData(ServerToClientID.bcastMoleculeLoad, molecule, chunkSize, false);
        }

        public void sendMRCapture(ushort client_id, bool rec)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.MRCapture);
            message.AddBool(rec);
            Server.Send(message, client_id);
        }

        public void bcastMoveAtom(Guid mol_id, ushort atom_id, Vector3 pos)
        {
            // Broadcast to other clients
            Message message = NetworkUtils.createMessage(MessageSendMode.Unreliable, ServerToClientID.bcastAtomMoved);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddUShort(atom_id);
            message.AddVector3(pos);
            Server.SendToAll(message);
        }

        public void bcastStopMoveAtom(Guid mol_id, ushort atom_id)
        {
            // Broadcast to other clients
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastStopMoveAtom);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddUShort(atom_id);
            Server.SendToAll(message);
        }

        public void bcastMoveMolecule(Guid mol_id, Vector3 pos, Quaternion quat)
        {
            // Broadcast to other clients
            Message message = NetworkUtils.createMessage(MessageSendMode.Unreliable, ServerToClientID.bcastMoleculeMoved);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddVector3(pos);
            message.AddQuaternion(quat);
            Server.SendToAll(message);
        }

        public void bcastMergeMolecule(Guid mol1ID, ushort atom1ID, Guid mol2ID, ushort atom2ID)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastMoleculeMerged);
            message.AddUShort(0);
            message.AddGuid(mol1ID);
            message.AddUShort(atom1ID);
            message.AddGuid(mol2ID);
            message.AddUShort(atom2ID);
            Server.SendToAll(message);
        }

        public void bcastSelectAtom(Guid mol_id, ushort atom_id, bool selected)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSelectAtom);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddUShort(atom_id);
            message.AddBool(selected);
            Server.SendToAll(message);
        }

        public void bcastSelectMolecule(Guid mol_id, bool selected)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSelectMolecule);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddBool(selected);
            Server.SendToAll(message);
        }

        public void bcastSelectBond(ushort bond_id, Guid mol_id, bool selected)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSelectBond);
            message.AddUShort(0);
            message.AddUShort(bond_id);
            message.AddGuid(mol_id);
            message.AddBool(selected);
            Server.SendToAll(message);
        }

        public void bcastCreateAtom(Guid mol_id, string abbre, Vector3 pos, ushort hyb)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastAtomCreated);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddString(abbre);
            message.AddVector3(pos);
            message.AddUShort(hyb);
            Server.SendToAll(message);
        }

        public void bcastDeleteAtom(Guid mol_id, ushort atom_id)
        {
            //Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastDeleteAtom);
            //message.AddUShort(0);
            //message.AddGuid(mol_id);
            //message.AddUShort(atom_id);
            //Server.SendToAll(message);
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld());
        }

        public void bcastDeleteMolecule(Guid mol_id)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastDeleteMolecule);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            Server.SendToAll(message);
        }

        public void bcastReplaceDummies(Guid mol_id)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastReplaceDummies);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            Server.SendToAll(message);
        }

        public void bcastMarkTerm(ushort term_type, Guid mol_id, ushort term_id, bool marked)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastMarkTerm);
            message.AddUShort(0);
            message.AddUShort(term_type);
            message.AddGuid(mol_id);
            message.AddUShort(term_id);
            message.AddBool(marked);
            Server.SendToAll(message);
        }

        public void bcastChangeBondTerm(ForceField.BondTerm term, Guid mol_id, ushort term_id)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastChangeBondTerm);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddUShort(term_id);
            message.AddBondTerm(term);
            Server.SendToAll(message);
        }

        public void bcastChangeAngleTerm(ForceField.AngleTerm term, Guid mol_id, ushort term_id)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastChangeAngleTerm);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddUShort(term_id);
            message.AddAngleTerm(term);
            Server.SendToAll(message);
        }

        public void bcastChangeTorsionTerm(ForceField.TorsionTerm term, Guid mol_id, ushort term_id)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastChangeTorsionTerm);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddUShort(term_id);
            message.AddTorsionTerm(term);
            Server.SendToAll(message);
        }

        public void bcastModifyHyb(Guid mol_id, ushort atom_id, ushort hyb)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastModifyHyb);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddUShort(atom_id);
            message.AddUShort(hyb);
            Server.SendToAll(message);
        }

        public void bcastChangeAtom(Guid mol_id, ushort atom_id, string chemAbbre)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastChangeAtom);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddUShort(atom_id);
            message.AddString(chemAbbre);
            Server.SendToAll(message);
        }

        public void bcastDeleteBond(ushort bond_id, Guid mol_id)
        {
            //Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastDeleteBond);
            //message.AddUShort(0);
            //message.AddUShort(bond_id);
            //message.AddGuid(mol_id);
            //Server.SendToAll(message);
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld());
        }

        public void bcastSettings()
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSettings);
            message.AddUShort(0);
            message.AddUShort(SettingsData.bondStiffness);
            message.AddFloat(SettingsData.repulsionScale);
            message.AddBool(SettingsData.forceField);
            message.AddBool(SettingsData.spatialMesh);
            message.AddBool(SettingsData.handMesh);
            message.AddBool(SettingsData.handJoints);
            message.AddBool(SettingsData.handRay);
            message.AddBool(SettingsData.handMenu);
            message.AddString(SettingsData.language);
            message.AddBool(SettingsData.gazeHighlighting);
            message.AddBool(SettingsData.pointerHighlighting);
            message.AddBool(SettingsData.showAllHighlightsOnClients);
            message.AddString(SettingsData.integrationMethod.ToString());
            message.AddFloats(SettingsData.timeFactors);
            message.AddString(SettingsData.interactionMode.ToString());
            message.AddBools(SettingsData.coop);
            message.AddBool(SettingsData.networkMeasurements);
            message.AddBool(SettingsData.interpolateColors);
            message.AddBool(SettingsData.licoriceRendering);
            message.AddBool(SettingsData.useAngstrom);
            message.AddVector2(SettingsData.serverViewport);
            message.AddInt((int)SettingsData.syncMode);
            message.AddInt((int)SettingsData.transitionMode);
            message.AddInt((int)SettingsData.immersiveTarget);
            message.AddBool(SettingsData.requireGrabHold);
            message.AddInt((int)SettingsData.handedness);
            message.AddInt((int)SettingsData.transitionAnimation);
            message.AddFloat(SettingsData.transitionAnimationDuration);
            message.AddInt((int)SettingsData.desktopTarget);
            message.AddInt(SettingsData.randomSeed);
            message.AddInt((int)SettingsData.allowedTransitionInteractions);
            message.AddBool(SettingsData.allowThrowing);
            message.AddBool(SettingsData.hoverGazeAsSelection);
            message.AddFloat(SettingsData.defaultMoleculeSize);
            message.AddBool(SettingsData.twoDimensionalMode);
            Server.SendToAll(message);
        }

        public void bcastScaleMolecule(Guid mol_id, float scale)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastScaleMolecule);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddFloat(scale);
            Server.SendToAll(message);
        }

        public void bcastCreateMeasurement(Guid mol1_id, ushort atom1_id, Guid mol2_id, ushort atom2_id)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastCreateMeasurement);
            message.AddUShort(0);
            message.AddGuid(mol1_id);
            message.AddUShort(atom1_id);
            message.AddGuid(mol2_id);
            message.AddUShort(atom2_id);
            Server.SendToAll(message);
        }

        public void bcastClearMeasurements()
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastClearMeasurements);
            message.AddUShort(0);
            Server.SendToAll(message);
        }

        public void bcastFreezeAtom(Guid mol_id, ushort atom_id, bool freeze)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastFreezeAtom);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddUShort(atom_id);
            message.AddBool(freeze);
            Server.SendToAll(message);
        }

        public void bcastFreezeMolecule(Guid mol_id, bool freeze)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastFreezeMolecule);
            message.AddUShort(0);
            message.AddGuid(mol_id);
            message.AddBool(freeze);
            Server.SendToAll(message);
        }

        public void bcastSetSnapColors(Guid mol1_id, Guid mol2_id)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSnapMolecules);
            message.AddUShort(0);
            message.AddGuid(mol1_id);
            message.AddGuid(mol2_id);
            Server.SendToAll(message);
        }

        public void bcastServerFocusHighlight(Guid mol_id, ushort atom_id, bool active)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastServerFocusHighlight);
            message.AddGuid(mol_id);
            message.AddUShort(atom_id);
            message.AddBool(active);
            Server.SendToAll(message);
        }

        public void bcastNumOutlines(int num_outlines)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastNumOutlines);
            message.AddInt(num_outlines);
            Server.SendToAll(message);
        }

        public void bcastServerViewport(Vector2 port)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastServerViewport);
            message.AddVector2(port);
            Server.SendToAll(message);
        }

        public void bcastSyncMode(TransitionManager.SyncMode mode)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSyncMode);
            message.AddInt((int)mode);
            Server.SendToAll(message);
        }

        public void bcastMousePosition()
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Unreliable, ServerToClientID.bcastMousePosition);
            message.AddVector2(Input.mousePosition);
            Server.SendToAll(message);
        }

        private bool transitionOnCooldown = false;
        IEnumerator startCooldown()
        {
            transitionOnCooldown = true;
            yield return new WaitForSeconds(1.5f);
            transitionOnCooldown = false;
        }
        public void transitionMol(Molecule mol, TransitionManager.InteractionType triggered_by, int from_id)
        {
            if (transitionOnCooldown) return;
            StartCoroutine(startCooldown());

            //var q = Quaternion.Inverse(GlobalCtrl.Singleton.currentCamera.transform.rotation) * mol.transform.rotation;
            //var q = Quaternion.Inverse(Quaternion.LookRotation(mol.transform.position - GlobalCtrl.Singleton.currentCamera.transform.position)) * mol.transform.rotation;
            var q = Quaternion.Inverse(Quaternion.LookRotation(GlobalCtrl.Singleton.currentCamera.transform.forward)) * mol.transform.rotation;

            var cml = mol.AsCML();
            cml.assignRelativeQuaternion(q);

            var cam = GlobalCtrl.Singleton.currentCamera;
            if (cam == null)
            {
                cam = Camera.main;
            }
            var ss_coords = cam.WorldToScreenPoint(mol.transform.position);
            Debug.Log($"[TransitionCoords] {ss_coords}");
            cml.assignSSPos(ss_coords);
            if (SettingsData.transitionMode != TransitionManager.TransitionMode.INSTANT)
            {
                var ss_bounds = mol.GetComponent<myBoundingBox>().getScreenSpaceBounds();
                cml.assignSSBounds(ss_bounds);
                Debug.Log($"[Transition] ss bounds: {ss_bounds}");
            }
            cml.setTransitionFlag();
            cml.setTriggeredFromId(from_id);
            Debug.Log($"[NetworkManagerServer] transition Mol: Triggered by {triggered_by}");
            cml.setTransitionTriggeredBy(triggered_by);
            if (SettingsData.twoDimensionalMode)
            {
                if (mol.svgFormula != string.Empty)
                {
                    cml.setFormulaString(mol.svgFormula);
                    var coords = new List<Vector2>();
                    var origPositions = new List<Vector3>();
                    foreach (var atom in mol.atomList)
                    {
                        coords.Add(atom.structure_coords);
                        origPositions.Add(atom.originalPosition.Value);
                    }
                    cml.setFormulaCoords(coords);
                    cml.setOriginalPositions(origPositions);
                }
                else
                {
                    Debug.LogError("[NetworkManagerServer] transition Mol: 2D mode active but no svg formua found.");
                }
            }

            NetworkUtils.serializeCmlData(ServerToClientID.transitionMolecule, new List<cmlData> { cml }, chunkSize, false);
            GlobalCtrl.Singleton.deleteMolecule(mol);
        }

        public void transitionGenericObject(GenericObject go, TransitionManager.InteractionType triggered_by, int from_id)
        {
            //var q = Quaternion.Inverse(GlobalCtrl.Singleton.currentCamera.transform.rotation) * go.transform.rotation;
            var q = Quaternion.Inverse(Quaternion.LookRotation(go.transform.position - GlobalCtrl.Singleton.currentCamera.transform.position)) * go.transform.rotation;

            var sgo = go.AsSerializable();
            sgo.assignRelativeQuaternion(q);
            var cam = GlobalCtrl.Singleton.currentCamera;
            if (cam == null)
            {
                cam = Camera.main;
            }
            var ss_coords = cam.WorldToScreenPoint(go.transform.position);
            Debug.Log($"[TransitionCoords] {ss_coords}");
            sgo.assignSSPos(ss_coords);

            if (SettingsData.transitionMode != TransitionManager.TransitionMode.INSTANT)
            {
                var ss_bounds = go.GetComponent<myBoundingBox>().getScreenSpaceBounds();
                sgo.assignSSBounds(ss_bounds);
                Debug.Log($"[Transition] ss bounds: {ss_bounds}");
            }
            sgo.setTransitionFlag();
            sgo.setTranstionTriggeredFromId(from_id);
            Debug.Log($"[NetworkManagerServer] transition Mol: Triggered by {triggered_by}");
            sgo.setTransitionTriggeredBy(triggered_by);

            NetworkUtils.serializeGenericObject(ServerToClientID.transitionGenericObject, sgo, chunkSize, false);
            GenericObject.delete(go);
        }

        public void requestTransition(TransitionManager.InteractionType triggered_by)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.requestTransition);
            message.AddInt((int)triggered_by);

            // only send to one client
            var first_client = Server.Clients.First();
            Server.Send(message, first_client);
        }

        public void bcastDeleteMarkedRequest()
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastRequestDeleteMarked);
            Server.SendToAll(message);
        }

        public void bcastDeleteEverything()
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastDeleteEverything);
            message.AddUShort(0);
            Server.SendToAll(message);
        }

        public void sendSpawnGhostObject(string path)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.sendSpawnGhostObject);
            message.AddUShort(0);
            message.AddString(path);
            Server.SendToAll(message);
        }

        public void sendObjectToTrack(Guid id)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.sendObjectToTrack);
            message.AddUShort(0);
            message.AddGuid(id);
            Server.SendToAll(message);
        }

        public void sendSpawnObjectCollection(int task_id)
        {
            Debug.Log("[NetworkManagerServer] Sending request to spawn obejct collection");
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.sendSpawnObjectCollection);
            message.AddUShort(0);
            message.AddInt(task_id);
            Server.SendToAll(message);
        }

        public void requestResults(int task_id)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.requestResults);
            message.AddUShort(0);
            message.AddInt(task_id);
            Server.SendToAll(message);
        }

        public void bcastScreenSizeChanged()
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastScreenSizeChanged);
            message.AddVector2(SettingsData.serverViewport);
            Server.SendToAll(message);
        }

        public void sendInterpolationState(bool state)
        {
            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastInterpolationState);
            message.AddBool(state);
            Server.SendToAll(message);
        }

        #endregion

        #region MessageHandler

        [MessageHandler((ushort)ClientToServerID.atomCreated)]
        private static void getAtomCreated(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var abbre = message.GetString();
            var pos = message.GetVector3();
            var hyb = message.GetUShort();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the create on the server
                GlobalCtrl.Singleton.CreateAtom(mol_id, abbre, pos, hyb, true);
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastAtomCreated);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddString(abbre);
            outMessage.AddVector3(pos);
            outMessage.AddUShort(hyb);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.moleculeMoved)]
        private static void getMoleculeMoved(ushort fromClientId, Message message)
        {
            var molecule_id = message.GetGuid();
            var pos = message.GetVector3();
            var quat = message.GetQuaternion();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                if (!GlobalCtrl.Singleton.moveMolecule(molecule_id, pos, quat))
                {
                    Debug.LogError($"[NetworkManagerServer:getMoleculeMoved] Molecule with id {molecule_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Unreliable, ServerToClientID.bcastMoleculeMoved);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(molecule_id);
            outMessage.AddVector3(pos);
            outMessage.AddQuaternion(quat);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.atomMoved)]
        private static void getAtomMoved(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var atom_id = message.GetUShort();
            var pos = message.GetVector3();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                if (!GlobalCtrl.Singleton.moveAtom(mol_id, atom_id, pos))
                {
                    Debug.LogError($"[NetworkManagerServer:getAtomMoved] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Unreliable, ServerToClientID.bcastAtomMoved);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(atom_id);
            outMessage.AddVector3(pos);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.stopMoveAtom)]
        private static void getStopMoveAtom(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var atom_id = message.GetUShort();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the stop on the server
                if (!GlobalCtrl.Singleton.stopMoveAtom(mol_id, atom_id))
                {
                    Debug.LogError($"[NetworkManagerServer:getStopMoveAtom] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastStopMoveAtom);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(atom_id);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.moleculeMerged)]
        private static void getMoleculeMerged(ushort fromClientId, Message message)
        {
            var mol1ID = message.GetGuid();
            var atom1ID = message.GetUShort();
            var mol2ID = message.GetGuid();
            var atom2ID = message.GetUShort();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the merge on the server
                // fist check the existence of atoms with the correspoinding ids
                var mol1 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol1ID);
                var atom1 = mol1?.atomList.ElementAtOrNull(atom1ID, null);
                var mol2 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol2ID);
                var atom2 = mol2?.atomList.ElementAtOrNull(atom2ID, null);
                if (mol1 == null || atom1 == null || mol2 == null || atom2 == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getMoleculeMerged] Merging operation cannot be executed. Atom IDs do not exist (Atom1 {mol1ID}:{atom1ID}, Atom2 {mol2ID}:{atom2ID}).\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                GlobalCtrl.Singleton.MergeMolecule(mol1ID, atom1ID, mol2ID, atom2ID);
                NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld());
            }
            else
            {
                //// Broadcast to other clients
                Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastMoleculeMerged);
                outMessage.AddUShort(fromClientId);
                outMessage.AddGuid(mol1ID);
                outMessage.AddUShort(atom1ID);
                outMessage.AddGuid(mol2ID);
                outMessage.AddUShort(atom2ID);
                NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
            }
        }


        public void sendAtomWorld(List<cmlData> world, int toClientID = -1)
        {
            if (world.Count < 1) return;
            NetworkUtils.serializeCmlData(ServerToClientID.sendAtomWorld, world, chunkSize, false, toClientID);
        }

        // message not in use yet
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
                        NetworkUtils.serializeCmlData(ServerToClientID.bcastMoleculeLoad, cmlWorld, chunkSize, false, client.ID);
                    }
                }
            }
        }

        [MessageHandler((ushort)ClientToServerID.deleteEverything)]
        private static void bcastDeleteEverything(ushort fromClientId, Message message)
        {
            GlobalCtrl.Singleton.DeleteAll();
            GlobalCtrl.Singleton.SaveMolecule(true); // for working undo state
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastDeleteEverything);
            outMessage.AddUShort(fromClientId);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.selectAtom)]
        private static void getAtomSelected(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var atom_id = message.GetUShort();
            var selected = message.GetBool();
            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the select on the server
                // don't show the tooltip - may change later
                var atom = GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id) ? GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList.ElementAtOrNull(atom_id, null) : null;
                if (atom == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getAtomSelected] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                if (atom.m_molecule.isMarked)
                {
                    atom.m_molecule.markMolecule(false);
                }
                atom.advancedMarkAtom(selected, true, UserServer.list[fromClientId].highlightFocusID);
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSelectAtom);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(atom_id);
            outMessage.AddBool(selected);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.selectMolecule)]
        private static void getMoleculeSelected(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var selected = message.GetBool();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the select on the server
                // don't show the tooltip - may change later
                var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id);
                if (mol == null)
                {
                    Debug.LogError($"[NetworkManagerClient:getMoleculeSelected] Molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                GlobalCtrl.Singleton.List_curMolecules[mol_id].markMolecule(selected, true);
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSelectMolecule);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddBool(selected);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.selectBond)]
        private static void getBondSelected(ushort fromClientId, Message message)
        {
            var bond_id = message.GetUShort();
            var mol_id = message.GetGuid();
            var selected = message.GetBool();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the select on the server
                // don't show the tooltip - may change later
                var bond = GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id) ? GlobalCtrl.Singleton.List_curMolecules[mol_id].bondList.ElementAtOrNull(bond_id, null) : null;
                if (bond == null)
                {
                    Debug.LogError($"[NetworkManagerClient:getBondSelected] Bond with id {bond_id} or molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                bond.markBond(selected);
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSelectBond);
            outMessage.AddUShort(fromClientId);
            outMessage.AddUShort(bond_id);
            outMessage.AddGuid(mol_id);
            outMessage.AddBool(selected);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.deleteAtom)]
        private static void getAtomDeleted(ushort fromClientId, Message message)
        {
            Debug.Log("[NetworkManagerServer] Received delete atom");
            var mol_id = message.GetGuid();
            var atom_id = message.GetUShort();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the delete on the server
                var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id);
                var atom = mol?.atomList.ElementAtOrNull(atom_id, null);
                if (mol == null || atom == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getAtomDeleted] Atom with id {atom_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                GlobalCtrl.Singleton.deleteAtom(atom);
                NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld());
            }
            else
            {
                // Broadcast to other clients
                Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastDeleteAtom);
                outMessage.AddUShort(fromClientId);
                outMessage.AddGuid(mol_id);
                outMessage.AddUShort(atom_id);
                NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
            }
        }

        [MessageHandler((ushort)ClientToServerID.deleteMolecule)]
        private static void getMoleculeDeleted(ushort fromClientId, Message message)
        {
            Debug.Log("[NetworkManagerServer] Received delete molecule");
            var mol_id = message.GetGuid();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the delete on the server
                var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id);
                if (mol == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getMoleculeDeleted] Molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                GlobalCtrl.Singleton.deleteMolecule(GlobalCtrl.Singleton.List_curMolecules[mol_id]);
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastDeleteMolecule);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.deleteBond)]
        private static void getBondDeleted(ushort fromClientId, Message message)
        {
            Debug.Log("[NetworkManagerServer] Received delete bond");
            var bond_id = message.GetUShort();
            var mol_id = message.GetGuid();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the delete on the server
                var bond = GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id) ? GlobalCtrl.Singleton.List_curMolecules[mol_id].bondList.ElementAtOrNull(bond_id, null) : null;
                if (bond == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getBondDeleted] Bond with id {bond_id} or molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                GlobalCtrl.Singleton.deleteBond(bond);
                NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld());
            }
            else
            {
                // Broadcast to other clients
                Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastDeleteBond);
                outMessage.AddUShort(fromClientId);
                outMessage.AddUShort(bond_id);
                outMessage.AddGuid(mol_id);
                NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
            }
        }

        [MessageHandler((ushort)ClientToServerID.syncMe)]
        private static void getSyncRequest(ushort fromClientId, Message message)
        {
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
        }

        [MessageHandler((ushort)ClientToServerID.changeAtom)]
        private static void getAtomChanged(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var atom_id = message.GetUShort();
            var chemAbbre = message.GetString();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the change on the server
                if (!GlobalCtrl.Singleton.changeAtom(mol_id, atom_id, chemAbbre))
                {
                    Debug.LogError($"[NetworkManagerServer:getAtomChanged] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Unreliable, ServerToClientID.bcastChangeAtom);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(atom_id);
            outMessage.AddString(chemAbbre);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.undo)]
        private static void getUndo(ushort fromClientId, Message message)
        {
            // do the undo
            GlobalCtrl.Singleton.undo();
            // Broadcast undone world to other clients
            NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld());
        }

        [MessageHandler((ushort)ClientToServerID.enableForceField)]
        private static void getEnableForceField(ushort fromClientId, Message message)
        {
            // process message
            var ffEnabled = message.GetBool();

            // do the enable/disable
            ForceField.Singleton.enableForceFieldMethod(ffEnabled);

            // Broadcast
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastEnableForceField);
            outMessage.AddUShort(fromClientId);
            outMessage.AddBool(ffEnabled);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.changeBondTerm)]
        private static void getChangeBondTerm(ushort fromClientId, Message message)
        {
            // process message
            var mol_id = message.GetGuid();
            var term_id = message.GetUShort();
            var term = message.GetBondTerm();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the change
                if (!GlobalCtrl.Singleton.changeBondTerm(mol_id, term_id, term))
                {
                    Debug.LogError($"[NetworkManagerServer:getChangeBondTerm] Molecule with id {mol_id} or bond term with id {term_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
            }

            // Broadcast
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastChangeBondTerm);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(term_id);
            outMessage.AddBondTerm(term);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.changeAngleTerm)]
        private static void getChangeAngleTerm(ushort fromClientId, Message message)
        {
            // process message
            var mol_id = message.GetGuid();
            var term_id = message.GetUShort();
            var term = message.GetAngleTerm();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the change
                if (!GlobalCtrl.Singleton.changeAngleTerm(mol_id, term_id, term))
                {
                    Debug.LogError($"[NetworkManagerServer:changeAngleTerm] Molecule with id {mol_id} or angle term with id {term_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
            }

            // Broadcast
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastChangeAngleTerm);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(term_id);
            outMessage.AddAngleTerm(term);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.changeTorsionTerm)]
        private static void getChangeTorsionTerm(ushort fromClientId, Message message)
        {
            // process message
            var mol_id = message.GetGuid();
            var term_id = message.GetUShort();
            var term = message.GetTorsionTerm();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the change
                if (!GlobalCtrl.Singleton.changeTorsionTerm(mol_id, term_id, term))
                {
                    Debug.LogError($"[NetworkManagerServer:getChangeTorsionTerm] Molecule with id {mol_id} or torsion term with id {term_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
            }

            // Broadcast
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastChangeTorsionTerm);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(term_id);
            outMessage.AddTorsionTerm(term);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.markTerm)]
        private static void getMarkTerm(ushort fromClientId, Message message)
        {
            // process message
            var term_type = message.GetUShort();
            var mol_id = message.GetGuid();
            var term_id = message.GetUShort();
            var marked = message.GetBool();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the change
                var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id);
                if (mol == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getMarkTerm] Molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
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

            // Broadcast
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastMarkTerm);
            outMessage.AddUShort(fromClientId);
            outMessage.AddUShort(term_type);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(term_id);
            outMessage.AddBool(marked);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.modifyHyb)]
        private static void getModifyHyb(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var atom_id = message.GetUShort();
            var hyb = message.GetUShort();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                if (!GlobalCtrl.Singleton.modifyHybrid(mol_id, atom_id, hyb))
                {
                    Debug.LogError($"[NetworkManagerServer:getModifyHyb] Atom with id {atom_id} of Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastModifyHyb);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(atom_id);
            outMessage.AddUShort(hyb);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.keepConfig)]
        private static void getKeepConfig(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var keep_config = message.GetBool();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                if (!GlobalCtrl.Singleton.setKeepConfig(mol_id, keep_config))
                {
                    Debug.LogError($"[NetworkManagerServer:getModifyHyb] Molecule {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastKeepConfig);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddBool(keep_config);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.replaceDummies)]
        private static void getReplaceDummies(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id);
                if (mol == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getReplaceDummies] Molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                GlobalCtrl.Singleton.List_curMolecules[mol_id].toggleDummies();
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastReplaceDummies);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.focusHighlight)]
        private static void getFocusHighlight(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var atom_id = message.GetUShort();
            var active = message.GetBool();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                var atom = GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id) ? GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList.ElementAtOrNull(atom_id, null) : null;
                if (atom == null)
                {
                    Debug.LogWarning($"[NetworkManagerServer:getFocusHighlight] Molecule with id {mol_id} or atom with id {atom_id} do not exist. Abort\n");
                    return;
                }
                atom.networkSetFocus(active, UserServer.list[fromClientId].highlightFocusID);
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastFocusHighlight);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(atom_id);
            outMessage.AddBool(active);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.scaleMolecule)]
        private static void getScaleMolecule(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var scale = message.GetFloat();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id);
                if (mol == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getScaleMolecule] Molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                GlobalCtrl.Singleton.List_curMolecules[mol_id].transform.localScale = scale * Vector3.one;
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastScaleMolecule);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddFloat(scale);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.freezeAtom)]
        private static void getFreezeAtom(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var atom_id = message.GetUShort();
            var freeze = message.GetBool();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                var atom = GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id) ? GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList.ElementAtOrNull(atom_id, null) : null;
                if (atom == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getFreezeAtom] Molecule with id {mol_id} or atom with id {atom_id} do not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                atom.freeze(freeze);
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastFreezeAtom);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddUShort(atom_id);
            outMessage.AddBool(freeze);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.freezeMolecule)]
        private static void getFreezeMolecule(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var freeze = message.GetBool();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id);
                if (mol == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getFreezeMolecule] Molecule with id {mol_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                GlobalCtrl.Singleton.List_curMolecules[mol_id].freeze(freeze);
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastFreezeMolecule);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol_id);
            outMessage.AddBool(freeze);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.snapMolecules)]
        private static void getSnapColors(ushort fromClientId, Message message)
        {
            var mol1_id = message.GetGuid();
            var mol2_id = message.GetGuid();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                var mol1 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol1_id);
                var mol2 = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol2_id);
                if (!mol1 || !mol2)
                {
                    Debug.LogError($"[NetworkManagerServer:getSnapMolecules] Molecule with id {mol1_id} or {mol2_id} does not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                mol1.setSnapColors(mol2);
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastSnapMolecules);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol1_id);
            outMessage.AddGuid(mol2_id);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.createMeasurement)]
        private static void getCreateMeasurement(ushort fromClientId, Message message)
        {
            var mol1ID = message.GetGuid();
            var atom1ID = message.GetUShort();
            var mol2ID = message.GetGuid();
            var atom2ID = message.GetUShort();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the create
                if (!DistanceMeasurement.Create(mol1ID, atom1ID, mol2ID, atom2ID))
                {
                    Debug.LogError($"[NetworkManagerClient] Create measuurement cannot be executed. Atom IDs do not exist (Atom1: {atom1ID}, Atom2 {atom2ID}).\nRequesting world sync.");
                    NetworkManagerClient.Singleton.sendSyncRequest();
                    return;
                }
            }

            // Broadcast to other clients
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastCreateMeasurement);
            outMessage.AddUShort(fromClientId);
            outMessage.AddGuid(mol1ID);
            outMessage.AddUShort(atom1ID);
            outMessage.AddGuid(mol2ID);
            outMessage.AddUShort(atom2ID);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.clearMeasurements)]
        private static void getClearMeasurements(ushort fromClientId, Message message)
        {
            // TODO: What to do in async mode?
            GlobalCtrl.Singleton.deleteAllMeasurements();
            Message outMessage = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToClientID.bcastClearMeasurements);
            outMessage.AddUShort(fromClientId);
            NetworkManagerServer.Singleton.Server.SendToAll(outMessage);
        }

        [MessageHandler((ushort)ClientToServerID.grabAtom)]
        private static void getGrabAtom(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var atom_id = message.GetUShort();
            var grab = message.GetBool();

            if (SettingsData.syncMode == TransitionManager.SyncMode.Sync)
            {
                // do the move on the server
                var atom = GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id) ? GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList.ElementAtOrNull(atom_id, null) : null;
                if (atom == null)
                {
                    Debug.LogError($"[NetworkManagerServer:getGrabAtom] Molecule with id {mol_id} or atom with id {atom_id} do not exist.\nSynchronizing world with client {fromClientId}.");
                    NetworkManagerServer.Singleton.sendAtomWorld(GlobalCtrl.Singleton.saveAtomWorld(), fromClientId);
                    return;
                }
                atom.isGrabbed = grab;
                atom.grabHighlight(grab);
#if UNITY_STANDALONE || UNITY_EDITOR
                if (RunMolecularDynamics.Singleton && RunMolecularDynamics.Singleton.isRunning)
                {
                    RunMolecularDynamics.Singleton.applyConstraint(atom, grab);
                }
#endif
            }
            // no bradcasting
        }
        #endregion


        #region Async Setup

        [MessageHandler((ushort)ClientToServerID.transitionGrabOnScreen)]
        private static void getTransitionGrabOnScreen(ushort fromClientId, Message message)
        {
            var ss_coords = message.GetVector2();
            var inter_type = (TransitionManager.InteractionType)message.GetInt();
            Debug.Log($"[GrabOnScreen] ss {ss_coords}, inter_type {inter_type}");

            if (TransitionManager.Singleton != null)
            {
                TransitionManager.Singleton.initializeTransitionServer(ss_coords, inter_type, fromClientId);
            }
            if (HoverMarker.Singleton != null)
            {
                if (!HoverMarker.Singleton.isVisible() && !HoverMarker.Singleton.settingsActive)
                {
                    HoverMarker.Singleton.show();
                }
                HoverMarker.Singleton.setMiddleGrab();
            }
        }

        [MessageHandler((ushort)ClientToServerID.releaseTransitionGrabOnScreen)]
        private static void getReleaseTransitionGrabOnScreen(ushort fromClientId, Message message)
        {
            if (TransitionManager.Singleton != null)
            {
                TransitionManager.Singleton.release();
            }
            if (HoverMarker.Singleton != null)
            {
                if (!HoverMarker.Singleton.isVisible() && !HoverMarker.Singleton.settingsActive)
                {
                    HoverMarker.Singleton.show();
                }
                HoverMarker.Singleton.setHover();
            }
        }

        [MessageHandler((ushort)ClientToServerID.hoverOverScreen)]
        private static void getHoverOverScreen(ushort fromClientId, Message message)
        {
            var ss_coords = message.GetVector2();
            if (TransitionManager.Singleton != null)
            {
                TransitionManager.Singleton.hover(ss_coords);
            }
            if (HoverMarker.Singleton != null)
            {
                if (!HoverMarker.Singleton.isVisible() && !HoverMarker.Singleton.settingsActive)
                {
                    HoverMarker.Singleton.show();
                }
                HoverMarker.Singleton.setPosition(ss_coords);
            }
        }

        [MessageHandler((ushort)ClientToServerID.transitionMolecule)]
        private static void getMoleculeTransition(ushort fromClientId, Message message)
        {
            NetworkUtils.deserializeCmlData(message, ref cmlTotalBytes, ref cmlWorld, chunkSize, false);
        }

        [MessageHandler((ushort)ClientToServerID.transitionGenericObject)]
        private static void getGenericObjectTransition(ushort fromClientId, Message message)
        {
            NetworkUtils.deserializeGenericObject(message, ref cmlTotalBytes, ref sGO, chunkSize);
        }

        [MessageHandler((ushort)ClientToServerID.sendSpawnGhostObject)]
        private static void getSpawnGhostObject(ushort fromClientId, Message message)
        {
            var path = message.GetString();
            if (StudyTaskManager.Singleton != null)
            {
                StudyTaskManager.Singleton.spawnGhostObject(path);
            }
        }

        [MessageHandler((ushort)ClientToServerID.sendObjectToTrack)]
        private static void getObjectToTrack(ushort fromClientId, Message message)
        {
            var id = message.GetGuid();
            if (StudyTaskManager.Singleton != null)
            {
                StudyTaskManager.Singleton.setObjectToTrack(id);
            }
        }

        [MessageHandler((ushort)ClientToServerID.sendResults)]
        private static void getResults(ushort fromClientId, Message message)
        {
            var task_id = message.GetInt();
            var angle = message.GetFloat();
            var dist = message.GetFloat();
            var scale = message.GetFloat();
            if (StudyTaskManager.Singleton != null)
            {
                StudyLogger.Singleton.write($"AngleError: {angle}", task_id);
                StudyLogger.Singleton.write($"DistError: {dist}", task_id);
                StudyLogger.Singleton.write($"ScaleError: {scale}", task_id);
                // log finised task
                StudyLogger.Singleton.write($"finished.", task_id);
            }
        }

        private static Pose initialHandPose;
        private static Transform objectToManipulate;
        private static Quaternion initialObjectRotation;
        [MessageHandler((ushort)ClientToServerID.grabOnScreen)]
        private static void getGrabOnScreen(ushort fromClientId, Message message)
        {
            initialHandPose = message.GetPose();
            //initialHandPose.position = GlobalCtrl.Singleton.currentCamera.transform.TransformPoint(initialHandPose.position);

            // identify object
            objectToManipulate = TransitionManager.Singleton.getCurrentHoverTarget();
            if (objectToManipulate != null)
            {
                //initialObjectRotation = objectToManipulate.rotation;
                initialObjectRotation = Quaternion.Inverse(Quaternion.LookRotation(objectToManipulate.transform.position - GlobalCtrl.Singleton.currentCamera.transform.position)) * objectToManipulate.transform.rotation;
                chARpackUtils.setObjectGrabbed(objectToManipulate, true);
            }
            if (HoverMarker.Singleton != null)
            {
                if (!HoverMarker.Singleton.isVisible() && !HoverMarker.Singleton.settingsActive)
                {
                    HoverMarker.Singleton.show();
                }
                HoverMarker.Singleton.setIndexGrab();
            }
        }

        [MessageHandler((ushort)ClientToServerID.releaseGrabOnScreen)]
        private static void getReleaseGrabOnScreen(ushort fromClientId, Message message)
        {
            if (objectToManipulate != null)
            {
                chARpackUtils.setObjectGrabbed(objectToManipulate, false);
                objectToManipulate = null;
            }
            if (HoverMarker.Singleton != null)
            {
                if (!HoverMarker.Singleton.isVisible() && !HoverMarker.Singleton.settingsActive)
                {
                    HoverMarker.Singleton.show();
                }
                HoverMarker.Singleton.setHover();
            }
        }

        [MessageHandler((ushort)ClientToServerID.handPose)]
        private static void getHandPose(ushort fromClientId, Message message)
        {
            var current_hand_pose = message.GetPose();
            //current_hand_pose.position = GlobalCtrl.Singleton.currentCamera.transform.TransformPoint(current_hand_pose.position);

            if (objectToManipulate == null) return;
            var diff_vec = current_hand_pose.position - initialHandPose.position;
            var z_diff = Vector3.Dot(diff_vec, GlobalCtrl.Singleton.currentCamera.transform.forward);
            z_diff = z_diff < 0f ? 2f * z_diff : z_diff; // make interaction more sensitive to pulling (less space)

            //if (diff_vec.magnitude > 0.05f)
            //{
            //    objectToManipulate.position += 0.05f * diff_vec;
            //}
            var old_obj_pos = objectToManipulate.position;
            var old_obj_pos_in_cam_coords = GlobalCtrl.Singleton.currentCamera.transform.InverseTransformPoint(old_obj_pos);
            var cursor_pos = HoverMarker.Singleton.transform.position;
            cursor_pos.z = (objectToManipulate.position - GlobalCtrl.Singleton.currentCamera.transform.position).magnitude;
            //cursor_pos.z = GlobalCtrl.Singleton.currentCamera.nearClipPlane;

            objectToManipulate.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(cursor_pos);
            var obj_pos_in_cam_coords = GlobalCtrl.Singleton.currentCamera.transform.InverseTransformPoint(objectToManipulate.position);
            obj_pos_in_cam_coords.z = old_obj_pos_in_cam_coords.z;
            objectToManipulate.position = GlobalCtrl.Singleton.currentCamera.transform.TransformPoint(obj_pos_in_cam_coords);

            var z_movement_threshold = 0.07f;
            var z_movement = Mathf.Abs(z_diff) >= z_movement_threshold ? Mathf.Sign(z_diff) * 0.2f * Mathf.Pow(z_diff + z_movement_threshold, 2f) * GlobalCtrl.Singleton.currentCamera.transform.forward : Vector3.zero;
            objectToManipulate.position += z_movement;

            var relative_rotation = initialHandPose.rotation * Quaternion.Inverse(current_hand_pose.rotation);
            //objectToManipulate.Rotate(relative_rotation.eulerAngles);
            //objectToManipulate.rotation = Quaternion.Inverse(relative_rotation) * initialObjectRotation;
            objectToManipulate.rotation = GlobalCtrl.Singleton.currentCamera.transform.rotation * Quaternion.Euler(relative_rotation.eulerAngles.x, -relative_rotation.eulerAngles.y, relative_rotation.eulerAngles.z) * initialObjectRotation;

            // check if transition should happen
            var forward = GlobalCtrl.Singleton.currentCamera.transform.forward;
            var near_point = GlobalCtrl.Singleton.currentCamera.transform.position + forward * GlobalCtrl.Singleton.currentCamera.nearClipPlane;

            foreach (var corner in objectToManipulate.GetComponent<myBoundingBox>().cornerHandles)
            {
                var pos_projected_on_nearplane = Vector3.ProjectOnPlane(corner.transform.position, forward) + Vector3.Dot(near_point, forward) * forward;
                if (Vector3.Dot(forward, corner.transform.position - pos_projected_on_nearplane) < 0f)
                {
                    TransitionManager.Singleton.initializeTransitionServer(objectToManipulate, TransitionManager.InteractionType.ONSCREEN_PULL, fromClientId);
                    return;
                }
            }
        }

        #endregion

        #region Representations

        private void bcastFormula(Guid mol_id, string svg_string, List<Vector2> coords)
        {
            NetworkUtils.serializeFormula(ServerToClientID.sendFormula, mol_id, svg_string, coords, chunkSize);
        }
        #endregion


        #region StructureMessages


        public void requestStructureFormula(Molecule mol)
        {
            StartCoroutine(delayedRequestStructureFormula(mol));
        }

        private IEnumerator delayedRequestStructureFormula(Molecule mol)
        {
            yield return new WaitForSeconds(1f);
            // Pack molecule
            if (mol == null || !GlobalCtrl.Singleton.List_curMolecules.ContainsValue(mol))
            {
                Debug.LogError("[SimToServer:requestStructureFormula] No molecules in scene!");
                yield break;
            }

            Message message = NetworkUtils.createMessage(MessageSendMode.Reliable, ServerToStructureID.requestStrucutreFormula);
            message.AddGuid(mol.m_id);
            message.AddUShort((ushort)mol.atomList.Count);

            foreach (var atom in mol.atomList)
            {
                var pos = atom.transform.localPosition * GlobalCtrl.u2aa / GlobalCtrl.scale;
                message.AddFloat(pos.x);
                message.AddFloat(pos.y);
                message.AddFloat(pos.z);
            }

            foreach (var atom in mol.atomList)
            {
                if (atom.m_data.m_abbre.ToLower() == "dummy")
                {
                    message.AddString("H");
                }
                else
                {
                    message.AddString(atom.m_data.m_abbre);
                }

            }

            Server.Send(message, UserServer.structureID);
        }

        [MessageHandler((ushort)StructureToServerID.sendStructureFormula)]
        private static void getStructureFormula(ushort fromClientId, Message message)
        {
            // unpack message
            //NetworkUtils.deserializeFormula(message, ref svg_content, ref svg_coords);
        }

        private void structureReceiveComplete(Guid mol_id)
        {
            if (!GlobalCtrl.Singleton.List_curMolecules.ContainsKey(mol_id))
            {
                foreach (var id in GlobalCtrl.Singleton.List_curMolecules.Keys)
                {
                    Debug.LogError($"[structureReceiveComplete] {id}");
                }
                Debug.LogError($"[structureReceiveComplete] Molecule with ID {mol_id} does not exist.\nRequesting world sync.");
                NetworkManagerClient.Singleton.sendSyncRequest();
                return;
            }
            var mol = GlobalCtrl.Singleton.List_curMolecules[mol_id];

            for (int i = 0; i < svg_coords.Count; i++)
            {
                mol.atomList[i].structure_coords = svg_coords[i];
            }

            if (StructureFormulaManager.Singleton)
            {
                StructureFormulaManager.Singleton.pushContent(mol_id, svg_content);
            }
            else
            {
                Debug.LogError("[structureReceiveComplete] Could not find StructureFormulaManager");
                return;
            }

            //write svg to file (only executed on server anyway
            var file_path = Path.Combine(Application.streamingAssetsPath, $"{svg_content.Length}.svg");
            if (File.Exists(file_path))
            {
                Debug.Log(file_path + " already exists.");
                return;
            }
            var sr = File.CreateText(file_path);
            sr.Write(svg_content);
            sr.Close();
        }

        #endregion

        #region SimMessages

        [MessageHandler((ushort)SimToServerID.sendInit)]
        private static void getSimInit(ushort fromClientId, Message message)
        {
            Debug.Log($"[SimToServer] Simulation Connected. ID: {fromClientId}");
            UserServer.simID = fromClientId;
            SettingsData.forceField = false;
            settingsControl.Singleton.updateSettings();
        }

        [MessageHandler((ushort)StructureToServerID.sendInit)]
        private static void getStructureInit(ushort fromClientId, Message message)
        {
            Debug.Log($"[StructureToServer] Structure Generator Connected. ID: {fromClientId}");
            UserServer.structureID = fromClientId;
        }


        [MessageHandler((ushort)SimToServerID.sendMolecule)]
        private static void getMoleculeCreated(ushort fromClientId, Message message)
        {
            Debug.Log("[SimToServer] got molecule");
            var mol_id = message.GetGuid();
            var num_atoms = message.GetUShort();
            ushort[] ids = new ushort[num_atoms];
            string[] symbols = new string[num_atoms];
            Vector3[] positions = new Vector3[num_atoms];
            for (int i = 0; i < num_atoms; i++)
            {
                ids[i] = message.GetUShort();
                symbols[i] = message.GetString();
                positions[i] = message.GetVector3();
            }

            Debug.Log($"[SimToServer] Num Atoms {num_atoms}, Molecule ids {ids.AsCommaSeparatedString()} symbols {symbols.AsCommaSeparatedString()} positions {positions.AsCommaSeparatedString()}");

            // TODO change this to not write a file

            string filename = $"{mol_id}.xyz";
            //string path = Path.Combine(Application.persistentDataPath, filename);
            string path = Path.Combine(Application.streamingAssetsPath, filename); // only executed on server (no andriod special treatment necessary)
            var fi = new FileInfo(path);
            if (fi.Exists) fi.Delete();

            StreamWriter writer = new StreamWriter(path, true);
            writer.WriteLine($"{num_atoms}");
            writer.WriteLine("");
            for (int i = 0; i < num_atoms; i++)
            {
                writer.WriteLine($"{symbols[i]}    {positions[i].x}    {positions[i].y}    {positions[i].z}");
            }
            writer.WriteLine("");
            writer.Flush();
            writer.Close();

            var cml_mol = OpenBabelReadWrite.Singleton.loadMolecule(fi);
            GlobalCtrl.Singleton.createFromCML(cml_mol);
        }


        [MessageHandler((ushort)SimToServerID.sendMoleculeUpdate)]
        private static void getMoleculeUpdate(ushort fromClientId, Message message)
        {
            var mol_id = message.GetGuid();
            var num_atoms = message.GetUShort();
            ushort id;
            Vector3 pos;
            for (int i = 0; i < num_atoms; i++)
            {
                id = message.GetUShort();
                pos = message.GetVector3();
                GlobalCtrl.Singleton.moveAtom(mol_id, id, pos * GlobalCtrl.scale / GlobalCtrl.u2aa);
            }

        }

        #endregion
    }
}