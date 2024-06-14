using chARpackStructs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteAllAction : IUndoableAction
{
    private List<cmlData> atomWorld;

    public DeleteAllAction(List<cmlData> atomWorld)
    {
        this.atomWorld = atomWorld;
    }
    public void Execute()
    {
        GlobalCtrl.Singleton.DeleteAll();
    }

    public void Undo()
    {
        // Preserve previous IDs
        GlobalCtrl.Singleton.createFromCML(atomWorld, false);
    }
}
