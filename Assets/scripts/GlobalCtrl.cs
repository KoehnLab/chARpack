using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using StructClass;
using System.IO;
using System;
using System.Globalization;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using System.Reflection;
using Microsoft.MixedReality.Toolkit.Input;
using System.Linq;

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
    private bool isAnyAtomChanged;
    public Material selectedMat;
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

    [HideInInspector] public int numAtoms = 0;

    public Stack<List<cmlData>> systemState = new Stack<List<cmlData>>();

    #region Interaction
    // Interaction modes
    public enum InteractionModes {NORMAL, CHAIN};
    private InteractionModes _currentInteractionMode = InteractionModes.NORMAL;
    public InteractionModes currentInteractionMode { get => _currentInteractionMode; private set => _currentInteractionMode = value; }

    public void toggleInteractionMode()
    {
        if (currentInteractionMode == InteractionModes.NORMAL)
        {
            currentInteractionMode = InteractionModes.CHAIN;
            HandTracking.Singleton.enabled = true;
        }
        else
        {
            currentInteractionMode = InteractionModes.NORMAL;
            HandTracking.Singleton.enabled = false;
        }
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

        // check if file is found otherwise throw error
        string element_file_path = Path.Combine(Application.streamingAssetsPath, "ElementData.xml");
        if (!System.IO.File.Exists(element_file_path))
        {
            Debug.LogError("[GlobalCtrl] ElementData.xml not found.");
        }

        list_ElementData = (List<ElementData>)CFileHelper.LoadData(element_file_path, typeof(List<ElementData>));
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
        Atom.deleteMeButtonPrefab = (GameObject)Resources.Load("prefabs/DeleteMeButton");
        Atom.closeMeButtonPrefab = (GameObject)Resources.Load("prefabs/CloseMeButton");
        Atom.modifyMeButtonPrefab = (GameObject)Resources.Load("prefabs/ModifyMeButton");
        Atom.modifyHybridizationPrefab = (GameObject)Resources.Load("prefabs/modifyHybridization");

        // Molecule
        Molecule.myToolTipPrefab = (GameObject)Resources.Load("prefabs/MRTKAtomToolTip");
        Molecule.deleteMeButtonPrefab = (GameObject)Resources.Load("prefabs/DeleteMeButton");
        Molecule.closeMeButtonPrefab = (GameObject)Resources.Load("prefabs/CloseMeButton");
        Molecule.modifyMeButtonPrefab = (GameObject)Resources.Load("prefabs/ModifyMeButton");
        Molecule.changeBondWindowPrefab = (GameObject)Resources.Load("prefabs/ChangeBondWindow");

        Debug.Log("[GlobalCtrl] Initialization complete.");

    }

    [HideInInspector] public Camera mainCamera;
    [HideInInspector] public Camera currentCamera;
    private void Start()
    {
        mainCamera = Camera.main;
        // for use in mouse events
        currentCamera = mainCamera;
    }


    // on mol data changed (replacement for update loop checks)
    //void onMolDataChanged()
    //{
    //    SaveMolecule(true);
    //}


    #region atom_helper

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
                while (a.m_data.m_bondNum > a.connectedAtoms().Count)
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
            deleteBond(to_delete);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GlobalCrtl:deleteBondUI] Exception: {e.Message}");
        }
        finally
        {
            EventManager.Singleton.DeleteBond((ushort)bond_id, mol_id);
        }
    }

    public void deleteBond(Bond b)
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

        foreach (Molecule m in List_curMolecules) // only check addMoleculeList?
        {
            int size = m.atomList.Count;
            for (int i = 0; i < size; i++)
            {
                Atom a = m.atomList[i];
                int count = 0;
                Debug.Log($"[GlobalCrtl:deleteBond] a.connected {a.connectedAtoms().Count} a.numbond {a.m_data.m_bondNum}");
                while (a.m_data.m_bondNum > a.connectedAtoms().Count)
                {
                    CreateDummy(m.getFreshAtomID(), m, a, calcDummyPos(a, positionsRestore, count));
                    count++;
                }
            }
            m.shrinkAtomIDs();
        }
        SaveMolecule(true);
        // invoke data change event for new molecules
        foreach (Molecule m in addMoleculeList)
        {
            EventManager.Singleton.ChangeMolData(m);
        }
    }

    public void toggleKeepConfigUI(Molecule to_switch)
    {
        setKeepConfig(to_switch, !to_switch.keepConfig);
        EventManager.Singleton.SetKeepConfig(to_switch.m_id, to_switch.keepConfig);
    }

    public void setKeepConfig(Molecule to_switch, bool keep_config)
    {
        if (to_switch.keepConfig != keep_config)
        {
            to_switch.keepConfig = keep_config;
            to_switch.generateFF();
        }
    }

    public bool setKeepConfig(ushort mol_id, bool keep_config)
    {
        var to_switch = List_curMolecules.ElementAtOrDefault(mol_id);
        if (to_switch == default)
        {
            return false;
        }
        if (to_switch.keepConfig != keep_config)
        {
            to_switch.keepConfig = keep_config;
            to_switch.generateFF();
        }
        return true;
    }


    public void deleteMoleculeUI(Molecule to_delete)
    {
        var mol_id = to_delete.m_id;
        try
        {
            deleteMolecule(to_delete);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GlobalCrtl:deleteMoleculeUI] Exception: {e.Message}");
        }
        finally
        {
            EventManager.Singleton.DeleteMolecule(mol_id);
        }
    }

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
        m.markMolecule(false);
        List_curMolecules.Remove(m);
        Destroy(m.gameObject);
        shrinkMoleculeIDs();
        SaveMolecule(true);
        // no need to invoke change event
    }


    public void deleteAtomUI(Atom to_delete)
    {
        var mol_id = to_delete.m_molecule.m_id;
        var id = to_delete.m_id;
        try
        {
            deleteAtom(to_delete);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GlobalCrtl:deleteAtomUI] Exception: {e.Message}");
        }
        finally
        {
            EventManager.Singleton.DeleteAtom(mol_id, id);
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

    public void deleteAtom(Atom to_delete)
    {
        Dictionary<Atom, List<Vector3>> positionsRestore = new Dictionary<Atom, List<Vector3>>();
        List<Atom> delAtomList = new List<Atom>();
        List<Bond> delBondList = new List<Bond>();
        List<Molecule> delMoleculeList = new List<Molecule>();
        List<Molecule> addMoleculeList = new List<Molecule>();


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

        foreach (Molecule m in List_curMolecules)
        {
            int size = m.atomList.Count;
            for (int i = 0; i < size; i++)
            {
                Atom a = m.atomList[i];
                int count = 0;
                Debug.Log($"[GlobalCtrl:deleteAtom] should bonds: {a.m_data.m_bondNum}, actual bonds: {a.connectedAtoms().Count}");
                while (a.m_data.m_bondNum > a.connectedAtoms().Count)
                {
                    CreateDummy(m.getFreshAtomID(), m, a, calcDummyPos(a, positionsRestore, count));
                    count++;
                }
            }
            m.shrinkAtomIDs();
        }
        SaveMolecule(true);
        // invoke data change event for new molecules
        foreach (Molecule m in addMoleculeList)
        {
            EventManager.Singleton.ChangeMolData(m);
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
    // This method toggles everything in a background layer and 
    // toggles the bonds in the front layer 
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

    public bool moveAtom(ushort mol_id, ushort atom_id, Vector3 pos)
    {
        var atom = List_curMolecules.ElementAtOrDefault(mol_id).atomList.ElementAtOrDefault(atom_id);
        if (atom != null)
        {
            atom.transform.localPosition = pos;
            return true;
        }
        else
        {
            Debug.LogError($"[GlobalCtrl] Trying to move Atom {atom_id} of molecule {mol_id}, but it does not exist.");
            return false;
        }
    }

    public bool moveMolecule(ushort id, Vector3 pos, Quaternion quat)
    {
        var molecule = List_curMolecules.ElementAtOrDefault(id);
        if (molecule != null)
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

        SaveMolecule(true);

        EventManager.Singleton.ChangeMolData(tempMolecule);
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
        Atom chgAtom = List_curMolecules.ElementAtOrDefault(idMol).atomList.ElementAtOrDefault(idAtom);
        if (chgAtom == default)
        {
            return false;
        }

        ElementData tempData = Dic_ElementData[ChemicalAbbre];
        tempData.m_hybridization = chgAtom.m_data.m_hybridization;
        tempData.m_bondNum = calcNumBonds(tempData.m_hybridization, tempData.m_bondNum);

        chgAtom.f_Modify(tempData);

        SaveMolecule(true);
        EventManager.Singleton.ChangeMolData(List_curMolecules.ElementAtOrDefault(idMol));
        return true;
    }

    public void changeAtomUI(ushort idMol, ushort idAtom, string ChemicalAbbre)
    {
        EventManager.Singleton.ChangeAtom(idMol, idAtom, ChemicalAbbre);
        changeAtom(idMol, idAtom, ChemicalAbbre);
    }

    public void modifyHybridUI(Atom atom, ushort hybrid)
    {
        EventManager.Singleton.ModifyHyb(atom.m_molecule.m_id, atom.m_id, hybrid);
        modifyHybrid(atom, hybrid);
    }

    public void modifyHybrid(Atom atom, ushort hybrid)
    {
        ElementData tempData = Dic_ElementData[atom.m_data.m_abbre];
        tempData.m_hybridization = hybrid;
        tempData.m_bondNum = calcNumBonds(tempData.m_hybridization, tempData.m_bondNum);

        atom.f_Modify(tempData);
        SaveMolecule(true);
        EventManager.Singleton.ChangeMolData(atom.m_molecule);
    }

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
    /// all dummys are replaced by hydrogens
    /// </summary>
    public void replaceDummies()
    {
        // cancel any collision
        collision = false;
        foreach (Molecule curMol in List_curMolecules)
        {
            foreach (Atom a in curMol.atomList)
            {
                if (a.m_data.m_abbre=="Dummy")
                {
                    changeAtomUI(curMol.m_id ,a.m_id, "H");
                }
            }
        }
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

        Molecule molInHand = dummyInHand.m_molecule;
        Molecule molInAir = dummyInAir.m_molecule;
        // scale before merge
        molInAir.transform.localScale = molInHand.transform.localScale;
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


        CreateBond(atom1, atom2, molInAir);

        // DEBUG
        //Debug.Log($"[GlobalCtrl:MergeMolecule] Atoms in Molecule {molInAir.atomList.Count}, bonds in Molecule {molInAir.bondList.Count}"); 

        molInAir.shrinkAtomIDs();
        shrinkMoleculeIDs();

        SaveMolecule(true);

        EventManager.Singleton.ChangeMolData(molInAir);

    }

    // overload to handle IDs
    public void MergeMolecule(ushort molInHand, ushort dummyInHand, ushort molInAir, ushort dummyInAir)
    {
        MergeMolecule(List_curMolecules[molInHand].atomList[dummyInHand], List_curMolecules[molInAir].atomList[dummyInAir]);
    }

    public bool changeBondTerm(ushort mol_id, ushort term_id, ForceField.BondTerm new_term)
    {
        if (term_id >= List_curMolecules.ElementAtOrDefault(mol_id).bondTerms.Count)
        {
            return false;
        }
        List_curMolecules[mol_id].changeBondParameters(new_term, term_id);

        return true;
    }

    public bool changeAngleTerm(ushort mol_id, ushort term_id, ForceField.AngleTerm new_term)
    {
        if (term_id >= List_curMolecules.ElementAtOrDefault(mol_id).angleTerms.Count)
        {
            return false;
        }
        List_curMolecules[mol_id].changeAngleParameters(new_term, term_id);

        return true;
    }

    public bool changeTorsionTerm(ushort mol_id, ushort term_id, ForceField.TorsionTerm new_term)
    {
        if (term_id >= List_curMolecules.ElementAtOrDefault(mol_id).torsionTerms.Count)
        {
            return false;
        }
        List_curMolecules[mol_id].changeTorsionParameters(new_term, term_id);

        return true;
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
            CFileHelper.SaveData(Application.streamingAssetsPath + "/SavedMolecules/" + name + ".xml", saveData);
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

        loadData = (List<cmlData>)CFileHelper.LoadData(Application.streamingAssetsPath + "/SavedMolecules/" + name + ".xml", typeof(List<cmlData>));
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
            Vector3 current_pos = Camera.main.transform.position - atomWorld.transform.position; // transform this here onto atom world coordinates
            Vector3 current_lookat = Camera.main.transform.forward;
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

    public List<cmlData> getMoleculeData(string name)
    {
        return (List<cmlData>)CFileHelper.LoadData(Application.streamingAssetsPath + "/SavedMolecules/" + name + ".xml", typeof(List<cmlData>));
    }

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
            if (inputMole.keepConfig)
            {
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
            }
            else
            {
                List<cmlBond> list_bond = new List<cmlBond>();
                foreach (Bond b in inputMole.bondList)
                {
                    list_bond.Add(new cmlBond(b.atomID1, b.atomID2, b.m_bondOrder));
                }
                tempData = new cmlData(inputMole.transform.localPosition, inputMole.transform.localRotation, inputMole.m_id, list_atom, list_bond);
            }

            saveData.Add(tempData);
        }

        return saveData;
    }


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
                EventManager.Singleton.ChangeMolData(tempMolecule);
            }
        }
        SaveMolecule(true);
    }

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

    public void createAtomUI(string ChemicalID)
    {
        lastAtom = ChemicalID; // remember this for later
        Vector3 create_position = Camera.main.transform.position + 0.5f * Camera.main.transform.forward;
        var newID = getFreshMoleculeID();
        CreateAtom(newID, ChemicalID, create_position, curHybrid);

        // Let the networkManager know about the user action
        // Important: insert localPosition here
        EventManager.Singleton.CreateAtom(newID, ChemicalID, List_curMolecules[newID].transform.localPosition, curHybrid);
    }

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
    //    Vector3 current_pos = Camera.main.transform.position;
    //    Vector3 current_lookat = Camera.main.transform.forward;
    //    Vector3 create_position = current_pos + 0.5f * current_lookat;
    //    CreateAtom(getFreshMoleculeID(), favorites[pos - 1], create_position, curHybrid);
    //}


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

    public void toggleDebugWindow()
    {
        if (DebugWindow.Singleton)
        {
            DebugWindow.Singleton.toggleVisible();
        }
    }

    public void openSettingsWindow()
    {
        var settingsPrefab = (GameObject)Resources.Load("prefabs/Settings");
        Instantiate(settingsPrefab);
    }

    public void openAtomMenu()
    {
        var atomMenuPrefab = (GameObject)Resources.Load("prefabs/AtomMenu");
        GameObject atomMenu = Instantiate(atomMenuPrefab) as GameObject;
        atomMenu.transform.parent = GameObject.Find("NearMenu").transform;
        atomMenu.transform.localPosition = new Vector3(-0.45f, 0f, -0.22f);
    }

    public void openAtomMenuScrollable()
    {
        var atomMenuScrollablePrefab = (GameObject)Resources.Load("prefabs/AtomMenuScrollable");
        GameObject AtomMenuScrollable = Instantiate(atomMenuScrollablePrefab) as GameObject;
        AtomMenuScrollable.transform.parent = GameObject.Find("NearMenu").transform;
        AtomMenuScrollable.transform.localPosition = new Vector3(-0.95f, 0f, -0.25f);
    }

        #endregion

        public void backToMain()
    {
        var myDialog = Dialog.Open(exitConfirmPrefab, DialogButtonType.Yes | DialogButtonType.No, "Confirm Exit", $"Are you sure you want quit?", true);
        if (myDialog != null)
        {
            myDialog.OnClosed += OnBackToMainDialogEvent;
        }
    }

    private void OnBackToMainDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.Yes)
        {
            SceneManager.LoadScene("LoginScreenScene");
        }
    }

    /// <summary>
    /// when the application quits and there are unsaved changes to any molecule, these will be saved to an XML file
    /// </summary>
    private void OnApplicationQuit()
    {
        if (isAnyAtomChanged)
            CFileHelper.SaveData(Application.streamingAssetsPath + "/MoleculeFolder/ElementData.xml", list_ElementData);
    }

}