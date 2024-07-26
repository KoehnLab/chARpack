using System;
using UnityEngine;

/// <summary>
/// This class defines different events in the atom world that need to be synchronized between users.
/// </summary>
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

    public delegate void MolRelaxedAction(Molecule mol);
    public event MolRelaxedAction OnMolRelaxed;
    public void RelaxMol(Molecule mol)
    {
        OnMolRelaxed?.Invoke(mol);
    }

    public delegate void CreateAtomAction(Guid mol_id, string abbre, Vector3 pos, ushort hyb);
    public event CreateAtomAction OnCreateAtom;
    public void CreateAtom(Guid mol_id, string abbre, Vector3 pos, ushort hyb)
    {
        OnCreateAtom?.Invoke(mol_id, abbre, pos, hyb);
    }

    public delegate void MoveMoleculeAction(Guid id, Vector3 pos, Quaternion quat);
    public event MoveMoleculeAction OnMoveMolecule;
    public void MoveMolecule(Guid id, Vector3 pos, Quaternion quat)
    {
        OnMoveMolecule?.Invoke(id, pos, quat);
    }

    public delegate void MoveAtomAction(Guid mol_id, ushort atom_id, Vector3 pos);
    public event MoveAtomAction OnMoveAtom;
    public void MoveAtom(Guid mol_id, ushort atom_id, Vector3 pos)
    {
        OnMoveAtom?.Invoke(mol_id, atom_id, pos);
    }

    public delegate void StopMoveAtomAction(Guid mol_id, ushort atom_id);
    public event StopMoveAtomAction OnStopMoveAtom;
    public void StopMoveAtom(Guid mol_id, ushort atom_id)
    {
        OnStopMoveAtom?.Invoke(mol_id, atom_id);
    }

    public delegate void GrabAtomAction(Atom a, bool value);
    public event GrabAtomAction OnGrabAtom;
    public void GrabAtom(Atom a, bool value)
    {
        OnGrabAtom?.Invoke(a, value);
    }

    public delegate void MergeMoleculeAction(Guid molecule1ID, ushort atom1ID, Guid molecule2ID, ushort atom2ID);
    public event MergeMoleculeAction OnMergeMolecule;
    public void MergeMolecule(Guid molecule1ID, ushort atom1ID, Guid molecule2ID, ushort atom2ID)
    {
        OnMergeMolecule?.Invoke(molecule1ID, atom1ID, molecule2ID, atom2ID);
    }

    public delegate void MoleculeLoadedAction(Molecule mol);
    public event MoleculeLoadedAction OnMoleculeLoaded;
    public void MoleculeLoaded(Molecule mol)
    {
        OnMoleculeLoaded?.Invoke(mol);
    }

    public delegate void DeviceLoadMoleculeAction(string name);
    public event DeviceLoadMoleculeAction OnDeviceLoadMolecule;
    public void DeviceLoadMolecule(string name)
    {
        OnDeviceLoadMolecule?.Invoke(name);
    }

    public delegate void CmlReceiveCompletedAction();
    public event CmlReceiveCompletedAction OnCmlReceiveCompleted;
    public void CmlReceiveCompleted()
    {
        OnCmlReceiveCompleted?.Invoke();
    }

    public delegate void StructureReceiveCompletedAction(Guid mol_id);
    public event StructureReceiveCompletedAction OnStructureReceiveCompleted;
    public void StructureReceiveCompleted(Guid mol_id)
    {
        OnStructureReceiveCompleted?.Invoke(mol_id);
    }

    public delegate void DeleteEverythingAction();
    public event DeleteEverythingAction OnDeleteEverything;
    public void DeleteEverything()
    {
        OnDeleteEverything?.Invoke();
    }

    public delegate void SelectAtomAction(Guid mol_id, ushort atom_id, bool select_deselect);
    public event SelectAtomAction OnSelectAtom;
    public void SelectAtom(Guid mol_id, ushort atom_id, bool select_deselect)
    {
        OnSelectAtom?.Invoke(mol_id, atom_id, select_deselect);
    }

    public delegate void SelectMoleculeAction(Guid mol_id, bool select_deselect);
    public event SelectMoleculeAction OnSelectMolecule;
    public void SelectMolecule(Guid mol_id, bool select_deselect)
    {
        OnSelectMolecule?.Invoke(mol_id, select_deselect);
    }

    public delegate void SelectBondAction(ushort bond_id, Guid mol_id, bool select_deselect);
    public event SelectBondAction OnSelectBond;
    public void SelectBond(ushort bond_id, Guid mol_id, bool select_deselect)
    {
        OnSelectBond?.Invoke(bond_id, mol_id, select_deselect);
    }

    public delegate void DeleteAtomAction(Guid mol_id, ushort atom_id);
    public event DeleteAtomAction OnDeleteAtom;
    public void DeleteAtom(Guid mol_id, ushort atom_id)
    {
        OnDeleteAtom?.Invoke(mol_id, atom_id);
    }

    public delegate void DeleteMoleculeAction(Guid mol_id);
    public event DeleteMoleculeAction OnDeleteMolecule;
    public void DeleteMolecule(Guid mol_id)
    {
        OnDeleteMolecule?.Invoke(mol_id);
    }

    public delegate void DeleteBondAction(ushort bond_id, Guid mol_id);
    public event DeleteBondAction OnDeleteBond;
    public void DeleteBond(ushort bond_id, Guid mol_id)
    {
        OnDeleteBond?.Invoke(bond_id, mol_id);
    }

    public delegate void ChangeAtomAction(Guid mol_id, ushort atom_id, string chemAbbre);
    public event ChangeAtomAction OnChangeAtom;
    public void ChangeAtom(Guid mol_id, ushort atom_id, string chemAbbre)
    {
        OnChangeAtom?.Invoke(mol_id, atom_id, chemAbbre);
    }

    public delegate void ReplaceDummiesAction(Guid mol_id);
    public event ReplaceDummiesAction OnReplaceDummies;
    public void ReplaceDummies(Guid mol_id)
    {
        OnReplaceDummies?.Invoke(mol_id);
    }

    public delegate void UndoAction();
    public event UndoAction OnUndo;
    public void Undo()
    {
        OnUndo?.Invoke();
    }

    public delegate void enableForceFieldAction(bool enable);
    public event enableForceFieldAction OnEnableForceField;
    public void EnableForceField(bool enable)
    {
        OnEnableForceField?.Invoke(enable);
    }

    public delegate void ChangeBondTermAction(ForceField.BondTerm term, Guid mol_id, ushort term_id);
    public event ChangeBondTermAction OnChangeBondTerm;
    public void ChangeBondTerm(ForceField.BondTerm term, Guid mol_id, ushort term_id)
    {
        OnChangeBondTerm?.Invoke(term, mol_id, term_id);
    }

    public delegate void ChangeAngleTermAction(ForceField.AngleTerm term, Guid mol_id, ushort term_id);
    public event ChangeAngleTermAction OnChangeAngleTerm;
    public void ChangeAngleTerm(ForceField.AngleTerm term, Guid mol_id, ushort term_id)
    {
        OnChangeAngleTerm?.Invoke(term, mol_id, term_id);
    }

    public delegate void ChangeTorsionTermAction(ForceField.TorsionTerm term, Guid mol_id, ushort term_id);
    public event ChangeTorsionTermAction OnChangeTorsionTerm;
    public void ChangeTorsionTerm(ForceField.TorsionTerm term, Guid mol_id, ushort term_id)
    {
        OnChangeTorsionTerm?.Invoke(term, mol_id, term_id);
    }


    public delegate void MarkTermAction(ushort term_type, Guid mol_id, ushort term_id, bool marked);
    public event MarkTermAction OnMarkTerm;
    public void MarkTerm(ushort term_type, Guid mol_id, ushort term_id, bool marked)
    {
        OnMarkTerm?.Invoke(term_type, mol_id, term_id, marked);
    }

    public delegate void ModifyHybAction(Guid mol_id, ushort atom_id, ushort hyb);
    public event ModifyHybAction OnModifyHyb;
    public void ModifyHyb(Guid mol_id, ushort atom_id, ushort hyb)
    {
        OnModifyHyb?.Invoke(mol_id, atom_id, hyb);
    }

    public delegate void SetKeepConfigAction(Guid mol_id, bool keep_config);
    public event SetKeepConfigAction OnSetKeepConfig;
    public void SetKeepConfig(Guid mol_id, bool keep_config)
    {
        OnSetKeepConfig?.Invoke(mol_id, keep_config);
    }

    public delegate void UpdateSettingsAction();
    public event UpdateSettingsAction OnUpdateSettings;
    public void UpdateSettings()
    {
        OnUpdateSettings?.Invoke();
    }

    public delegate void FocusHighlightAction(Guid mol_id, ushort atom_id, bool active);
    public event FocusHighlightAction OnFocusHighlight;
    public void FocusHighlight(Guid mol_id, ushort atom_id, bool active)
    {
        OnFocusHighlight?.Invoke(mol_id, atom_id, active);
    }

    public delegate void ServerFocusHighlightAction(Guid mol_id, ushort atom_id, bool active);
    public event ServerFocusHighlightAction OnServerFocusHighlight;
    public void ServerFocusHighlight(Guid mol_id, ushort atom_id, bool active)
    {
        OnServerFocusHighlight?.Invoke(mol_id, atom_id, active);
    }

    public delegate void ChangeMoleculeScaleAction(Guid mol_id, float scale);
    public event ChangeMoleculeScaleAction OnChangeMoleculeScale;
    public void ChangeMoleculeScale(Guid mol_id, float scale)
    {
        OnChangeMoleculeScale?.Invoke(mol_id, scale);
    }

    public delegate void FreezeAtomAction(Guid mol_id, ushort atom_id, bool value);
    public event FreezeAtomAction OnFreezeAtom;
    public void FreezeAtom(Guid mol_id, ushort atom_id, bool value)
    {
        OnFreezeAtom?.Invoke(mol_id, atom_id, value);
    }

    public delegate void FreezeMoleculeAction(Guid mol_id, bool value);
    public event FreezeMoleculeAction OnFreezeMolecule;
    public void FreezeMolecule(Guid mol_id, bool value)
    {
        OnFreezeMolecule?.Invoke(mol_id, value);
    }

    public delegate void CreateMeasurementAction(Guid mol1_id, ushort atom1_id, Guid mol2_id, ushort atom2_id);
    public event CreateMeasurementAction OnCreateMeasurement;
    public void CreateMeasurement(Guid mol1_id, ushort atom1_id, Guid mol2_id, ushort atom2_id)
    {
        OnCreateMeasurement?.Invoke(mol1_id, atom1_id, mol2_id, atom2_id);
    }

    public delegate void ClearMeasurementsAction();
    public event ClearMeasurementsAction OnClearMeasurements;
    public void ClearMeasurements()
    {
        OnClearMeasurements?.Invoke();
    }

    public delegate void MRCaptureAction(ushort client_id, bool rec);
    public event MRCaptureAction OnMRCapture;
    public void MRCapture(ushort client_id, bool rec)
    {
        OnMRCapture?.Invoke(client_id, rec);
    }

    public delegate void SetSnapColorsAction(Guid mol1_id, Guid mol2_id);
    public event SetSnapColorsAction OnSetSnapColors;
    public void SetSnapColors(Guid mol1_id, Guid mol2_id)
    {
        OnSetSnapColors?.Invoke(mol1_id, mol2_id);
    }

    public delegate void SetNumOutlinesAction(int num_outlines);
    public event SetNumOutlinesAction OnSetNumOutlines;
    public void SetNumOutlines(int num_outlines)
    {
        OnSetNumOutlines?.Invoke(num_outlines);
    }

    public delegate void GrabOnScreenAction(Vector2 ss_coords, bool distant);
    public event GrabOnScreenAction OnGrabOnScreen;
    public void GrabOnScreen(Vector2 ss_coords, bool distant)
    {
        OnGrabOnScreen?.Invoke(ss_coords, distant);
    }

    public delegate void ReleaseGrabOnScreenAction();
    public event ReleaseGrabOnScreenAction OnReleaseGrabOnScreen;
    public void ReleaseGrabOnScreen()
    {
        OnReleaseGrabOnScreen?.Invoke();
    }

    public delegate void SyncModeChangedAction(TransitionManager.SyncMode mode);
    public event SyncModeChangedAction OnSyncModeChanged;
    public void ChangeSyncMode(TransitionManager.SyncMode mode)
    {
        OnSyncModeChanged?.Invoke(mode);
    }

    public delegate void HoverOverScreenAction(Vector2 ss_coords);
    public event HoverOverScreenAction OnHoverOverScreen;
    public void HoverOverScreen(Vector2 ss_coords)
    {
        OnHoverOverScreen?.Invoke(ss_coords);
    }

    public delegate void TransitionMoleculeAction(Molecule mol, TransitionManager.InteractionType triggered_by);
    public event TransitionMoleculeAction OnTransitionMolecule;
    public void TransitionMolecule(Molecule mol, TransitionManager.InteractionType triggered_by)
    {
        OnTransitionMolecule?.Invoke(mol, triggered_by);
    }

    public delegate void ReceiveMoleculeTransitionAction(Molecule mol, TransitionManager.InteractionType triggered_by);
    public event ReceiveMoleculeTransitionAction OnReceiveMoleculeTransition;
    public void ReceiveMoleculeTransition(Molecule mol, TransitionManager.InteractionType triggered_by)
    {
        OnReceiveMoleculeTransition?.Invoke(mol, triggered_by);
    }

    public delegate void TransitionGenericObjectAction(GenericObject go, TransitionManager.InteractionType triggered_by);
    public event TransitionGenericObjectAction OnTransitionGenericObject;
    public void TransitionGenericObject(GenericObject go, TransitionManager.InteractionType triggered_by)
    {
        OnTransitionGenericObject?.Invoke(go, triggered_by);
    }

    public delegate void ReceiveGenericObjectTransitionAction(GenericObject go, TransitionManager.InteractionType triggered_by);
    public event ReceiveGenericObjectTransitionAction OnReceiveGenericObjectTransition;
    public void ReceiveGenericObjectTransition(GenericObject go, TransitionManager.InteractionType triggered_by)
    {
        OnReceiveGenericObjectTransition?.Invoke(go, triggered_by);
    }

    public delegate void CreateGenericObjectAction(GenericObject go);
    public event CreateGenericObjectAction OnCreateGenericObject;
    public void CreateGenericObject(GenericObject go)
    {
        OnCreateGenericObject?.Invoke(go);
    }

    public delegate void RequestTransitionAction(TransitionManager.InteractionType triggered_by);
    public event RequestTransitionAction OnRequestTransition;
    public void RequestTransition(TransitionManager.InteractionType triggered_by)
    {
        OnRequestTransition?.Invoke(triggered_by);
    }

    public delegate void ForwardDeleteMarkedRequestAction();
    public event ForwardDeleteMarkedRequestAction OnForwardDeleteMarkedRequest;
    public void ForwardDeleteMarkedRequest()
    {
        OnForwardDeleteMarkedRequest?.Invoke();
    }

    #endregion


}
