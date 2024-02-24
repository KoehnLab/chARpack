using chARpackStructs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteAtomAction : IUndoableAction
{
    private cmlData before;
    private List<cmlData> after;

    public DeleteAtomAction(cmlData before, List<cmlData> after)
    {
        this.before = before;
        this.after = after;
    }
    public void Execute()
    {
        // Only needed if we decide to implement a redo system
        throw new System.NotImplementedException();
    }

    public void Undo()
    {
        // Set position to that of one of the resulting molecules since
        // they might have been moved anywhere separately
        var newPos = GlobalCtrl.Singleton.Dict_curMolecules[after[0].moleID].transform.localPosition;
        foreach (cmlData molecule in after)
        {
            GlobalCtrl.Singleton.deleteMolecule(molecule.moleID, false);
        }
        before.molePos = newPos;
        GlobalCtrl.Singleton.BuildMoleculeFromCML(before, before.moleID);
    }
}
