using StructClass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoableChange
{
    public cmlData before;
    public cmlData after;
    public GameObject target;
    public Type type;

    public enum Type
    {
        CREATE,
        DELETE,
        CHANGE
    }
    
    public UndoableChange(Type type, GameObject target)
    {
        this.type = type;
        this.target = target;
        switch (type)
        {
            case Type.CREATE:
                if (target.GetComponent<Molecule>())
                {
                    after = getMoleculeData(target);
                }
                // Merging molecules
                if (target.GetComponent<Bond>())
                {

                }
                break;
            case Type.DELETE:
                if (target.GetComponent<Molecule>() || target.GetComponent<Atom>())
                {
                    before = getMoleculeData(target);
                }
                // Separating molecules
                if (target.GetComponent<Bond>())
                {

                }
                break;
            case Type.CHANGE:
                break;
        }
    }

    private cmlData getMoleculeData(GameObject target)
    {
        Molecule mol = target.GetComponent<Molecule>();
        List<cmlAtom> list_atom = new List<cmlAtom>();
        foreach (Atom a in mol.atomList)
        {

            list_atom.Add(new cmlAtom(a.m_id, a.m_data.m_abbre, a.m_data.m_hybridization, a.transform.localPosition));
        }
        List<cmlBond> list_bond = new List<cmlBond>();
        foreach (Bond b in mol.bondList)
        {
            list_bond.Add(new cmlBond(b.atomID1, b.atomID2, b.m_bondOrder));
        }
        return new cmlData(mol.transform.position, mol.transform.rotation, mol.m_id, list_atom, list_bond);
    }

    public void Undo()
    {
        if(type == Type.CREATE)
        {
            if (target.GetComponent<Molecule>())
            {
                GlobalCtrl.Singleton.deleteMolecule(target.GetComponent<Molecule>(), false);
            }
        }
        else if (type == Type.DELETE)
        {
            GlobalCtrl.Singleton.rebuildMolecule(before);
        }
    }
}
