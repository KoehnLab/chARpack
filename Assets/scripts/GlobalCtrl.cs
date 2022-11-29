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

[Serializable]
/// <summary>
/// globalctrl, where everything starts
/// </summary>
public class GlobalCtrl : MonoBehaviour
{
    /// <summary>
    /// instance of global control
    /// </summary>
    private static GlobalCtrl instance;
    public static GlobalCtrl Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GlobalCtrl>();
            }
            return instance;
        }
    }

    // TODO HOLOLENS DEACTIVATED
    //public static ControllerManager CtrlManager { get; private set; }
    //public static UIMainMenu UIMain { get; private set; }
    //public static UISaveMenu UISave { get; private set; }

    public GameObject myBoundingBoxPrefab;
    public GameObject myAtomPrefab;

    public Material atomMatPrefab;
    /// <summary>
    /// list with all currently existing atoms
    /// </summary>
    public List<Atom> List_curAtoms { get; private set; }
    //public Dictionary<int, Atom> Dic_curAtoms{get;private set;}

    public int idInScene=0;
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

    /// <summary>
    /// gameObject of Controllers for direct access
    /// </summary>
    //public GameObject controllerLeft;
    //public GameObject controllerRight;
    //public GameObject pointerLeft;
    //public GameObject pointerRight;
    //public GameObject savedFileButtonPrefab;
    //public GameObject scrollview;

    //public GameObject contextMenu;
    //public GameObject periodictable;
    //public GameObject btnPerTable;

    public Bond bondPrefab;
    private bool isAnyAtomChanged;
    public Material selectedMat;
    public Material markedMat;
    public Material bondMat;

    public bool forceField = true;
    public bool allAtomMode = true;
    public bool collision = false;
    public Atom collider1;
    public Atom collider2;

    public float repulsionScale = 0.1f;

    public int curHybrid = 3;

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

        ///<summary>
        ///Manages Controller, gets called by individual scripts for each controller (Vive, Oculus, ...)
        ///Directly calls the methods which should be executed
        /// </summary>
        // CtrlManager = GetComponent<ControllerManager>();

        // CtrlManager.f_Init();
        // UIMain = FindObjectOfType<UIMainMenu>();
        // UIMain.f_Init();
        // UIMain.gameObject.SetActive(false);

        //UISave = FindObjectOfType<UISaveMenu>();
        //if (UISave) UISave.gameObject.SetActive(false);

        //contextMenu.SetActive(false);
        //if (periodictable) periodictable.SetActive(false);

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



        Debug.Log("[GlobalCtrl] Initialization complete.");

    }

    // Update is called once per frame
    void Update()
    {
        /*
        #region Controls
        //print(CtrlManager.L)
        //Press Touchpad
        if (CtrlManager.GetPrimary2DPress(WhichHand.left))
        {
            SwitchUIMain();
        }
            //            Vector2 posPadTouch = CtrlManager.GetPrimary2DTouch(WhichHand.left);
            //            Debug.Log(string.Format("2DTouch: {0,8:f3} {1,8:f3}", posPadTouch.x, posPadTouch.y));

            //            if (posPadTouch.x <= 0.5f && posPadTouch.x >= -0.5f && posPadTouch.y <= -0.5f)
            //{
            //Press Back: Toggle Main Menu
            //if (!UIMain.gameObject.activeSelf)
            //    UIMain.deactivateKeyboard();
            //}
            //            else if(posPadTouch.x <= 0.5f && posPadTouch.x >= -0.5f && posPadTouch.y >=0.5f){
            //                //Press Front:
            //                CreateAtom(idInScene, lastAtom, controllerLeft.transform.position + new Vector3(0.01f, 0, 0.01f));
            // }
            //            else if (posPadTouch.x <= -0.7f && posPadTouch.y >= -0.3f && posPadTouch.y <= 0.3f)
            //            {
            //                //Press Left:
            //                CreateAtom(idInScene, "H", controllerLeft.transform.position + new Vector3(0.1f, 0, 0));
            //            }
            //            else if (posPadTouch.x >= 0.7f && posPadTouch.y >= -0.3f && posPadTouch.y <= 0.3f)
            //            {
            //                //Press Right:
            //                CreateAtom(idInScene, "O", controllerLeft.transform.position + new Vector3(0.1f, 0, 0));
            //            }

        if (CtrlManager.GetPrimary2DPress(WhichHand.right))
        {
            CreateAtom(idInScene, lastAtom, controllerRight.transform.position + new Vector3(0.01f, 0, 0.01f));
        }
        
        if (CtrlManager.GraspingDown(WhichHand.left))
        {
            //Pickup left
            controllerLeft.GetComponent<InteractionGrab>().Pickup();
        }
        if (CtrlManager.GraspingDown(WhichHand.right))
        {
            //Pickup right
            controllerRight.GetComponent<InteractionGrab>().Pickup();
        }
        if (CtrlManager.Grasping(WhichHand.left))
        {
            //Hold left
            controllerLeft.GetComponent<InteractionGrab>().HoldDown();
        }
        if (CtrlManager.Grasping(WhichHand.right))
        {
            //Hold right
            controllerRight.GetComponent<InteractionGrab>().HoldDown();
        }
        if (CtrlManager.GraspingUp(WhichHand.left))
        {
            //Drop left
            controllerLeft.GetComponent<InteractionGrab>().Drop();
        }
        if (CtrlManager.GraspingUp(WhichHand.right))
        {
            //Drop right
            controllerRight.GetComponent<InteractionGrab>().Drop();
        }



        //TODO:
        //Set Laserpointer as soon as trigger is touched, confirm Laserpointer on Trigger click

        //Trigger Laserpointer left Hand
        if (CtrlManager.triggerPercent(WhichHand.left) > 0.001f)
            controllerLeft.transform.GetChild(1).gameObject.SetActive(true);
        else
            controllerLeft.transform.GetChild(1).gameObject.SetActive(false);

        //Trigger Laserpointer Right Hand
        if (CtrlManager.triggerPercent(WhichHand.right) > 0.001f)
            controllerRight.transform.GetChild(1).gameObject.SetActive(true);
        else
            controllerRight.transform.GetChild(1).gameObject.SetActive(false);


        if (CtrlManager.triggerDown(WhichHand.left))
        {
            GameObject hit = controllerLeft.transform.GetChild(1).GetComponent<PointerRay>().getTarget();
            if(hit != null)
                objSelect(hit);
        }
        if (CtrlManager.triggerDown(WhichHand.right))
        {
            GameObject hit = controllerRight.transform.GetChild(1).GetComponent<PointerRay>().getTarget();
            if (hit != null)
                objSelect(hit);
        }

        #endregion
        */



        if (numAtoms != List_curAtoms.Count)
        {
            SaveMolecule(1);
            numAtoms = List_curAtoms.Count;
        }
            


    }


    /// <summary>
    /// this method selects an object if it is hit with the laserpointer and confirmed with a trigger click
    /// </summary>
    /// <param name="hit">the gameobject which is hit</param>
    public void objSelect(GameObject hit)
    {
        //if (allAtomMode)
        //{
        //    //if it is an atom (Molecule)
        //    if (hit.layer == 6)
        //    {
        //        if (hit.GetComponent<Atom>().isMarked)
        //        {
        //            hit.GetComponentInParent<Molecule>().markMolecule(false);
        //            Molecule intermediate = (Molecule)getNextMarked(0);
        //            if (intermediate == null)
        //                contextMenu.SetActive(false);
        //            else
        //                contextMenu.GetComponent<ContextMenu>().setMoleculeOption(intermediate);
        //        }
        //        else
        //        {
        //            hit.GetComponentInParent<Molecule>().markMolecule(true);
        //            contextMenu.SetActive(true);
        //            Vector3 tempForward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        //            Ray ray = new Ray(Camera.main.transform.position, tempForward);
        //            contextMenu.transform.position = ray.GetPoint(1);
        //            contextMenu.transform.LookAt(ray.GetPoint(7));
        //            //contextMenu.transform.position = UIMain.transform.position + new Vector3(8, 0, -3);
        //            contextMenu.GetComponent<ContextMenu>().setMoleculeOption(hit.GetComponentInParent<Molecule>());
        //        }
        //    }
        //    //if it is a bond
        //    if (hit.layer == 7)
        //    {
        //        if (hit.GetComponentInParent<Bond>().isMarked)
        //        {
        //            hit.GetComponentInParent<Molecule>().markMolecule(false);
        //            Bond intermediate = (Bond)getNextMarked(2);
        //            if (intermediate == null)
        //                contextMenu.SetActive(false);
        //            else
        //                contextMenu.GetComponent<ContextMenu>().setBondOption(intermediate);
        //        }
        //        else
        //        {
        //            hit.GetComponentInParent<Molecule>().markMolecule(true);
        //            contextMenu.SetActive(true);
        //            Vector3 tempForward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        //            Ray ray = new Ray(Camera.main.transform.position, tempForward);
        //            contextMenu.transform.position = ray.GetPoint(1);
        //            contextMenu.transform.LookAt(ray.GetPoint(7));
        //            contextMenu.GetComponent<ContextMenu>().setMoleculeOption(hit.GetComponentInParent<Molecule>());
        //        }
        //    }

        //}
        //else
        //{
        //    //if it is an atom
        //    if (hit.layer == 6)
        //    {
        //        if (hit.GetComponent<Atom>().isMarked)
        //        {
        //            hit.GetComponent<Atom>().markAtom(false);
        //            Atom intermediate = (Atom)getNextMarked(1);
        //            if (intermediate == null)
        //                contextMenu.SetActive(false);
        //            else
        //                contextMenu.GetComponent<ContextMenu>().setAtomOption(intermediate);
        //        }
        //        else
        //        {
        //            hit.GetComponent<Atom>().markAtom(true);
        //            contextMenu.SetActive(true);
        //            Vector3 tempForward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        //            Ray ray = new Ray(Camera.main.transform.position, tempForward);
        //            contextMenu.transform.position = ray.GetPoint(1);
        //            contextMenu.transform.LookAt(ray.GetPoint(7));
        //            contextMenu.GetComponent<ContextMenu>().setAtomOption(hit.GetComponent<Atom>());
        //        }
        //    }
        //    //if if is a bond
        //    if (hit.layer == 7)
        //    {
        //        if (hit.GetComponentInParent<Bond>().isMarked)
        //        {
        //            hit.GetComponentInParent<Bond>().markBond(false);
        //            Bond intermediate = (Bond)getNextMarked(2);
        //            if (intermediate == null)
        //                contextMenu.SetActive(false);
        //            else
        //                contextMenu.GetComponent<ContextMenu>().setBondOption(intermediate);
        //        }
        //        else
        //        {
        //            hit.GetComponentInParent<Bond>().markBond(true);
        //            contextMenu.SetActive(true);
        //            Vector3 tempForward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        //            Ray ray = new Ray(Camera.main.transform.position, tempForward);
        //            contextMenu.transform.position = ray.GetPoint(1);
        //            contextMenu.transform.LookAt(ray.GetPoint(7));
        //            contextMenu.GetComponent<ContextMenu>().setBondOption(hit.GetComponentInParent<Bond>());
        //        }
        //    }

        //}
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
    /// this method deletes everything in the scene, it is called on clicking the delete button in the UI
    /// </summary>
    public void DeleteAll()
    {
        markToDeleteCore(true);
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
            foreach(Atom a in m.atomList)
            {
                List<Vector3> conPos = new List<Vector3>();
                foreach (Atom at in a.connectedAtoms(a))
                {
                    if (at.isMarked || deleteAll)
                    {
                        if(!delAtomList.Contains(at))
                            delAtomList.Add(at);

                        if (a.m_data.m_abbre == "H" && !delAtomList.Contains(a))
                            delAtomList.Add(a);

                        conPos.Add(at.transform.localPosition);
                    }
                }
                positionsRestore.Add(a, conPos);
            }

            // add to delete List
            foreach(Bond b in m.bondList)
            {
                if (b.isMarked || deleteAll)
                {
                    if(Atom.Instance.getAtomByID(b.atomID1).m_data.m_abbre != "Dummy" && Atom.Instance.getAtomByID(b.atomID2).m_data.m_abbre != "Dummy")
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

        foreach(Molecule m in delMoleculeList)
        {
            List_curMolecules.Remove(m);
            Destroy(m.gameObject);

        }

        foreach (Molecule m in addMoleculeList)
        {
            List_curMolecules.Add(m);
        }

        foreach (Molecule m in List_curMolecules)
        {
            int size = m.atomList.Count;
            for(int i = 0; i < size; i++)
            {
                Atom a = m.atomList[i];
                int count = 0;
                while (a.m_data.m_bondNum > a.connectedAtoms(a).Count)
                {
                    CreateDummy(idInScene, m, a, calcDummyPos(a, positionsRestore, count));
                    count++;

                }
            }
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
        for(int i = 0; i < m.atomList.Count; i++)
        {
            List<Atom> groupingStash = new List<Atom>();
            Atom a = m.atomList[i];
            bool search = true;
            // for all groups
            foreach(KeyValuePair<Atom, List<Atom>> pair in groupedAtoms)
            {
                // for each atom in a group
                foreach(Atom at in pair.Value)
                {
                    // if the atom is in the group
                    if (at.m_idInScene == a.m_idInScene)
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
        foreach (Atom ca in a.connectedAtoms(a))
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
        foreach(Atom a in delAtomList)
        {
            List<Atom> conDummys = a.connectedDummys(a);
            i++;
            foreach(Atom d in conDummys)
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

        // delete Atoms from List
        foreach (Atom a in deleteList)
        {
            List_curAtoms.Remove(a);
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
        foreach(KeyValuePair<Atom, List<Atom>> pair in groupedAtoms)
        {
            Molecule tempMolecule = new GameObject().AddComponent<Molecule>();
            tempMolecule.transform.position = m.transform.position;
            tempMolecule.f_Init(idInScene, atomWorld.transform);
            addMoleculeList.Add(tempMolecule);
            idInScene++;
            foreach(Atom a in pair.Value)
            {
                a.transform.parent = tempMolecule.transform;
                a.m_molecule = tempMolecule;
                tempMolecule.atomList.Add(a);
            }
        }

        Dictionary<Bond, Molecule> tempBondDict = new Dictionary<Bond, Molecule>();
        foreach(Bond b in m.bondList)
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

        foreach(KeyValuePair<Bond, Molecule> pair in tempBondDict)
        {
            pair.Key.transform.parent = pair.Value.transform;
            pair.Key.m_molecule = pair.Value;
            pair.Value.bondList.Add(pair.Key);
        }

        //if (addMoleculeList.Count == 0)
        //    contextMenu.SetActive(false);

        finalDelete(m, delAtomList);
    }

    public Vector3 calcDummyPos(Atom a, Dictionary<Atom, List<Vector3>> positionsRestore, int count)
    {
        positionsRestore.TryGetValue(a, out List<Vector3> values);
        Vector3 newPos = values[count];
        return newPos;
    }

    #endregion

    #region atom building functions

    /// <summary>
    /// Creates a new atom from the given information
    /// The information source is a textfile with all parameters to each atomtype
    /// </summary>
    /// <param name="ChemicalID">chemical ID of the atom which should be created</param>
    /// <param name="pos">position, where the atom should be created</param>
    public void CreateAtom(int idAtom, string ChemicalAbbre, Vector3 pos)
    {
        GameObject tempMoleculeGO = Instantiate(myBoundingBoxPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        //tempMoleculeGO.GetComponent<Collider>().enabled = !allAtomMode;
        //tempMoleculeGO.GetComponent<BoundingBox>().enabled = !allAtomMode;
        //tempMoleculeGO.GetComponent<ObjectManipulator>().enabled = !allAtomMode;
        //tempMoleculeGO.GetComponent<NearInteractionGrabbable>().enabled = !allAtomMode;
        //tempMoleculeGO.GetComponent<ConstraintManager>().enabled = !allAtomMode;
        Molecule tempMolecule = tempMoleculeGO.AddComponent<Molecule>();

        //Molecule tempMolecule = new GameObject().AddComponent<Molecule>();
        tempMolecule.transform.position = pos;
        tempMolecule.f_Init(idAtom, atomWorld.transform);

        // 0: none; 1: sp1; 2: sp2;  3: sp3;  4: hypervalent trig. bipy; 5: unused;  6: hypervalent octahedral
        ElementData tempData = Dic_ElementData[ChemicalAbbre];
        tempData.m_hybridization = curHybrid;
        tempData.m_bondNum = Mathf.Max(0, tempData.m_bondNum - (3 - tempData.m_hybridization)); // a preliminary solution

        //Atom tempAtom = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<Atom>();
        GameObject tempAtomGO = Instantiate(myAtomPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        Atom tempAtom = tempAtomGO.AddComponent<Atom>();
        tempAtom.f_Init(tempData, tempMolecule, Vector3.zero ,idAtom);
        List_curAtoms.Add(tempAtom);
        //Dic_curAtoms.Add(idInScene, tempAtom);
        idInScene++;
        foreach (Vector3 posForDummy in tempAtom.m_posForDummies)
        {
            CreateDummy(idInScene, tempMolecule, tempAtom, posForDummy);
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
        tempData.m_bondNum = Mathf.Max(0, tempData.m_bondNum - (3 - tempData.m_hybridization));

        chgAtom.f_Modify(tempData);
    }

    public void modifyHybrid(Atom atom, int hybrid)
    {
        SaveMolecule(1);
        print("hybrid:   " + hybrid);
        ElementData tempData = Dic_ElementData[atom.m_data.m_abbre];
        tempData.m_hybridization = hybrid;
        tempData.m_bondNum = Mathf.Max(0, tempData.m_bondNum - (3 - tempData.m_hybridization));

        atom.f_Modify(tempData);
        atom.markAtom(true);
    }


    /// <summary>
    /// this method saturates all atoms, which means that all dummys are replaced by hydrogens
    /// </summary>
    public void SaturateAll()
    {
        // cancel any collision
        collision = false;
        foreach (Molecule curMole in List_curMolecules)
        {
            foreach (Atom a in curMole.atomList)
            {
                if (a.m_data.m_abbre=="Dummy")
                {
                    ChangeAtom(a.m_idInScene, "H");
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
    public Atom RebuildAtom(int idAtom, string ChemicalAbbre, int hybrid, Vector3 pos, Molecule mol)
    {
        ElementData tempData = Dic_ElementData[ChemicalAbbre];
        tempData.m_hybridization = hybrid;
        Atom tempAtom = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<Atom>();
        tempAtom.f_Init(tempData, mol, pos, idAtom);
        List_curAtoms.Add(tempAtom);
        idInScene++;
        return tempAtom;
    }


    /// <summary>
    /// Creates a dummy atom to fill empty binding positions
    /// </summary>
    /// <param name="inputMole">the molecule to which the dummy belongs</param>
    /// <param name="mainAtom">the main atom to which the dummy is connected</param>
    /// <param name="pos">the position, where the dummy is created</param>
    public void CreateDummy(int idDummy, Molecule inputMole, Atom mainAtom, Vector3 pos)
    {
        //Atom dummy = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<Atom>();
        GameObject dummyGO = Instantiate(myAtomPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        Atom dummy = dummyGO.AddComponent<Atom>();
        dummy.f_Init(Dic_ElementData["Dummy"], inputMole, pos, idDummy);//0 for dummy
        List_curAtoms.Add(dummy);
        CreateBond(mainAtom, dummy, inputMole);
        idInScene++;
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
        Bond bondInHand = moleInhand.bondList.Find(p=>p.atomID1==dummyInHand.m_idInScene|| p.atomID2 == dummyInHand.m_idInScene);
        Bond bondInAir = moleInAir.bondList.Find(p => p.atomID1 == dummyInAir.m_idInScene || p.atomID2 == dummyInAir.m_idInScene);
        if(moleInhand!=moleInAir)
        {
            moleInhand.givingOrphans(moleInAir, moleInhand);
        }

        Atom atom1 = List_curAtoms.Find((x) => x.GetComponent<Atom>() == bondInHand.findTheOther(dummyInHand));
        Atom atom2 = List_curAtoms.Find((x) => x.GetComponent<Atom>() == bondInAir.findTheOther(dummyInAir));

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
    }

    #endregion

    #region export import

    /// <summary>
    /// this method initialises the saved files and refreshes them
    /// </summary>
    public void initSavedFiles()
    {
        //int counter = -25;
        //string path = Application.dataPath + "/SavedMolecules/";
        //DirectoryInfo info = new DirectoryInfo(path);
        //FileInfo[] fileInfo = info.GetFiles();
        //foreach(FileInfo file in fileInfo)
        //{
        //    if (file.Extension.Equals(".xml"))
        //    {
        //        string name = file.Name.Substring(0, file.Name.Length - 4);
        //        GameObject button = Instantiate(savedFileButtonPrefab);
        //        button.transform.parent = scrollview.transform;
                
                
        //        button.transform.localScale = Vector3.one;
        //        button.transform.localRotation = Quaternion.Euler(0, 0, 0);

        //        button.GetComponentInChildren<Text>().text = name;
        //        button.transform.localPosition = new Vector3(125, counter, 0);
        //        counter -= 40;
        //    }
            
        //}
    }

    /// <summary>
    /// this method converts the selected input molecule to lists which then are saved in an XML format
    /// </summary>
    /// <param name="inputMole">the molecule which will be saved</param>
    public void SaveMolecule(int param)
    {
        Vector3 meanPos = new Vector3(0.0f, 0.0f, 0.0f); // average position if several molecule blocks are saved
        int nMol = 0;   // number of molecule blocks
        List<cmlData> saveData = new List<cmlData>();
        if(param == 0)
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

                list_atom.Add(new cmlAtom(a.m_idInScene, a.m_data.m_abbre, a.m_data.m_hybridization, a.transform.localPosition));
            }
            List<cmlBond> list_bond = new List<cmlBond>();
            foreach (Bond b in inputMole.bondList)
            {
                list_bond.Add(new cmlBond(b.atomID1, b.atomID2, b.m_bondOrder));
            }
            cmlData tempData = new cmlData(molePos, list_atom, list_bond);
            saveData.Add(tempData);
        }

        if(param == 0)
        {
            //CFileHelper.SaveData(Application.dataPath + "/SavedMolecules/" + UISave.inputfield + ".xml", saveData);
            //ForceFieldConsole.Instance.statusOut("Saved Molecule as : " + UISave.inputfield + ".xml");
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
            coords[iAtom] = (coords[iAtom] - COM) * GlobalCtrl.Instance.u2aa / GlobalCtrl.Instance.scale ;
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
    public void LoadMolecule(string name, int param)
    {
        int maxID = getMaxID() + 1;

        List<cmlData> loadData;

        Vector3 meanPos = new Vector3(0.0f, 0.0f, 0.0f);

        if(param == 0)
        {
            loadData = (List<cmlData>)CFileHelper.LoadData(Application.dataPath + "/SavedMolecules/" + name + ".xml", typeof(List<cmlData>));
            int nMol = 0;
            foreach (cmlData molecule in loadData)
            {
                nMol++;
                meanPos += molecule.molePos;
            }
            if (nMol > 0) meanPos /= (float)nMol;

            Debug.Log(string.Format("meanPos : {0,12:f6} {1,12:f6} {2,12:f6} ", meanPos.x, meanPos.y, meanPos.z));

            // new mean position should be controller left - meanPos of loaded molecule (thus shifted to origin + controller left) 
            // meanPos = controllerRight.transform.position - meanPos;  // add molecules relative to this position
            Debug.Log(string.Format("-meanPos + C : {0,12:f6} {1,12:f6} {2,12:f6} ", meanPos.x, meanPos.y, meanPos.z));
        } else
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


                for (int i = 0; i < molecule.atomArray.Length; i++)
                {
                    molecule.atomArray[i].id += maxID;
                }

                for (int i = 0; i < molecule.bondArray.Length; i++)
                {
                    molecule.bondArray[i].id1 += maxID;
                    molecule.bondArray[i].id2 += maxID;
                }

                Molecule tempMolecule = new GameObject().AddComponent<Molecule>();
                tempMolecule.transform.position = molecule.molePos + meanPos;
                tempMolecule.f_Init(idInScene, atomWorld.transform);
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



            }
            shrinkIDs();
            idInScene = getMaxID() + 1;
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
    public int getMaxID()
    {
        int id = 0;
        foreach(Atom a in List_curAtoms)
        {
            if (a.m_idInScene >= id)
                id = a.m_idInScene;
        }

        return id;
    }

    /// <summary>
    /// this method shrinks the IDs of the atoms to prevent an overflow
    /// </summary>
    public void shrinkIDs()
    {
        int numAtoms = List_curAtoms.Count;

        for(int i = 0; i <= numAtoms; i++)
        {
            if(Atom.Instance.getAtomByID(i) == null)
            {
                swapID(i);
            }
        }
    }

    /// <summary>
    /// this method swaps the ID of an atom with a new ID
    /// </summary>
    /// <param name="idNew">new ID</param>
    public void swapID(int idNew)
    {
        foreach(Atom a in List_curAtoms)
        {
            if(a.m_idInScene > idNew)
            {
                foreach (Bond b in a.m_molecule.bondList)
                {
                    if (b.atomID1 == a.m_idInScene)
                        b.atomID1 = idNew;
                    else if (b.atomID2 == a.m_idInScene)
                        b.atomID2 = idNew;
                }

                if (a.m_idInScene == a.m_molecule.m_id)
                    a.m_molecule.m_id = idNew;

                a.m_idInScene = idNew;


                break;
            }
        }
    }

    #endregion

    #region ui functions

    /// <summary>
    /// this method controls the main UI
    /// UI can be toggled on and off
    /// </summary>
    public void SwitchUIMain()
    {
        //UIMain.gameObject.SetActive(!UIMain.gameObject.activeSelf);
        //Vector3 tempForward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        //Ray ray = new Ray(Camera.main.transform.position, tempForward);
        //if (UIMain.gameObject.activeSelf)
        //{
        //    UIMain.transform.position = ray.GetPoint(6);
        //    UIMain.transform.LookAt(ray.GetPoint(7));
        //}
    }


    public void switchPeriodicTable()
    {
        //periodictable.SetActive(!periodictable.activeSelf);
        //if (btnPerTable.GetComponentInChildren<Text>().text == "show periodic table")
        //{
        //    btnPerTable.GetComponentInChildren<Text>().text = "hide periodic table";
        //}
        //else
        //{
        //    btnPerTable.GetComponentInChildren<Text>().text = "show periodic table";
        //}
    }

    public void createAtomUI(string ChemicalID)
    {
        lastAtom = ChemicalID; // remember this for later
        //CreateAtom(idInScene, ChemicalID, controllerLeft.transform.position + new Vector3(0.01f, 0, 0.01f));
        Vector3 current_pos = Camera.main.transform.position;
        Vector3 current_lookat = Camera.main.transform.forward;
        Vector3 create_position = current_pos + 0.5f * current_lookat;

        CreateAtom(idInScene, ChemicalID, create_position);
    }

    /// <summary>
    /// this method changes the atom mode
    /// either the whole molecule or only a single atom is grabbed, depending on the mode
    /// </summary>
    public void changeAtomMode()
    {
        allAtomMode = !allAtomMode;
        foreach (Molecule molecule in List_curMolecules)
        {
            //molecule.GetComponent<Collider>().enabled = !allAtomMode;
            //molecule.GetComponent<BoundingBox>().enabled = !allAtomMode;
            //molecule.GetComponent<ObjectManipulator>().enabled = !allAtomMode;
            //molecule.GetComponent<NearInteractionGrabbable>().enabled = !allAtomMode;
            //molecule.GetComponent<ConstraintManager>().enabled = !allAtomMode;
            Debug.Log($"[GlobalCtrl] All Atom Mode is {allAtomMode}");
        }

    }

    public void getRepulsionScale(float value)
    {
        // set value from slider
        repulsionScale = 0.1f + value*0.9f;
        Debug.Log(string.Format("repulsionScale, new val: {0, 6:f3}", repulsionScale));
    }

    public void getHybridization(float value)
    {
        // set value from slider
        curHybrid = (int)value;
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
        //favorites[pos - 1] = abbre;
        //Transform temp = null;
        //for(int i = 0; i<periodictable.transform.childCount; i++)
        //{
        //    if (periodictable.transform.GetChild(i).transform.Find("Btn_" + GetElementbyAbbre(abbre).m_name) != null)
        //        temp = periodictable.transform.GetChild(i).transform.Find("Btn_" + GetElementbyAbbre(abbre).m_name);
        //}
        //
        //favMenu[pos - 1].transform.GetComponent<Image>().sprite = temp.GetComponent<Image>().sprite;
        //favoritesGO[pos - 1].transform.GetComponent<Image>().sprite = temp.GetComponent<Image>().sprite;
    }

    public void createFavoriteElement(int pos)
    {
        lastAtom = favorites[pos - 1]; // remember this for later
                                       //CreateAtom(idInScene, favorites[pos - 1], controllerRight.transform.position + new Vector3(0.01f, 0, 0.01f));
        Vector3 current_pos = Camera.main.transform.position;
        Vector3 current_lookat = Camera.main.transform.forward;
        Vector3 create_position = current_pos + 0.5f * current_lookat;
        CreateAtom(idInScene, favorites[pos - 1], create_position);
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

    #endregion

    public void BackToMain()
    {
        // TODO: should have a save guard
        SceneManager.LoadScene("Init");
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