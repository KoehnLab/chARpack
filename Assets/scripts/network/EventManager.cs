using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{

    private static EventManager _singleton;

    public static EventManager Singleton
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
                Debug.Log($"[{nameof(EventManager)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    #region Delegates

    public delegate void CreateAtomAction(int id, string abbre, Vector3 pos);
    public event CreateAtomAction OnCreateAtom;
    public void CreateAtom(int id, string abbre, Vector3 pos)
    {
        if (OnCreateAtom != null)
        {
            OnCreateAtom(id, abbre, pos);
        }
    }

    public delegate void MoveMoleculeAction();
    public static event MoveMoleculeAction OnMoveMolecule;

    public delegate void MoveAtomAction();
    public static event MoveAtomAction OnMoveAtom;

    #endregion


}
