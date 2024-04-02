using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using chARpackStructs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Molecule : MonoBehaviour, IMixedRealityPointerHandler
{
    private Stopwatch stopwatch;
    [HideInInspector] public bool isGrabbed = false;
    private Vector3 pickupPos = Vector3.zero;
    private Quaternion pickupRot = Quaternion.identity;

    private List<Tuple<ushort, Vector3>> atomState = new List<Tuple<ushort, Vector3>>();

    /// <summary>
    /// This method is triggered when a grab/select gesture is started.
    /// Sets the molecule to grabbed unless measurement mode is active.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        pickupPos = transform.localPosition;
        pickupRot = transform.localRotation;

        isGrabbed = true;
        stopwatch = Stopwatch.StartNew();
        // change material of grabbed object
        if (GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.NORMAL ||
            GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.FRAGMENT_ROTATION)
        {
            GetComponent<myBoundingBox>().setGrabbed(true);
        }
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        // Intentionally empty
    }

    /// <summary>
    /// This method is triggered when the grabbed molecule is dragged.
    /// It invokes a network event to keep molecule positions synchronized.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        if (!frozen)
        {
            // keep everything relative to atom world
            EventManager.Singleton.MoveMolecule(m_id, transform.localPosition, transform.localRotation);
        }
    }

    /// <summary>
    /// This function is triggered when a grabbed molecule is dropped.
    /// It ends the grabbed status of the molecule, marks it if less than
    /// the maximum timespan for the select gesture has elapsed and checks for/performs
    /// potential merges.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        stopwatch?.Stop();
        if (isGrabbed)
        {
            isGrabbed = false;
            if (GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.NORMAL ||
                GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.FRAGMENT_ROTATION)
            {
                if (stopwatch?.ElapsedMilliseconds < 200)
                {
                    transform.localPosition = pickupPos;
                    transform.localRotation = pickupRot;
                    EventManager.Singleton.MoveMolecule(m_id, transform.localPosition, transform.localRotation);
                    markMoleculeUI(!isMarked, true);
                }
                else
                {
                    if (GlobalCtrl.Singleton.collision)
                    {
                        Atom d1 = GlobalCtrl.Singleton.collider1;
                        Atom d2 = GlobalCtrl.Singleton.collider2;

                        Atom a1 = d1.dummyFindMain();
                        Atom a2 = d2.dummyFindMain();

                        if (!a1.alreadyConnected(a2))
                        {
                            if (atomList.Contains(a1))
                            {
                                EventManager.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1.m_molecule.m_id, GlobalCtrl.Singleton.collider1.m_id, GlobalCtrl.Singleton.collider2.m_molecule.m_id, GlobalCtrl.Singleton.collider2.m_id);
                                GlobalCtrl.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1, GlobalCtrl.Singleton.collider2);
                            }
                            else
                            {
                                EventManager.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider2.m_molecule.m_id, GlobalCtrl.Singleton.collider2.m_id, GlobalCtrl.Singleton.collider1.m_molecule.m_id, GlobalCtrl.Singleton.collider1.m_id);
                                GlobalCtrl.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider2, GlobalCtrl.Singleton.collider1);
                            }
                        }
                    }
                }
                // change material back to normal
                GetComponent<myBoundingBox>().setGrabbed(false);
            }
        }
    }

    /// <summary>
    /// Scales the molecule based on the slider value and invokes a 
    /// change molecule scale event.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnSliderUpdated(mySliderEventData eventData)
    {
        gameObject.transform.localScale = eventData.NewValue * startingScale;
        // networking
        EventManager.Singleton.ChangeMoleculeScale(m_id, gameObject.transform.localScale.x);
    }

    public void Update()
    {
        if (toolTipInstance)
        {
            if(type == toolTipType.SINGLE)
            {
                string[] text = toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText.Split("\n");
                string[] distance = text[2].Split(": ");
                double dist = SettingsData.useAngstrom ? toolTipInstance.transform.Find("Distance Measurement").GetComponent<DistanceMeasurement>().getDistanceInAngstrom()
                    : toolTipInstance.transform.Find("Distance Measurement").GetComponent<DistanceMeasurement>().getDistanceInAngstrom()*100;
                string distanceString = SettingsData.useAngstrom ? $"{dist:0.00}\u00C5" : $"{dist:0}pm";
                string newDistance = string.Concat(distance[0], ": ", distanceString);
                text[2] = newDistance;

                toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = string.Join("\n", text);

            }
            else if(type == toolTipType.ANGLE)
            {
                string[] text = toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText.Split("\n");
                string[] ang = text[3].Split(": ");
                double angle = toolTipInstance.transform.Find("Angle Measurement").GetComponent<AngleMeasurement>().getAngle();
                string newAng = string.Concat(ang[0], ": ", $"{ angle:0.00}°");
                text[3] = newAng;

                toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = string.Join("\n", text);
            }
            else if(type == toolTipType.TORSION)
            {
                string[] text = toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText.Split("\n");
                string[] ang = text[2].Split(": ");
                double angle = toolTipInstance.transform.Find("Dihedral Angle Measurement").GetComponent<DihedralAngleMeasurement>().getAngle();
                string newAng = string.Concat(ang[0], ": ", $"{ angle:0.00}°");
                text[2] = newAng;

                toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = string.Join("\n", text);
            }
        }
    }

    //private void HandleOnManipulationStarted(ManipulationEventData eventData)
    //{
    //    var pointer = eventData.Pointer;


    //    UnityEngine.Debug.Log("[Molecule] Manipulation started");

    //    // whatever shall happen when manipulation started
    //}

    [HideInInspector] public static GameObject myToolTipPrefab;
    [HideInInspector] public static GameObject mySnapToolTipPrefab;
    [HideInInspector] public static GameObject deleteMeButtonPrefab;
    [HideInInspector] public static GameObject closeMeButtonPrefab;
    [HideInInspector] public static GameObject modifyMeButtonPrefab;
    [HideInInspector] public static GameObject toggleDummiesButtonPrefab;
    [HideInInspector] public static GameObject undoButtonPrefab;
    [HideInInspector] public static GameObject changeBondWindowPrefab;
    [HideInInspector] public static GameObject copyButtonPrefab;
    [HideInInspector] public static GameObject scaleMoleculeButtonPrefab;
    [HideInInspector] public static GameObject scalingSliderPrefab;
    [HideInInspector] public static GameObject freezeMeButtonPrefab;
    [HideInInspector] public static GameObject snapMeButtonPrefab;
    [HideInInspector] public static GameObject distanceMeasurementPrefab;
    [HideInInspector] public static GameObject angleMeasurementPrefab;

    [HideInInspector] public static Material compMaterialA;
    [HideInInspector] public static Material compMaterialB;

    public GameObject toolTipInstance;
    private GameObject freezeButton;
    public GameObject scalingSliderInstance;
    private float toolTipDistanceWeight = 0.01f;
    private Vector3 startingScale;
    public bool frozen = false;

    public enum toolTipType
    {
        MOLECULE,
        SINGLE,
        ANGLE,
        TORSION
    }
    public toolTipType type;

    private Color orange = new Color(1.0f, 0.5f, 0.0f);

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
        startingScale = transform.localScale;
        atomList = new List<Atom>();
        bondList = new List<Bond>();
        // TODO put collider into a corner
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.001f, 0.001f, 0.001f);
        //collider.center = GetComponent<myBoundingBox>().cornerHandles[1].transform.position;
        // these objects take input from corner colliders and manipulate the moluecule
        var om = gameObject.AddComponent<ObjectManipulator>();
        //om.OnManipulationStarted.AddListener(HandleOnManipulationStarted);

        gameObject.AddComponent<NearInteractionGrabbable>();

        compMaterialA = Resources.Load("materials/ComparisonMaterialA") as Material;
        compMaterialB = Resources.Load("materials/ComparisonMaterialB") as Material;

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
#if !WINDOWS_UWP
        GetComponent<myBoundingBox>().setNormalMaterial(false);
#else
        if (mol == this)
        {
            if (GlobalCtrl.Singleton.List_curMolecules.Contains(mol))
            {
                StartCoroutine(adjustBBoxCoroutine());
            }
        }
#endif
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

    /// <summary>
    /// Outlines the molecule in the selection color and potentially spawns a molecule tool tip.
    /// </summary>
    /// <param name="mark"></param>
    /// <param name="showToolTip"></param>
    public void markMolecule(bool mark, bool showToolTip = false)
    {
        isMarked = mark;
        if (showToolTip && toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
        foreach (Atom a in atomList)
        {
            a.markAtom(mark);
            // Remove single marked atoms from list when whole molecule is selected
            Atom.markedAtoms.Remove(a);
        }

        foreach (Bond b in bondList)
        {
            b.markBond(mark);
        }
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

        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules)
        {
            if (mol != this && mol.isMarked)
            {
                if (!GlobalCtrl.Singleton.snapToolTipInstances.ContainsKey(new Tuple<ushort,ushort>(m_id, mol.m_id)) && 
                    !GlobalCtrl.Singleton.snapToolTipInstances.ContainsKey(new Tuple<ushort, ushort>(mol.m_id, m_id)))
                {
                    createSnapToolTip(mol.m_id);
                }
                else
                {
                    if (mark == false)
                    {
                        if (GlobalCtrl.Singleton.snapToolTipInstances.ContainsKey(new Tuple<ushort, ushort>(m_id, mol.m_id)))
                        {
                            Destroy(GlobalCtrl.Singleton.snapToolTipInstances[new Tuple<ushort, ushort>(m_id, mol.m_id)]);
                            GlobalCtrl.Singleton.snapToolTipInstances.Remove(new Tuple<ushort, ushort>(m_id, mol.m_id));
                        }
                        else
                        {
                            Destroy(GlobalCtrl.Singleton.snapToolTipInstances[new Tuple<ushort, ushort>(mol.m_id, m_id)]);
                            GlobalCtrl.Singleton.snapToolTipInstances.Remove(new Tuple<ushort, ushort>(mol.m_id, m_id));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Marks the molecule and invokes a mark molecule event.
    /// </summary>
    /// <param name="mark"></param>
    /// <param name="showToolTip"></param>
    public void markMoleculeUI(bool mark, bool showToolTip = true)
    {
        EventManager.Singleton.SelectMolecule(m_id, !isMarked);
        markMolecule(mark, showToolTip);
    }

    /// <summary>
    /// Creates a snap tool tip connected to the current molecule and the
    /// other selected molecule.
    /// It contains information about the molecules and a button that provides
    /// the option to perform the snap.
    /// </summary>
    /// <param name="otherMolID">ID of the other selected molecule</param>
    public void createSnapToolTip(ushort otherMolID)
    {
        // create tool tip
        var snapToolTip = Instantiate(mySnapToolTipPrefab);

        // put tool top to the right 
        snapToolTip.transform.position = (GlobalCtrl.Singleton.List_curMolecules[otherMolID].transform.position - transform.position)/2f + transform.position - 0.25f * Vector3.up;
        // add atom as connector
        snapToolTip.GetComponent<myDoubleLineToolTipConnector>().Target1 = gameObject;
        snapToolTip.GetComponent<myDoubleLineToolTipConnector>().Target2 = GlobalCtrl.Singleton.List_curMolecules[otherMolID].gameObject;
        string toolTipText = $"Molecule1 ID: {m_id}\nMolecule2 ID: {otherMolID}";
        snapToolTip.GetComponent<DoubleLineDynamicToolTip>().ToolTipText = toolTipText;

        var snapMoleculeButtonInstance = Instantiate(snapMeButtonPrefab);
        snapMoleculeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { snapUI(otherMolID); });
        snapToolTip.GetComponent<DoubleLineDynamicToolTip>().addContent(snapMoleculeButtonInstance);

        var closeSnapButtonInstance = Instantiate(closeMeButtonPrefab);
        closeSnapButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { closeSnapUI(otherMolID); });
        snapToolTip.GetComponent<DoubleLineDynamicToolTip>().addContent(closeSnapButtonInstance);

        GlobalCtrl.Singleton.snapToolTipInstances[new Tuple<ushort, ushort>(m_id, otherMolID)] = snapToolTip;
    }

    private void snapUI(ushort otherMolID)
    {

        var otherMol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(otherMolID);
        if (otherMol == default)
        {
            UnityEngine.Debug.LogError($"[Molecule:snapUI] Could not find Molecule with ID {otherMolID}");
            return;
        }
        snap(otherMolID);
        //EventManager.Singleton.MoveMolecule(m_id, otherMol.transform.localPosition, otherMol.transform.localRotation);
        //EventManager.Singleton.SelectMolecule(m_id, false);
        //EventManager.Singleton.SelectMolecule(otherMolID, false);
        EventManager.Singleton.SnapMolecules(m_id, otherMolID);
    }

    public bool snap(ushort otherMolID)
    {
        var otherMol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(otherMolID);
        if (otherMol == default)
        {
            return false;
        }
        markMolecule(false);
        otherMol.markMolecule(false);
        // apply transformation
        transform.localPosition = otherMol.transform.localPosition;
        transform.localRotation = otherMol.transform.localRotation;
        // TODO: Add advanced alignment mode
        // add coloring
        addSnapColor(ref compMaterialA);
        otherMol.addSnapColor(ref compMaterialB);

        return true;
    }

    private void addSnapColor(ref Material mat)
    {
        foreach (var atom in atomList)
        {
            // Append comparison material to end of list
            Material[] comp = atom.GetComponent<MeshRenderer>().sharedMaterials.ToList().Append(mat).ToArray();
            atom.GetComponent<MeshRenderer>().sharedMaterials = comp;
        }
        foreach (var bond in bondList)
        {
            // Append comparison material to end of list
            Material[] comp = bond.GetComponentInChildren<MeshRenderer>().sharedMaterials.ToList().Append(mat).ToArray();
            bond.GetComponentInChildren<MeshRenderer>().sharedMaterials = comp;
        }
    }

    private void closeSnapUI(ushort otherMolID)
    {
        var otherMol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrDefault(otherMolID);
        if (otherMol == default)
        {
            UnityEngine.Debug.LogError($"[Molecule:closeSnapUI] Could not find Molecule with ID {otherMolID}");
            return;
        }
        markMolecule(false);
        otherMol.markMolecule(false);
        EventManager.Singleton.SelectMolecule(m_id, false);
        EventManager.Singleton.SelectMolecule(otherMolID, false);
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
    }

    /// <summary>
    /// all dummys are replaced by hydrogens
    /// </summary>
    public void toggleDummiesUI()
    {
        toggleDummies();
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

    /// <summary>
    /// Computes the center of the molecule relative to the atom world.
    /// </summary>
    /// <returns>a vector describing the molecule's center in the atom world</returns>
    public Vector3 getCenterInAtomWorld()
    {
        Vector3 center = new Vector3(0.0f, 0.0f, 0.0f);
        int num_atoms = atomList.Count;

        foreach (Atom atom in atomList)
        {
            center += GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(atom.transform.position);
        }
        center /= num_atoms > 0 ? num_atoms : 1;

        return center;
    }

    /// <summary>
    /// Computes the center of the molecule in global coordinates.
    /// </summary>
    /// <returns>a vector describing the molecule's center</returns>
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

    /// <summary>
    /// Calculates the maximum distance any atom in the current molecule
    /// has from a given point.
    /// </summary>
    /// <param name="center"></param>
    /// <returns>the maximum distance from <c>center</c></returns>
    public float getMaxDistFromCenter(Vector3 center)
    {
        List<float> dists = new List<float>();

        foreach (Atom atom in atomList)
        {
            Vector3 atom_pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(atom.transform.position);
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
    /// <summary>
    /// Creates a molecule tool tip with information about the molecule as well
    /// as buttons to provide interactions like copying and toggling dummies.
    /// </summary>
    public void createToolTip()
    {
        // create tool tip
        toolTipInstance = Instantiate(myToolTipPrefab);
        type = toolTipType.MOLECULE;
        // put tool top to the right 
        Vector3 ttpos = transform.position + toolTipDistanceWeight * GlobalCtrl.Singleton.mainCamera.transform.right + toolTipDistanceWeight * GlobalCtrl.Singleton.mainCamera.transform.up;
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
        //var keepConfigSwitchButtonInstance = Instantiate(modifyMeButtonPrefab);
        //keepConfigSwitchButtonInstance.GetComponent<ButtonConfigHelper>().MainLabelText = "keepConfig";
        //keepConfigSwitchButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.toggleKeepConfigUI(this); });
        //toolTipInstance.GetComponent<DynamicToolTip>().addContent(keepConfigSwitchButtonInstance);
        var toggleDummiesButtonInstance = Instantiate(toggleDummiesButtonPrefab);
        toggleDummiesButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { toggleDummiesUI(); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(toggleDummiesButtonInstance);
        var copyButtonInstance = Instantiate(copyButtonPrefab);
        copyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.copyMolecule(this); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(copyButtonInstance);
        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markMoleculeUI(false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);

        // making sure the delete and close buttons are not too close together; has to be improved
        //toolTipInstance.GetComponent<DynamicToolTip>().addContent(new GameObject());

        var scaleMoleculeButtonInstance = Instantiate(scaleMoleculeButtonPrefab);
        scaleMoleculeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { toggleScalingSlider(); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(scaleMoleculeButtonInstance);

        var freezeMoleculeButtonInstance = Instantiate(freezeMeButtonPrefab);
        freezeMoleculeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { freezeUI(!frozen); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(freezeMoleculeButtonInstance);
        freezeButton = freezeMoleculeButtonInstance;

        var delButtonInstance = Instantiate(deleteMeButtonPrefab);
        delButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.deleteMoleculeUI(this); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(delButtonInstance);

        // Starting color for indicators
        setFrozenVisual(frozen);
    }

    public void toggleScalingSlider()
    {
        if (!scalingSliderInstance)
        {
            // position needs to be optimized
            scalingSliderInstance = Instantiate(scalingSliderPrefab, gameObject.transform.position - 0.17f*GlobalCtrl.Singleton.currentCamera.transform.forward - 0.05f*Vector3.up, GlobalCtrl.Singleton.currentCamera.transform.rotation);
            scalingSliderInstance.GetComponent<mySlider>().maxVal = 2;
            scalingSliderInstance.GetComponent<mySlider>().minVal = 0.1f;
            var currentScale = transform.localScale.x / startingScale.x;
            // Set effective starting value and default to 1
            scalingSliderInstance.GetComponent<mySlider>().SliderValue = (currentScale - scalingSliderInstance.GetComponent<mySlider>().minVal)/ (scalingSliderInstance.GetComponent<mySlider>().maxVal - scalingSliderInstance.GetComponent<mySlider>().minVal);
            scalingSliderInstance.GetComponent<mySlider>().defaultVal = (1 - scalingSliderInstance.GetComponent<mySlider>().minVal) / (scalingSliderInstance.GetComponent<mySlider>().maxVal - scalingSliderInstance.GetComponent<mySlider>().minVal);
            //startingScale = gameObject.transform.localScale;
            scalingSliderInstance.GetComponent<mySlider>().OnValueUpdated.AddListener(OnSliderUpdated);
        }
        else
        {
            Destroy(scalingSliderInstance);
        }
    }

    /// <summary>
    /// Creates a tool tip for a single bond that contains both static and dynamic information about
    /// its length and buttons, including the option to change the bonds equilibrium parameters.
    /// </summary>
    /// <param name="term"></param>
    public void createBondToolTip(ForceField.BondTerm term)
    {
        markBondTerm(term, true);
        var bond = atomList[term.Atom1].getBond(atomList[term.Atom2]);
        // create tool tip
        toolTipInstance = Instantiate(Atom.myAtomToolTipPrefab);
        type = toolTipType.SINGLE;
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

        // get current measurements
        var distGO = Instantiate(distanceMeasurementPrefab);
        distGO.transform.parent = toolTipInstance.transform;
        distGO.name = "Distance Measurement";
        distGO.transform.Find("Line").gameObject.SetActive(false);
        var atom1 = atomList.ElementAtOrDefault(term.Atom1);
        var atom2 = atomList.ElementAtOrDefault(term.Atom2);
        DistanceMeasurement dist = distGO.GetComponent<DistanceMeasurement>();
        dist.StartAtom = atom1;
        dist.EndAtom = atom2;

        // show meta data (in Angstrom)
        string toolTipText = getBondToolTipText(term.eqDist/100, dist.getDistanceInAngstrom(), term.kBond, term.order);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;

        var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
        modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { createChangeBondWindow(term); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);

        if (atom1.m_data.m_abbre != "Dummy" && atom2.m_data.m_abbre != "Dummy")
        {
            var delButtonInstance = Instantiate(deleteMeButtonPrefab);
            delButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.deleteBondUI(bond); });
            toolTipInstance.GetComponent<DynamicToolTip>().addContent(delButtonInstance);
        }

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

        var dist = toolTipInstance.transform.Find("Distance Measurement").GetComponent<DistanceMeasurement>();
        // Update tool tip
        string toolTipText = getBondToolTipText(bt.eqDist, dist.getDistanceInAngstrom(), bt.kBond, bt.order);
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


    /// <summary>
    /// Creates a tool tip for an angle bond that contains both static and dynamic information about
    /// its size and buttons, including the option to change the bonds equilibrium parameters.
    /// </summary>
    /// <param name="term"></param>
    public void createAngleToolTip(ForceField.AngleTerm term)
    {
        markAngleTerm(term, true);
        var middleAtom = atomList[term.Atom2];
        // create tool tip
        toolTipInstance = Instantiate(Atom.myAtomToolTipPrefab);
        type = toolTipType.ANGLE;
        // put tool top to the right 
        Vector3 ttpos = middleAtom.transform.position + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.right + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add atom as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = middleAtom.gameObject;
        AngleMeasurement angle = getMeasurements(term);

        // show angle term data
        string toolTipText = getAngleToolTipText(term.eqAngle, term.kAngle, angle.getAngle());
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;

        var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
        modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { createChangeAngleWindow(term); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);

        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markAngleTermUI(term, false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);
    }

    private AngleMeasurement getMeasurements(ForceField.AngleTerm term)
    {
        var dist1 = Instantiate(distanceMeasurementPrefab);
        var dist2 = Instantiate(distanceMeasurementPrefab);
        var angle = Instantiate(angleMeasurementPrefab);
        dist1.transform.parent = toolTipInstance.transform;
        dist2.transform.parent = toolTipInstance.transform;
        angle.transform.parent = toolTipInstance.transform;
        dist1.transform.Find("Line").gameObject.SetActive(false);
        dist2.transform.Find("Line").gameObject.SetActive(false);
        angle.transform.Find("Line").gameObject.SetActive(false);

        angle.name = "Angle Measurement";

        dist1.GetComponent<DistanceMeasurement>().StartAtom = atomList[term.Atom1];
        dist1.GetComponent<DistanceMeasurement>().EndAtom = atomList[term.Atom2];
        dist2.GetComponent<DistanceMeasurement>().StartAtom = atomList[term.Atom2];
        dist2.GetComponent<DistanceMeasurement>().EndAtom = atomList[term.Atom3];
        angle.GetComponent<AngleMeasurement>().distMeasurement1 = dist1.GetComponent<DistanceMeasurement>();
        angle.GetComponent<AngleMeasurement>().distMeasurement2 = dist2.GetComponent<DistanceMeasurement>();
        angle.GetComponent<AngleMeasurement>().distMeasurement1Sign = -1f;

        return angle.GetComponent<AngleMeasurement>();
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

    /// <summary>
    /// Creates a tool tip for a torsion bond that contains both static and dynamic information about
    /// its angle and buttons, including the option to change the bonds equilibrium parameters.
    /// </summary>
    /// <param name="term"></param>
    public void createTorsionToolTip(ForceField.TorsionTerm term)
    {
        markTorsionTerm(term, true);
        var middlebond = atomList[term.Atom2].getBond(atomList[term.Atom3]);
        // create tool tip
        toolTipInstance = Instantiate(Atom.myAtomToolTipPrefab);
        type = toolTipType.TORSION;
        // put tool top to the right 
        Vector3 ttpos = middlebond.transform.position + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.right + toolTipDistanceWeight * GlobalCtrl.Singleton.currentCamera.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add atom as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = middlebond.gameObject;

        var curAngle = getDihedralAngle(term.Atom1, term.Atom2, term.Atom3, term.Atom4);

        // show angle term data
        string toolTipText = getTorsionToolTipText(term.eqAngle, term.vk, term.nn, curAngle);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;

        var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
        modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { createChangeTorsionWindow(term); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);

        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markTorsionTermUI(term, false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);

    }

    private double getDihedralAngle(ushort atom1, ushort atom2, ushort atom3, ushort atom4)
    {
        Atom a1 = atomList[atom1];
        Atom a2 = atomList[atom2];
        Atom a3 = atomList[atom3];
        Atom a4 = atomList[atom4];

        GameObject measurement = Instantiate((GameObject)Resources.Load("prefabs/DihedralAngleMeasurementPrefab"));
        measurement.transform.parent = toolTipInstance.transform;
        measurement.name = "Dihedral Angle Measurement";
        measurement.GetComponent<DihedralAngleMeasurement>().atoms = new List<Atom> { a1, a2, a3, a4 };

        return measurement.GetComponent<DihedralAngleMeasurement>().getAngle();
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
        string numAtoms = GlobalCtrl.Singleton.GetLocalizedString("NUM_ATOMS");
        string numBonds = GlobalCtrl.Singleton.GetLocalizedString("NUM_BONDS");
        string mass = GlobalCtrl.Singleton.GetLocalizedString("TOT_MASS");
        string toolTipText = $"{numAtoms}: {atomList.Count}\n{numBonds}: {bondList.Count}\n{mass}: {totMass:0.00}\nMaxRadius: {maxDist:0.00}";
        return toolTipText;
    }

    private string getBondToolTipText(double eqDist, double curDist, double kBond, double order)
    {
        string dist = GlobalCtrl.Singleton.GetLocalizedString("EQ_DIST");
        string singleBond = GlobalCtrl.Singleton.GetLocalizedString("SINGLE_BOND");
        string current = GlobalCtrl.Singleton.GetLocalizedString("CURRENT");
        string ord = GlobalCtrl.Singleton.GetLocalizedString("ORDER");
        string distanceInCorrectUnit = SettingsData.useAngstrom ? $"{ dist}: { eqDist: 0.00}\u00C5" : $"{dist}: {eqDist*100:0}pm";
        string curDistanceInCorrectUnit = SettingsData.useAngstrom ? $"{ current}: { curDist: 0.00}\u00C5" : $"{current}: {curDist*100:0}pm";
        string toolTipText = $"{singleBond}\n{distanceInCorrectUnit}\n{curDistanceInCorrectUnit}\nk: {kBond:0.00}\n{ord}: {order:0.00}";
        return toolTipText;
    }

    private string getAngleToolTipText(double eqAngle, double kAngle, double curAngle = 0)
    {
        string angleBond = GlobalCtrl.Singleton.GetLocalizedString("ANGLE_BOND");
        string eqAngleStr = GlobalCtrl.Singleton.GetLocalizedString("EQUI_ANGLE");
        string kAngleStr = GlobalCtrl.Singleton.GetLocalizedString("K_ANGLE");
        string current = GlobalCtrl.Singleton.GetLocalizedString("CURRENT");
        string toolTipText = $"{angleBond}\n{kAngleStr}: {kAngle:0.00}\n{eqAngleStr}: {eqAngle:0.00}\u00B0\n{current}: {curAngle:0.00}\u00B0";
        return toolTipText;
    }
    
    private string getTorsionToolTipText(double eqAngle, double vk, double nn, double curAngle = 0f)
    {
        //$"Torsion Bond\nEqui. Angle: {term.eqAngle}\nvk: {term.vk}\nnn: {term.nn}"
        string torsionBond = GlobalCtrl.Singleton.GetLocalizedString("TORSION_BOND");
        string eqAngleStr = GlobalCtrl.Singleton.GetLocalizedString("EQUI_ANGLE");
        string current = GlobalCtrl.Singleton.GetLocalizedString("CURRENT");
        string toolTipText = $"{torsionBond}\n{eqAngleStr}: {eqAngle:0.00}\u00B0\n{current}: {curAngle:0.00}\u00B0\nvk: {vk:0.00}\nnn: {nn:0.00}";
        return toolTipText;
    }

    /// <summary>
    /// Freezes/unfreezes the molecule and invokes a freeze molecule event.
    /// </summary>
    /// <param name="value">whether to freeze or unfreeze the molecule</param>
    public void freezeUI(bool value)
    {
        if (value == frozen) return;
        freeze(value);
        EventManager.Singleton.FreezeMolecule(m_id, value);
    }

    /// <summary>
    /// Freezes the molecule; this changes its appearance and makes it non-interactable.
    /// </summary>
    /// <param name="value">whether to freeze or unfreeze the molecule</param>
    public void freeze(bool value)
    {
        foreach (var atom in atomList)
        {
            atom.freeze(value);
        }
        GetComponent<NearInteractionGrabbable>().enabled = !value;
        GetComponent<ObjectManipulator>().enabled = !value;
        frozen = value;
        if (freezeButton)
        {
            setFrozenVisual(frozen);
        }
    }

    /// <summary>
    /// Updates the indicator on the freeze button depending on whether the molecule is frozen.
    /// </summary>
    /// <param name="value"></param>
    public void setFrozenVisual(bool value)
    {
        var FrozenIndicator = freezeButton.transform.Find("IconAndText").gameObject.transform.Find("Indicator").gameObject;
        if (value)
        {
            FrozenIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            FrozenIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    #endregion

    #region atom_state
    public void saveAtomState()
    {
        atomState.Clear();
        foreach (var a in atomList)
        {
            atomState.Add(new Tuple<ushort, Vector3>(a.m_id, a.transform.localPosition));
        }
    }

    public void popAtomState()
    {
        foreach (var s in atomState)
        {
            var atom = atomList.Where(a => a.m_id == s.Item1).ToList();
            if (atom.Count != 1)
            {
                UnityEngine.Debug.LogError("[Trying to pop atom state but atoms do not exist]");
                return;
            }
            atom.First().transform.localPosition = s.Item2;
            EventManager.Singleton.MoveAtom(m_id, s.Item1, s.Item2);
        }
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
    public List<Vector3> FFlastForces = new List<Vector3>();
    public List<Vector3> FFforces_pass2 = new List<Vector3>();
    public List<Vector3> FFmovement = new List<Vector3>();
    public List<Vector3> FFtimeStep = new List<Vector3>();
    public List<float> FFlambda = new List<float>();
    public List<Vector3> FFposDiff = new List<Vector3>();

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

    /// <summary>
    /// Generates a force field including the current molecule.
    /// </summary>
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
            mol.FFlastForces.Clear();
            mol.FFforces_pass2.Clear();
            mol.FFmovement.Clear();
            mol.FFtimeStep.Clear();
            mol.FFlambda.Clear();
            mol.FFposDiff.Clear();
            foreach (var a in mol.atomList)
            {
                mol.FFposition.Add(a.transform.position * (1f / ForceField.scalingfactor));
                mol.FFvelocity.Add(Vector3.zero);
                mol.FFforces.Add(Vector3.zero);
                mol.FFlastForces.Add(Vector3.zero);
                mol.FFforces_pass2.Add(Vector3.zero);
                mol.FFmovement.Add(Vector3.zero);
                mol.FFtimeStep.Add(Vector3.one * ForceField.Singleton.RKtimeFactor);
                mol.FFlambda.Add(ForceField.SDdefaultLambda);
                mol.FFposDiff.Add(Vector3.zero);
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
                                newTorsion.nn = 2;
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
        if (scalingSliderInstance)
        {
            Destroy(scalingSliderInstance);
            scalingSliderInstance = null;
        }
        EventManager.Singleton.OnMolDataChanged -= triggerGenerateFF;
        EventManager.Singleton.OnMolDataChanged -= adjustBBox;
    }
}
