using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoManager : MonoBehaviour
{
    private static UndoManager _singleton;

    public static UndoManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(UndoManager)}] Instance already exists, destroying duplicate!");
                Destroy(value.gameObject);
            }

        }
    }

    public Stack<UndoableChange> undoStack;

    private void Awake()
    {
        Singleton = this;
        undoStack = new Stack<UndoableChange>();
    }

    public void Undo()
    {
        UndoableChange lastChange = undoStack.Pop();
        lastChange.Undo();
    }

    public void AddChange(UndoableChange change)
    {
        undoStack.Push(change);
    }

    public void AddChange(GameObject toChange, UndoableChange.Type type)
    {
        if (toChange.GetComponent<Molecule>()) { AddChange(new UndoableChange(toChange.GetComponent<Molecule>(), type)); }
        else if (toChange.GetComponent<Atom>()) { AddChange(new UndoableChange(toChange.GetComponent<Atom>(), type)); }
        else if (toChange.GetComponent<Bond>()) { AddChange(new UndoableChange(toChange.GetComponent<Bond>(), type)); }
        else Debug.LogError("GameObject does not have a Molecule, Atom or Bond Component!");
    }
}
