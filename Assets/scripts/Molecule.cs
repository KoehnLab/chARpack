using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using StructClass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class Molecule : MonoBehaviour, IMixedRealityPointerHandler
{
    private Stopwatch stopwatch;
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        stopwatch = Stopwatch.StartNew();
        // change material of grabbed object
        GetComponent<myBoundingBox>().setGrabbed(true);

    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        // Intentionally empty
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        // keep everything relative to atom world
        EventManager.Singleton.MoveMolecule(m_id, transform.localPosition, transform.localRotation);
    }

    // This function is triggered when a grabbed object is dropped
    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        stopwatch?.Stop();
        if (stopwatch?.ElapsedMilliseconds < 200)
        {
            markMoleculeUI(!isMarked, true);
        }
        if (GlobalCtrl.Singleton.collision)
        {
            Atom d1 = GlobalCtrl.Singleton.collider1;
            Atom d2 = GlobalCtrl.Singleton.collider2;

            Atom a1 = d1.dummyFindMain();
            Atom a2 = d2.dummyFindMain();

            if (!a1.alreadyConnected(a2))
            {
                EventManager.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1.m_molecule.m_id, GlobalCtrl.Singleton.collider1.m_id, GlobalCtrl.Singleton.collider2.m_molecule.m_id, GlobalCtrl.Singleton.collider2.m_id);
                GlobalCtrl.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1, GlobalCtrl.Singleton.collider2);
            }

        }
        // change material back to normal
        GetComponent<myBoundingBox>().setGrabbed(false);
    }

    //private void HandleOnManipulationStarted(ManipulationEventData eventData)
    //{
    //    var pointer = eventData.Pointer;


    //    UnityEngine.Debug.Log("[Molecule] Manipulation started");

    //    // whatever shall happen when manipulation started
    //}

    [HideInInspector] public static GameObject myToolTipPrefab;
    [HideInInspector] public static GameObject deleteMeButtonPrefab;
    [HideInInspector] public static GameObject closeMeButtonPrefab;
    [HideInInspector] public static GameObject modifyMeButtonPrefab;
    [HideInInspector] public static GameObject toggleDummiesButtonPrefab;
    [HideInInspector] public static GameObject undoButtonPrefab;
    [HideInInspector] public static GameObject changeBondWindowPrefab;
    public GameObject toolTipInstance;
    private float toolTipDistanceWeight = 0.01f;

    /// <summary>
    /// molecule id
    /// </summary>
    private ushort _id;
    public ushort m_id { get { return _id; } set { _id = value; name = "molecule_" + value; } }


    public bool isMarked;
    /// <summary>
    /// atom list contains all atoms which belong to this molecule
    /// </summary>
    public List<Atom> atomList { get; private set; }
    /// <summary>
    /// bond list contains all bonds which belong to this molecule
    /// </summary>
    public List<Bond> bondList { get; private set; }

    /// <summary>
    /// this method initialises a new molecule, it is called when a new atom with it's dummies is created from scratch
    /// </summary>
    /// <param name="idInScene">the ID in the scene o the molecule</param>
    /// <param name="inputParent"> the parent of the molecule</param>
    public void f_Init(ushort idInScene, Transform inputParent, cmlData mol_data = new cmlData())
    {
        m_id = idInScene;
        isMarked = false;
        transform.parent = inputParent;
        atomList = new List<Atom>();
        bondList = new List<Bond>();
        // TODO put collider into a corner
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.001f, 0.001f, 0.001f);
        // these objects take input from corner colliders and manipulate the moluecule
        var om = gameObject.AddComponent<ObjectManipulator>();
        //om.OnManipulationStarted.AddListener(HandleOnManipulationStarted);

        gameObject.AddComponent<NearInteractionGrabbable>();

        if (mol_data.keepConfig)
        {
            bondTerms.Clear();
            angleTerms.Clear();
            torsionTerms.Clear();
            if (mol_data.bondArray != null)
            {
                foreach (var current_bond in mol_data.bondArray)
                {
                    var new_bond = new ForceField.BondTerm();
                    new_bond.Atom1 = current_bond.id1;
                    new_bond.Atom2 = current_bond.id2;
                    new_bond.eqDist = current_bond.eqDist;
                    new_bond.kBond = current_bond.kb > 0 ? current_bond.kb : ForceField.kb;
                }
            }
            if (mol_data.angleArray != null)
            {
                foreach (var current_angle in mol_data.angleArray)
                {
                    var new_angle = new ForceField.AngleTerm();
                    new_angle.Atom1 = current_angle.id1;
                    new_angle.Atom2 = current_angle.id2;
                    new_angle.Atom3 = current_angle.id3;
                    new_angle.eqAngle = current_angle.angle;
                    new_angle.kAngle = current_angle.ka > 0 ? current_angle.ka : ForceField.ka;
                }
            }
            if (mol_data.torsionArray != null)
            {
                foreach (var current_torsion in mol_data.torsionArray)
                {
                    var new_torsion = new ForceField.TorsionTerm();
                    new_torsion.Atom1 = current_torsion.id1;
                    new_torsion.Atom2 = current_torsion.id2;
                    new_torsion.Atom3 = current_torsion.id3;
                    new_torsion.Atom4 = current_torsion.id4;
                    new_torsion.eqAngle = current_torsion.angle;
                    new_torsion.nn = 1;
                    new_torsion.vk = current_torsion.k0 > 0 ? current_torsion.k0 : 0.01f * ForceField.k0;
                }
            }
        }

        EventManager.Singleton.OnMolDataChanged += triggerGenerateFF;
        EventManager.Singleton.OnMolDataChanged += adjustBBox;
    }

    private void adjustBBox(Molecule mol)
    {
        if (mol == this)
        {
            if (GlobalCtrl.Singleton.List_curMolecules.Contains(mol))
            {
                StartCoroutine(adjustBBoxCoroutine());
            }
        }
    }

    // Need coroutine to use sleep
    private IEnumerator adjustBBoxCoroutine()
    {
        yield return new WaitForSeconds(0.1f);
        var current_size = getLongestBBoxEdge();
        GetComponent<myBoundingBox>().scaleCorners(0.02f + 0.02f * current_size);
        if (current_size > 0.25f)
        {
            GetComponent<myBoundingBox>().setNormalMaterial(false);
        }
        else
        {
            GetComponent<myBoundingBox>().setNormalMaterial(true);
        }

    }

    /// <summary>
    /// if two molecules are merged, all atoms from the old molecule need to be transferred to the new molecule
    /// </summary>
    /// <param name="newParent"> the molecule which is the new parent to all atoms</param>
    public void givingOrphans(Molecule newParent)
    {
        ushort maxID = newParent.getFreshAtomID();
        foreach (Atom a in atomList)
        {
            a.transform.parent = newParent.transform;
            a.m_molecule = newParent;
            a.m_id += maxID;
            newParent.atomList.Add(a);
        }
        foreach (Bond b in bondList)
        {
            b.transform.parent = newParent.transform;
            b.m_molecule = newParent;
            b.atomID1 += maxID;
            b.atomID2 += maxID;
            newParent.bondList.Add(b);
        }
        GlobalCtrl.Singleton.List_curMolecules.Remove(this);
        Destroy(gameObject);
    }


    public void markMolecule(bool mark, bool showToolTip = false)
    {
        if (showToolTip && toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
        foreach (Atom a in atomList)
        {
            a.markAtom(mark);
        }

        foreach (Bond b in bondList)
        {
            b.markBond(mark);
        }
        isMarked = mark;
        if (!mark)
        {
            if (toolTipInstance)
            {
                Destroy(toolTipInstance);
                toolTipInstance = null;
            }
        } 
        else
        {
            if (!toolTipInstance && showToolTip)
            {
                createToolTip();
            }
        }
    }

    public void markMoleculeUI(bool mark, bool showToolTip = true)
    {
        EventManager.Singleton.SelectMolecule(m_id, !isMarked);
        markMolecule(mark, showToolTip);
    }

    /// <summary>
    /// all dummys are replaced by hydrogens
    /// </summary>
    public void toggleDummies()
    {
        var dummyCount = countAtoms("Dummy");
        var hydrogenCount = countAtoms("H");
        if (dummyCount >= hydrogenCount)
        {
            foreach (Atom a in atomList)
            {
                if (a.m_data.m_abbre == "Dummy")
                {
                    GlobalCtrl.Singleton.switchDummyHydrogen(m_id, a.m_id);
                }
            }
        } 
        else
        {
            foreach (Atom a in atomList)
            {
                if (a.m_data.m_abbre == "H")
                {
                    GlobalCtrl.Singleton.switchDummyHydrogen(m_id, a.m_id, false);
                }
            }
        }
        GlobalCtrl.Singleton.SaveMolecule(true);
        EventManager.Singleton.ChangeMolData(this);
        EventManager.Singleton.ReplaceDummies(m_id);
    }

    private int countAtoms(String name)
    {
        int atomCount = 0;
        foreach (Atom a in atomList)
        {
            if (a.m_data.m_abbre == name)
            {
                atomCount++;
            }
        }
        return atomCount;
    }

    public Vector3 getCenter()
    {
        Vector3 center = new Vector3(0.0f, 0.0f, 0.0f);
        int num_atoms = atomList.Count;

        foreach (Atom atom in atomList)
        {
            center += atom.transform.position;
        }
        center /= num_atoms > 0 ? num_atoms : 1;

        return center;
    }

    public float getMaxDistFromCenter(Vector3 center)
    {
        List<float> dists = new List<float>();

        foreach (Atom atom in atomList)
        {
            Vector3 atom_pos = atom.transform.position;
            dists.Add(Mathf.Sqrt(center[0]*atom_pos[0] + center[1] * atom_pos[1] + center[2] * atom_pos[2]));
        }

        float max_dist = 0.0f;
        foreach (float dist in dists)
        {
            max_dist = Mathf.Max(max_dist, dist);
        }

        return max_dist;
    }

    public float getLongestBBoxEdge()
    {
        return GetComponent<myBoundingBox>().getSize().maxDimValue();
    }

    private void calcMetaData(ref float mass)
    {
        // calc total mass
        float tot_mass = 0.0f;
        foreach (var atom in atomList)
        {
            tot_mass += atom.m_data.m_mass;
        }
        mass = tot_mass;

    }

    #region ToolTips

    private void createToolTip()
    {
        // create tool tip
        toolTipInstance = Instantiate(myToolTipPrefab);
        // put tool top to the right 
        Vector3 ttpos = transform.position + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.right + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add atom as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = gameObject;
        // calc some meta data to show
        float tot_mass = 0.0f;
        calcMetaData(ref tot_mass);
        var mol_center = getCenter();
        var max_dist = getMaxDistFromCenter(mol_center);
        string toolTipText = getAtomToolTipText(tot_mass,max_dist);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;
        var keepConfigSwitchButtonInstance = Instantiate(modifyMeButtonPrefab);
        keepConfigSwitchButtonInstance.GetComponent<ButtonConfigHelper>().MainLabelText = "keepConfig";
        keepConfigSwitchButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.toggleKeepConfigUI(this); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(keepConfigSwitchButtonInstance);
        var toggleDummiesButtonInstance = Instantiate(toggleDummiesButtonPrefab);
        toggleDummiesButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { toggleDummies(); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(toggleDummiesButtonInstance);
        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markMoleculeUI(false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);

        // making sure the delete and close buttons are not too close together; has to be improved
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(new GameObject());

        var delButtonInstance = Instantiate(deleteMeButtonPrefab);
        delButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.deleteMoleculeUI(this); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(delButtonInstance);

    }

    public void createBondToolTip(ForceField.BondTerm term)
    {
        markBondTerm(term, true);
        var bond = atomList[term.Atom1].getBond(atomList[term.Atom2]);
        // create tool tip
        toolTipInstance = Instantiate(myToolTipPrefab);
        // calc position for tool tip
        // first: get position in the bounding box and decide if the tool tip spawns left, right, top or bottom of the box
        Vector3 mol_center = getCenter();
        // project to camera coordnates
        Vector2 mol_center_in_cam = new Vector2(Vector3.Dot(mol_center, GlobalCtrl.Singleton.currentCamera.transform.right), Vector3.Dot(mol_center, GlobalCtrl.Singleton.currentCamera.transform.up));
        Vector2 atom_pos_in_cam = new Vector2(Vector3.Dot(transform.position, GlobalCtrl.Singleton.currentCamera.transform.right), Vector3.Dot(transform.position, GlobalCtrl.Singleton.currentCamera.transform.up));
        // calc diff
        Vector2 diff_mol_atom = atom_pos_in_cam - mol_center_in_cam;
        // enhance diff for final tool tip pos
        Vector3 ttpos = transform.position + toolTipDistanceWeight * diff_mol_atom[0] * GlobalCtrl.Singleton.currentCamera.transform.right + toolTipDistanceWeight * diff_mol_atom[1] * GlobalCtrl.Singleton.currentCamera.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add bond as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = bond.gameObject;
        // show meta data 
        var atom1 = atomList.ElementAtOrDefault(term.Atom1);
        var atom2 = atomList.ElementAtOrDefault(term.Atom2);
        string toolTipText = getBondToolTipText(term.eqDist, term.kBond, term.order);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;

        var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
        modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { createChangeBondWindow(term); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);

        var delButtonInstance = Instantiate(deleteMeButtonPrefab);
        delButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.deleteBondUI(bond); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(delButtonInstance);

        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markBondTermUI(term, false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);
    }

    private void createChangeBondWindow(ForceField.BondTerm bond)
    {
        var changeBondWindowInstance = Instantiate(changeBondWindowPrefab);
        var cb = changeBondWindowInstance.GetComponent<ChangeBond>();
        cb.bt = bond;
        var id = bondTerms.IndexOf(bond);
        cb.okButton.GetComponent<Button>().onClick.AddListener(delegate { changeBondParametersUI(changeBondWindowInstance, id); });
    }

    private void changeBondParametersUI(GameObject windowInstance, int id)
    {
        var cb = windowInstance.GetComponent<ChangeBond>();
        cb.changeBondParametersBT();
        var bt = cb.bt;
        // Update tool tip
        string toolTipText = getBondToolTipText(bt.eqDist, bt.kBond, bt.order);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;

        changeBondParameters(bt, id);
        EventManager.Singleton.ChangeBondTerm(bt, m_id, (ushort)id);

        Destroy(windowInstance);

    }

    public void changeBondParameters(ForceField.BondTerm bond, int id)
    {        
        // Update real term
        bondTerms[id] = bond;
        // unmark bond
        markBondTerm(bond, false);
    }

    public void markBondTermUI(ForceField.BondTerm term, bool mark)
    {
        markBondTerm(term, mark);
        EventManager.Singleton.MarkTerm(0, m_id, (ushort)bondTerms.FindIndex(t => t.Equals(term)), mark);
    }

    public void markBondTerm(ForceField.BondTerm term, bool mark)
    {
        atomList[term.Atom1].markAtom(mark,3);
        atomList[term.Atom2].markAtom(mark,3);
        atomList[term.Atom1].getBond(atomList[term.Atom2])?.markBond(mark,3);

        if (toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
    }



    public void createAngleToolTip(ForceField.AngleTerm term)
    {
        markAngleTerm(term, true);
        var middleAtom = atomList[term.Atom2];
        // create tool tip
        toolTipInstance = Instantiate(myToolTipPrefab);
        // put tool top to the right 
        Vector3 ttpos = middleAtom.transform.position + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.right + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add atom as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = middleAtom.gameObject;
        // show angle term data
        string toolTipText = getAngleToolTipText(term.eqAngle, term.kAngle);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;

        var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
        modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { createChangeAngleWindow(term); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);

        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markAngleTermUI(term, false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);
    }

    private void createChangeAngleWindow(ForceField.AngleTerm bond)
    {
        var changeBondWindowInstance = Instantiate(changeBondWindowPrefab);
        var cb = changeBondWindowInstance.GetComponent<ChangeBond>();
        cb.at = bond;
        var id = angleTerms.IndexOf(bond);
        cb.okButton.GetComponent<Button>().onClick.AddListener(delegate { changeAngleParametersUI(changeBondWindowInstance, id); });
    }

    private void changeAngleParametersUI(GameObject windowInstance, int id)
    {
        var cb = windowInstance.GetComponent<ChangeBond>();
        cb.changeBondParametersAT();
        var at = cb.at;
        // Update tool tip
        string toolTipText = getAngleToolTipText(at.eqAngle, at.kAngle);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;

        changeAngleParameters(at, id);
        EventManager.Singleton.ChangeAngleTerm(at, m_id, (ushort)id);

        Destroy(windowInstance);

    }

    public void changeAngleParameters(ForceField.AngleTerm angle, int id)
    {
        // Update real term
        angleTerms[id] = angle;
        // unmark term
        markAngleTerm(angle, false);
    }

    public void markAngleTermUI(ForceField.AngleTerm term, bool mark)
    {
        markAngleTerm(term, mark);
        EventManager.Singleton.MarkTerm(1, m_id, (ushort)angleTerms.FindIndex(t => t.Equals(term)), mark);
    }

    public void markAngleTerm(ForceField.AngleTerm term, bool mark)
    {
        atomList[term.Atom1].markAtom(mark,4);
        atomList[term.Atom2].markAtom(mark,4);
        atomList[term.Atom3].markAtom(mark,4);
        atomList[term.Atom1].getBond(atomList[term.Atom2])?.markBond(mark,4);
        atomList[term.Atom2].getBond(atomList[term.Atom3])?.markBond(mark,4);

        if (toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
    }


    public void createTorsionToolTip(ForceField.TorsionTerm term)
    {
        markTorsionTerm(term, true);
        var middlebond = atomList[term.Atom2].getBond(atomList[term.Atom3]);
        // create tool tip
        toolTipInstance = Instantiate(myToolTipPrefab);
        // put tool top to the right 
        Vector3 ttpos = middlebond.transform.position + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.right + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add atom as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = middlebond.gameObject;
        // show angle term data
        string toolTipText = getTorsionToolTipText(term.eqAngle, term.vk, term.nn);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;

        var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
        modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { createChangeTorsionWindow(term); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);

        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markTorsionTermUI(term, false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);

    }

    private void createChangeTorsionWindow(ForceField.TorsionTerm bond)
    {
        var changeBondWindowInstance = Instantiate(changeBondWindowPrefab);
        var cb = changeBondWindowInstance.GetComponent<ChangeBond>();
        cb.tt = bond;
        var id = torsionTerms.IndexOf(bond);
        cb.okButton.GetComponent<Button>().onClick.AddListener(delegate { changeTorsionParametersUI(changeBondWindowInstance, id); });
    }

    private void changeTorsionParametersUI(GameObject windowInstance, int id)
    {
        var cb = windowInstance.GetComponent<ChangeBond>();
        cb.changeBondParametersTT();
        var tt = cb.tt;
        // Update tool tip
        string toolTipText = getTorsionToolTipText(tt.eqAngle, tt.vk, tt.nn);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;

        changeTorsionParameters(tt, id);
        EventManager.Singleton.ChangeTorsionTerm(tt, m_id, (ushort)id);

        Destroy(windowInstance);
    }

    public void changeTorsionParameters(ForceField.TorsionTerm torsion, int id)
    {
        // Update real term
        torsionTerms[id] = torsion;
        // unmark torsion
        markTorsionTerm(torsion, false);
    }

    public void markTorsionTermUI(ForceField.TorsionTerm term, bool mark)
    {
        markTorsionTerm(term, mark);
        EventManager.Singleton.MarkTerm(2, m_id, (ushort)torsionTerms.FindIndex(t => t.Equals(term)), mark);
    }

    public void markTorsionTerm(ForceField.TorsionTerm term, bool mark)
    {
        atomList[term.Atom1].markAtom(mark,5);
        atomList[term.Atom2].markAtom(mark,5);
        atomList[term.Atom3].markAtom(mark,5);
        atomList[term.Atom4].markAtom(mark,5);
        atomList[term.Atom1].getBond(atomList[term.Atom2])?.markBond(mark,5);
        atomList[term.Atom2].getBond(atomList[term.Atom3])?.markBond(mark,5);
        atomList[term.Atom3].getBond(atomList[term.Atom4])?.markBond(mark,5);

        if (toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
    }

    // Helper methods to generate localized tool tip text
    private string getAtomToolTipText(double totMass, double maxDist)
    {
        string numAtoms = GetLocalizedString("NUM_ATOMS");
        string numBonds = GetLocalizedString("NUM_BONDS");
        string mass = GetLocalizedString("TOT_MASS");
        string toolTipText = $"{numAtoms}: {atomList.Count}\n{numBonds}: {bondList.Count}\n{mass}: {totMass.ToString("0.00")}\nMaxRadius: {maxDist.ToString("0.00")}";
        return toolTipText;
    }

    private string getBondToolTipText(double eqDist, double kBond, double order)
    {
        string dist = GetLocalizedString("EQ_DIST");
        string singleBond = GetLocalizedString("SINGLE_BOND");
        string ord = GetLocalizedString("ORDER");
        string toolTipText = $"{singleBond}\n{dist}: {eqDist}\nk: {kBond}\n{ord}: {order}";
        return toolTipText;
    }

    private string getAngleToolTipText(double eqAngle, double kAngle)
    {
        string angleBond = GetLocalizedString("ANGLE_BOND");
        string eqAngleStr = GetLocalizedString("EQUI_ANGLE");
        string kAngleStr = GetLocalizedString("K_ANGLE");
        string toolTipText = $"{angleBond}\n{eqAngleStr}: {eqAngle}\n{kAngleStr}: {kAngle}";
        return toolTipText;
    }
    
    private string getTorsionToolTipText(double eqAngle, double vk, double nn)
    {
        //$"Torsion Bond\nEqui. Angle: {term.eqAngle}\nvk: {term.vk}\nnn: {term.nn}"
        string torsionBond = GetLocalizedString("TORSION_BOND");
        string eqAngleStr = GetLocalizedString("EQUI_ANGLE");
        string toolTipText = $"{torsionBond}\n{eqAngleStr}: {eqAngle}\nvk: {vk}\nnn: {nn}";
        return toolTipText;
    }

    public string GetLocalizedString(string text)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString("My Strings", text);
    }

    #endregion

    #region id_management
    /// <summary>
    /// this method gets the maximum atomID currently in the scene
    /// </summary>
    /// <returns>id</returns>
    public ushort getMaxAtomID()
    {
        ushort id = 0;
        if (atomList.Count > 0)
        {
            foreach (Atom a in atomList)
            {
                id = Math.Max(id, a.m_id);
            }
        }
        return id;
    }

    /// <summary>
    /// this method shrinks the IDs of the atoms to prevent an overflow
    /// </summary>
    public void shrinkAtomIDs()
    {
        var from = new List<ushort>();
        var to = new List<ushort>();
        var bonds = new List<Bond>();
        for (ushort i = 0; i < atomList.Count; i++)
        {
            // also change ids in bond
            if (atomList[i].m_id != i)
            {
                from.Add(atomList[i].m_id);
                to.Add(i);
                foreach (var bond in atomList[i].connectedBonds())
                {
                    if (!bonds.Contains(bond))
                    {
                        bonds.Add(bond);
                    }
                }

            }
            atomList[i].m_id = i;
        }
        foreach (var bond in bonds)
        {
            if (from.Contains(bond.atomID1))
            {
                bond.atomID1 = to[from.FindIndex(a => a == bond.atomID1)];
            }
            if (from.Contains(bond.atomID2))
            {
                bond.atomID2 = to[from.FindIndex(a => a == bond.atomID2)];
            }
        }
        // DEBUG
        //for (ushort i = 0; i < atomList.Count; i++)
        //{
        //    UnityEngine.Debug.Log($"[Molecule:shrinkAtomIDs] list ID {i} atom ID {atomList[i].m_id}");
        //}
    }

    /// <summary>
    /// gets a fresh available atom id
    /// </summary>
    /// <param name="idNew">new ID</param>
    public ushort getFreshAtomID()
    {
        if (atomList.Count == 0)
        {
            return 0;
        }
        else
        {
            shrinkAtomIDs();
            return (ushort)(getMaxAtomID() + 1);
        }
    }
    #endregion

    #region ForceField

    public List<Vector3> FFposition = new List<Vector3>();
    public List<Vector3> FFlastPosition = new List<Vector3>();
    public List<Vector3> FFlastlastPosition = new List<Vector3>();
    public List<Vector3> FFvelocity = new List<Vector3>();
    public List<Vector3> FFforces = new List<Vector3>();
    public List<Vector3> FFforces_pass2 = new List<Vector3>();
    public List<Vector3> FFmovement = new List<Vector3>();
    public List<Vector3> FFtimeStep = new List<Vector3>();

    public List<ForceField.BondTerm> bondTerms = new List<ForceField.BondTerm>();
    public List<ForceField.AngleTerm> angleTerms = new List<ForceField.AngleTerm>();
    public List<ForceField.TorsionTerm> torsionTerms = new List<ForceField.TorsionTerm>();
    public List<ForceField.HardSphereTerm> hsTerms = new List<ForceField.HardSphereTerm>();

    private void triggerGenerateFF(Molecule mol)
    {
        if (mol == this)
        {
            generateFF();
        }
    }

    public void generateFF()
    {

        // Clear lists beforehand
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {
            mol.FFposition.Clear();
            mol.FFlastPosition.Clear();
            mol.FFlastlastPosition.Clear();
            mol.FFvelocity.Clear();
            mol.FFforces.Clear();
            mol.FFforces_pass2.Clear();
            mol.FFmovement.Clear();
            mol.FFtimeStep.Clear();
            foreach (var a in mol.atomList)
            {
                mol.FFposition.Add(a.transform.position * (1f / ForceField.scalingfactor));
                mol.FFvelocity.Add(Vector3.zero);
                mol.FFforces.Add(Vector3.zero);
                mol.FFforces_pass2.Add(Vector3.zero);
                mol.FFmovement.Add(Vector3.zero);
                mol.FFtimeStep.Add(Vector3.one * ForceField.Singleton.RKtimeFactor);
            }
            mol.FFlastPosition = mol.FFposition;
            mol.FFlastlastPosition = mol.FFposition;
        }

        bondTerms.Clear();
        angleTerms.Clear();
        hsTerms.Clear();
        torsionTerms.Clear();

        var num_atoms = atomList.Count;
        //UnityEngine.Debug.LogError($"[Molecule:generateFF] num num_atoms {num_atoms}");
        // set topology array       
        bool[,] topo = new bool[num_atoms, num_atoms];
        for (int iAtom = 0; iAtom < num_atoms; iAtom++)
        {
            for (int jAtom = 0; jAtom < num_atoms; jAtom++)
            {
                topo[iAtom, jAtom] = false;
            }
        }

        {
            int iAtom = 0;
            foreach (Atom At1 in atomList)
            {
                if (At1 != null)
                {
                    // cycle through connection points
                    // ConnectionStatus does not exist anymore, instead use Atom.connectedAtoms(); this returns a List of all directly connected Atoms
                    //UnityEngine.Debug.LogError($"[Molecule:generateFF] num connections {At1.connectedAtoms().Count}"); 
                    foreach (Atom conAtom in At1.connectedAtoms())
                    {
                        int jAtom = conAtom.m_id;
                        if (jAtom >= 0)
                        {
                            //UnityEngine.Debug.LogError($"[Molecule:generateFF] num_atoms {num_atoms}; iAtom {iAtom}; jAtom {jAtom}");
                            topo[iAtom, jAtom] = true;
                            topo[jAtom, iAtom] = true;
                        }
                    }
                    iAtom++;
                }
            }
        }

        var nBondP = new List<int>(num_atoms);
        for (int iAtom = 0; iAtom < num_atoms; iAtom++)
        {
            int nBondingPartner = 0;
            for (int jAtom = 0; jAtom < num_atoms; jAtom++)
            {
                if (topo[iAtom, jAtom]) nBondingPartner++;
            }
            nBondP.Add(nBondingPartner);
        }

        // now set all FF terms
        // pairwise terms, run over unique atom pairs
        for (ushort iAtom = 0; iAtom < num_atoms; iAtom++)
        {
            for (ushort jAtom = 0; jAtom < iAtom; jAtom++)
            {
                if (topo[iAtom, jAtom])
                {
                    ForceField.BondTerm newBond = new ForceField.BondTerm();
                    newBond.Atom1 = jAtom;
                    newBond.Atom2 = iAtom;

                    string key1 = string.Format("{0}_{1}", atomList[jAtom].m_data.m_abbre, atomList[jAtom].m_data.m_hybridization);
                    string key2 = string.Format("{0}_{1}", atomList[iAtom].m_data.m_abbre, atomList[iAtom].m_data.m_hybridization);

                    float[] value1;
                    float[] value2;

                    float R01 = ForceField.DREIDINGConst.TryGetValue(key1, out value1) ? value1[0] : 70f;
                    float R02 = ForceField.DREIDINGConst.TryGetValue(key2, out value2) ? value2[0] : 70f;

                    var dreiding_eqDist = R01 + R02 - 1f;

                    if (atomList[iAtom].keepConfig && atomList[jAtom].keepConfig)
                    {
                        var currentDist = (FFposition[iAtom] - FFposition[jAtom]).magnitude;
                        if (currentDist.approx(0.0f, 0.00001f))
                        {
                            newBond.eqDist = dreiding_eqDist;
                        }
                        else
                        {
                            newBond.eqDist = currentDist;
                        }
                        //UnityEngine.Debug.Log($"[Molecule:generateFF] keepConfig - Single Req: {newBond.eqDist}");
                    }
                    else
                    {
                        newBond.eqDist = dreiding_eqDist;
                        //UnityEngine.Debug.Log($"[Molecule:generateFF] Eq dist: {newBond.eqDist}");
                    }
                    newBond.kBond = ForceField.kb;
                    // TODO estimate bond order from equilibrium distance
                    newBond.order = 1.0f;

                    bondTerms.Add(newBond);
                }
                else if (atomList[iAtom].m_data.m_abbre != "Dummy" && atomList[jAtom].m_data.m_abbre != "Dummy")  // avoid dummy terms right away
                {
                    bool avoid = false;
                    // check for next-nearest neighborhood (1-3 interaction)
                    for (int kAtom = 0; kAtom < num_atoms; kAtom++)
                    {
                        if (topo[iAtom, kAtom] && topo[jAtom, kAtom])
                        {
                            avoid = true; break;
                        }
                    }

                    if (!avoid)
                    {
                        ForceField.HardSphereTerm newHS = new ForceField.HardSphereTerm();
                        newHS.Atom1 = jAtom;
                        newHS.Atom2 = iAtom;
                        newHS.kH = 10f;
                        newHS.Rcrit = ForceField.rhs[atomList[iAtom].m_data.m_abbre] + ForceField.rhs[atomList[jAtom].m_data.m_abbre];
                        hsTerms.Add(newHS);
                    }
                }
            }

        }


        // angle terms
        // run over unique bond pairs
        foreach (ForceField.BondTerm bond1 in bondTerms)
        {
            foreach (ForceField.BondTerm bond2 in bondTerms)
            {
                // if we reached the same atom pair, we can skip
                if (bond1.Atom1 == bond2.Atom1 && bond1.Atom2 == bond2.Atom2) break;

                int idx = -1, jdx = -1, kdx = -1;
                if (bond1.Atom1 == bond2.Atom1)
                {
                    idx = bond1.Atom2; jdx = bond1.Atom1; kdx = bond2.Atom2;
                }
                else if (bond1.Atom1 == bond2.Atom2)
                {
                    idx = bond1.Atom2; jdx = bond1.Atom1; kdx = bond2.Atom1;
                }
                else if (bond1.Atom2 == bond2.Atom1)
                {
                    idx = bond1.Atom1; jdx = bond1.Atom2; kdx = bond2.Atom2;
                }
                else if (bond1.Atom2 == bond2.Atom2)
                {
                    idx = bond1.Atom1; jdx = bond1.Atom2; kdx = bond2.Atom1;
                }
                if (idx > -1) // if anything was found: set term
                {
                    ForceField.AngleTerm newAngle = new ForceField.AngleTerm();
                    newAngle.Atom1 = (ushort)kdx;  // I put kdx->Atom1 and idx->Atom3 just for aesthetical reasons ;)
                    newAngle.Atom2 = (ushort)jdx;
                    newAngle.Atom3 = (ushort)idx;

                    float phi0;
                    if (atomList[newAngle.Atom1].keepConfig && atomList[newAngle.Atom2].keepConfig && atomList[newAngle.Atom3].keepConfig)
                    {
                        var vec1 = FFposition[newAngle.Atom3] - FFposition[newAngle.Atom2];
                        var vec2 = FFposition[newAngle.Atom1] - FFposition[newAngle.Atom2];
                        phi0 = Mathf.Acos(Vector3.Dot(vec1.normalized, vec2.normalized)) * Mathf.Rad2Deg;
                        //UnityEngine.Debug.Log($"[Molecule:generateFF] keepConfig - Angle phi: {phi0}");
                    }
                    else
                    {
                        float[] value;
                        string key = string.Format("{0}_{1}", atomList[jdx].m_data.m_abbre, atomList[jdx].m_data.m_hybridization);

                        if (ForceField.DREIDINGConst.TryGetValue(key, out value))
                        {
                            phi0 = value[1];
                        }
                        else
                        {
                            phi0 = ForceField.alphaNull;
                            //ForceFieldConsole.Instance.statusOut(string.Format("Warning {0} : unknown atom or hybridization", key));
                        }
                    }


                    if (!Mathf.Approximately(phi0, 180f))
                    {
                        newAngle.kAngle = ForceField.ka / (Mathf.Sin(phi0 * (Mathf.PI / 180f)) * Mathf.Sin(phi0 * (Mathf.PI / 180f)));
                    }
                    else
                    {
                        newAngle.kAngle = ForceField.ka;
                    }

                    newAngle.eqAngle = phi0;
                    angleTerms.Add(newAngle);
                }
            }
        }

        if (ForceField.torsionActive)
        {
            foreach (ForceField.AngleTerm threebond1 in angleTerms)
            {
                //if (atomList[threebond1.Atom1].m_data.m_abbre == "Dummy" || atomList[threebond1.Atom2].m_data.m_abbre == "Dummy" || atomList[threebond1.Atom3].m_data.m_abbre == "Dummy")
                //{
                //    continue;
                //}
                //if (threebond1.Aeq == 180f)break; ??
                foreach (ForceField.BondTerm bond2 in bondTerms)
                {
                    //if (atomList[bond2.Atom1].m_data.m_abbre == "Dummy" || atomList[bond2.Atom2].m_data.m_abbre == "Dummy")
                    //{
                    //    continue;
                    //}
                    // if the bond is in our threebond we can skip
                    if (threebond1.Atom1 == bond2.Atom1 && threebond1.Atom2 == bond2.Atom2) continue; // break;
                    if (threebond1.Atom1 == bond2.Atom2 && threebond1.Atom2 == bond2.Atom1) continue; // break;
                    if (threebond1.Atom2 == bond2.Atom1 && threebond1.Atom3 == bond2.Atom2) continue; // break;
                    if (threebond1.Atom2 == bond2.Atom2 && threebond1.Atom3 == bond2.Atom1) continue; // break;

                    int idx = -1, jdx = -1, kdx = -1, ldx = -1;
                    bool improper = false;

                    if (threebond1.Atom3 == bond2.Atom1)
                    {
                        //new l atom connects to k
                        idx = threebond1.Atom1; jdx = threebond1.Atom2; kdx = threebond1.Atom3; ldx = bond2.Atom2;
                    }
                    else if (threebond1.Atom3 == bond2.Atom2)
                    {
                        //new l atom connects to k, but the other way around 
                        idx = threebond1.Atom1; jdx = threebond1.Atom2; kdx = threebond1.Atom3; ldx = bond2.Atom1;
                    }
                    else if (threebond1.Atom1 == bond2.Atom1)
                    {
                        // new l connects to i, new definition of i j k l, so that j and k are in the middle. i and j are the new j and k now
                        idx = bond2.Atom2; jdx = threebond1.Atom1; kdx = threebond1.Atom2; ldx = threebond1.Atom3;
                    }
                    else if (threebond1.Atom1 == bond2.Atom2)
                    {
                        // new l connects to i, new definition of i j k l, so that j and k are in the middle. i and j are the new j and k now
                        idx = bond2.Atom1; jdx = threebond1.Atom1; kdx = threebond1.Atom2; ldx = threebond1.Atom3;
                    }
                    // improper case, that means that all 3 atoms are connected to atom2
                    else if (threebond1.Atom2 == bond2.Atom1)
                    {
                        // j is Atom which connects to i k l 
                        idx = threebond1.Atom1; jdx = threebond1.Atom2; kdx = bond2.Atom2; ldx = threebond1.Atom3;
                        improper = true;
                    }
                    else if (threebond1.Atom2 == bond2.Atom2)
                    {
                        // j is Atom which connects to i k l 
                        idx = threebond1.Atom1; jdx = threebond1.Atom2; kdx = bond2.Atom1; ldx = threebond1.Atom3;
                        improper = true;
                    }
                    //if (improper) break;
                    if (ldx > -1) // if anything was found: set term
                    {

                        ForceField.TorsionTerm newTorsion = new ForceField.TorsionTerm();
                        newTorsion.Atom1 = (ushort)idx;
                        newTorsion.Atom2 = (ushort)jdx;
                        newTorsion.Atom3 = (ushort)kdx;
                        newTorsion.Atom4 = (ushort)ldx;
                        if (!improper)
                        {
                            float nTorsTerm = Mathf.Max(1f, (nBondP[jdx] - 1) * (nBondP[kdx] - 1));
                            //Debug.Log(string.Format(" nTorsTerm  {1} {2} {3} {4} : {0} {5} {6} ", nTorsTerm, idx, jdx, kdx, ldx, nBondP[jdx], nBondP[kdx]));

                            if (atomList[jdx].m_data.m_hybridization == 3 && atomList[kdx].m_data.m_hybridization == 3) //two sp3 atoms
                            {
                                newTorsion.vk = 0.02f * ForceField.k0 / nTorsTerm;
                                newTorsion.nn = 3;
                                newTorsion.eqAngle = 180f; // Mathf.PI;
                                //print("1. Case 2 sp3");
                            }
                            else if (atomList[jdx].m_data.m_hybridization == 2 && atomList[kdx].m_data.m_hybridization == 3 ||
                                     atomList[jdx].m_data.m_hybridization == 3 && atomList[kdx].m_data.m_hybridization == 2)
                            {
                                newTorsion.vk = 0.01f * ForceField.k0 / nTorsTerm;
                                newTorsion.nn = 6;
                                newTorsion.eqAngle = 0;
                                //print("2. Case sp3 und sp2");
                            }
                            else if (atomList[jdx].m_data.m_hybridization == 2 && atomList[kdx].m_data.m_hybridization == 2)
                            {
                                newTorsion.vk = 0.05f * ForceField.k0 / nTorsTerm;
                                newTorsion.nn = 2;
                                newTorsion.eqAngle = 180f; // Mathf.PI
                                //print("3. Case 2 sp2");
                            }
                            else if (atomList[jdx].m_data.m_hybridization == 4 && atomList[kdx].m_data.m_hybridization == 4)
                            {
                                newTorsion.vk = 0.25f * ForceField.k0 / nTorsTerm;
                                newTorsion.nn = 2;
                                newTorsion.eqAngle = 180f; // Mathf.PI;
                                //print("resonance bond");
                            }
                            else if (atomList[jdx].m_data.m_hybridization == 1 || atomList[kdx].m_data.m_hybridization == 1)
                            {
                                newTorsion.vk = 0f;
                                newTorsion.nn = 0;
                                newTorsion.eqAngle = 180f; //Mathf.PI;
                                //print("4. Case 2 sp1");
                            }
                            else // take default values
                            {

                                newTorsion.vk = 0.1f * ForceField.k0 / nTorsTerm;
                                newTorsion.nn = 3;
                                newTorsion.eqAngle = 180f; //Mathf.PI;
                                //print("DEFAULT Case");
                            }
                        }
                        else //improper
                        {
                            /*
                            Vector3 rij = position[idx] - position[jdx];
                            Vector3 rkj = position[kdx] - position[jdx];
                            Vector3 rkl = position[kdx] - position[ldx];                            
                            Vector3 mNormalized = Vector3.Cross(rij, rkj).normalized;
                            Vector3 nNormalized = Vector3.Cross(rkj, rkl).normalized;

                            float cosAlpha = Mathf.Min(1.0f, Mathf.Max(-1.0f, (Vector3.Dot(nNormalized, mNormalized))));                      
                            float phi = Mathf.Sign(Vector3.Dot(rij, nNormalized)) * Mathf.Acos(cosAlpha);
                            */

                            // TRY:
                            float fImproper = 1f / 12f; // usual case for 4 bond partners
                            if (nBondP[jdx] == 3) fImproper = 1f / 6f;
                            newTorsion.vk = 2 * ForceField.kim * fImproper;

                            //newTorsion.vk = 2 * kim;
                            newTorsion.nn = 1;
                            if (atomList[jdx].m_data.m_hybridization == 3)
                            {
                                newTorsion.nn = 3; // TRY:
                                                   // if (phi > 0f)
                                                   //{
                                newTorsion.eqAngle = 120f;
                                // }
                                //else
                                //{
                                //    newTorsion.phieq = -120f;
                                //}
                            }
                            else if (atomList[jdx].m_data.m_hybridization == 2)
                            {
                                newTorsion.eqAngle = 180f;
                            }
                            else
                            {
                                //ForceFieldConsole.Instance.statusOut(string.Format("Warning {0} : improper for unknown hybridization", jdx));
                                newTorsion.eqAngle = 90f;
                            }
                        }
                        if (atomList[newTorsion.Atom1].keepConfig && atomList[newTorsion.Atom2].keepConfig && atomList[newTorsion.Atom3].keepConfig && atomList[newTorsion.Atom4].keepConfig)
                        {
                            //var vec1 = FFposition[idx] - FFposition[jdx];
                            //var vec2 = FFposition[ldx] - FFposition[kdx];
                            //var inner_vec = FFposition[jdx] - FFposition[kdx];
                            //var cross1 = Vector3.Cross(vec1, inner_vec).normalized;
                            //var cross2 = Vector3.Cross(vec2, -inner_vec).normalized;
                            //newTorsion.phieq = Mathf.Acos(Vector3.Dot(cross1, cross2)) * Mathf.Rad2Deg;

                            Vector3 rij = FFposition[idx] - FFposition[jdx];
                            Vector3 rkj = FFposition[kdx] - FFposition[jdx];
                            Vector3 rkl = FFposition[kdx] - FFposition[ldx];
                            Vector3 mNormal = Vector3.Cross(rij, rkj);
                            Vector3 mNormalized = Vector3.Cross(rij, rkj).normalized;
                            Vector3 nNormal = Vector3.Cross(rkj, rkl);
                            Vector3 nNormalized = Vector3.Cross(rkj, rkl).normalized;

                            float cosAlpha = Mathf.Min(1.0f, Mathf.Max(-1.0f, (Vector3.Dot(nNormalized, mNormalized))));
                            newTorsion.eqAngle = Mathf.Sign(Vector3.Dot(rij, nNormal)) * Mathf.Acos(cosAlpha) * Mathf.Rad2Deg;

                            //UnityEngine.Debug.Log($"[Molecule:generateFF] keepConfig - Torsion phi: {newTorsion.eqAngle}");
                            newTorsion.nn = 1; //TODO check if we can use real nn
                        }
                        torsionTerms.Add(newTorsion);
                    }
                }
            }
        }
    }

    #endregion

    public void OnDestroy()
    {
        if (toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
        EventManager.Singleton.OnMolDataChanged -= triggerGenerateFF;
        EventManager.Singleton.OnMolDataChanged -= adjustBBox;
    }
}
