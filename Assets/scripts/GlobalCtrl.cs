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
    /// <summary>
    /// list with all currently existing atoms
    /// </summary>
    public List<Atom> List_curAtoms { get; private set; }
    //public Dictionary<int, Atom> Dic_curAtoms{get;private set;}

    /// <summary>
    /// all data of element
    /// </summary>
    public Dictionary<string, ElementData> Dic_ElementData { get; private set; }

    public List<Molecule> List_curMolecules { get; private set; }
    //public Dictionary<int, Molecule> Dic_curMolecules { get; private set; }

    public Dictionary<int, Material> Dic_AtomMat { get; private set; }
    /// <summary>
    /// the list to save/load element data via XML.
    /// </summary>
    private List<ElementData> list_ElementData;
    /// <summary>
    /// scaling factor for visible models
    /// </summary>
    public float scale = 0.5f;
    /// <summary>
    /// 1m in unity = 1000pm in atomic world
    /// </summary>
    public float u2pm = 1000f;
    /// <summary>
    /// 1m in unity = 10 Aangstroem in atomic world
    /// </summary>
    public float u2aa = 10f;
    /// <summary>
    /// scale covalent radius to atom diameter
    /// </summary>
    public float atomScale = 0.4f;
    /// <summary>
    /// the folder used to restore molecular data
    /// </summary>
    public string dataFolder = "MoleculeFolder";

    /// <summary>
    /// the space for creating molecules
    /// </summary>
    public GameObject atomWorld;

    public Bond bondPrefab;
    private bool isAnyAtomChanged;
    public Material selectedMat;
    public Material markedMat;
    public Material bondMat;

    private bool bondsInForeground = false;

    [HideInInspector] public bool collision = false;
    [HideInInspector] public Atom collider1;
    [HideInInspector] public Atom collider2;

    public float repulsionScale = 0.1f;

    public ushort curHybrid = 3;

    Dictionary<Atom, List<Atom>> groupedAtoms = new Dictionary<Atom, List<Atom>>();
    public List<string> favorites = new List<string>(new string[5]);
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

    public int numAtoms = 0;

    public Stack<List<cmlData>> systemState = new Stack<List<cmlData>>();

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

        List_curAtoms = new List<Atom>();
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

        favorites.Add("C");
        favorites.Add("N");
        favorites.Add("O");
        favorites.Add("Cl");
        favorites.Add("F");
        //favoritesGO.Add(fav1);
        //favoritesGO.Add(fav2);
        //favoritesGO.Add(fav3);
        //favoritesGO.Add(fav4);
        //favoritesGO.Add(fav5);

        // Init some prefabs
        Atom.myAtomToolTipPrefab = (GameObject)Resources.Load("prefabs/MRTKAtomToolTip");
        Atom.deleteMeButtonPrefab = (GameObject)Resources.Load("prefabs/DeleteMeButton");
        Atom.closeMeButtonPrefab = (GameObject)Resources.Load("prefabs/CloseMeButton");


        Debug.Log("[GlobalCtrl] Initialization complete.");

    }

    // Update is called once per frame
    void Update()
    {
        if (numAtoms != List_curAtoms.Count)
        {
            SaveMolecule(true);
            numAtoms = List_curAtoms.Count;
        }
    }


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
        markToDeleteCore(true);
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
                    if (Atom.Instance.getAtomByID(b.atomID1).m_data.m_abbre != "Dummy" && Atom.Instance.getAtomByID(b.atomID2).m_data.m_abbre != "Dummy")
                    {
                        delBondList.Add(b);

                        //Atom1
                        positionsRestore.TryGetValue(Atom.Instance.getAtomByID(b.atomID1), out List<Vector3> temp1);
                        positionsRestore.Remove(Atom.Instance.getAtomByID(b.atomID1));
                        temp1.Add(Atom.Instance.getAtomByID(b.atomID2).transform.localPosition);
                        positionsRestore.Add(Atom.Instance.getAtomByID(b.atomID1), temp1);

                        //Atom2
                        positionsRestore.TryGetValue(Atom.Instance.getAtomByID(b.atomID2), out List<Vector3> temp2);
                        positionsRestore.Remove(Atom.Instance.getAtomByID(b.atomID2));
                        temp2.Add(Atom.Instance.getAtomByID(b.atomID1).transform.localPosition);
                        positionsRestore.Add(Atom.Instance.getAtomByID(b.atomID2), temp2);
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
                    CreateDummy(getFreshAtomID(), m, a, calcDummyPos(a, positionsRestore, count));
                    count++;

                }
            }
        }
        shrinkAtomIDs();
    }

    public void deleteBondUI(Bond to_delete)
    {
        var bond_id = to_delete.m_molecule.bondList.IndexOf(to_delete);
        if (bond_id == -1)
        {
            Debug.LogError("[GlobalCtrl:deleteBondUI] Did not fond bond ID in molecule's bond list.");
            return;
        }
        deleteBond(to_delete);
        EventManager.Singleton.DeleteBond((ushort)bond_id, to_delete.m_molecule.m_id);
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

        if (Atom.Instance.getAtomByID(b.atomID1).m_data.m_abbre != "Dummy" && Atom.Instance.getAtomByID(b.atomID2).m_data.m_abbre != "Dummy")
        {
            delBondList.Add(b);

            //Atom1
            positionsRestore.TryGetValue(Atom.Instance.getAtomByID(b.atomID1), out List<Vector3> temp1);
            positionsRestore.Remove(Atom.Instance.getAtomByID(b.atomID1));
            temp1.Add(Atom.Instance.getAtomByID(b.atomID2).transform.localPosition);
            positionsRestore.Add(Atom.Instance.getAtomByID(b.atomID1), temp1);

            //Atom2
            positionsRestore.TryGetValue(Atom.Instance.getAtomByID(b.atomID2), out List<Vector3> temp2);
            positionsRestore.Remove(Atom.Instance.getAtomByID(b.atomID2));
            temp2.Add(Atom.Instance.getAtomByID(b.atomID1).transform.localPosition);
            positionsRestore.Add(Atom.Instance.getAtomByID(b.atomID2), temp2);
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
                    CreateDummy(getFreshAtomID(), m, a, calcDummyPos(a, positionsRestore, count));
                    count++;

                }
            }
        }
        shrinkAtomIDs();
    }

    public void deleteMoleculeUI(Molecule to_delete)
    {
        deleteMolecule(to_delete);
        EventManager.Singleton.DeleteMolecule(to_delete.m_id);
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
        Destroy(m.gameObject);
        shrinkMoleculeIDs();
        shrinkAtomIDs();
    }


    public void deleteAtomUI(Atom to_delete)
    {
        deleteAtom(to_delete);
        EventManager.Singleton.DeleteAtom(to_delete.m_id);
    }

    public void deleteAtom(ushort id)
    {
        var atom = List_curAtoms.ElementAtOrDefault(id);
        if (atom != default)
        {
            deleteAtom(atom);
        }
        else
        {
            Debug.LogError($"[GlobalCtrl:deleteAtom] Atom with ID {id} does not exist. Cannot delete.");
        }
    }

    // TODO make this work
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
                while (a.m_data.m_bondNum > a.connectedAtoms().Count)
                {
                    CreateDummy(getFreshAtomID(), m, a, calcDummyPos(a, positionsRestore, count));
                    count++;
                }
            }
        }
        shrinkAtomIDs();
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
            if (!delAtomList.Contains(a) && search && !(a.m_data.m_abbre == "Dummy" && delAtomList.Contains(Atom.Instance.dummyFindMain(a))))
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
            Bond b = Bond.Instance.getBond(a, ca);
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
    /// <param name="m">molecule which contains the atoms</param>
    /// <param name="delAtomList">atoms which should be deleted</param>
    public void finalDelete(Molecule m, List<Atom> delAtomList)
    {
        int i = 0;
        List<Atom> deleteList = new List<Atom>();
        foreach (Atom a in delAtomList)
        {
            List<Atom> conDummys = a.connectedDummys(a);
            i++;
            foreach (Atom d in conDummys)
            {
                if (!delAtomList.Contains(d))
                {
                    deleteList.Add(d);
                }
            }
        }

        // delete Atoms from List
        foreach (Atom a in delAtomList)
        {
            List_curAtoms.Remove(a);
        }
        shrinkAtomIDs();
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
                Atom at1 = Atom.Instance.getAtomByID(b.atomID1);
                Atom at2 = Atom.Instance.getAtomByID(b.atomID2);

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

        finalDelete(m, delAtomList);
    }

    public Vector3 calcDummyPos(Atom a, Dictionary<Atom, List<Vector3>> positionsRestore, int count)
    {
        positionsRestore.TryGetValue(a, out List<Vector3> values);
        Vector3 newPos = values[count];
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

    public void moveAtom(ushort id, Vector3 pos)
    {
        var atom = List_curAtoms.ElementAtOrDefault(id);
        if (atom != null)
        {
            atom.transform.localPosition = pos;
        }
        else
        {
            Debug.LogError($"[GlobalCtrl] Trying to move Atom {id}, but it does not exist.");
        }
    }

    public void moveMolecule(ushort id, Vector3 pos, Quaternion quat)
    {
        var molecule = List_curMolecules.ElementAtOrDefault(id);
        if (molecule != null)
        {
            molecule.transform.localPosition = pos;
            molecule.transform.localRotation = quat;
        }
        else
        {
            Debug.LogError($"[GlobalCtrl] Trying to move Molecule {id}, but it does not exist.");
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
    public void CreateAtom(ushort moleculeID, string ChemicalAbbre, Vector3 pos, bool createLocal = false)
    {
        // create atom from atom prefab
        GameObject tempMoleculeGO = Instantiate(myBoundingBoxPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        Molecule tempMolecule = tempMoleculeGO.AddComponent<Molecule>();

        //Molecule tempMolecule = new GameObject().AddComponent<Molecule>();
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
        tempData.m_hybridization = curHybrid;
        tempData.m_bondNum = (ushort)Mathf.Max(0, tempData.m_bondNum - (3 - tempData.m_hybridization)); // a preliminary solution

        Atom tempAtom = Instantiate(myAtomPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Atom>();
        tempAtom.f_Init(tempData, tempMolecule, Vector3.zero , getFreshAtomID());
        List_curAtoms.Add(tempAtom);
        // add dummies
        foreach (Vector3 posForDummy in tempAtom.m_posForDummies)
        {
            CreateDummy(getFreshAtomID(), tempMolecule, tempAtom, posForDummy);
        }

        List_curMolecules.Add(tempMolecule);

    }

    /// <summary>
    /// this method changes the type of an atom
    /// </summary>
    /// <param name="idAtom">ID of the selected atom</param>
    /// <param name="ChemicalAbbre">chemical abbrevation of the new atom type</param>
    public void ChangeAtom(int idAtom, string ChemicalAbbre)
    {
        // TODO: do not overwrite runtime data
        Atom chgAtom = Atom.Instance.getAtomByID(idAtom);

        ElementData tempData = Dic_ElementData[ChemicalAbbre];
        tempData.m_hybridization = chgAtom.m_data.m_hybridization;
        tempData.m_bondNum = (ushort)Mathf.Max(0, tempData.m_bondNum - (3 - tempData.m_hybridization));

        chgAtom.f_Modify(tempData);
    }

    public void modifyHybrid(Atom atom, ushort hybrid)
    {
        SaveMolecule(true);
        print("hybrid:   " + hybrid);
        ElementData tempData = Dic_ElementData[atom.m_data.m_abbre];
        tempData.m_hybridization = hybrid;
        tempData.m_bondNum = (ushort)Mathf.Max(0, tempData.m_bondNum - (3 - tempData.m_hybridization));

        atom.f_Modify(tempData);
        atom.markAtom(true);
    }


    /// <summary>
    /// all dummys are replaced by hydrogens
    /// </summary>
    public void replaceDummies()
    {
        // cancel any collision
        collision = false;
        foreach (Molecule curMole in List_curMolecules)
        {
            foreach (Atom a in curMole.atomList)
            {
                if (a.m_data.m_abbre=="Dummy")
                {
                    ChangeAtom(a.m_id, "H");
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
        List_curAtoms.Add(tempAtom);
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
        List_curAtoms.Add(dummy);
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

        Molecule moleInhand = dummyInHand.m_molecule;
        Molecule moleInAir = dummyInAir.m_molecule;
        Bond bondInHand = moleInhand.bondList.Find(p=>p.atomID1==dummyInHand.m_id || p.atomID2 == dummyInHand.m_id);
        Bond bondInAir = moleInAir.bondList.Find(p => p.atomID1 == dummyInAir.m_id || p.atomID2 == dummyInAir.m_id);
        if(moleInhand!=moleInAir)
        {
            moleInhand.givingOrphans(moleInAir, moleInhand);
        }

        Atom atom1 = List_curAtoms.Find((x) => x == bondInHand.findTheOther(dummyInHand));
        Atom atom2 = List_curAtoms.Find((x) => x == bondInAir.findTheOther(dummyInAir));

        //remove dummy and dummy bond of molecule in air
        moleInAir.atomList.Remove(dummyInAir);
        List_curAtoms.Remove(dummyInAir);
        Destroy(dummyInAir.gameObject);
        moleInAir.bondList.Remove(bondInAir);
        Destroy(bondInAir.gameObject);
        moleInAir.atomList.Remove(dummyInHand);
        List_curAtoms.Remove(dummyInHand);
        Destroy(dummyInHand.gameObject);
        moleInAir.bondList.Remove(bondInHand);
        Destroy(bondInHand.gameObject);

        CreateBond(atom1, atom2, moleInAir);

        shrinkAtomIDs();
        shrinkMoleculeIDs();

    }

    // overload to habdle IDs
    public void MergeMolecule(ushort dummyInHand, ushort dummyInAir)
    {
        MergeMolecule(GlobalCtrl.Singleton.List_curAtoms[dummyInHand], GlobalCtrl.Singleton.List_curAtoms[dummyInAir]);
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
    /// this method converts the selected input molecule to the XYZ file format
    /// </summary>
    public void ExportMolecule()
    {
        List<Vector3> coords = new List<Vector3>();
        List<string> names = new List<string>();

        // collect data and determine center of mass
        Vector3 COM = new Vector3(0.0f, 0.0f, 0.0f);
        int nAtoms = 0;
        float tMass = 0.0f;
        foreach (Molecule inputMole in List_curMolecules)
        {
            foreach (Atom at in inputMole.atomList)
            {
                nAtoms++;
                Vector3 pos = at.transform.position;
                string name = at.m_data.m_abbre;
                if (name == "Dummy") name = "X";  // rename dummies
                coords.Add(pos);
                names.Add(name);
                float mass = at.m_data.m_mass;
                tMass += mass;
                COM += mass * pos;
            }
        }
        COM /= tMass;
        Debug.Log(string.Format("COM: {0,12:f6} {1,12:f6} {2,12:f6}",COM.x,COM.y,COM.z));

        // shift coordinates such that COM is at origin (and convert to Aangstroem units)
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            coords[iAtom] = (coords[iAtom] - COM) * this.u2aa / this.scale ;
        }

        // Write to file
        //StreamWriter XYZexport;
        //XYZexport = File.CreateText(UISave.inputfield+".xyz");
        //XYZexport.WriteLine(nAtoms);
        //XYZexport.WriteLine("exported from VRmck");
        //for (int iAtom = 0; iAtom<nAtoms; iAtom++)
        //{
        //    XYZexport.WriteLine(string.Format("{0,2} {1,12:f6} {2,12:f6} {3,12:f6}",names[iAtom],coords[iAtom].x,coords[iAtom].y,coords[iAtom].z));
        //}
        //XYZexport.Close();
        //ForceFieldConsole.Instance.statusOut("Exported molecule as : " + UISave.inputfield + ".xyz");
    }

    /// <summary>
    /// this method loads a saved molecule into the workspace
    /// </summary>
    /// <param name="name">name of the saved molecule</param>
    public void LoadMolecule(string name, int param = 0)
    {
        List<cmlData> loadData;

        Vector3 meanPos = new Vector3(0.0f, 0.0f, 0.0f);

        if(param == 0)
        {
            loadData = (List<cmlData>)CFileHelper.LoadData(Application.streamingAssetsPath + "/SavedMolecules/" + name + ".xml", typeof(List<cmlData>));
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
        }
        else
        {
            loadData = null;
            for(int i = 0; i < 2; i++)
            {
                if (systemState.Count > 0)
                {
                    loadData = systemState.Pop();
                }
                else
                    loadData = null;
            }

            meanPos = new Vector3(0.0f, 0.0f, 0.0f);
        }

        if(loadData != null)
        {
            foreach (cmlData molecule in loadData)
            {
                var freshAtomID = getFreshAtomID();
                var freshMoleculeID = getFreshMoleculeID();
                for (int i = 0; i < molecule.atomArray.Length; i++)
                {
                    molecule.atomArray[i].id += freshAtomID;
                }

                for (int i = 0; i < molecule.bondArray.Length; i++)
                {
                    molecule.bondArray[i].id1 += freshAtomID;
                    molecule.bondArray[i].id2 += freshAtomID;
                }

                Molecule tempMolecule = Instantiate(myBoundingBoxPrefab, molecule.molePos, Quaternion.identity).AddComponent<Molecule>();
                tempMolecule.f_Init(freshMoleculeID, atomWorld.transform);
                List_curMolecules.Add(tempMolecule);


                //LOAD STRUCTURE CHECK LIST / DICTIONNARY

                for (int i = 0; i < molecule.atomArray.Length; i++)
                {
                    Atom a = RebuildAtom(molecule.atomArray[i].id, molecule.atomArray[i].abbre, molecule.atomArray[i].hybrid, molecule.atomArray[i].pos, tempMolecule);
                }
                for (int i = 0; i < molecule.bondArray.Length; i++)
                {
                    CreateBond(Atom.Instance.getAtomByID(molecule.bondArray[i].id1), Atom.Instance.getAtomByID(molecule.bondArray[i].id2), tempMolecule);
                }
                moveMolecule(freshMoleculeID, molecule.molePos + meanPos, molecule.moleQuat);
                EventManager.Singleton.MoveMolecule(freshMoleculeID, molecule.molePos + meanPos, molecule.moleQuat);
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
        shrinkAtomIDs();
        shrinkMoleculeIDs();
        // this method preserves the position of the molecules and atoms (and rotation)
        List<cmlData> saveData = new List<cmlData>();
        foreach (Molecule inputMole in List_curMolecules)
        {
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
            cmlData tempData = new cmlData(inputMole.transform.localPosition, inputMole.transform.localRotation, inputMole.m_id, list_atom, list_bond);
            saveData.Add(tempData);
        }

        return saveData;
    }


    public void rebuildAtomWorld(List<cmlData> data, bool add = false)
    {
        // this method preserves the ids of all objects
        if (data != null)
        {
            SaveMolecule(true);
            ushort freshMoleculeID = 0;
            foreach (cmlData molecule in data)
            {
                if (add)
                {
                    var freshAtomID = getFreshAtomID();
                    freshMoleculeID = getFreshMoleculeID();
                    for (int i = 0; i < molecule.atomArray.Length; i++)
                    {
                        molecule.atomArray[i].id += freshAtomID;
                    }

                    for (int i = 0; i < molecule.bondArray.Length; i++)
                    {
                        molecule.bondArray[i].id1 += freshAtomID;
                        molecule.bondArray[i].id2 += freshAtomID;
                    }
                }

                Molecule tempMolecule = Instantiate(myBoundingBoxPrefab).AddComponent<Molecule>();
                tempMolecule.f_Init(add == true ? freshMoleculeID : molecule.moleID, atomWorld.transform);
                tempMolecule.transform.localPosition = molecule.molePos;
                tempMolecule.transform.localRotation = molecule.moleQuat;
                List_curMolecules.Add(tempMolecule);

                for (int i = 0; i < molecule.atomArray.Length; i++)
                {
                    Atom a = RebuildAtom(molecule.atomArray[i].id, molecule.atomArray[i].abbre, molecule.atomArray[i].hybrid, molecule.atomArray[i].pos, tempMolecule);
                }
                for (int i = 0; i < molecule.bondArray.Length; i++)
                {
                    CreateBond(Atom.Instance.getAtomByID(molecule.bondArray[i].id1), Atom.Instance.getAtomByID(molecule.bondArray[i].id2), tempMolecule);
                }
            }
        }
    }


    public void LoadPreset(int number)
    {
        DeleteAll();
        switch(number)
        {
            case 1:
                LoadMolecule("Aminole", 0);
                break;
            case 2:
                LoadMolecule("AcrylicAcid", 0);
                break;
            case 3:
                LoadMolecule("Adamantane", 0);
                break;
            case 4:
                LoadMolecule("Indole", 0);
                break;
            case 5:
                LoadMolecule("MirrorTask", 0);
                break;
            case 6:
                LoadMolecule("PlanarChirality", 0);
                break;
        }
    }

    #endregion

    #region id management
    /// <summary>
    /// this method gets the maximum atomID currently in the scene
    /// </summary>
    /// <returns>id</returns>
    public ushort getMaxAtomID()
    {
        ushort id = 0;
        if (List_curAtoms.Count > 0)
        {
            foreach (Atom a in List_curAtoms)
            {
                id = Math.Max(id, a.m_id);
            }
        }
        return id;
    }

    /// <summary>
    /// this method shrinks the IDs of the atoms to prevent an overflow
    /// </summary>
    public void shrinkAtomIDs()
    {
        var from = new List<ushort>();
        var to = new List<ushort>();
        var bondList = new List<Bond>();
        for (ushort i = 0; i < List_curAtoms.Count; i++)
        {
            // also change ids in bond
            if (List_curAtoms[i].m_id != i)
            {
                from.Add(List_curAtoms[i].m_id);
                to.Add(i);
                foreach (var bond in List_curAtoms[i].connectedBonds())
                {
                    if (!bondList.Contains(bond))
                    {
                        bondList.Add(bond);
                    }
                }

            }
            List_curAtoms[i].m_id = i;
        }
        foreach (var bond in bondList)
        {
            if (from.Contains(bond.atomID1))
            {
                bond.atomID1 = to[from.FindIndex(a => a == bond.atomID1)];
            }
            if (from.Contains(bond.atomID2))
            {
                bond.atomID2 = to[from.FindIndex(a => a == bond.atomID2)];
            }
        }
    }

    /// <summary>
    /// gets a fresh available atom id
    /// </summary>
    /// <param name="idNew">new ID</param>
    public ushort getFreshAtomID()
    {
        if (List_curAtoms.Count == 0)
        {
            return 0;
        }
        else
        {
            shrinkAtomIDs();
            return (ushort)(getMaxAtomID() + 1);
        }
    }

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
        CreateAtom(newID, ChemicalID, create_position);

        // Let the networkManager know about the user action
        // Important: insert localPosition here
        EventManager.Singleton.CreateAtom(newID, ChemicalID, List_curMolecules[newID].transform.localPosition);
    }

    public void getRepulsionScale(float value)
    {
        // set value from slider
        repulsionScale = 0.1f + value*0.9f;
        Debug.Log(string.Format("repulsionScale, new val: {0, 6:f3}", repulsionScale));
    }

    public void setHybridization(float value)
    {
        // set value from slider
        curHybrid = (ushort)value;
        Debug.Log(string.Format("curHybrid, new val: {0, 6}", curHybrid));
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

    public void setFavorite(int pos, string abbre, List<GameObject> favMenu)
    {
        favorites[pos - 1] = abbre;
        //Transform temp = null;
        //for (int i = 0; i < periodictable.transform.childCount; i++)
        //{
        //    if (periodictable.transform.GetChild(i).transform.Find("Btn_" + GetElementbyAbbre(abbre).m_name) != null)
        //        temp = periodictable.transform.GetChild(i).transform.Find("Btn_" + GetElementbyAbbre(abbre).m_name);
        //}

        //favMenu[pos - 1].transform.GetComponent<Image>().sprite = temp.GetComponent<Image>().sprite;
        //favoritesGO[pos - 1].transform.GetComponent<Image>().sprite = temp.GetComponent<Image>().sprite;
    }

    public void createFavoriteElement(int pos)
    {
        lastAtom = favorites[pos - 1]; // remember this for later
        Vector3 current_pos = Camera.main.transform.position;
        Vector3 current_lookat = Camera.main.transform.forward;
        Vector3 create_position = current_pos + 0.5f * current_lookat;
        CreateAtom(getFreshMoleculeID(), favorites[pos - 1], create_position);
    }


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
            foreach (Atom a in List_curAtoms)
            {
                if (a.isMarked)
                    return a;
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

    #endregion

    public void backToMain()
    {
        var myDialog = Dialog.Open(exitConfirmPrefab, DialogButtonType.Yes | DialogButtonType.No, "Confirm Exit", $"Are you sure you want quit?", true);
        if (myDialog != null)
        {
            myDialog.OnClosed += OnClosedDialogEvent;
        }
    }

    private void OnClosedDialogEvent(DialogResult obj)
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