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

    public delegate void CreateAtomAction(ushort id, string abbre, Vector3 pos);
    public event CreateAtomAction OnCreateAtom;
    public void CreateAtom(ushort id, string abbre, Vector3 pos)
    {
        if (OnCreateAtom != null)
        {
            OnCreateAtom(id, abbre, pos);
        }
    }

    public delegate void MoveMoleculeAction(ushort id, Vector3 pos, Quaternion quat);
    public static event MoveMoleculeAction OnMoveMolecule;
    public void MoveMolecule(ushort id, Vector3 pos, Quaternion quat)
    {
        if (OnMoveMolecule != null)
        {
            OnMoveMolecule(id, pos, quat);
        }
    }

    public delegate void MoveAtomAction(ushort id, Vector3 pos);
    public static event MoveAtomAction OnMoveAtom;
    public void MoveAtom(ushort id, Vector3 pos)
    {
        if (OnMoveAtom != null)
        {
            OnMoveAtom(id, pos);
        }
    }

    #endregion


}
