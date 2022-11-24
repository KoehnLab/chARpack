using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Molecule : MonoBehaviour
{
    /// <summary>
    /// molecule id
    /// </summary>
    public int m_id;


    public bool isMarked;
    /// <summary>
    /// atom list contains all atoms which belong to this molecule
    /// </summary>
    public List<Atom> atomList { get; private set; }
    /// <summary>
    /// bond list contains all bonds which belong to this molecule
    /// </summary>
    public List<Bond> bondList { get; private set; }

    /// <summary>
    /// this method initialises a new molecule, it is called when a new atom with it's dummies is created from scratch
    /// </summary>
    /// <param name="idInScene">the ID in the scene o the molecule</param>
    /// <param name="inputParent"> the parent of the molecule</param>
    public void f_Init(int idInScene, Transform inputParent)
    {
        m_id = idInScene;
        isMarked = false;
        this.name = "molecule_" + m_id;
        this.transform.parent = inputParent;
        atomList = new List<Atom>();
        bondList = new List<Bond>();
        
    }

    /// <summary>
    /// if two molecules are merged, all atoms from the old molecule need to be transferred to the new molecule
    /// </summary>
    /// <param name="newParent"> the molecule which is the new parent to all atoms</param>
    public void givingOrphans(Molecule newParent, Molecule oldParent)
    {
        foreach(Atom a in atomList)
        {
            a.transform.parent = newParent.transform;
            a.m_molecule = newParent;
            newParent.atomList.Add(a);
        }
        foreach (Bond b in bondList)
        {
            b.transform.parent = newParent.transform;
            b.m_molecule = newParent;
            newParent.bondList.Add(b);
        }
        GlobalCtrl.Instance.List_curMolecules.Remove(oldParent);
        //GlobalCtrl.Instance.Dic_curMolecules.Remove(m_id);
        Destroy(this.gameObject);
    }


    public void markMolecule(bool mark)
    {
        foreach (Atom a in this.atomList)
        {
            a.markAtom(mark);
        }

        foreach (Bond b in this.bondList)
        {
            b.markBond(mark);
        }
        isMarked = mark;
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
