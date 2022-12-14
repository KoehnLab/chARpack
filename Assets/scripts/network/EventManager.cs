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

    private void Awake()
    {
        Singleton = this;
    }

    #region Delegates

    public delegate void CreateAtomAction(ushort id, string abbre, Vector3 pos);
    public event CreateAtomAction OnCreateAtom;
    public void CreateAtom(ushort id, string abbre, Vector3 pos)
    {
        OnCreateAtom?.Invoke(id, abbre, pos);
    }

    public delegate void MoveMoleculeAction(ushort id, Vector3 pos, Quaternion quat);
    public event MoveMoleculeAction OnMoveMolecule;
    public void MoveMolecule(ushort id, Vector3 pos, Quaternion quat)
    {
        OnMoveMolecule?.Invoke(id, pos, quat);
    }

    public delegate void MoveAtomAction(ushort id, Vector3 pos);
    public event MoveAtomAction OnMoveAtom;
    public void MoveAtom(ushort id, Vector3 pos)
    {
        OnMoveAtom?.Invoke(id, pos);
    }

    public delegate void MergeMoleculeAction(ushort atom1ID, ushort atom2ID);
    public event MergeMoleculeAction OnMergeMolecule;
    public void MergeMolecule(ushort atom1ID, ushort atom2ID)
    {
        OnMergeMolecule?.Invoke(atom1ID, atom2ID);
    }

    #endregion


}
