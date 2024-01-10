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
    }

    public void Undo()
    {
        undoStack.Pop().Undo();
    }

    public int getUndoStackSize()
    {
        return undoStack.Count;
    }
}
