using StructClass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeMoleculeAction : IUndoableAction
{
    private List<cmlData> before;
    private cmlData after;

    public MergeMoleculeAction(List<cmlData> before, cmlData after)
    {
        this.before = before;
        this.after = after;
    }
    public void Execute()
    {
        // Only needed if we decide to implement redo
        throw new System.NotImplementedException();
    }

    public void Undo()
    {
        // Adapt position
        for (int i = 0; i < before.Count; i++)
        {
            cmlData molecule = before[i];
            molecule.molePos = GlobalCtrl.Singleton.Dict_curMolecules[after.moleID].transform.position;
            before[i] = molecule;
        }
        GlobalCtrl.Singleton.deleteMolecule(after.moleID, false);
        foreach(cmlData molecule in before)
        {
            GlobalCtrl.Singleton.BuildMoleculeFromCML(molecule, molecule.moleID);
        }
    }
}
