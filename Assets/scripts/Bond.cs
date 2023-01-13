using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class Bond : MonoBehaviour, IMixedRealityPointerHandler
{
    /// <summary>
    /// instance of global control
    /// </summary>
    private static Bond instance;
    public static Bond Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Bond>();
            }
            return instance;
        }
    }


    private Stopwatch stopwatch;
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
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
        stopwatch?.Stop();
        if (stopwatch?.ElapsedMilliseconds < 200)
        {
            var bond_id = (ushort)m_molecule.bondList.IndexOf(this);
            EventManager.Singleton.SelectBond(bond_id, m_molecule.m_id, !isMarked);
            markBond(!isMarked, true);
        }
    }

    private GameObject myToolTipPrefab;
    private GameObject deleteMeButtonPrefab;
    private GameObject closeMeButtonPrefab;
    private GameObject toolTipInstance;
    private float toolTipDistanceWeight = 2.5f;
    [HideInInspector] public ushort atomID1;
    [HideInInspector] public ushort atomID2;
    [HideInInspector] public float m_bondOrder;  // 1.0 for single bonds; 1.5 for resonant bonds; 2.0 for double bonds; idea is to scale the bond diameter by this value
    [HideInInspector] public float m_bondDistance;
    [HideInInspector] public Molecule m_molecule;
    [HideInInspector] public bool isMarked = false;

    /// <summary>
    /// initialises the bond between two atoms
    /// </summary>
    /// <param name="_atom1">the first atom of the bond</param>
    /// <param name="_atom2">the second atom of the bond</param>
    /// <param name="inputMole">the molecule to which the bond belongs</param>
    public void f_Init(Atom _atom1, Atom _atom2, Molecule inputMole)
    {
        atomID1 = _atom1.m_id;
        atomID2 = _atom2.m_id;
        m_molecule = inputMole;
        m_bondOrder = 1.0f;   // standard
        m_bondDistance = 1.0f;
        this.gameObject.tag = "Bond";
        this.gameObject.layer = 7;
        m_molecule.bondList.Add(this);
        transform.position = (_atom1.transform.position + _atom2.transform.position) / 2;
        transform.LookAt(_atom1.transform);
        transform.parent = inputMole.transform;
        float distance = Vector3.Distance(_atom1.transform.position, _atom2.transform.position);
        transform.localScale = new Vector3(m_bondOrder, m_bondOrder, distance);

        // load prefabs
        myToolTipPrefab = (GameObject)Resources.Load("prefabs/MRTKAtomToolTip");
        if (myToolTipPrefab == null)
        {
            throw new FileNotFoundException("[Molecule] MRTKAtomToolTip prefab not found - please check the configuration");
        }
        deleteMeButtonPrefab = (GameObject)Resources.Load("prefabs/DeleteMeButton");
        if (deleteMeButtonPrefab == null)
        {
            throw new FileNotFoundException("[Molecule] DeleteMeButton prefab not found - please check the configuration");
        }
        closeMeButtonPrefab = (GameObject)Resources.Load("prefabs/CloseMeButton");
        if (closeMeButtonPrefab == null)
        {
            throw new FileNotFoundException("[Molecule] CloseMeButton prefab not found - please check the configuration");
        }
    }

    /// <summary>
    /// finds the other atom of a bond by entering the ID of the first atom of the bond
    /// </summary>
    /// <param name="num">ID of one atom</param>
    /// <returns></returns>
    public Atom findTheOther(Atom at)
    {
        if (at.m_id == atomID1)
            return Atom.Instance.getAtomByID(atomID2);
        else if (at.m_id == atomID2)
            return Atom.Instance.getAtomByID(atomID1);
        else
            return null;
    }

    /// <summary>
    /// this method marks a bond in a different color if it is selected
    /// </summary>
    /// <param name="mark">true or false if selected</param>
    public void markBond(bool mark, bool toolTip = false)
    {
        isMarked = mark;

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
            colorSwapSelect(0);
            if (toolTipInstance != null)
            {
                Destroy(toolTipInstance);
            }
        }
        // destroy tooltip of marked without flag
        if (!toolTip && toolTipInstance != null)
        {
            Destroy(toolTipInstance);
        }
    }

    /// <summary>
    /// changes color of selected and deselected bonds
    /// </summary>
    /// <param name="isOn">if this bond is selected</param>
    public void colorSwapSelect(int col)
    {
        if (col == 2)
            this.GetComponentInChildren<Renderer>().material = GlobalCtrl.Singleton.markedMat;
        else
            this.GetComponentInChildren<Renderer>().material = GlobalCtrl.Singleton.bondMat;
    }

    /// <summary>
    /// this method returns a bond between two atoms
    /// </summary>
    /// <param name="a1">first atom of the bond</param>
    /// <param name="a2">second atom of the bond</param>
    /// <returns>the bond between the two atoms</returns>
    public Bond getBond(Atom a1, Atom a2)
    {
        foreach(Bond b in a1.m_molecule.bondList)
        {
            if (b.atomID1 == a1.m_id && b.atomID2 == a2.m_id)
                return b;
            else if (b.atomID2 == a1.m_id && b.atomID1 == a2.m_id)
                return b;
        }
        return null;
    }

    private void createToolTip()
    {
        // create tool tip
        toolTipInstance = Instantiate(myToolTipPrefab);
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
        // add bond as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = gameObject;
        // show meta data
        var atom1 = Atom.Instance.getAtomByID(atomID1);
        var atom2 = Atom.Instance.getAtomByID(atomID2);
        string toolTipText = $"Distance: {m_bondDistance}\nOrder: {m_bondOrder}\nAtom1: {atom1.m_data.m_name}\nAtom2: {atom2.m_data.m_name}";
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;
        if (atom1.m_data.m_abbre != "Dummy" && atom2.m_data.m_abbre != "Dummy")
        {
            var delButtonInstance = Instantiate(deleteMeButtonPrefab);
            delButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.deleteBondUI(this); });
            toolTipInstance.GetComponent<DynamicToolTip>().addContent(delButtonInstance);
        }
        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markBond(false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);
    }

    public void OnDestroy()
    {
        if (toolTipInstance != null)
        {
            Destroy(toolTipInstance);
        }
        m_molecule.bondList.Remove(this);
    }

}
