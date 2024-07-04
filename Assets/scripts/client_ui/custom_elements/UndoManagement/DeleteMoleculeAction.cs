using chARpackStructs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteMoleculeAction : IUndoableAction
{
    private cmlData molecule;

    public DeleteMoleculeAction(cmlData molecule)
    {
        this.molecule = molecule;
    }
    public void Execute()
    {
        GlobalCtrl.Singleton.deleteMolecule(molecule.moleID);
        EventManager.Singleton.DeleteMolecule(molecule.moleID);
    }

    public void Undo()
    {
        GlobalCtrl.Singleton.BuildMoleculeFromCML(molecule, molecule.moleID);
    }
}
