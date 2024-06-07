using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoStack
{
    private Stack<IUndoableAction> undoStack;

    public UndoStack()
    {
        undoStack = new Stack<IUndoableAction>();
    }

    public void AddChange(IUndoableAction lastChange)
    {
        undoStack.Push(lastChange);
        if (lastChange.GetType().Equals(typeof(ScaleMoleculeAction)))
        {
            MergeChanges();
        }
    }

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            undoStack.Pop().Undo();
        }
    }

    public int getUndoStackSize()
    {
        return undoStack.Count;
    }

    private IUndoableAction GetLastChange()
    {
        return undoStack.Count > 0 ? undoStack.Pop() : null;
    }

    public void SignalUndoSlider()
    {
        //if (undoStack.Peek().GetType().Equals(typeof(ScaleMoleculeAction))) undoStack.Pop();
    }

    // Merges multiple changes of type scaleMolecule so 
    // they are put together into one instead of one every frame
    private void MergeChanges()
    {
        //if (undoStack.Peek().GetType().Equals(typeof(MoveMoleculeAction)))
        //{
        //    var lastChange = GetLastChange();
        //    var merged = new MoveMoleculeAction(lastChange as MoveMoleculeAction);
        //    while(lastChange!=null && lastChange.GetType().Equals(typeof(MoveMoleculeAction)))
        //    {
        //        if ((lastChange as MoveMoleculeAction).before.moleID != merged.before.moleID) break;
        //        merged.before = (lastChange as MoveMoleculeAction).before;
        //        lastChange = GetLastChange();
        //    }
        //    // Put the last change back onto the stack since it was not a move molecule action on the same molecule
        //    if (!lastChange.GetType().Equals(typeof(MoveMoleculeAction)) || 
        //                (lastChange as MoveMoleculeAction).before.moleID!=merged.before.moleID) undoStack.Push(lastChange);
        //    undoStack.Push(merged);
        //}
        //else
        //{
            if (!undoStack.Peek().GetType().Equals(typeof(ScaleMoleculeAction))) return;
            var lastChange = GetLastChange();
            var merged = new ScaleMoleculeAction(lastChange as ScaleMoleculeAction);
            while (lastChange != null && lastChange.GetType().Equals(typeof(ScaleMoleculeAction)))
            {
                if ((lastChange as ScaleMoleculeAction).before.moleID != merged.before.moleID) break;
                merged.before = (lastChange as ScaleMoleculeAction).before;
                lastChange = GetLastChange();
            }
            // Put the last change back onto the stack since it was not a scale molecule action
            if (!lastChange.GetType().Equals(typeof(ScaleMoleculeAction)) || 
                        (lastChange as ScaleMoleculeAction).before.moleID!=merged.before.moleID) undoStack.Push(lastChange);
            undoStack.Push(merged);
        //}
    }
}
