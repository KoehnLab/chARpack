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

    public delegate void MolDataChangedAction(Molecule mol);
    public event MolDataChangedAction OnMolDataChanged;
    public void ChangeMolData(Molecule mol)
    {
        OnMolDataChanged?.Invoke(mol);
    }

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

    public delegate void MoveAtomAction(ushort mol_id, ushort atom_id, Vector3 pos);
    public event MoveAtomAction OnMoveAtom;
    public void MoveAtom(ushort mol_id, ushort atom_id, Vector3 pos)
    {
        OnMoveAtom?.Invoke(mol_id, atom_id, pos);
    }

    public delegate void MergeMoleculeAction(ushort molecule1ID, ushort atom1ID, ushort molecule2ID, ushort atom2ID);
    public event MergeMoleculeAction OnMergeMolecule;
    public void MergeMolecule(ushort molecule1ID, ushort atom1ID, ushort molecule2ID, ushort atom2ID)
    {
        OnMergeMolecule?.Invoke(molecule1ID, atom1ID, molecule2ID, atom2ID);
    }

    public delegate void LoadMoleculeAction(string name);
    public event LoadMoleculeAction OnLoadMolecule;
    public void LoadMolecule(string name)
    {
        OnLoadMolecule?.Invoke(name);
    }

    public delegate void CmlReceiveCompletedAction();
    public event CmlReceiveCompletedAction OnCmlReceiveCompleted;
    public void CmlReceiveCompleted()
    {
        OnCmlReceiveCompleted?.Invoke();
    }


    public delegate void DeleteEverythingAction();
    public event DeleteEverythingAction OnDeleteEverything;
    public void DeleteEverything()
    {
        OnDeleteEverything?.Invoke();
    }

    public delegate void SelectAtomAction(ushort mol_id, ushort atom_id, bool select_deselect);
    public event SelectAtomAction OnSelectAtom;
    public void SelectAtom(ushort mol_id, ushort atom_id, bool select_deselect)
    {
        OnSelectAtom?.Invoke(mol_id, atom_id, select_deselect);
    }

    public delegate void SelectMoleculeAction(ushort id, bool select_deselect);
    public event SelectMoleculeAction OnSelectMolecule;
    public void SelectMolecule(ushort id, bool select_deselect)
    {
        OnSelectMolecule?.Invoke(id, select_deselect);
    }

    public delegate void SelectBondAction(ushort bond_id, ushort mol_id, bool select_deselect);
    public event SelectBondAction OnSelectBond;
    public void SelectBond(ushort bond_id, ushort mol_id, bool select_deselect)
    {
        OnSelectBond?.Invoke(bond_id, mol_id, select_deselect);
    }

    public delegate void DeleteAtomAction(ushort mol_id, ushort atom_id);
    public event DeleteAtomAction OnDeleteAtom;
    public void DeleteAtom(ushort mol_id, ushort atom_id)
    {
        OnDeleteAtom?.Invoke(mol_id, atom_id);
    }

    public delegate void DeleteMoleculeAction(ushort id);
    public event DeleteMoleculeAction OnDeleteMolecule;
    public void DeleteMolecule(ushort id)
    {
        OnDeleteMolecule?.Invoke(id);
    }

    public delegate void DeleteBondAction(ushort bond_id, ushort mol_id);
    public event DeleteBondAction OnDeleteBond;
    public void DeleteBond(ushort bond_id, ushort mol_id)
    {
        OnDeleteBond?.Invoke(bond_id, mol_id);
    }

    #endregion


}
