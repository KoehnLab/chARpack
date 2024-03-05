using chARpackStructs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveMoleculeAction : IUndoableAction
{
    public cmlData before;
    public cmlData after { get; private set; }

    public MoveMoleculeAction(cmlData before, cmlData after)
    {
        this.before = before;
        this.after = after;
    }

    public MoveMoleculeAction(MoveMoleculeAction action)
    {
        this.before = action.before;
        this.after = action.after;
    }

    public void Execute()
    {
        throw new System.NotImplementedException();
    }

    public void Undo()
    {
        GlobalCtrl.Singleton.Dict_curMolecules[after.moleID].transform.localPosition = before.molePos;
    }
}
