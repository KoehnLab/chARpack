using StructClass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteBondAction : IUndoableAction
{
    private cmlData before;
    private List<cmlData> after;

    public DeleteBondAction(cmlData before, List<cmlData> after)
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
        var newPos = GlobalCtrl.Singleton.Dict_curMolecules[after[0].moleID].transform.position;
        foreach(cmlData molecule in after)
        {
            GlobalCtrl.Singleton.deleteMolecule(molecule.moleID, false);
        }
        before.molePos = newPos;
        GlobalCtrl.Singleton.BuildMoleculeFromCML(before, before.moleID);
    }
}
