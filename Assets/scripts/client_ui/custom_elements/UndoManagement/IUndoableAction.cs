using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUndoableAction
{
    public void Execute();
    public void Undo();
}
