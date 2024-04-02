using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using chARpackStructs;
using System.IO;
using System;
using System.Globalization;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using System.Reflection;
using Microsoft.MixedReality.Toolkit.Input;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using chARpackTypes;

/*! \mainpage 
 * API reference page for chARpack
 */

[Serializable]
public class GlobalCtrl : MonoBehaviour
{
    /// <summary>
    /// singleton of global control
    /// </summary>
    private static GlobalCtrl _singleton;
    public static GlobalCtrl Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(GlobalCtrl)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    [HideInInspector] public GameObject exitConfirmPrefab;
    public GameObject myBoundingBoxPrefab;
    public GameObject myAtomPrefab;

    public Material atomMatPrefab;
    public Material dummyMatPrefab;

    /// <summary>
    /// all data of element
    /// </summary>
    [HideInInspector] public Dictionary<string, ElementData> Dic_ElementData { get; private set; }

    [HideInInspector] public List<Molecule> List_curMolecules { get; private set; }
    //public Dictionary<int, Molecule> Dic_curMolecules { get; private set; }

    [HideInInspector] public Dictionary<int, Material> Dic_AtomMat { get; private set; }

    /// <summary>
    /// the list to save/load element data via XML.
    /// </summary>
    [HideInInspector] public List<ElementData> list_ElementData;
    /// <summary>
    /// scaling factor for visible models
    /// </summary>
    public static float scale = 0.5f;
    //public float scale = 1.0f;
    /// <summary>
    /// 1m in unity = 1000pm in atomic world
    /// </summary>
    public static float u2pm = 1000f;
    /// <summary>
    /// 1m in unity = 10 Aangstroem in atomic world
    /// </summary>
    public static float u2aa = 10f;
    /// <summary>
    /// scale covalent radius to atom diameter
    /// </summary>
    public static float atomScale = 0.4f;

    /// <summary>
    /// the space for creating molecules
    /// </summary>
    public GameObject atomWorld;

    public Bond bondPrefab;
    public Material overlapMat;
    public Material bondMat;

    private bool bondsInForeground = false;

    [HideInInspector] public bool collision = false;
    [HideInInspector] public Atom collider1;
    [HideInInspector] public Atom collider2;

    [HideInInspector] public ushort curHybrid = 3;

    Dictionary<Atom, List<Atom>> groupedAtoms = new Dictionary<Atom, List<Atom>>();
    //public List<string> favorites = new List<string>(new string[5]);
    //public GameObject fav1;
    //public GameObject fav2;
    //public GameObject fav3;
    //public GameObject fav4;
    //public GameObject fav5;
    //public List<GameObject> favoritesGO = new List<GameObject>(5);

    /// <summary>
    /// the last created atom, init to default "C"
    /// </summary>
    private string lastAtom = "C";

    private Locale currentLocale;

    [HideInInspector] public int numAtoms = 0;

    public Stack<List<cmlData>> systemState = new Stack<List<cmlData>>();

    // tooltips to connect two molecules
    [HideInInspector] public Dictionary<Tuple<ushort, ushort>, GameObject> snapToolTipInstances = new Dictionary<Tuple<ushort, ushort>, GameObject>();

    // measurmemt dict
    [HideInInspector] public Dictionary<DistanceMeasurement, Tuple<Atom, Atom>> distMeasurementDict = new Dictionary<DistanceMeasurement, Tuple<Atom, Atom>>();
    [HideInInspector] public Dictionary<AngleMeasurement, Triple<Atom, DistanceMeasurement, DistanceMeasurement>> angleMeasurementDict = new Dictionary<AngleMeasurement, Triple<Atom, DistanceMeasurement, DistanceMeasurement>>();
    [HideInInspector] public GameObject measurmentInHand = null; 

    #region Interaction
    // Interaction modes
    public enum InteractionModes {NORMAL, FRAGMENT_ROTATION, MEASUREMENT};
    private InteractionModes _currentInteractionMode = InteractionModes.NORMAL;
    // Setter can't be private because settingsControl needs to access it
    public InteractionModes currentInteractionMode { get => _currentInteractionMode; /*private*/ set => _currentInteractionMode = value; }

    /// <summary>
    /// Toggles the interaction mode "fragment rotation":
    /// unfreezes molecules if they were frozen and updates indicators.
    /// </summary>
    public void toggleFragmentRotationMode()
    {
        if (currentInteractionMode != InteractionModes.FRAGMENT_ROTATION)
        {
            currentInteractionMode = InteractionModes.FRAGMENT_ROTATION;
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.gameObject.SetActive(true);
                HandTracking.Singleton.showVisual(true);
            }
            freezeWorld(false);
        }
        else
        {
            currentInteractionMode = InteractionModes.NORMAL;
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.gameObject.SetActive(false);
            }
        }

        // Update visuals on hand menu toggle buttons
        handMenu.Singleton.setVisuals();
    }

    /// <summary>
    /// Toggles the interaction mode "measurement":
    /// freezes molecules if activated and updates indicators.
    /// </summary>
    public void toggleMeasurementMode()
    {
        if (currentInteractionMode != InteractionModes.MEASUREMENT)
        {
            currentInteractionMode = InteractionModes.MEASUREMENT;
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.gameObject.SetActive(true);
                HandTracking.Singleton.showVisual(false);
            }
            freezeWorld(true);
        }
        else
        {
            currentInteractionMode = InteractionModes.NORMAL;
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.gameObject.SetActive(false);
            }
            freezeWorld(false);
        }

        // Update visuals on hand menu toggle buttons
        handMenu.Singleton.setVisuals();
    }

    /// <summary>
    /// Sets the interaction mode.
    /// Used to change the interaction mode upon receiving a corresponding
    /// message from the server.
    /// </summary>
    public void setInteractionMode(InteractionModes mode)
    {
        if (currentInteractionMode == mode) return;
        currentInteractionMode = mode;
        if(mode == InteractionModes.FRAGMENT_ROTATION)
        {
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.gameObject.SetActive(true);
                HandTracking.Singleton.showVisual(true);
            }
            freezeWorld(false);
        } else if(mode == InteractionModes.MEASUREMENT)
        {
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.gameObject.SetActive(true);
                HandTracking.Singleton.showVisual(false);
            }
            freezeWorld(true);
        } else
        {
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.gameObject.SetActive(false);
            }
            freezeWorld(false);
        }
        handMenu.Singleton?.setVisuals();
    }
    #endregion

    // Start is called before the first frame update
    void Awake()
    {
        // create singleton
        Singleton = this;
        // make sure that numbers are printed with a dot as required by any post-processing with standard software
        CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
        CultureInfo.CurrentUICulture = new CultureInfo("en-US", false);

        currentLocale = LocalizationSettings.SelectedLocale;

        // check if file is found otherwise throw error
        string element_file_path = Path.Combine(Application.streamingAssetsPath, "ElementData.xml");
        if (!System.IO.File.Exists(element_file_path))
        {
            Debug.LogError("[GlobalCtrl] ElementData.xml not found.");
        }

        list_ElementData = (List<ElementData>)XMLFileHelper.LoadData(element_file_path, typeof(List<ElementData>));
        if (!(list_ElementData.Count > 0))
        {
            Debug.LogError("[GlobalCtrl] list_ElementData is empty.");
        }

        List_curMolecules = new List<Molecule>();
        Dic_ElementData = new Dictionary<string, ElementData>();
        Dic_AtomMat = new Dictionary<int, Material>();
        foreach (ElementData e in list_ElementData)
        {
            Dic_ElementData.Add(e.m_abbre, e);
        }

        if (!(Dic_ElementData.Count > 0))
        {
            Debug.LogError("[GlobalCtrl] Dic_ElementData is empty.");
        }

        exitConfirmPrefab = (GameObject)Resources.Load("prefabs/confirmDialog");

        //favorites.Add("C");
        //favorites.Add("N");
        //favorites.Add("O");
        //favorites.Add("Cl");
        //favorites.Add("F");
        //favoritesGO.Add(fav1);
        //favoritesGO.Add(fav2);
        //favoritesGO.Add(fav3);
        //favoritesGO.Add(fav4);
        //favoritesGO.Add(fav5);

        // Init some prefabs
        // Atom
        Atom.myAtomToolTipPrefab = (GameObject)Resources.Load("prefabs/MRTKAtomToolTip");
        Atom.distMeasurementPrefab = (GameObject)Resources.Load("prefabs/DistanceMeasurementPrefab");
        Atom.angleMeasurementPrefab = (GameObject)Resources.Load("prefabs/AngleMeasurementPrefab");
        Atom.deleteMeButtonPrefab = (GameObject)Resources.Load("prefabs/DeleteMeButton");
        Atom.closeMeButtonPrefab = (GameObject)Resources.Load("prefabs/CloseMeButton");
        Atom.modifyMeButtonPrefab = (GameObject)Resources.Load("prefabs/ModifyMeButton");
        Atom.modifyHybridizationPrefab = (GameObject)Resources.Load("prefabs/modifyHybridization");
        Atom.freezeMePrefab = (GameObject)Resources.Load("prefabs/FreezeMeButton");

        // Molecule
        Molecule.myToolTipPrefab = (GameObject)Resources.Load("prefabs/MRTKMoleculeTooltip");
        Molecule.mySnapToolTipPrefab = (GameObject)Resources.Load("prefabs/MRTKSnapMoleculeTooltip");
        Molecule.deleteMeButtonPrefab = (GameObject)Resources.Load("prefabs/DeleteMeButton");
        Molecule.closeMeButtonPrefab = (GameObject)Resources.Load("prefabs/CloseMeButton");
        Molecule.modifyMeButtonPrefab = (GameObject)Resources.Load("prefabs/ModifyMeButton");
        Molecule.changeBondWindowPrefab = (GameObject)Resources.Load("prefabs/ChangeBondWindow");
        Molecule.toggleDummiesButtonPrefab = (GameObject)Resources.Load("prefabs/ToggleDummiesButton");
        Molecule.undoButtonPrefab = (GameObject)Resources.Load("prefabs/UndoButton");
        Molecule.copyButtonPrefab = (GameObject)Resources.Load("prefabs/CopyMeButton");
        Molecule.scaleMoleculeButtonPrefab = (GameObject)Resources.Load("prefabs/ScaleMoleculeButton");
        Molecule.scalingSliderPrefab = (GameObject)Resources.Load("prefabs/myTouchSlider");
        Molecule.freezeMeButtonPrefab = (GameObject)Resources.Load("prefabs/FreezeMeButton");
        Molecule.snapMeButtonPrefab = (GameObject)Resources.Load("prefabs/SnapMeButton");
        Molecule.distanceMeasurementPrefab = (GameObject)Resources.Load("prefabs/DistanceMeasurementPrefab");
        Molecule.angleMeasurementPrefab = (GameObject)Resources.Load("prefabs/AngleMeasurementPrefab");

        // Measuremet
        DistanceMeasurement.distMeasurementPrefab = (GameObject)Resources.Load("prefabs/DistanceMeasurementPrefab");
        DistanceMeasurement.angleMeasurementPrefab = (GameObject)Resources.Load("prefabs/AngleMeasurementPrefab");

        Debug.Log("[GlobalCtrl] Initialization complete.");

    }

    [HideInInspector] public Camera mainCamera;
    [HideInInspector] public Camera currentCamera;
    private void Start()
    {
        mainCamera = Camera.main;
        // for use in mouse events
        currentCamera = mainCamera;
        Debug.Log($"DEVICE Type: {SystemInfo.deviceType}, Model: {SystemInfo.deviceModel}");
    }

    private void Update()
    {
        if(currentLocale != LocalizationSettings.SelectedLocale)
        {
            regenerateTooltips();
            currentLocale = LocalizationSettings.SelectedLocale;
            if (SceneManager.GetActiveScene().name.Equals("MainScene"))
            {
                appSettings.Singleton.updateVisuals();
            }
        }
    }


    // on mol data changed (replacement for update loop checks)
    //void onMolDataChanged()
    //{
    //    SaveMolecule(true);
    //}


    #region atom_helper
    /// <summary>
    /// Try to get an atom based on molecule and atom id.
    /// </summary>
    /// <param name="mol_id"></param>
    /// <param name="atom_id"></param>
    /// <param name="atomInstance"></param>
    /// <returns>Whether the atom was found</returns>
    public bool getAtom(ushort mol_id, ushort atom_id, ref Atom atomInstance)
    {
        var mol = List_curMolecules.ElementAtOrDefault(mol_id);
        if (mol == default)
        {
            return false;
        }

        var atom = mol.atomList.ElementAtOrDefault(atom_id);
        if (atom == default)
        {
            return false;
        }
        atomInstance = atom;

        return true;
    }

    /// <summary>
    /// Counts the atoms in the entire scene.
    /// </summary>
    /// <returns>the number of atoms in the scene</returns>
    public int getNumAtoms()
    {
        int num_atoms = 0;
        foreach (var mol in List_curMolecules)
        {
            mol.shrinkAtomIDs();
            num_atoms += mol.getMaxAtomID() + 1;
        }
        return num_atoms;
    }
    #endregion

    #region delete

    /// <summary>
    /// this method initialises the delete process, it is called on clicking the delete button in the UI
    /// </summary>
    public void markToDelete()
    {
        markToDeleteCore(false);
    }

    /// <summary>
    /// this method deletes everything in the scene
    /// </summary>
    public void DeleteAll()
    {
        markToDeleteCore(true);
    }

    /// <summary>
    /// This method deletes everything in the scene, and invokes a delete all event.
    /// Should be used for UI buttons
    /// </summary>
    public void DeleteAllUI()
    {
        DeleteAll();
        SaveMolecule(true);
        EventManager.Singleton.DeleteEverything();
    }

    /// <summary>
    /// this method initialises the delete process, it is called by markToDelete or DeleteAll
    /// </summary>
    public void markToDeleteCore(bool deleteAll)
    {
        Dictionary<Atom, List<Vector3>> positionsRestore = new Dictionary<Atom, List<Vector3>>();
        List<Atom> delAtomList = new List<Atom>();
        List<Bond> delBondList = new List<Bond>();
        List<Molecule> delMoleculeList = new List<Molecule>();
        List<Molecule> addMoleculeList = new List<Molecule>();

        foreach (Molecule m in List_curMolecules)
        {
            // add to delete list
            foreach (Atom a in m.atomList)
            {
                List<Vector3> conPos = new List<Vector3>();
                foreach (Atom at in a.connectedAtoms())
                {
                    if (at.isMarked || deleteAll)
                    {
                        if (!delAtomList.Contains(at))
                            delAtomList.Add(at);

                        if (a.m_data.m_abbre == "H" && !delAtomList.Contains(a))
                            delAtomList.Add(a);

                        conPos.Add(at.transform.localPosition);
                    }
                }
                positionsRestore.Add(a, conPos);
            }

            // add to delete List
            foreach (Bond b in m.bondList)
            {
                if (b.isMarked || deleteAll)
                {
                    if (b.m_molecule.atomList.ElementAtOrDefault(b.atomID1).m_data.m_abbre != "Dummy" && b.m_molecule.atomList.ElementAtOrDefault(b.atomID2).m_data.m_abbre != "Dummy")
                    {
                        delBondList.Add(b);

                        //Atom1
                        positionsRestore.TryGetValue(b.m_molecule.atomList.ElementAtOrDefault(b.atomID1), out List<Vector3> temp1);
                        positionsRestore.Remove(b.m_molecule.atomList.ElementAtOrDefault(b.atomID1));
                        temp1.Add(b.m_molecule.atomList.ElementAtOrDefault(b.atomID2).transform.localPosition);
                        positionsRestore.Add(b.m_molecule.atomList.ElementAtOrDefault(b.atomID1), temp1);

                        //Atom2
                        positionsRestore.TryGetValue(b.m_molecule.atomList.ElementAtOrDefault(b.atomID2), out List<Vector3> temp2);
                        positionsRestore.Remove(b.m_molecule.atomList.ElementAtOrDefault(b.atomID2));
                        temp2.Add(b.m_molecule.atomList.ElementAtOrDefault(b.atomID1).transform.localPosition);
                        positionsRestore.Add(b.m_molecule.atomList.ElementAtOrDefault(b.atomID2), temp2);
                    } else
                    {
                        b.markBond(false);
                    }
                }
            }

            createTopoMap(m, delAtomList, delBondList, addMoleculeList);
            delAtomList.Clear();
            delBondList.Clear();
            delMoleculeList.Add(m);
        }

        foreach (Molecule m in delMoleculeList)
        {
            List_curMolecules.Remove(m);
            foreach (var atom in m.atomList)
            {
                atom.markAtom(false);
            }
            foreach (var bond in m.bondList)
            {
                bond.markBond(false);
            }
            m.markMolecule(false);
            List_curMolecules.Remove(m);
            Destroy(m.gameObject);

        }

        foreach (Molecule m in addMoleculeList)
        {
            List_curMolecules.Add(m);
        }
        shrinkMoleculeIDs();

        foreach (Molecule m in List_curMolecules)
        {
            int size = m.atomList.Count;
            for (int i = 0; i < size; i++)
            {
                Atom a = m.atomList[i];
                int count = 0;
                var num_bonds = calcNumBonds(a.m_data.m_hybridization, a.m_data.m_bondNum);
                while (num_bonds > a.connectedAtoms().Count)
                {
                    CreateDummy(m.getFreshAtomID(), m, a, calcDummyPos(a, positionsRestore, count));
                    count++;

                }
            }
            m.shrinkAtomIDs();
        }
        // invoke data change event for new molecules
        foreach (Molecule m in addMoleculeList)
        {
            EventManager.Singleton.ChangeMolData(m);
        }
    }

    /// <summary>
    /// Deletes a given bond and invokes a delete bond event.
    /// </summary>
    /// <param name="to_delete"></param>
    public void deleteBondUI(Bond to_delete)
    {
        var mol_id = to_delete.m_molecule.m_id;
        var bond_id = to_delete.m_molecule.bondList.IndexOf(to_delete);
        if (bond_id == -1)
        {
            Debug.LogError("[GlobalCtrl:deleteBondUI] Did not fond bond ID in molecule's bond list.");
            return;
        }

        try
        {
            if (!deleteBond(to_delete))
            {
                return;
            }
            EventManager.Singleton.DeleteBond((ushort)bond_id, mol_id);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GlobalCrtl:deleteBondUI] Exception: {e.Message}");
        }
    }

    /// <summary>
    /// Try to delete a given bond (without triggering a network event).
    /// </summary>
    /// <param name="b"></param>
    /// <returns>whether the deletion was successful</returns>
    public bool deleteBond(Bond b)
    {

        if (b.isMarked)
        {
            b.markBond(false);
        }

        // add to delete List
        List<Bond> delBondList = new List<Bond>();
        Dictionary<Atom, List<Vector3>> positionsRestore = new Dictionary<Atom, List<Vector3>>();
        List<Molecule> addMoleculeList = new List<Molecule>();
        List<Molecule> delMoleculeList = new List<Molecule>();
        Atom a1 = b.m_molecule.atomList.ElementAtOrDefault(b.atomID1);
        Atom a2 = b.m_molecule.atomList.ElementAtOrDefault(b.atomID2);

        if (a1 == default || a2 == default)
        {
            Debug.LogError("[GlobalCtrl:delteBond] Could not access connected atoms.");
            return false;
        }

        var a1_con = a1.connectedAtoms();
        var a2_con = a2.connectedAtoms();
        int num_a1_connections = a1_con.Count;
        int num_a2_connections = a2_con.Count;

        Dictionary<Atom, int> numConnectedAtoms = new Dictionary<Atom, int>();
        if (a1.m_data.m_abbre != "H" && a1.m_data.m_abbre != "Dummy")
            numConnectedAtoms[a1] = num_a1_connections;
        if (a2.m_data.m_abbre != "H" && a2.m_data.m_abbre != "Dummy")
            numConnectedAtoms[a2] = num_a2_connections;

        foreach (Atom a in b.m_molecule.atomList)
        {
            positionsRestore.Add(a, new List<Vector3>());
        }

        if (b.m_molecule.atomList.ElementAtOrDefault(b.atomID1).m_data.m_abbre != "Dummy" && b.m_molecule.atomList.ElementAtOrDefault(b.atomID2).m_data.m_abbre != "Dummy")
        {
            delBondList.Add(b);

            //Atom1
            positionsRestore.TryGetValue(b.m_molecule.atomList.ElementAtOrDefault(b.atomID1), out List<Vector3> temp1);
            positionsRestore.Remove(b.m_molecule.atomList.ElementAtOrDefault(b.atomID1));
            temp1.Add(b.m_molecule.atomList.ElementAtOrDefault(b.atomID2).transform.localPosition);
            positionsRestore.Add(b.m_molecule.atomList.ElementAtOrDefault(b.atomID1), temp1);

            //Atom2
            positionsRestore.TryGetValue(b.m_molecule.atomList.ElementAtOrDefault(b.atomID2), out List<Vector3> temp2);
            positionsRestore.Remove(b.m_molecule.atomList.ElementAtOrDefault(b.atomID2));
            temp2.Add(b.m_molecule.atomList.ElementAtOrDefault(b.atomID1).transform.localPosition);
            positionsRestore.Add(b.m_molecule.atomList.ElementAtOrDefault(b.atomID2), temp2);
        }

        createTopoMap(b.m_molecule, new List<Atom>(), delBondList, addMoleculeList);
        delMoleculeList.Add(b.m_molecule);

        foreach (Molecule m in delMoleculeList)
        {
            List_curMolecules.Remove(m);
            foreach (var atom in m.atomList)
            {
                atom.markAtom(false);
            }
            foreach (var bond in m.bondList)
            {
                bond.markBond(false);
            }
            m.markMolecule(false);
            List_curMolecules.Remove(m);
            Destroy(m.gameObject);
        }

        foreach (Molecule m in addMoleculeList)
        {
            List_curMolecules.Add(m);
        }
        shrinkMoleculeIDs();

        foreach (var entry in numConnectedAtoms)
        {
            var a = entry.Key;
            int count = 0;
            while (entry.Value > a.connectedAtoms().Count)
            {
                CreateDummy(a.m_molecule.getFreshAtomID(), a.m_molecule, a, calcDummyPos(a, positionsRestore, count));
                count++;
            }
            a.m_molecule.shrinkAtomIDs();
        }

        reloadShaders();


        SaveMolecule(true);
        // invoke data change event for new molecules
        foreach (Molecule m in addMoleculeList)
        {
            EventManager.Singleton.ChangeMolData(m);
        }
        return true;
    }

    public void reloadShaders()
    {
        foreach(Molecule m in List_curMolecules)
        {
            foreach(Bond b in m.bondList)
            {
                b.setShaderProperties();
            }
        }
    }

    public void toggleKeepConfigUI(Molecule to_switch)
    {
        //setKeepConfig(to_switch, !to_switch.keepConfig);
        //EventManager.Singleton.SetKeepConfig(to_switch.m_id, to_switch.keepConfig);
    }

    public void setKeepConfig(Molecule to_switch, bool keep_config)
    {
        //if (to_switch.keepConfig != keep_config)
        //{
        //    to_switch.keepConfig = keep_config;
        //    to_switch.generateFF();
        //}
    }

    public bool setKeepConfig(ushort mol_id, bool keep_config)
    {
        //var to_switch = List_curMolecules.ElementAtOrDefault(mol_id);
        //if (to_switch == default)
        //{
        //    return false;
        //}
        //if (to_switch.keepConfig != keep_config)
        //{
        //    to_switch.keepConfig = keep_config;
        //    to_switch.generateFF();
        //}
        return true;
    }

    /// <summary>
    /// Deletes a given molecule and invokes a delete molecule event.
    /// </summary>
    /// <param name="to_delete"></param>
    public void deleteMoleculeUI(Molecule to_delete)
    {
        var mol_id = to_delete.m_id;
        try
        {
            deleteMolecule(to_delete);
            EventManager.Singleton.DeleteMolecule(mol_id);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GlobalCrtl:deleteMoleculeUI] Exception: {e.Message}");
        }
    }

    /// <summary>
    /// Deletes a given molecule; removes all outlines on 
    /// atoms and bonds contained in the molecule;
    /// </summary>
    /// <param name="m"></param>
    public void deleteMolecule(Molecule m)
    {
        if (m.isMarked)
        {
            m.markMolecule(false);
        }

        List_curMolecules.Remove(m);
        foreach (var atom in m.atomList)
        {
            atom.markAtom(false);
        }
        foreach (var bond in m.bondList)
        {
            bond.markBond(false);
        }
        //m.markMolecule(false);
        //List_curMolecules.Remove(m);
        Destroy(m.gameObject);
        shrinkMoleculeIDs();
        SaveMolecule(true);
        // no need to invoke change event
    }

    /// <summary>
    /// Deletes a given atom and invokes a delete atom event.
    /// </summary>
    /// <param name="to_delete"></param>
    public void deleteAtomUI(Atom to_delete)
    {
        var mol_id = to_delete.m_molecule.m_id;
        var id = to_delete.m_id;
        try
        {
            deleteAtom(to_delete);
            EventManager.Singleton.DeleteAtom(mol_id, id);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GlobalCrtl:deleteAtomUI] Exception: {e.Message}");
        }
    }

    public void deleteAtom(ushort mol_id, ushort atom_id)
    {
        var atom = List_curMolecules.ElementAtOrDefault(mol_id).atomList.ElementAtOrDefault(atom_id);
        if (atom != default)
        {
            deleteAtom(atom);
        }
        else
        {
            Debug.LogError($"[GlobalCtrl:deleteAtom] Atom with ID {atom_id} of molecule {mol_id} does not exist. Cannot delete.");
        }
    }

    /// <summary>
    /// Deletes a given atom and restores positions of dependent molecules.
    /// </summary>
    /// <param name="to_delete"></param>
    public void deleteAtom(Atom to_delete)
    {
        Dictionary<Atom, List<Vector3>> positionsRestore = new Dictionary<Atom, List<Vector3>>();
        List<Atom> delAtomList = new List<Atom>();
        List<Bond> delBondList = new List<Bond>();
        List<Molecule> delMoleculeList = new List<Molecule>();
        List<Molecule> addMoleculeList = new List<Molecule>();

        List<Atom> connectedAtomList = to_delete.connectedAtoms();
        Dictionary<Atom, int> numConnectedAtoms = new Dictionary<Atom, int>();
        foreach (var a in connectedAtomList)
        {
            if (a.m_data.m_abbre != "H" && a.m_data.m_abbre != "Dummy")
            {
                var conAtoms = a.connectedAtoms();
                numConnectedAtoms[a] = conAtoms.Count;
            }
        }


        // add to delete list
        foreach (Atom a in to_delete.m_molecule.atomList)
        {
            List<Vector3> conPos = new List<Vector3>();
            foreach (Atom at in a.connectedAtoms())
            {
                if (at == to_delete)
                {
                    if (!delAtomList.Contains(at))
                        delAtomList.Add(at);

                    if (a.m_data.m_abbre == "H" && !delAtomList.Contains(a))
                        delAtomList.Add(a);

                    conPos.Add(at.transform.localPosition);
                }
            }
            positionsRestore.Add(a, conPos);
        }

        createTopoMap(to_delete.m_molecule, delAtomList, delBondList, addMoleculeList);
        delAtomList.Clear();
        delBondList.Clear();
        delMoleculeList.Add(to_delete.m_molecule);

        foreach (Molecule m in delMoleculeList)
        {
            List_curMolecules.Remove(m);
            Destroy(m.gameObject);
        }

        foreach (Molecule m in addMoleculeList)
        {
            List_curMolecules.Add(m);
        }
        shrinkMoleculeIDs();

        foreach (var entry in numConnectedAtoms)
        {
            var a = entry.Key;
            int count = 0;
            while (entry.Value > a.connectedAtoms().Count)
            {
                CreateDummy(a.m_molecule.getFreshAtomID(), a.m_molecule, a, calcDummyPos(a, positionsRestore, count));
                count++;
            }
            a.m_molecule.shrinkAtomIDs();
        }

        SaveMolecule(true);
        // invoke data change event for new molecules
        foreach (Molecule m in addMoleculeList)
        {
            EventManager.Singleton.ChangeMolData(m);
        }
    }

    /// <summary>
    /// Deletes the measurements starting from or ending in the 
    /// given atom.
    /// </summary>
    /// <param name="atom"></param>
    public void deleteMeasurmentsOf(Atom atom)
    {
        // Distances
        List<DistanceMeasurement> distToRemove = new List<DistanceMeasurement>();
        foreach (var entry in distMeasurementDict)
        {
            if (entry.Value.Item1 == atom || entry.Value.Item2 == atom)
            {
                distToRemove.Add(entry.Key);
            }
        }
        distToRemove = new List<DistanceMeasurement>(new HashSet<DistanceMeasurement>(distToRemove)); // remove duplicates
        foreach (var dist in distToRemove)
        {
            distMeasurementDict.Remove(dist);
            if (dist)
            {
                Destroy(dist.gameObject); 
            }

        }

        // Angles
        //List<AngleMeasurment> angleToRemove = new List<AngleMeasurment>();
        //foreach (var entry in angleMeasurmentDict)
        //{
        //    if (entry.Value.Item1 == atom)
        //    {
        //        angleToRemove.Add(entry.Key);
        //    }
        //}
        //angleToRemove = new List<AngleMeasurment>(new HashSet<AngleMeasurment>(angleToRemove)); // remove duplicates
        //foreach (var angle in angleToRemove)
        //{
        //    angleMeasurmentDict.Remove(angle);
        //    Destroy(angle.gameObject);
        //}
    }

    /// <summary>
    /// Deletes angle measurements that depend on the given
    /// distance measurement.
    /// </summary>
    /// <param name="dist"></param>
    public void deleteAngleMeasurmentsOf(DistanceMeasurement dist)
    {
        List<AngleMeasurement> angleToRemove = new List<AngleMeasurement>();
        foreach (var entry in angleMeasurementDict)
        {
            if (entry.Value.Item2 == dist || entry.Value.Item3 == dist)
            {
                angleToRemove.Add(entry.Key);
            }
        }
        angleToRemove = new List<AngleMeasurement>(new HashSet<AngleMeasurement>(angleToRemove)); // remove duplicates
        foreach (var angle in angleToRemove)
        {
            angleMeasurementDict.Remove(angle);
            if (angle)
            {
                Destroy(angle.gameObject);
            }
        }
    }


    public void deleteAllMeasurementsUI()
    {
        deleteAllMeasurements();
        if (SettingsData.networkMeasurements)
        {
            EventManager.Singleton.ClearMeasurements();
        }
    }

    /// <summary>
    /// Deletes all registered measurements in the scene and clears the register.
    /// </summary>
    public void deleteAllMeasurements()
    {
        foreach (var entry in distMeasurementDict)
        {
            Destroy(entry.Key.gameObject);
        }
        distMeasurementDict.Clear();

        foreach (var entry in angleMeasurementDict)
        {
            Destroy(entry.Key.gameObject);
        }
        angleMeasurementDict.Clear();
    }

    /// <summary>
    /// Gets all distance measurements that start or end in 
    /// the given atom.
    /// </summary>
    /// <param name="atom"></param>
    /// <returns>a list of distance measurements that include <c>atom</c></returns>
    public List<DistanceMeasurement> getDistanceMeasurmentsOf(Atom atom)
    {

        List<DistanceMeasurement> contained_in = new List<DistanceMeasurement>();
        foreach (var entry in distMeasurementDict)
        {
            if (entry.Value.Item1 == atom || entry.Value.Item2 == atom)
            {
                contained_in.Add(entry.Key);
            }
        }
        contained_in = new List<DistanceMeasurement>(new HashSet<DistanceMeasurement>(contained_in)); // remove duplicates

        return contained_in;
    }

    /// <summary>
    /// Freezes/unfreezes all molecules in the scene.
    /// </summary>
    /// <param name="value">whether to freeze or unfreeze</param>
    public void freezeWorld(bool value)
    {
        foreach (var mol in List_curMolecules)
        {
            mol.freeze(value);
        }
    }

    /// <summary>
    /// this method creates a topological map of a molecule
    /// one needs to know which atoms are interconnected to delete them safely
    /// </summary>
    /// <param name="m">conducted molecule</param>
    /// <param name="delAtomList">list of atoms to delete</param>
    /// <param name="delBondList">list of bonds to delete</param>
    /// <param name="addMoleculeList">list of new molecules</param>
    public void createTopoMap(Molecule m, List<Atom> delAtomList, List<Bond> delBondList, List<Molecule> addMoleculeList)
    {
        groupedAtoms.Clear();
        // for all atoms in the molecule
        for (int i = 0; i < m.atomList.Count; i++)
        {
            List<Atom> groupingStash = new List<Atom>();
            Atom a = m.atomList[i];
            bool search = true;
            // for all groups
            foreach (KeyValuePair<Atom, List<Atom>> pair in groupedAtoms)
            {
                // for each atom in a group
                foreach (Atom at in pair.Value)
                {
                    // if the atom is in the group
                    if (at.m_id == a.m_id)
                    {
                        search = false;
                        break;
                    }
                }
            }
            // if atom isn't looked at yet or in the delete list
            if (!delAtomList.Contains(a) && search && !(a.m_data.m_abbre == "Dummy" && delAtomList.Contains(a.dummyFindMain())))
            {
                checkNeighbours(a, delAtomList, delBondList, groupingStash);
                groupedAtoms.Add(a, groupingStash);
            }
        }

        //sort Objects & lists regarding the dictionary
        cleanup(m, delAtomList, delBondList, addMoleculeList);

    }

    /// <summary>
    /// this method checks recursively for all connected atoms, if they are on one of the lists which will be deleted
    /// if they aren't on one of the lists, they will be added to a "group"
    /// </summary>
    /// <param name="a">starting atom</param>
    /// <param name="delAtomList">list of atoms to delete</param>
    /// <param name="delBondList">list of bonds to delete</param>
    /// <param name="groupingStash">group of all connected atoms</param>
    public void checkNeighbours(Atom a, List<Atom> delAtomList, List<Bond> delBondList, List<Atom> groupingStash)
    {
        if (!groupingStash.Contains(a))
            groupingStash.Add(a);

        //for each connected atom
        foreach (Atom ca in a.connectedAtoms())
        {
            Bond b = a.getBond(ca);
            if (!delBondList.Contains(b) && !delAtomList.Contains(ca) && !groupingStash.Contains(ca))
            {
                groupingStash.Add(ca);
                checkNeighbours(ca, delAtomList, delBondList, groupingStash);
            }
        }
    }

    /// <summary>
    /// this method finally deletes all marked atoms
    /// </summary>
    /// <param name="delAtomList">atoms which should be deleted</param>
    public void addDummiesToDelete(List<Atom> delAtomList)
    {
        int i = 0;
        List<Atom> deleteList = new List<Atom>();
        foreach (Atom a in delAtomList)
        {
            List<Atom> conDummys = a.connectedDummys();
            i++;
            foreach (Atom d in conDummys)
            {
                if (!delAtomList.Contains(d))
                {
                    deleteList.Add(d);
                }
            }
        }
    }

    /// <summary>
    /// this method moves the remaining molecule parts to new molecules
    /// </summary>
    /// <param name="m">old molecule</param>
    /// <param name="delAtomList">atoms which should be deleted</param>
    /// <param name="delBondList">bonds which should be deleted</param>
    /// <param name="addMoleculeList">list of new molecules</param>
    public void cleanup(Molecule m, List<Atom> delAtomList, List<Bond> delBondList, List<Molecule> addMoleculeList)
    {
        var fresh_id = getFreshMoleculeID();
        foreach (KeyValuePair<Atom, List<Atom>> pair in groupedAtoms)
        {
            Molecule tempMolecule = Instantiate(myBoundingBoxPrefab, new Vector3(0, 0, 0), Quaternion.identity).AddComponent<Molecule>();
            tempMolecule.transform.position = m.transform.position;
            tempMolecule.f_Init(fresh_id++, atomWorld.transform);
            addMoleculeList.Add(tempMolecule);
            foreach (Atom a in pair.Value)
            {
                a.transform.parent = tempMolecule.transform;
                a.m_molecule = tempMolecule;
                tempMolecule.atomList.Add(a);
            }
        }

        Dictionary<Bond, Molecule> tempBondDict = new Dictionary<Bond, Molecule>();
        foreach (Bond b in m.bondList)
        {
            if (!delBondList.Contains(b))
            {
                Atom at1 = b.m_molecule.atomList.ElementAtOrDefault(b.atomID1);
                Atom at2 = b.m_molecule.atomList.ElementAtOrDefault(b.atomID2);

                if (at1.transform.parent == at2.transform.parent)
                {
                    Molecule newMol = at1.transform.parent.GetComponent<Molecule>();
                    tempBondDict.Add(b, newMol);
                }
            }
        }

        foreach (KeyValuePair<Bond, Molecule> pair in tempBondDict)
        {
            pair.Key.transform.parent = pair.Value.transform;
            pair.Key.m_molecule = pair.Value;
            pair.Value.bondList.Add(pair.Key);
        }

        addDummiesToDelete(delAtomList);
    }

    public Vector3 calcDummyPos(Atom a, Dictionary<Atom, List<Vector3>> positionsRestore, int count)
    {
        positionsRestore.TryGetValue(a, out List<Vector3> values);
        Vector3 newPos = values.ElementAtOrDefault(count);
        if (newPos == default)
        {
            newPos = a.transform.position;
        }
        return newPos;
    }

    #endregion

    #region layer management
    /// <summary>
    /// This method toggles everything in a background layer and 
    /// toggles the bonds in the front layer
    /// </summary> 
    public void toggleBondLayer()
    {
        bondsInForeground = !bondsInForeground;
        int bondLayer = 7;
        int atomLayer = 0;
        if (bondsInForeground)
        {
            bondLayer = 0;
            atomLayer = 6;
        }
        foreach (var molecule in List_curMolecules)
        {
            molecule.gameObject.layer = atomLayer;
            foreach (Transform child in molecule.transform)
            {
                child.gameObject.layer = atomLayer;
                if (child.name == "box")
                {
                    foreach (Transform box_collider in child)
                    {
                        box_collider.gameObject.layer = atomLayer;
                    }
                }
            }
            foreach (var bond in molecule.bondList)
            {
                bond.gameObject.layer = bondLayer;
            }
        }
    }
    #endregion

    #region move functions
    /// <summary>
    /// Try to move an atom of a given molecule to a given position.
    /// </summary>
    /// <param name="mol_id"></param>
    /// <param name="atom_id"></param>
    /// <param name="pos"></param>
    /// <returns>whether the atom could successfully be moved</returns>
    public bool moveAtom(ushort mol_id, ushort atom_id, Vector3 pos)
    {
        var mol = Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
        var atom = mol?.atomList.ElementAtOrNull(atom_id, null);
        if (mol == null || atom == null)
        {
            Debug.LogError($"[GlobalCtrl:moveAtom] Trying to move Atom {atom_id} of molecule {mol_id}, but it does not exist.");
            return false;
        }
        else
        {
            atom.transform.localPosition = pos;
            return true;
        }
    }

    /// <summary>
    /// Try to stop the movement of an atom (needed for correct networking).
    /// </summary>
    /// <param name="mol_id"></param>
    /// <param name="atom_id"></param>
    /// <returns>whether the stop was completed successfully</returns>
    public bool stopMoveAtom(ushort mol_id, ushort atom_id)
    {
        var mol = Singleton.List_curMolecules.ElementAtOrNull(mol_id, null);
        var atom = mol?.atomList.ElementAtOrNull(atom_id, null);
        if (mol == null || atom == null)
        {
            Debug.LogError($"[GlobalCtrl:stopMoveAtom] Trying to resetMolPositionAfterMove of Atom {atom_id} of molecule {mol_id}, but it does not exist.");
            return false;
        }
        else
        {
            atom.resetMolPositionAfterMove();
            return true;
        }
    }

    /// <summary>
    /// Attempt to move a molecule to a given position in a given rotation.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pos"></param>
    /// <param name="quat"></param>
    /// <returns>whether the molecule was moved successfully</returns>
    public bool moveMolecule(ushort id, Vector3 pos, Quaternion quat)
    {
        var molecule = List_curMolecules.ElementAtOrDefault(id);
        if (molecule != default)
        {
            molecule.transform.localPosition = pos;
            molecule.transform.localRotation = quat;
            return true;
        }
        else
        {
            Debug.LogError($"[GlobalCtrl] Trying to move Molecule {id}, but it does not exist.");
            return false;
        }
    }

    #endregion

    #region atom building functions

    /// <summary>
    /// Creates a new atom from the given information
    /// The information source is a textfile with all parameters to each atomtype
    /// </summary>
    /// <param name="ChemicalID">chemical ID of the atom which should be created</param>
    /// <param name="pos">position, where the atom should be created</param>
    public void CreateAtom(ushort moleculeID, string ChemicalAbbre, Vector3 pos, ushort hyb, bool createLocal = false)
    {
        // create atom from atom prefab
        GameObject tempMoleculeGO = Instantiate(myBoundingBoxPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        Molecule tempMolecule = tempMoleculeGO.AddComponent<Molecule>();

        if (!createLocal)
        {
            tempMolecule.transform.position = pos;
            tempMolecule.f_Init(moleculeID, atomWorld.transform);
        }
        else
        {
            tempMolecule.f_Init(moleculeID, atomWorld.transform);
            tempMolecule.transform.localPosition = pos;
        }

        // 0: none; 1: sp1; 2: sp2;  3: sp3;  4: hypervalent trig. bipy; 5: unused;  6: hypervalent octahedral
        ElementData tempData = Dic_ElementData[ChemicalAbbre];
        tempData.m_hybridization = hyb;
        tempData.m_bondNum = calcNumBonds(tempData.m_hybridization, tempData.m_bondNum);

        ushort atom_id = 0;
        Atom tempAtom = Instantiate(myAtomPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Atom>();
        tempAtom.f_Init(tempData, tempMolecule, Vector3.zero , atom_id);

        // add dummies
        foreach (Vector3 posForDummy in tempAtom.m_posForDummies)
        {
            atom_id++;
            CreateDummy(atom_id, tempMolecule, tempAtom, posForDummy);
        }

        List_curMolecules.Add(tempMolecule);

        if(currentInteractionMode == InteractionModes.MEASUREMENT)
        {
            tempMolecule.freezeUI(true);
        }

        SaveMolecule(true);

        EventManager.Singleton.ChangeMolData(tempMolecule);
    }

    /// <summary>
    /// Creates an atom of the given type and invokes a create atom event.
    /// </summary>
    /// <param name="ChemicalID"></param>
    public void createAtomUI(string ChemicalID)
    {
        lastAtom = ChemicalID; // remember this for later
        Vector3 create_position = currentCamera.transform.position + 0.5f * currentCamera.transform.forward;
        var newID = getFreshMoleculeID();
        CreateAtom(newID, ChemicalID, create_position, curHybrid);

        // Let the networkManager know about the user action
        // Important: insert localPosition here
        EventManager.Singleton.CreateAtom(newID, ChemicalID, List_curMolecules[newID].transform.localPosition, curHybrid);
    }

    public ushort calcNumBonds(ushort hyb, ushort element_bondNum)
    {
        return (ushort)Mathf.Max(0, element_bondNum - (3 - hyb)); // a preliminary solution
    }

    /// <summary>
    /// this method changes the type of an atom
    /// </summary>
    /// <param name="idAtom">ID of the selected atom</param>
    /// <param name="ChemicalAbbre">chemical abbrevation of the new atom type</param>
    public bool changeAtom(ushort idMol, ushort idAtom, string ChemicalAbbre)
    {
        // TODO: do not overwrite runtime data
        var mol = Singleton.List_curMolecules.ElementAtOrNull(idMol, null);
        var chgAtom = mol?.atomList.ElementAtOrNull(idAtom, null);
        if (mol == null || chgAtom == null)
        {
            return false;
        }

        ElementData tempData = Dic_ElementData[ChemicalAbbre];
        tempData.m_hybridization = chgAtom.m_data.m_hybridization;
        tempData.m_bondNum = calcNumBonds(tempData.m_hybridization, tempData.m_bondNum);

        chgAtom.f_Modify(tempData);
        foreach(Bond b in chgAtom.connectedBonds())
        {
            b.setShaderProperties();
        }

        SaveMolecule(true);
        EventManager.Singleton.ChangeMolData(List_curMolecules.ElementAtOrDefault(idMol));
        return true;
    }

    /// <summary>
    /// changes the type of an atom and invokes a change atom event.
    /// </summary>
    /// <param name="idMol">ID of the molecule containing the selected atom</param>
    /// <param name="idAtom">ID of the selected atom</param>
    /// <param name="ChemicalAbbre">chemical abbrevation of the new atom type</param>
    public void changeAtomUI(ushort idMol, ushort idAtom, string ChemicalAbbre)
    {
        EventManager.Singleton.ChangeAtom(idMol, idAtom, ChemicalAbbre);
        changeAtom(idMol, idAtom, ChemicalAbbre);
    }

    /// <summary>
    /// Modifies the hybridization of a given atom and invokes a modify hybridization event.
    /// </summary>
    /// <param name="atom">the atom of which to modify the hybridization</param>
    /// <param name="hybrid">the new hybridization</param>
    public void modifyHybridUI(Atom atom, ushort hybrid)
    {
        EventManager.Singleton.ModifyHyb(atom.m_molecule.m_id, atom.m_id, hybrid);
        modifyHybrid(atom, hybrid);
    }

    /// <summary>
    /// Modifies the hybridization of a given atom.
    /// </summary>
    /// <param name="atom">the atom of which to modify the hybridization</param>
    /// <param name="hybrid">the new hybridization</param>
    public void modifyHybrid(Atom atom, ushort hybrid)
    {
        ElementData tempData = Dic_ElementData[atom.m_data.m_abbre];
        tempData.m_hybridization = hybrid;
        tempData.m_bondNum = calcNumBonds(tempData.m_hybridization, tempData.m_bondNum);

        atom.f_Modify(tempData);
        SaveMolecule(true);
        EventManager.Singleton.ChangeMolData(atom.m_molecule);
    }

    /// <summary>
    /// Try to modify the hybridization of an atom in a molecule given by IDs.
    /// </summary>
    /// <param name="mol_id">ID of the molecule containing the selected atom</param>
    /// <param name="atom_id">ID of the selected atom</param>
    /// <param name="hybrid">new hybridization to use</param>
    /// <returns>whether the hybridization could be successfully modified</returns>
    public bool modifyHybrid(ushort mol_id, ushort atom_id, ushort hybrid)
    {

        Atom chgAtom = List_curMolecules.ElementAtOrDefault(mol_id).atomList.ElementAtOrDefault(atom_id);
        if (chgAtom == default)
        {
            return false;
        }

        modifyHybrid(chgAtom, hybrid);

        return true;
    }


    /// <summary>
    /// change atom method without saving; this ensures that replace dummies will be saved as a single action
    /// that can be reversed with undo
    /// </summary>
    /// <param name="idAtom">ID of the selected atom</param>
    public bool switchDummyHydrogen(ushort idMol, ushort idAtom, bool isDummy=true)
    {
        // TODO: do not overwrite runtime data
        Atom chgAtom = List_curMolecules.ElementAtOrDefault(idMol).atomList.ElementAtOrDefault(idAtom);
        if (chgAtom == default)
        {
            return false;
        }

        String type = isDummy ? "H" : "Dummy";

        ElementData tempData = Dic_ElementData[type];
        tempData.m_hybridization = chgAtom.m_data.m_hybridization;
        tempData.m_bondNum = calcNumBonds(tempData.m_hybridization, tempData.m_bondNum);

        chgAtom.f_Modify(tempData);
        foreach(Bond b in chgAtom.connectedBonds())
        {
            b.setShaderProperties();
        }
        return true;
    }

    /// <summary>
    /// this method rebuilds an atom
    /// </summary>
    /// <param name="idAtom">id of the new atom</param>
    /// <param name="ChemicalAbbre">chemical abbrevation</param>
    /// <param name="pos">position, where the atom should be created</param>
    /// <param name="mol">molecule, to which the atom belongs</param>
    public Atom RebuildAtom(ushort idAtom, string ChemicalAbbre, ushort hybrid, Vector3 pos, Molecule mol)
    {
        //var currentMax = getMaxAtomID();
        //if (idAtom < currentMax)
        //{
        //    Debug.LogError($"[GlobalCtrl] RebuildAtom: requested id {idAtom} lies in current AtomID range {currentMax}");
        //}

        ElementData tempData = Dic_ElementData[ChemicalAbbre];
        tempData.m_hybridization = hybrid;
        Atom tempAtom = Instantiate(myAtomPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Atom>();
        tempAtom.f_Init(tempData, mol, pos, idAtom);
        return tempAtom;
    }

    /// <summary>
    /// Creates a dummy atom to fill empty binding positions
    /// </summary>
    /// <param name="inputMole">the molecule to which the dummy belongs</param>
    /// <param name="mainAtom">the main atom to which the dummy is connected</param>
    /// <param name="pos">the position, where the dummy is created</param>
    public void CreateDummy(ushort idDummy, Molecule inputMole, Atom mainAtom, Vector3 pos)
    {
        Atom dummy = Instantiate(myAtomPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Atom>();
        dummy.f_Init(Dic_ElementData["Dummy"], inputMole, pos, idDummy);//0 for dummy
        CreateBond(mainAtom, dummy, inputMole);
    }

    /// <summary>
    /// creates a bond between two atoms
    /// This method is called when the mergeMolecule method is executed, as this controls when a new bond is created
    /// </summary>
    /// <param name="atom1">first atom of the connection</param>
    /// <param name="atom2">second atom of the connection</param>
    /// <param name="inputMole">molecule to which the atoms and the connection belong</param>
    public void CreateBond(Atom atom1, Atom atom2, Molecule inputMole)
    {
        Bond tempBond = Instantiate(bondPrefab);
        tempBond.f_Init(atom1, atom2, inputMole);
    }


    /// <summary>
    /// This method merges two molecules together
    /// This method is called when a bond will be created and two atoms are linked
    /// </summary>
    /// <param name="dummyInHand">the dummy of the molecule which is grabbed</param>
    /// <param name="dummyInAir">the dummy of the molecule which is in the air</param>
    public void MergeMolecule(Atom dummyInHand, Atom dummyInAir)
    {
        collision = false;
        dummyInHand.dummyFindMain().keepConfig = false;

        //Debug.Log($"[MergeMolecule] Mol in hand num_atoms {dummyInHand.m_molecule.atomList.Count}");
        //Debug.Log($"[MergeMolecule] Mol in air num_atoms {dummyInAir.m_molecule.atomList.Count}");

        Molecule molInHand = dummyInHand.m_molecule;
        Molecule molInAir = dummyInAir.m_molecule;
        // scale before merge
        molInHand.transform.localScale = molInAir.transform.localScale;
        Bond bondInHand = molInHand.bondList.Find(p=>p.atomID1 == dummyInHand.m_id || p.atomID2 == dummyInHand.m_id);
        Bond bondInAir = molInAir.bondList.Find(p => p.atomID1 == dummyInAir.m_id || p.atomID2 == dummyInAir.m_id);
        if(molInHand != molInAir)
        {
            molInHand.givingOrphans(molInAir);
        }

        //Atom atom1 = List_curAtoms.Find((x) => x == bondInHand.findTheOther(dummyInHand));
        //Atom atom2 = List_curAtoms.Find((x) => x == bondInAir.findTheOther(dummyInAir));
        var atom1 = dummyInHand.dummyFindMain();
        var atom2 = dummyInAir.dummyFindMain();

        //remove dummy and dummy bond of molecule in air
        molInAir.atomList.Remove(dummyInAir);
        Destroy(dummyInAir.gameObject);
        molInAir.bondList.Remove(bondInAir);
        Destroy(bondInAir.gameObject);
        molInAir.atomList.Remove(dummyInHand);
        Destroy(dummyInHand.gameObject);
        molInAir.bondList.Remove(bondInHand);
        Destroy(bondInHand.gameObject);

        // DEBUG
        //Debug.Log($"[GlobalCtrl:MergeMolecule] Atoms in Molecule {molInAir.atomList.Count}, bonds in Molecule {molInAir.bondList.Count}"); 

        molInAir.shrinkAtomIDs();
        shrinkMoleculeIDs();

        CreateBond(atom1, atom2, molInAir);

        // Clear selection
        // TODO differentiate between problematic and not problematic cases
        molInAir.markMolecule(false);

        foreach(Bond bond in molInAir.bondList)
        {
            bond.setShaderProperties();
        }

        SaveMolecule(true);

        EventManager.Singleton.ChangeMolData(molInAir);

    }

    // overload to handle IDs
    public void MergeMolecule(ushort molInHand, ushort dummyInHand, ushort molInAir, ushort dummyInAir)
    {
        MergeMolecule(List_curMolecules[molInHand].atomList[dummyInHand], List_curMolecules[molInAir].atomList[dummyInAir]);
    }

    /// <summary>
    /// This method separates a molecule between two grabbed atoms
    /// This method is called when a bond will be deleted and two atoms are separated
    /// </summary>
    /// <param name="atom1">the first of the grabbed atoms</param>
    /// <param name="atom2">the second of the grabbed atoms</param>
    public void SeparateMolecule(Atom atom1, Atom atom2)
    {
        Molecule mol = atom1.m_molecule;

        Bond bond = mol.bondList.Find(p => (p.atomID1 == atom1.m_id && p.atomID2 == atom2.m_id)
                                            || (p.atomID1 == atom2.m_id && p.atomID2 == atom1.m_id));

        deleteBondUI(bond);

    }

    /// <summary>
    /// Try to change the force field term of a single bond.
    /// </summary>
    /// <param name="mol_id">ID of the molecule containing the selected bond</param>
    /// <param name="term_id">ID of the selected bond term</param>
    /// <param name="new_term">the new bond term to use</param>
    /// <returns>whether the change was successful</returns>
    public bool changeBondTerm(ushort mol_id, ushort term_id, ForceField.BondTerm new_term)
    {
        if (term_id >= List_curMolecules.ElementAtOrDefault(mol_id).bondTerms.Count)
        {
            return false;
        }
        List_curMolecules[mol_id].changeBondParameters(new_term, term_id);

        return true;
    }

    /// <summary>
    /// Try to change the force field term of an angle bond.
    /// </summary>
    /// <param name="mol_id">ID of the molecule containing the selected angle bond</param>
    /// <param name="term_id">ID of the selected angle term</param>
    /// <param name="new_term">the new angle term to use</param>
    /// <returns>whether the change was successful</returns>
    public bool changeAngleTerm(ushort mol_id, ushort term_id, ForceField.AngleTerm new_term)
    {
        if (term_id >= List_curMolecules.ElementAtOrDefault(mol_id).angleTerms.Count)
        {
            return false;
        }
        List_curMolecules[mol_id].changeAngleParameters(new_term, term_id);

        return true;
    }

    /// <summary>
    /// Try to change the force field term of a torsion bond.
    /// </summary>
    /// <param name="mol_id">ID of the molecule containing the selected bond</param>
    /// <param name="term_id">ID of the selected torsion term</param>
    /// <param name="new_term">the new torsion term to use</param>
    /// <returns>whether the change was successful</returns>
    public bool changeTorsionTerm(ushort mol_id, ushort term_id, ForceField.TorsionTerm new_term)
    {
        if (term_id >= List_curMolecules.ElementAtOrDefault(mol_id).torsionTerms.Count)
        {
            return false;
        }
        List_curMolecules[mol_id].changeTorsionParameters(new_term, term_id);

        return true;
    }


    /// <summary>
    /// Creates a copy of the given molecule slightly upwards of it.
    /// Invokes a network event for the new molecule.
    /// </summary>
    /// <param name="molecule">the molecule to copy</param>
    public void copyMolecule(Molecule molecule)
    {
        // save old molecule data
        Vector3 molePos = molecule.transform.localPosition;
        List<cmlAtom> list_atom = new List<cmlAtom>();
        foreach (Atom a in molecule.atomList)
        {

            list_atom.Add(new cmlAtom(a.m_id, a.m_data.m_abbre, a.m_data.m_hybridization, a.transform.localPosition));
        }
        List<cmlBond> list_bond = new List<cmlBond>();
        foreach (Bond b in molecule.bondList)
        {
            list_bond.Add(new cmlBond(b.atomID1, b.atomID2, b.m_bondOrder));
        }
        cmlData moleData = new cmlData(molePos, molecule.transform.rotation, molecule.m_id, list_atom, list_bond);


        // Create new molecule
        var freshMoleculeID = getFreshMoleculeID();

        Molecule tempMolecule = Instantiate(myBoundingBoxPrefab, moleData.molePos, Quaternion.identity).AddComponent<Molecule>();
        tempMolecule.f_Init(freshMoleculeID, atomWorld.transform, moleData);
        List_curMolecules.Add(tempMolecule);

        //LOAD STRUCTURE CHECK LIST / DICTIONNARY

        for (int i = 0; i < moleData.atomArray.Length; i++)
        {
            RebuildAtom(moleData.atomArray[i].id, moleData.atomArray[i].abbre, moleData.atomArray[i].hybrid, moleData.atomArray[i].pos, tempMolecule);
        }
        for (int i = 0; i < moleData.bondArray.Length; i++)
        {
            CreateBond(tempMolecule.atomList.ElementAtOrDefault(moleData.bondArray[i].id1), tempMolecule.atomList.ElementAtOrDefault(moleData.bondArray[i].id2), tempMolecule);
        }
        moveMolecule(freshMoleculeID, moleData.molePos + Vector3.up*0.05f, moleData.moleQuat);
        EventManager.Singleton.MoveMolecule(freshMoleculeID, moleData.molePos + Vector3.up * 0.05f, moleData.moleQuat);
        EventManager.Singleton.ChangeMolData(tempMolecule);
    }
    #endregion

    #region export import

    /// <summary>
    /// this method converts the selected input molecule to lists which then are saved in an XML format
    /// </summary>
    /// <param name="inputMole">the molecule which will be saved</param>
    public void SaveMolecule(bool onStack, string name = "")
    {
        Vector3 meanPos = new Vector3(0.0f, 0.0f, 0.0f); // average position if several molecule blocks are saved
        int nMol = 0;   // number of molecule blocks
        List<cmlData> saveData = new List<cmlData>();
        if(!onStack)
        {
            foreach (Molecule inputMole in List_curMolecules)
            {
                meanPos += inputMole.transform.localPosition;
                nMol++;
            }
            if (nMol >= 1) meanPos /= (float)nMol;
        }


        foreach (Molecule inputMole in List_curMolecules)
        {
            Vector3 molePos = inputMole.transform.localPosition - meanPos;  // relative to mean position
            List<cmlAtom> list_atom = new List<cmlAtom>();
            foreach (Atom a in inputMole.atomList)
            {

                list_atom.Add(new cmlAtom(a.m_id, a.m_data.m_abbre, a.m_data.m_hybridization, a.transform.localPosition));
            }
            List<cmlBond> list_bond = new List<cmlBond>();
            foreach (Bond b in inputMole.bondList)
            {
                list_bond.Add(new cmlBond(b.atomID1, b.atomID2, b.m_bondOrder));
            }
            cmlData tempData = new cmlData(molePos, inputMole.transform.rotation, inputMole.m_id, list_atom, list_bond);
            saveData.Add(tempData);
        }

        if(!onStack)
        {
            XMLFileHelper.SaveData(Application.streamingAssetsPath + "/SavedMolecules/" + name + ".xml", saveData);
            Debug.Log($"[GlobalCtrl] Saved Molecule as: {name}.xml");
        } else
        {
            systemState.Push(saveData);
        }

    }

    /// <summary>
    /// this method loads a saved molecule into the workspace
    /// </summary>
    /// <param name="name">name of the saved molecule</param>
    public void LoadMolecule(string name)
    {
        List<cmlData> loadData;

        Vector3 meanPos = new Vector3(0.0f, 0.0f, 0.0f);

        loadData = (List<cmlData>)XMLFileHelper.LoadData(Application.streamingAssetsPath + "/SavedMolecules/" + name + ".xml", typeof(List<cmlData>));
        if (loadData != null)
        {
            int nMol = 0;
            foreach (cmlData molecule in loadData)
            {
                nMol++;
                meanPos += molecule.molePos;
                Debug.Log($"[GlobalCtrl] LoadMolecule: {molecule.ToString()}");
            }
            if (nMol > 0) meanPos /= (float)nMol;


            // new mean position should be in front of camera
            Vector3 current_pos = currentCamera.transform.position - atomWorld.transform.position; // transform this here onto atom world coordinates
            Vector3 current_lookat = currentCamera.transform.forward;
            Vector3 create_position = current_pos + 0.5f * current_lookat;
            meanPos = create_position - meanPos;  // add molecules relative to this position


            foreach (cmlData molecule in loadData)
            {
                var freshMoleculeID = getFreshMoleculeID();

                Molecule tempMolecule = Instantiate(myBoundingBoxPrefab, molecule.molePos, Quaternion.identity).AddComponent<Molecule>();
                tempMolecule.f_Init(freshMoleculeID, atomWorld.transform, molecule);
                List_curMolecules.Add(tempMolecule);


                //LOAD STRUCTURE CHECK LIST / DICTIONNARY

                for (int i = 0; i < molecule.atomArray.Length; i++)
                {
                    RebuildAtom(molecule.atomArray[i].id, molecule.atomArray[i].abbre, molecule.atomArray[i].hybrid, molecule.atomArray[i].pos, tempMolecule);
                }
                for (int i = 0; i < molecule.bondArray.Length; i++)
                {
                    CreateBond(tempMolecule.atomList.ElementAtOrDefault(molecule.bondArray[i].id1), tempMolecule.atomList.ElementAtOrDefault(molecule.bondArray[i].id2), tempMolecule);
                }
                moveMolecule(freshMoleculeID, molecule.molePos + meanPos, molecule.moleQuat);
                EventManager.Singleton.MoveMolecule(freshMoleculeID, molecule.molePos + meanPos, molecule.moleQuat);
                EventManager.Singleton.ChangeMolData(tempMolecule);
            }
        }
    }

    /// <summary>
    /// Reads molecule data from an xml file.
    /// </summary>
    /// <param name="name">the file name (without type extension and path)</param>
    /// <returns>a list of cmlData</returns>
    public List<cmlData> getMoleculeData(string name)
    {
        return (List<cmlData>)XMLFileHelper.LoadData(Application.streamingAssetsPath + "/SavedMolecules/" + name + ".xml", typeof(List<cmlData>));
    }

    /// <summary>
    /// Saves all molecules, atoms and different bonds.
    /// </summary>
    /// <returns>list of cmlData representing the entire atom world</returns>
    public List<cmlData> saveAtomWorld()
    {
        // flatten IDs first
        shrinkMoleculeIDs();
        // this method preserves the position of the molecules and atoms (and rotation)
        List<cmlData> saveData = new List<cmlData>();
        foreach (Molecule inputMole in List_curMolecules)
        {
            inputMole.shrinkAtomIDs();
            List<cmlAtom> list_atom = new List<cmlAtom>();
            foreach (Atom a in inputMole.atomList)
            {
                list_atom.Add(new cmlAtom(a.m_id, a.m_data.m_abbre, a.m_data.m_hybridization, a.transform.localPosition));
            }

            cmlData tempData;
            List<cmlBond> list_bond = new List<cmlBond>();
            foreach (var b in inputMole.bondTerms)
            {
                list_bond.Add(new cmlBond(b.Atom1, b.Atom2, b.order, b.eqDist, b.kBond));
            }
            List<cmlAngle> list_angle = new List<cmlAngle>();
            foreach (var b in inputMole.angleTerms)
            {
                list_angle.Add(new cmlAngle(b.Atom1, b.Atom2, b.Atom3, b.eqAngle, b.kAngle));
            }
            List<cmlTorsion> list_torsion = new List<cmlTorsion>();
            foreach (var b in inputMole.torsionTerms)
            {
                list_torsion.Add(new cmlTorsion(b.Atom1, b.Atom2, b.Atom3, b.Atom4, b.eqAngle, b.vk, b.nn));
            }

            tempData = new cmlData(inputMole.transform.localPosition, inputMole.transform.localRotation, inputMole.m_id, list_atom, list_bond, list_angle, list_torsion, true);
            saveData.Add(tempData);
        }

        return saveData;
    }

    /// <summary>
    /// Rebuilds the atom world.
    /// </summary>
    /// <param name="data">list of cmlData that represents the world state to rebuild</param>
    /// <param name="add">whether to add to an existing atom world</param>
    public void rebuildAtomWorld(List<cmlData> data, bool add = false)
    {
        // this method preserves the ids of all objects
        if (data != null)
        {
            foreach (cmlData molecule in data)
            {
                var freshMoleculeID = getFreshMoleculeID();

                Molecule tempMolecule = Instantiate(myBoundingBoxPrefab).AddComponent<Molecule>();
                tempMolecule.f_Init(add == true ? freshMoleculeID : molecule.moleID, atomWorld.transform, molecule);
                tempMolecule.transform.localPosition = molecule.molePos;
                tempMolecule.transform.localRotation = molecule.moleQuat;
                List_curMolecules.Add(tempMolecule);

                for (int i = 0; i < molecule.atomArray.Length; i++)
                {
                    RebuildAtom(molecule.atomArray[i].id, molecule.atomArray[i].abbre, molecule.atomArray[i].hybrid, molecule.atomArray[i].pos, tempMolecule);
                }
                for (int i = 0; i < molecule.bondArray.Length; i++)
                {
                    CreateBond(tempMolecule.atomList.ElementAtOrDefault(molecule.bondArray[i].id1), tempMolecule.atomList.ElementAtOrDefault(molecule.bondArray[i].id2), tempMolecule);
                }
                if (molecule.keepConfig)
                {
                    foreach (var atom in tempMolecule.atomList)
                    {
                        atom.keepConfig = true;
                    }
                }
                EventManager.Singleton.ChangeMolData(tempMolecule);
            }
        }
        SaveMolecule(true);
    }

    /// <summary>
    /// Performs an undo operation and invokes an undo event.
    /// </summary>
    public void undoUI()
    {
        if (LoginData.normal_mode)
        {
            undo();
        }
        else
        {
            EventManager.Singleton.Undo();
        }
    }

    /// <summary>
    /// Performs an undo operation by loading the last saved system state.
    /// </summary>
    public void undo()
    {
        Debug.Log($"[GlobalCrtl:undo] Stack size: {systemState.Count}.");
        List<cmlData> loadData = null;
        for (int i = 0; i < 2; i++)
        {
            if (systemState.Count > 0)
            {
                loadData = systemState.Pop();
            }
            else
            {
                loadData = null;
            }
        }

        if (loadData != null)
        {
            DeleteAll();
            rebuildAtomWorld(loadData);
        }
    }


    public void LoadPreset(int number)
    {
        DeleteAll();
        switch(number)
        {
            case 1:
                LoadMolecule("Aminole");
                break;
            case 2:
                LoadMolecule("AcrylicAcid");
                break;
            case 3:
                LoadMolecule("Adamantane");
                break;
            case 4:
                LoadMolecule("Indole");
                break;
            case 5:
                LoadMolecule("MirrorTask");
                break;
            case 6:
                LoadMolecule("PlanarChirality");
                break;
        }
    }

    #endregion

    #region id management

    /// <summary>
    /// this method gets the maximum atomID currently in the scene
    /// </summary>
    /// <returns>id</returns>
    public ushort getMaxMoleculeID()
    {
        ushort id = 0;
        foreach (Molecule m in List_curMolecules)
        {
            id = Math.Max(id, m.m_id);
        }
        return id;
    }

    /// <summary>
    /// this method shrinks the IDs of the molecules to prevent an overflow
    /// </summary>
    public void shrinkMoleculeIDs()
    {
        for (ushort i = 0; i < List_curMolecules.Count; i++)
        {
            List_curMolecules[i].m_id = i;
        }
    }

    /// <summary>
    /// gets a fresh available atom id
    /// </summary>
    /// <param name="idNew">new ID</param>
    public ushort getFreshMoleculeID()
    {
        if (List_curMolecules.Count == 0)
        {
            return 0;
        }
        else
        {
            shrinkMoleculeIDs();
            return (ushort)(getMaxMoleculeID() + 1);
        }
    }


    #endregion

    #region ui functions


    /// <summary>
    /// Gets data of an element based on its chemical abbreviation.
    /// </summary>
    /// <param name="abbre">chemical abbreviation of the needed element</param>
    /// <returns>data of the requested element</returns>
    public ElementData GetElementbyAbbre(string abbre)
    {
        foreach(KeyValuePair<string, ElementData> pair in Dic_ElementData)
        {
            if (pair.Key == abbre)
                return pair.Value;
        }
        ElementData none = new ElementData();
        return none;
    }

    //public void setFavorite(int pos, string abbre, List<GameObject> favMenu)
    //{
    //    favorites[pos - 1] = abbre;
    //    //Transform temp = null;
    //    //for (int i = 0; i < periodictable.transform.childCount; i++)
    //    //{
    //    //    if (periodictable.transform.GetChild(i).transform.Find("Btn_" + GetElementbyAbbre(abbre).m_name) != null)
    //    //        temp = periodictable.transform.GetChild(i).transform.Find("Btn_" + GetElementbyAbbre(abbre).m_name);
    //    //}

    //    //favMenu[pos - 1].transform.GetComponent<Image>().sprite = temp.GetComponent<Image>().sprite;
    //    //favoritesGO[pos - 1].transform.GetComponent<Image>().sprite = temp.GetComponent<Image>().sprite;
    //}

    //public void createFavoriteElement(int pos)
    //{
    //    lastAtom = favorites[pos - 1]; // remember this for later
    //    Vector3 current_pos = currentCamera.transform.position;
    //    Vector3 current_lookat = currentCamera.transform.forward;
    //    Vector3 create_position = current_pos + 0.5f * current_lookat;
    //    CreateAtom(getFreshMoleculeID(), favorites[pos - 1], create_position, curHybrid);
    //}

    /// <summary>
    /// Returns the first marked object of type <c>type</c> in the current list of molecules.
    /// </summary>
    /// <param name="type">the type of object to search (0: Molecule, 1: Atom, 2: Bond)</param>
    /// <returns>the first marked object of the requested type</returns>
    public object getNextMarked(int type)
    {
        if(type == 0)
        {
            foreach (Molecule m in List_curMolecules)
            {
                if (m.isMarked)
                    return m;
            }
        }
        else if(type == 1)
        {
            foreach (Molecule m in List_curMolecules)
            {
                foreach (Atom a in m.atomList)
                {
                    if (a.isMarked)
                        return a;
                }
            }
        } else if(type == 2)
        {
            foreach (Molecule m in List_curMolecules)
            {
                foreach (Bond b in m.bondList)
                {
                    if (b.isMarked)
                        return b;
                }
            }
        }
        return null;
    }
    #endregion

    #region Settings
    /// <summary>
    /// Toggles visibility of the debug window.
    /// </summary>
    public void toggleDebugWindow()
    {
        if (DebugWindow.Singleton)
        {
            DebugWindow.Singleton.toggleVisible();
        }
    }

    /// <summary>
    /// Opens an instance of the settings window.
    /// </summary>
    public void openSettingsWindow()
    {
        var settingsPrefab = (GameObject)Resources.Load("prefabs/Settings");
        Instantiate(settingsPrefab);
    }

    /// <summary>
    /// Opens an instance of the Load/Save window.
    /// </summary>
    public void openLoadSaveWindow()
    {
        var load_save = (GameObject)Resources.Load("prefabs/LoadSaveList");
        Instantiate(load_save);
    }

    /// <summary>
    /// Opens an instance of the scrollable atom menu.
    /// </summary>
    public void openAtomMenuScrollable()
    {
        var atomMenuScrollablePrefab = (GameObject)Resources.Load("prefabs/AtomMenuScrollable");
        Instantiate(atomMenuScrollablePrefab);
    }

    /// <summary>
    /// Toggles visibility of the hand menu.
    /// </summary>
    public void toggleHandMenu()
    {
        handMenu.Singleton.toggleVisible();
    }
    #endregion

    /// <summary>
    /// Destroys and regenerates all tool tips in the scene.
    /// This is used when changing the locale to properly update the tool tip text.
    /// </summary>
    private void regenerateTooltips()
    {
        foreach(Molecule mol in List_curMolecules)
        {
            // Single atom tool tips
            foreach(Atom a in mol.atomList)
            {
                if (a.toolTipInstance)
                {
                    Destroy(a.toolTipInstance);
                    a.createToolTip();
                }
            }

            // Molecule and bond tool tips
            if (mol.toolTipInstance)
            {
                Molecule.toolTipType type = mol.type;
                var target = mol.toolTipInstance.GetComponent<myToolTipConnector>().Target;
                Destroy(mol.toolTipInstance);
                if(type == Molecule.toolTipType.MOLECULE)
                {
                    mol.createToolTip();
                }
                else if(type == Molecule.toolTipType.SINGLE || type == Molecule.toolTipType.TORSION)
                {
                    ushort id = target.GetComponent<Bond>().atomID1;
                    Atom a = findAtomById(mol, id);
                    a.markConnections(true);
                }
                else if(type == Molecule.toolTipType.ANGLE)
                {
                    Atom a = target.GetComponent<Atom>();
                    a.markConnections(true);
                }
            }
        }
    }

    public void regenerateSingleBondTooltips()
    {
        foreach(Molecule mol in List_curMolecules)
        {
            if (mol.toolTipInstance)
            {
                Molecule.toolTipType type = mol.type;
                if (type != Molecule.toolTipType.SINGLE) return;
                var target = mol.toolTipInstance.GetComponent<myToolTipConnector>().Target;
                Destroy(mol.toolTipInstance);
                ushort id = target.GetComponent<Bond>().atomID1;
                Atom a = mol.atomList[id];
                a.markConnections(true);
            }
        }
    }

    public void regenerateAtomTooltips()
    {
        foreach(Molecule mol in List_curMolecules)
        {
            foreach(Atom a in mol.atomList)
            {
                if (a.toolTipInstance)
                {
                    Destroy(a.toolTipInstance);
                    a.createToolTip();
                }
            }
        }
    }

    public void regenerateChangeBondWindows()
    {
        foreach(var cb in FindObjectsOfType<ChangeBond>())
        {
            cb.reloadTextFieldsBT();
        }
    }

    private Atom findAtomById(Molecule m, ushort id)
    {
        foreach (Atom a in m.atomList)
        {
            if(a.m_id == id)
            {
                return a;
            }
        }
        return null;
    }

    /// <summary>
    /// Starts the process of exiting the main scene and returning to the login screen.
    /// Prompts the user to confirm their wish to exit.
    /// </summary>
    public void backToMain()
    {
        MainActionMenu.Singleton.gameObject.SetActive(false);
        var myDialog = Dialog.Open(exitConfirmPrefab, DialogButtonType.Yes | DialogButtonType.No, "Confirm Exit", $"Are you sure you want quit?", true);
        //make sure the dialog is rotated to the camera
        myDialog.transform.forward = -GlobalCtrl.Singleton.mainCamera.transform.forward;
        myDialog.transform.position = GlobalCtrl.Singleton.mainCamera.transform.position + 0.01f * myDialog.transform.forward;

        if (myDialog != null)
        {
            myDialog.OnClosed += OnBackToMainDialogEvent;
        }
    }

    private void OnBackToMainDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.Yes)
        {
            if (!LoginData.normal_mode)
            {
                NetworkManagerClient.Singleton.controlledExit = true;
            }
            SceneManager.LoadScene("LoginScreenScene");
        } 
        else
        {
            MainActionMenu.Singleton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// when the application quits and there are unsaved changes to any molecule, these will be saved to an XML file
    /// </summary>
    private void OnApplicationQuit()
    {
        //if (isAnyAtomChanged)
        //    CFileHelper.SaveData(Application.streamingAssetsPath + "/MoleculeFolder/ElementData.xml", list_ElementData);
    }

    /// <summary>
    /// Gets the appropriate version of given text for the current loacle.
    /// </summary>
    /// <param name="text"></param>
    /// <returns>a localized version of the given text</returns>
    public string GetLocalizedString(string text)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString("My Strings", text);
    }

}