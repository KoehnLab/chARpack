using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bond : MonoBehaviour
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


    [HideInInspector] public int atomID1;
    [HideInInspector] public int atomID2;
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
        atomID1 = _atom1.m_idInScene;
        atomID2 = _atom2.m_idInScene;
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
    }

    /// <summary>
    /// finds the other atom of a bond by entering the ID of the first atom of the bond
    /// </summary>
    /// <param name="num">ID of one atom</param>
    /// <returns></returns>
    public Atom findTheOther(Atom at)
    {
        if (at.m_idInScene == atomID1)
            return Atom.Instance.getAtomByID(atomID2);
        else if (at.m_idInScene == atomID2)
            return Atom.Instance.getAtomByID(atomID1);
        else
            return null;
    }

    /// <summary>
    /// this method marks a bond in a different color if it is selected
    /// </summary>
    /// <param name="mark">true or false if selected</param>
    public void markBond(bool mark)
    {
        this.isMarked = mark;

        if (this.isMarked)
        {
            colorSwapSelect(2);
        }
        else
        {
            colorSwapSelect(0);
        }
    }

    /// <summary>
    /// changes color of selected and deselected bonds
    /// </summary>
    /// <param name="isOn">if this bond is selected</param>
    public void colorSwapSelect(int col)
    {
        if (col == 2)
            this.GetComponentInChildren<Renderer>().material = GlobalCtrl.Instance.markedMat;
        else
            this.GetComponentInChildren<Renderer>().material = GlobalCtrl.Instance.bondMat;
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
            if (b.atomID1 == a1.m_idInScene && b.atomID2 == a2.m_idInScene)
                return b;
            else if (b.atomID2 == a1.m_idInScene && b.atomID1 == a2.m_idInScene)
                return b;
        }
        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
