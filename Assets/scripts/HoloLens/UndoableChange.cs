using StructClass;
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
    public enum Argument
    {
        Molecule,
        Atom,
        Bond,
        Generic
    }

    public cmlData molecule;
    public cmlBond bond;
    public cmlAtom atom;
    public Type type;
    public Argument argument;

    public UndoableChange(Molecule molecule, Type type)
    {
        this.molecule = molecule.getCmlData();
        this.type = type;
        argument = Argument.Molecule;
    }

    public UndoableChange(Atom atom, Type type)
    {
        this.atom = atom.GetCmlAtom();
        molecule = atom.m_molecule.getCmlData();
        this.type = type;
        argument = Argument.Atom;
    }

    public UndoableChange(Bond bond, Type type)
    {
        this.bond = bond.GetCmlBond();
        molecule = bond.m_molecule.getCmlData();
        this.type = type;
        argument = Argument.Bond;
    }

    public void Undo()
    {
        //TODO: manage network messages
        // TODO: deal with IDs
        if (type == Type.Create)
        {
            if (argument == Argument.Atom)
            {
                Atom a = GlobalCtrl.Singleton.findAtomWithCml(atom, molecule);
                GlobalCtrl.Singleton.deleteAtom(a, false);
            }
            else if(argument == Argument.Bond)
            {
                Bond b = GlobalCtrl.Singleton.findBondWithCml(bond, molecule);
                GlobalCtrl.Singleton.deleteBond(b, false);
            }
            else if (argument == Argument.Molecule)
            {
                Molecule mol = GlobalCtrl.Singleton.List_curMolecules[molecule.moleID];
                GlobalCtrl.Singleton.deleteMolecule(mol, false);
            }
        } 
        else if(type == Type.Destroy)
        {
            if (argument == Argument.Molecule)
            {
                GlobalCtrl.Singleton.recreateMolecule(molecule);
            }
            else if (argument == Argument.Atom)
            {

            }
            else if (argument == Argument.Bond)
            {

            }
        }
        // TODO: delete all
    }
}
