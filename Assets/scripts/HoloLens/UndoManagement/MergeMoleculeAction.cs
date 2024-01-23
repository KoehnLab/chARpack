using StructClass;
using System;
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
        var meanPos = Vector3.zero;
        foreach(var molecule in before)
        {
            meanPos += molecule.molePos;
        }
        meanPos /= before.Count;
        // Adapt position
        for (int i = 0; i < before.Count; i++)
        {
            cmlData molecule = before[i];
            var offset = molecule.molePos - meanPos;
            molecule.molePos = GlobalCtrl.Singleton.Dict_curMolecules[after.moleID].transform.localPosition + offset;
            before[i] = molecule;
        }
        GlobalCtrl.Singleton.deleteMolecule(after.moleID, false);
        foreach(cmlData molecule in before)
        {
            GlobalCtrl.Singleton.BuildMoleculeFromCML(molecule, molecule.moleID);
        }
    }
}
