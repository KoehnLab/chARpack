using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoableChange
{
    public enum Type
    {
        Create,
        Destroy
    }

    public Molecule molecule;
    public Bond bond;
    public Atom atom;
    public Type type;

    public UndoableChange(Molecule molecule, Type type)
    {
        this.molecule = molecule;
        this.type = type;
    }

    public UndoableChange(Atom atom, Type type)
    {
        this.atom = atom;
        this.type = type;
    }

    public UndoableChange(Bond bond, Type type)
    {
        this.bond = bond;
        this.type = type;
    }

    public void Undo()
    {
        //TODO: manage network messages
        if (type == Type.Create)
        {
            if (molecule!=null)
            {
                GlobalCtrl.Singleton.deleteMolecule(molecule, false);
            }
            else if (atom != null)
            {
                GlobalCtrl.Singleton.deleteAtom(atom, false);
            }
            else if(bond != null)
            {
                GlobalCtrl.Singleton.deleteBond(bond, false);
            }
        } 
        else if(type == Type.Destroy)
        {
            if (molecule != null)
            {
                // TODO: deal with conflicting IDs
                GlobalCtrl.Singleton.recreateMolecule(molecule);
            }
            else if (atom != null)
            {

            }
            else if (bond != null)
            {
                Debug.Log("Undoing delete bond");
                Atom atom1 = bond.m_molecule.atomList[bond.atomID1];
                Atom atom2 = bond.m_molecule.atomList[bond.atomID2];
                GlobalCtrl.Singleton.CreateBond(atom1, atom2, bond.m_molecule);
            }
        }
        // TODO: delete all
    }
}
