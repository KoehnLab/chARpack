using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using StructClass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

[Serializable]
public class Atom : MonoBehaviour, IMixedRealityPointerHandler
{
    public GameObject myAtomToolTipPrefab;
    public GameObject deleteMeButtonPrefab;
    private Stopwatch stopwatch;
    private GameObject toolTipInstance = null;
    private float toolTipDistanceWeight = 2.5f;

    private static Atom instance;
    public static Atom Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Atom>();
            }
            return instance;
        }
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        // give it a glow halo
        (GetComponent("Halo") as Behaviour).enabled = true;
        stopwatch = Stopwatch.StartNew();
    }
    public void OnPointerClicked(MixedRealityPointerEventData eventData) 
    {
        // Intentionally empty
    }
    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        // Intentionally empty
    }

    // This function is triggered when a grabbed object is dropped
    public void OnPointerUp(MixedRealityPointerEventData eventData) 
    {
        // remove glow
        (GetComponent("Halo") as Behaviour).enabled = false;

        stopwatch.Stop();
        //UnityEngine.Debug.Log($"[Atom] Interaction stopwatch: {stopwatch.ElapsedMilliseconds} [ms]");
        if (stopwatch.ElapsedMilliseconds < 200)
        {
            if (m_molecule.isMarked)
            {
                m_molecule.markMolecule(false);
            }
            else
            {
                if (isMarked)
                {
                    markAtom(false, true);
                }
                else
                {
                    markAtom(true, true);
                }
            }
        }

        // check for potential merge
        if (GlobalCtrl.Singleton.collision)
        {
            Atom d1 = GlobalCtrl.Singleton.collider1;
            Atom d2 = GlobalCtrl.Singleton.collider2;

            Atom a1 = Atom.Instance.dummyFindMain(d1);
            Atom a2 = Atom.Instance.dummyFindMain(d2);

            if (!Atom.Instance.alreadyConnected(a1, a2))
                GlobalCtrl.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1, GlobalCtrl.Singleton.collider2);

        }
        //Debug.Log($"[Atom] OnPointerUp: {eventData}");
    }

    [HideInInspector] public ushort m_idInScene;
    [HideInInspector] public Molecule m_molecule;
    [HideInInspector] public ElementData m_data { get; private set; }
    // we have to clarify the role of m_data: Is this just basic (and constant) data?
    // 0: none; 1: sp1; 2: sp2;  3: sp3;  4: hypervalent trig. bipy; 5: unused;  6: hypervalent octahedral
    [HideInInspector] public Material m_mat;
    //public int m_nBondP;
    [HideInInspector] public Rigidbody m_rigid;
    [HideInInspector] public bool isGrabbed = false;
    [HideInInspector] public List<Vector3> m_posForDummies;

    [HideInInspector] public bool isMarked = false;

    [HideInInspector] public GameObject m_ActiveHand = null;


    /// <summary>
    /// initialises the atom with all it's attributes
    /// </summary>
    /// <param name="inputData"></param>
    /// <param name="inputMole"></param>
    /// <param name="pos"></param>
    /// <param name="idInScene"></param>
    public void f_Init(ElementData inputData, Molecule inputMole, Vector3 pos, ushort idInScene)
    {
        m_idInScene = idInScene;
        m_molecule = inputMole;
        m_molecule.atomList.Add(this);
        m_data = inputData;


        this.gameObject.name = m_data.m_name;
        this.gameObject.tag = "Atom";
        //this.gameObject.layer = 6;
        //this.GetComponent<SphereCollider>().isTrigger = true;
        this.GetComponent<BoxCollider>().isTrigger = true;

        //I don't want to create the materials for all elements from the beginning,
        //so I only create a material for an element at the first time when I create this element,
        //and then add this material to the dictionary
        //So next time when I need to create this element,
        //I will use the dictionary to get a copy of an existent material.
        if (!GlobalCtrl.Singleton.Dic_AtomMat.ContainsKey(m_data.m_id))
        {
            Material tempMat = Instantiate(GlobalCtrl.Singleton.atomMatPrefab);
            tempMat.color = m_data.m_color;
            GlobalCtrl.Singleton.Dic_AtomMat.Add(m_data.m_id, tempMat);
        }
        GetComponent<MeshRenderer>().material = GlobalCtrl.Singleton.Dic_AtomMat[m_data.m_id];
        m_mat = GetComponent<MeshRenderer>().material;

        this.transform.parent = inputMole.transform;
        this.transform.localPosition = pos;    
        this.transform.localScale = Vector3.one * m_data.m_radius * (GlobalCtrl.Singleton.scale/GlobalCtrl.Singleton.u2pm) * GlobalCtrl.Singleton.atomScale;
        // at this point we have the size of the atom, so we can adjust the size of the halo
        //


        //Debug.Log(string.Format("Added latest {0}:  rad={1}  scale={2}  hyb={3}  nBonds={4}", m_data.m_abbre, m_data.m_radius, GlobalCtrl.Singleton.atomScale, m_data.m_hybridization, m_data.m_bondNum));

        //Initial positions for dummies
        m_posForDummies = new List<Vector3>();
        Vector3 offset = new Vector3(0, 100, 0);
        // TODO: make this dependent on m_nBond and m_hybridization:

        //Debug.Log("Hybrid: " + m_data.m_hybridization.ToString());

        switch (m_data.m_hybridization)
        {
            case (0):
                break;
            case (1): // linear, max 2 bonds
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 120) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            case (2): // trigonal, max 3 bonds
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 120) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 240) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            case (3): // tetrahedral
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(70.53f, 60, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(-70.53f, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 3) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(-70.53f, 120, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            case (4): // trigonal bipyramidal
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 3) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(120, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 4) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(240, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            case (6): // octahedral  (with 4 bonds: quadratic planar)
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 180) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 3) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(180, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 4) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(90, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                if (m_data.m_bondNum > 5) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(270, 0, 90) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                break;
            default:  // fall-back ... we have to see how to do error handling here
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm));
                UnityEngine.Debug.Log("[Atom] InitDummies: Landed in Fallback!");
                break;

        }

    }


    /// <summary>
    /// modify the atom using the info on ElementData
    /// </summary>
    /// <param name="newData"></param>
    public void f_Modify(ElementData newData)
    {
        int numConnected = this.connectedAtoms(this).Count;
        m_data = newData;
        uint dummyLimit = this.m_data.m_bondNum;
        this.gameObject.name = m_data.m_name;
        if (!GlobalCtrl.Singleton.Dic_AtomMat.ContainsKey(m_data.m_id))
        {
            Material tempMat = Instantiate(GlobalCtrl.Singleton.atomMatPrefab);
            tempMat.color = m_data.m_color;
            GlobalCtrl.Singleton.Dic_AtomMat.Add(m_data.m_id, tempMat);
        }
        GetComponent<MeshRenderer>().material = GlobalCtrl.Singleton.Dic_AtomMat[m_data.m_id];
        m_mat = GetComponent<MeshRenderer>().material;

        this.transform.localScale = Vector3.one * m_data.m_radius * (GlobalCtrl.Singleton.scale / GlobalCtrl.Singleton.u2pm) * GlobalCtrl.Singleton.atomScale;


        foreach(Atom a in this.connectedDummys(this))
        {
            if(numConnected > dummyLimit)
            {
                numConnected--;
                Destroy(a.gameObject);
                this.m_molecule.atomList.Remove(a);
                GlobalCtrl.Singleton.List_curAtoms.Remove(a);
                Bond b = a.connectedBonds()[0];
                Destroy(b.gameObject);
                this.m_molecule.bondList.Remove(b);
            }

        }

        while (dummyLimit > numConnected)
        {
            print("before Dummy Limit, numConnected:   " + dummyLimit + "   " + numConnected);
            addDummy(numConnected);
            numConnected++;
        }

        // Debug.Log(string.Format("Modified latest {0}:  rad={1}   scale={2} ", m_data.m_abbre, m_data.m_radius, GlobalCtrl.Singleton.atomScale));
    }


    public void addDummy(int numConnected)
    {
        List<Atom> conAtoms = this.connectedAtoms(this);

        Vector3 position = new Vector3();
        Vector3 firstVec = new Vector3();
        Vector3 secondVec = new Vector3();
        Vector3 normalVec = new Vector3();
        switch (numConnected)
        {
            case (0):
                position = this.transform.localPosition + new Vector3(0,0,0.05f);
                GlobalCtrl.Singleton.CreateDummy(GlobalCtrl.Singleton.idInScene, this.m_molecule, this, position);
                break;
            case (1):
                firstVec = this.transform.localPosition - conAtoms[0].transform.localPosition;
                position = this.transform.localPosition + firstVec;
                GlobalCtrl.Singleton.CreateDummy(GlobalCtrl.Singleton.idInScene, this.m_molecule, this, position);
                break;
            case (2):
                firstVec = this.transform.localPosition - conAtoms[0].transform.localPosition;
                secondVec = this.transform.localPosition - conAtoms[1].transform.localPosition;
                position = this.transform.localPosition + ((firstVec + secondVec) / 2.0f);
                if (position == this.transform.localPosition)
                    position = Vector3.Cross(firstVec, secondVec);
                GlobalCtrl.Singleton.CreateDummy(GlobalCtrl.Singleton.idInScene, this.m_molecule, this, position);
                break;
            case (3):
                firstVec = conAtoms[1].transform.localPosition - conAtoms[0].transform.localPosition;
                secondVec = conAtoms[2].transform.localPosition - conAtoms[0].transform.localPosition;
                normalVec = new Vector3(firstVec.y * secondVec.z - firstVec.z * secondVec.y, firstVec.z * secondVec.x - firstVec.x * secondVec.z, firstVec.x * secondVec.y - firstVec.y * secondVec.x);
                position = this.transform.localPosition + normalVec;

                float sideCheck1 = normalVec.x * this.transform.localPosition.x + normalVec.y * this.transform.localPosition.y + normalVec.z * this.transform.localPosition.z;
                float sideCheck2 = position.x * this.transform.localPosition.x + position.y * this.transform.localPosition.y + position.z * this.transform.localPosition.z;

                if ((sideCheck1 >= 0 && sideCheck2 >= 0) || (sideCheck1 <= 0 && sideCheck2 <= 0))
                    position = this.transform.localPosition - normalVec;

                GlobalCtrl.Singleton.CreateDummy(GlobalCtrl.Singleton.idInScene, this.m_molecule, this, position);
                break;
            default:
                break;
        } 
    }

    /// <summary>
    /// changes color of selected and deselected atoms
    /// </summary>
    /// <param name="isOn">if this atom is selected</param>
    public void colorSwapSelect(int col)
    {
        if (col == 1)
            this.GetComponent<Renderer>().material = GlobalCtrl.Singleton.selectedMat;
        else if (col == 2 || this.isMarked)
            //this.GetComponent<Renderer>().material.color = new Color(this.GetComponent<Renderer>().material.color.r, this.GetComponent<Renderer>().material.color.g, this.GetComponent<Renderer>().material.color.b, 0.5f);
            this.GetComponent<Renderer>().material = GlobalCtrl.Singleton.markedMat;
        else
            this.GetComponent<Renderer>().material = GlobalCtrl.Singleton.Dic_AtomMat[m_data.m_id];
    }

    private void OnTriggerEnter(Collider collider)
    {
        // Debug.Log($"[Atom] Collision Detected: {collider.name}");
        if (collider.name.StartsWith("Dummy") && this.name.StartsWith("Dummy") && GlobalCtrl.Singleton.collision == false)
        {

            GlobalCtrl.Singleton.collision = true;
            GlobalCtrl.Singleton.collider1 = collider.GetComponent<Atom>();
            GlobalCtrl.Singleton.collider2 = this.GetComponent<Atom>();
            GlobalCtrl.Singleton.collider1.colorSwapSelect(1);
            GlobalCtrl.Singleton.collider2.colorSwapSelect(1);
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.name.StartsWith("Dummy") && this.name.StartsWith("Dummy"))
        {
            if (GlobalCtrl.Singleton.collider1 != null)
            {
                GlobalCtrl.Singleton.collider1.colorSwapSelect(0);
                GlobalCtrl.Singleton.collider1 = null;
            }
            if (GlobalCtrl.Singleton.collider2 != null)
            {
                GlobalCtrl.Singleton.collider2.colorSwapSelect(0);
                GlobalCtrl.Singleton.collider2 = null;
            }
            GlobalCtrl.Singleton.collision = false;
        }
    }

    /// <summary>
    /// this method calculates a list of all connected atoms for a given atom
    /// </summary>
    /// <returns>list of connected atoms</returns>
    public List<Atom> connectedAtoms(Atom a)
    {
        List<Atom> conAtomList = new List<Atom>();
        foreach(Bond b in a.m_molecule.bondList)
        {
            
            if (b.atomID1 == a.m_idInScene || b.atomID2 == a.m_idInScene)
            {
                Atom otherAtom = getAtomByID(b.findTheOther(a).m_idInScene);
                if (!conAtomList.Contains(otherAtom))
                    conAtomList.Add(otherAtom);
            }
        }
        return conAtomList;
    }

    public List<Atom> connectedDummys(Atom a)
    {
        List<Atom> allConnected = connectedAtoms(a);
        List<Atom> conDummys = new List<Atom>();
        foreach(Atom at in allConnected)
        {
            if (at.m_data.m_abbre == "Dummy")
                conDummys.Add(at);
        }

        return conDummys;
    }

    /// <summary>
    /// this method calculates a list of all connected bonds for a given atom
    /// </summary>
    /// <returns>list of connected bonds</returns>
    public List<Bond> connectedBonds()
    {
        List<Bond> conBondList = new List<Bond>();
        foreach (Bond b in this.m_molecule.bondList)
        {
            if (b.atomID1 == this.m_idInScene || b.atomID2 == this.m_idInScene)
            {
                conBondList.Add(b);
            }
        }
        return conBondList;
    }


    /// <summary>
    /// this method returns the atom with the given ID
    /// </summary>
    /// <param name="id">ID of the atom</param>
    /// <returns>the searched atom</returns>
    public Atom getAtomByID(float id)
    {
        foreach (Atom atom in GlobalCtrl.Singleton.List_curAtoms)
        //foreach (KeyValuePair<int, Atom> atom in GlobalCtrl.Singleton.Dic_curAtoms)
        {
            if (atom.m_idInScene == (int)id)
                return atom;
        }

        return null;
    }

    /// <summary>
    /// this method returns the main atom for a given dummy atom
    /// </summary>
    /// <param name="dummy">the dummy atom</param>
    /// <returns>the main atom of the dummy</returns>
    public Atom dummyFindMain(Atom dummy)
    {
        if (dummy.m_data.m_name == "Dummy")
        {
            Bond b = dummy.m_molecule.bondList.Find(p => p.atomID1 == dummy.m_idInScene || p.atomID2 == dummy.m_idInScene);      
            Atom atom1 = GlobalCtrl.Singleton.List_curAtoms.Find((x) => x.GetComponent<Atom>() == b.findTheOther(dummy));
            return atom1;
        }
        else
            return null;
       
    }

    /// <summary>
    /// this method tests if two atoms are already connected
    /// </summary>
    /// <param name="a1">the first atom</param>
    /// <param name="a2">the second atom</param>
    /// <returns>true or false depending on if the atoms are connected</returns>
    public bool alreadyConnected(Atom a1, Atom a2)
    {
        foreach(Bond b in a1.m_molecule.bondList)
        {
            if (b.findTheOther(a1) == a2)
                return true;
        }

        if (a1 == a2)
            return true;

        return false;
    }

    /// <summary>
    /// this method marks the atom in a different color if selected
    /// </summary>
    /// <param name="mark">true or false if the atom should be marked</param>
    public void markAtom(bool mark, bool toolTip = false)
    {

        this.isMarked = mark;

        if (isMarked)
        {
            colorSwapSelect(2);
            if (toolTipInstance == null && toolTip)
            {
                createToolTip();
            }
        }
        else
        {
            if (toolTipInstance != null)
            {
                Destroy(toolTipInstance);
            }
            colorSwapSelect(0);
        }
        // destroy tooltip of marked without flag
        if (!toolTip)
        {
            Destroy(toolTipInstance);
        }
    }

    private void createToolTip()
    {
        // create tool tip
        toolTipInstance = Instantiate(myAtomToolTipPrefab);
        // calc position for tool tip
        // first: get position in the bounding box and decide if the tool tip spawns left, right, top or bottom of the box
        Vector3 mol_center = m_molecule.getCenter();
        // project to camera coordnates
        Vector2 mol_center_in_cam = new Vector2(Vector3.Dot(mol_center, Camera.main.transform.right), Vector3.Dot(mol_center, Camera.main.transform.up));
        Vector2 atom_pos_in_cam = new Vector2(Vector3.Dot(transform.position, Camera.main.transform.right), Vector3.Dot(transform.position, Camera.main.transform.up));
        // calc diff
        Vector2 diff_mol_atom = atom_pos_in_cam - mol_center_in_cam;
        // enhance diff for final tool tip pos
        Vector3 ttpos = transform.position + toolTipDistanceWeight * diff_mol_atom[0] * Camera.main.transform.right + toolTipDistanceWeight * diff_mol_atom[1] * Camera.main.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add atom as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = gameObject;
        string toolTipText = $"Name: {m_data.m_name}\nHybrid.: {m_data.m_hybridization}\nMass: {m_data.m_mass}\nRadius: {m_data.m_radius}\nNumBonds: {m_data.m_bondNum}";
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;
        if (m_data.m_abbre != "Dummy")
        {
            var delButtonInstance = Instantiate(deleteMeButtonPrefab);
            delButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.markToDelete(); });
            toolTipInstance.GetComponent<DynamicToolTip>().addContent(delButtonInstance);
        }
    }


    public void OnDestroy()
    {
        if (toolTipInstance != null)
        {
            Destroy(toolTipInstance);
        }
    }

}