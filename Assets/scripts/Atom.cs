using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using chARpackStructs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using chARpackTypes;
using chARpackColorPalette;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// A class that provides the functionalities of single atoms.
/// </summary>
[Serializable]
public class Atom : MonoBehaviour, IMixedRealityPointerHandler, IMixedRealityFocusHandler
{
    // prefabs initialized in GlobalCtrl
    [HideInInspector] public static GameObject myAtomToolTipPrefab;
    [HideInInspector] public static GameObject distMeasurementPrefab;
    [HideInInspector] public static GameObject angleMeasurementPrefab;
    [HideInInspector] public static GameObject deleteMeButtonPrefab;
    [HideInInspector] public static GameObject closeMeButtonPrefab;
    [HideInInspector] public static GameObject modifyMeButtonPrefab;
    [HideInInspector] public static GameObject modifyHybridizationPrefab;
    [HideInInspector] public static GameObject freezeMePrefab;
    [HideInInspector] public static GameObject serverTooltipPrefab;

    private Stopwatch stopwatch;
    public GameObject toolTipInstance = null;
    private GameObject freezeButton;
    private static float toolTipDistanceWeight = 2.5f;
    private static Color grabColor = chARpackColors.blue;
    public Color[] backupOutlineColor = new Color[4] { chARpackColors.notEnabledColor, chARpackColors.notEnabledColor, chARpackColors.notEnabledColor, chARpackColors.notEnabledColor };
    public bool keepConfig = false;
    public bool frozen = false;
    public bool[] focused = new bool[4] { false, false, false, false };
    public bool serverFocus = false;
    private float[] focus_alpha = new float[4] { 0f, 0f, 0f, 0f };
    private float focus_ramping_constant = 1f/(3f*40f);
    private float outline_radius_min = 5f;
    private float outline_radius_max = 8f;
    private float[] outline_radius_current = new float[4] { 5f, 5f, 5f, 5f };
    private FocusManager.HighlightType currentHighlightType = FocusManager.HighlightType.None;
    private FocusManager.HighlightType previousHighlightType = FocusManager.HighlightType.None;

    private cmlData before;

    private List<Atom> currentChain = new List<Atom>();

    public static List<Atom> markedAtoms = new List<Atom>();

    public Vector2 structure_coords;
    public GameObject structure_interactible;
    public int focus_id_tracker = -1;


    /// <summary>
    /// Outlines the current atom in grabColor; is used upon grabbing an atom.
    /// </summary>
    /// <param name="active">Whether to activate or deactivate the grabColor outline</param>
    public void grabHighlight(bool active)
    {
        var outline_component = GetComponent<OutlinePro>();
        if (active)
        {
            if (currentHighlightType != FocusManager.HighlightType.None)
            {
                if (currentHighlightType == FocusManager.HighlightType.Select)
                {
                    for (int i = 0; i < FocusManager.maxNumOutlines; i++)
                    {
                        backupOutlineColor[i] = outline_component.OutlineColor[i];
                    }
                }
            }
            else
            {
                outline_component.enabled = true;
                for (int i = 0; i < FocusManager.maxNumOutlines; i++)
                {
                    backupOutlineColor[i] = chARpackColors.notEnabledColor;
                }
            }
            for (int i = 0; i < FocusManager.maxNumOutlines; i++)
            {
                outline_component.OutlineColor[i] = grabColor;
            }
            previousHighlightType = currentHighlightType;
            currentHighlightType = FocusManager.HighlightType.Grab;
            outline_component.NeedsUpdate();
        }
        else
        {
            if (previousHighlightType == FocusManager.HighlightType.None)
            {
                previousHighlightType = currentHighlightType;
                currentHighlightType = FocusManager.HighlightType.None;
                outline_component.enabled = false;
            }
            else
            {
                for (int i = 0; i < FocusManager.maxNumOutlines; i++)
                {
                    outline_component.OutlineColor[i] = backupOutlineColor[i];
                }
                currentHighlightType = previousHighlightType;
                previousHighlightType = FocusManager.HighlightType.Grab;
                outline_component.NeedsUpdate();
            }
        }
    }

    #region StructureFormula
    public void focusHighlightInFormula(bool[] values, Color[] col)
    {
        StructureFormulaManager.Singleton?.addFocusHighlight(m_molecule.m_id, this, values, col);
    }

    public void serverFocusHighlightInFormula(Color[] col)
    {
        StructureFormulaManager.Singleton?.addServerFocusHighlight(m_molecule.m_id, this, col);
    }

    public void selectHighlightInFormula(Color[] selCol)
    {
        StructureFormulaManager.Singleton?.addSelectHighlight(m_molecule.m_id, this, selCol);
    }
    #endregion

    #region mouse_interaction

#if UNITY_STANDALONE || UNITY_EDITOR
    public static bool anyArcball;
    private bool arcball;
    private Vector3 oldMousePosition;
    private Vector3 newMousePosition;
    public void Update()
    {
        if (SceneManager.GetActiveScene().name == "ServerScene")
        {
            if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftShift) && mouseOverAtom())
            {
                arcball = true; anyArcball = true;
                oldMousePosition = Input.mousePosition;
                newMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1) || !Input.GetKey(KeyCode.LeftShift))
            {
                arcball = false; anyArcball = false;
            }

            if (arcball)
            {
                oldMousePosition = newMousePosition;
                newMousePosition = Input.mousePosition;
                if (newMousePosition != oldMousePosition)
                {
                    var vector2 = getArcballVector(newMousePosition);
                    var vector1 = getArcballVector(oldMousePosition);
                    float angle = (float)Math.Acos(Vector3.Dot(vector1, vector2));
                    var axis_cam = Vector3.Cross(vector1, vector2);

                    Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
                    Matrix4x4 modelMatrix = transform.localToWorldMatrix;
                    Matrix4x4 cameraToObjectMatrix = Matrix4x4.Inverse(viewMatrix * modelMatrix);
                    var axis_world = cameraToObjectMatrix * axis_cam;

                    m_molecule.transform.RotateAround(transform.position, axis_world, 2 * Mathf.Rad2Deg * angle);
                    EventManager.Singleton.MoveMolecule(m_molecule.m_id, m_molecule.transform.localPosition, m_molecule.transform.localRotation);
                }
            }
        }
    }

    private bool isBlockedByUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);

        if (raysastResults.Count > 0)
        {
            if (raysastResults[0].gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                return true;
            }
        }
        return false;
    }

    private bool mouseOverAtom()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if(hit.collider == GetComponent<BoxCollider>())
            {
                return true;
            }
        }
        return false;
    }

    private Vector3 getArcballVector(Vector3 inputPos)
    {
        Vector3 vector = CameraSwitcher.Singleton.currentCam.ScreenToViewportPoint(inputPos);
        vector = -vector;
        if (vector.x * vector.x + vector.y * vector.y <= 1)
        {
            vector.z = (float)Math.Sqrt(1 - vector.x * vector.x - vector.y * vector.y);
        }
        else
        {
            vector = vector.normalized;
        }
        return vector;
    }


    // offset for mouse interaction
    public Vector3 mouse_offset = Vector3.zero;
    void OnMouseDown()
    {
        // Handle server GUI interaction
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        m_molecule.saveAtomState();

        mouse_offset = gameObject.transform.position - GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
         new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f));

        if (GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.NORMAL)
        {
            stopwatch = Stopwatch.StartNew();
            grabHighlight(true);
            isGrabbed = true;
            before = m_molecule.AsCML();
        }
        else if(GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.MEASUREMENT)
        {
            handleMeasurements();
        }
    }

    void OnMouseDrag()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        if (!frozen && GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.NORMAL)
        {
            Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f);
            transform.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(newPosition) + mouse_offset;
            // position relative to molecule position
            EventManager.Singleton.MoveAtom(m_molecule.m_id, m_id, transform.localPosition);
        }
    }

    private void OnMouseUp()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        isGrabbed = false;
        // reset outline
        grabHighlight(false);


        if (GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.NORMAL)
        {
            // measure convergence
            ForceField.Singleton.resetMeasurment();

            stopwatch?.Stop();
            if (stopwatch?.ElapsedMilliseconds < 200)
            {
                focus_id_tracker = -1;
                this.m_molecule.focus_id_tracker = -1;
                m_molecule.popAtomState();
                if (m_molecule.isMarked)
                {
                    m_molecule.markMolecule(false);
                }
                else
                {
                    markAtomUI(!isMarked);
                }
            }

            else
            {
                resetMolPositionAfterMove();
                cmlData after = m_molecule.AsCML();
                GlobalCtrl.Singleton.undoStack.AddChange(new MoveMoleculeAction(before, after));
                EventManager.Singleton.StopMoveAtom(m_molecule.m_id, m_id);
                EventManager.Singleton.MoveMolecule(m_molecule.m_id, m_molecule.transform.localPosition, m_molecule.transform.localRotation);


                // check for potential merge
                if (GlobalCtrl.Singleton.collision)
                {
                    Atom d1 = GlobalCtrl.Singleton.collider1;
                    Atom d2 = GlobalCtrl.Singleton.collider2;

                    Atom a1 = d1.dummyFindMain();
                    Atom a2 = d2.dummyFindMain();

                    if (!a1.alreadyConnected(a2))
                    {
                        if (a1 == this)
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
            
        }
    }

    private void OnMouseOver()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (!serverFocus)
            {
                serverFocusHighlightUI(true);
            }
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            serverFocusHighlightUI(false);
        }
    }

    private void OnMouseExit()
    {
        if (serverFocus)
        {
            serverFocusHighlightUI(false);
        }
    }

#endif
    #endregion

    #region hand_interaction

    /// <summary>
    /// Handles the start of a grab gesture.
    /// The handling depends on the current interaction mode (e.g. in chain mode the correct chain of connected atoms is computed).
    /// </summary>
    /// <param name="eventData">The data of the triggering pointer event</param>
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer is SpherePointer)
        {
            m_molecule.saveAtomState();
            // give it a outline
            grabHighlight(true);

            stopwatch = Stopwatch.StartNew();
            isGrabbed = true;
            before = m_molecule.AsCML();

            // go through the chain of connected atoms and add the force there too
            if (GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.FRAGMENT_ROTATION)
            {
                // Get the bond that is closest to grab direction
                var fwd = HandTracking.Singleton.getForward();
                var con_atoms = connectedAtoms();
                var dot_products = new List<float>();
                foreach (var atom in con_atoms)
                {
                    var dir = atom.transform.position - transform.position;
                    dot_products.Add(Vector3.Dot(fwd, dir));
                }
                var start_atom = con_atoms[dot_products.maxElementIndex()];

                GetComponent<MoveAxisConstraint>().enabled = true;

                currentChain = start_atom.connectedChain(this);

                ConstraintSource cs = new ConstraintSource();
                cs.sourceTransform = transform;
                cs.weight = 1;
                foreach (var atom in currentChain)
                {
                    atom.grabHighlight(true);
                    atom.isGrabbed = true;
                    //atom.transform.parent = transform;
                    // use parent constraint

                    var pc = atom.gameObject.AddComponent<ParentConstraint>();
                    var positionDelta = pc.transform.position - transform.position;
                    pc.AddSource(cs);
                    pc.SetTranslationOffset(0, Quaternion.Inverse(transform.rotation) * positionDelta);
                    pc.constraintActive = true;

                }
            }
        }
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        // Intentionally empty
    }

    /// <summary>
    /// This function is triggered when a grabbed atom is dragged.
    /// Moves the current grabbed atom; if two atoms in a molecule are grabbed and pulled apart, they are separated.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        // position relative to molecule position
        if (!frozen)
        {
            EventManager.Singleton.MoveAtom(m_molecule.m_id, m_id, transform.localPosition);
        }

        if (m_data.m_abbre != "Dummy" && GlobalCtrl.Singleton.currentInteractionMode != GlobalCtrl.InteractionModes.FRAGMENT_ROTATION)
        {
            var con_atoms = connectedAtoms();
            foreach (var atom in con_atoms)
            {
                // make sure this is only executed once and not for both grabbed atoms (id check)
                if (atom.isGrabbed && atom.m_data.m_abbre != "Dummy" && m_id < atom.m_id)
                {
                    var term = m_molecule.bondTerms.Find(p => p.Contains(m_id, atom.m_id));
                    var current_dist = ((transform.localPosition - atom.transform.localPosition) / ForceField.scalingfactor).magnitude;
                    if (current_dist > 3 * term.eqDist)
                    {
                        GlobalCtrl.Singleton.SeparateMolecule(this, atom);
                    }
                }
            }
        }

        // if chain interaction send positions of all connected atoms
        if (currentChain.Any() && GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.FRAGMENT_ROTATION)
        {
            foreach (var atom in currentChain)
            {
                var mol_rel_pos = m_molecule.transform.InverseTransformPoint(atom.transform.position);
                EventManager.Singleton.MoveAtom(m_molecule.m_id, atom.m_id, mol_rel_pos);
            }
        }
    }

    /// <summary>
    /// This function is triggered when a grabbed object is dropped.
    /// It resets the grab and focus highlights on the current atom
    /// <para>
    /// In chain mode, highlights are reset on every atom in the current chain.
    /// </para>
    /// <para>
    /// In normal mode, the atom is selected if the triggering interaction 
    /// was shorter than 200ms (selection gesture).
    /// If the interaction was longer (i.e. the atom was moved), the method checks
    /// for and handles potential merges with other molecules.
    /// </para>
    /// <para>
    /// In measurement mode, measurements are made if the interaction was 
    /// shorter than 300ms (<see cref="handleMeasurements"/>).
    /// </para>
    /// If the atom was moved, the resulting new position of the entire molecule 
    /// is then computed.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        stopwatch?.Stop();
        if (isGrabbed)
        {
            isGrabbed = false;
            if (eventData.Pointer is MousePointer)
            {
                UnityEngine.Debug.Log("Mouse");
            }
            if (eventData.Pointer is SpherePointer)
            {
                // reset outline
                var focus_id = FocusManager.getMyFocusID() < 0 ? 0 : FocusManager.getMyFocusID();
                var pos = FocusManager.getPosInArray(focus_id);
                focused[pos] = false;
                grabHighlight(false);

                // measure convergence
                ForceField.Singleton.resetMeasurment();

                if (GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.FRAGMENT_ROTATION)
                {
                    GetComponent<MoveAxisConstraint>().enabled = false;
                    foreach (var atom in currentChain)
                    {
                        atom.grabHighlight(false);
                        atom.isGrabbed = false;
                        //atom.transform.parent = m_molecule.transform;
                        Destroy(atom.GetComponent<ParentConstraint>());
                    }
                    currentChain.Clear();
                    resetMolPositionAfterMove();
                    cmlData after = m_molecule.AsCML();
                    GlobalCtrl.Singleton.undoStack.AddChange(new MoveMoleculeAction(before, after));
                    EventManager.Singleton.StopMoveAtom(m_molecule.m_id, m_id);
                    EventManager.Singleton.MoveMolecule(m_molecule.m_id, m_molecule.transform.localPosition, m_molecule.transform.localRotation);
                }

                if (GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.NORMAL)
                {
                    //UnityEngine.Debug.Log($"[Atom] Interaction stopwatch: {stopwatch.ElapsedMilliseconds} [ms]");
                    if (stopwatch?.ElapsedMilliseconds < 200)
                    {
                        focus_id_tracker = -1;
                        this.m_molecule.focus_id_tracker = -1;
                        m_molecule.popAtomState();
                        resetMolPositionAfterMove();
                        EventManager.Singleton.StopMoveAtom(m_molecule.m_id, m_id);
                        EventManager.Singleton.MoveMolecule(m_molecule.m_id, m_molecule.transform.localPosition, m_molecule.transform.localRotation);
                        if (m_molecule.isMarked)
                        {
                            m_molecule.markMolecule(false);
                        }
                        else
                        {
                            markAtomUI(!isMarked);
                        }
                    }
                    else
                    {
                        resetMolPositionAfterMove();
                        cmlData after = m_molecule.AsCML();
                        GlobalCtrl.Singleton.undoStack.AddChange(new MoveMoleculeAction(before, after));
                        EventManager.Singleton.StopMoveAtom(m_molecule.m_id, m_id);
                        EventManager.Singleton.MoveMolecule(m_molecule.m_id, m_molecule.transform.localPosition, m_molecule.transform.localRotation);
                        // check for potential merge
                        if (GlobalCtrl.Singleton.collision)
                        {
                            Atom d1 = GlobalCtrl.Singleton.collider1;
                            Atom d2 = GlobalCtrl.Singleton.collider2;

                            Atom a1 = d1.dummyFindMain();
                            Atom a2 = d2.dummyFindMain();

                            if (!a1.alreadyConnected(a2))
                            {
                                if (a1 == this)
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
                }

                if (GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.MEASUREMENT)
                {
                    if (stopwatch?.ElapsedMilliseconds < 300)
                    {
                        handleMeasurements();
                    }
                }
            }
        }
    }
    #endregion

    /// <summary>
    /// This method is used in measurement mode to start, end and document distance and angle measurements.
    /// If the selected atom is the first of a pair, a distance measurement is created that connects to the tip of the index finger.
    /// If it is the second, the end of the distance measurement is attached to it.
    /// If there is a third atom connected by a distance measurement, an angle measurement is created between the three.
    /// 
    /// The new measurements are then registered in GlobalCtrl.
    /// </summary>
    private void handleMeasurements()
    {
        if (GlobalCtrl.Singleton.measurmentInHand == null)
        {
            var distMeasurementGO = Instantiate(distMeasurementPrefab);
            var distMeasurement = distMeasurementGO.GetComponent<DistanceMeasurement>();
            distMeasurement.StartAtom = this;
            GlobalCtrl.Singleton.measurmentInHand = distMeasurementGO;
            var otherDistanceMeasurments = GlobalCtrl.Singleton.getDistanceMeasurmentsOf(this); // order is important here
            GlobalCtrl.Singleton.distMeasurementDict[distMeasurement] = new Tuple<Atom, Atom>(this, null);
            if (otherDistanceMeasurments.Count > 0)
            {
                foreach (var m in otherDistanceMeasurments)
                {
                    var angleMeasurementGO = Instantiate(angleMeasurementPrefab);
                    var angleMeasurement = angleMeasurementGO.GetComponent<AngleMeasurement>();
                    angleMeasurement.originAtom = this;
                    angleMeasurement.distMeasurement1 = m;
                    if (m.StartAtom != this)
                    {
                        angleMeasurement.distMeasurement1Sign = -1f;
                    }
                    angleMeasurement.distMeasurement2 = distMeasurement;
                    GlobalCtrl.Singleton.angleMeasurementDict[angleMeasurement] = new Triple<Atom, DistanceMeasurement, DistanceMeasurement>(this, m, distMeasurement);
                }
            }
        }
        else
        {
            var distMeasurement = GlobalCtrl.Singleton.measurmentInHand.GetComponent<DistanceMeasurement>();
            if (distMeasurement.StartAtom == this) return;
            distMeasurement.EndAtom = this;
            var startAtom = GlobalCtrl.Singleton.distMeasurementDict[distMeasurement].Item1;
            GlobalCtrl.Singleton.distMeasurementDict[distMeasurement] = new Tuple<Atom, Atom>(startAtom, this);
            GlobalCtrl.Singleton.measurmentInHand = null;
            if (SettingsData.networkMeasurements)
            {
                EventManager.Singleton.CreateMeasurement(distMeasurement.StartAtom.m_molecule.m_id, distMeasurement.StartAtom.m_id, distMeasurement.EndAtom.m_molecule.m_id, distMeasurement.EndAtom.m_id);
            }
            var otherDistanceMeasurments = GlobalCtrl.Singleton.getDistanceMeasurmentsOf(this);
            if (otherDistanceMeasurments.Count > 1)
            {
                foreach (var m in otherDistanceMeasurments)
                {
                    if (m == distMeasurement) continue;
                    var angleMeasurementGO = Instantiate(angleMeasurementPrefab);
                    var angleMeasurement = angleMeasurementGO.GetComponent<AngleMeasurement>();
                    angleMeasurement.originAtom = this;
                    angleMeasurement.distMeasurement1 = m;
                    if (m.StartAtom != this)
                    {
                        angleMeasurement.distMeasurement1Sign = -1f;
                    }
                    angleMeasurement.distMeasurement2 = distMeasurement;
                    angleMeasurement.distMeasurement2Sign = -1f;
                    GlobalCtrl.Singleton.angleMeasurementDict[angleMeasurement] = new Triple<Atom, DistanceMeasurement, DistanceMeasurement>(this, m, distMeasurement);
                }
            }
        }
    }

    //[HideInInspector] public ushort m_id;
    public ushort m_id;
    [HideInInspector] public Molecule m_molecule;
    [HideInInspector] public ElementData m_data; // { get; private set; }
    // we have to clarify the role of m_data: Is this just basic (and constant) data?
    // 0: none; 1: sp1; 2: sp2;  3: sp3;  4: hypervalent trig. bipy; 5: unused;  6: hypervalent octahedral
    [HideInInspector] public Material m_mat;
    [HideInInspector] public Material frozen_mat;

    [HideInInspector] public Rigidbody m_rigid;
    [HideInInspector] public bool isGrabbed = false;
    [HideInInspector] public List<Vector3> m_posForDummies;

    [HideInInspector] public bool isMarked = false;

    [HideInInspector] public GameObject m_ActiveHand = null;

    #region manipulation

    /// <summary>
    /// initialises the atom with all it's attributes
    /// </summary>
    /// <param name="inputData"></param>
    /// <param name="inputMole"></param>
    /// <param name="pos"></param>
    /// <param name="idInScene"></param>
    public void f_Init(ElementData inputData, Molecule inputMole, Vector3 pos, ushort atom_id)
    {
        m_id = atom_id;
        m_molecule = inputMole;
        m_molecule.atomList.Add(this);
        m_data = inputData;


        gameObject.name = m_data.m_name;
        gameObject.tag = "Atom";
        //gameObject.layer = 6;
        //GetComponent<SphereCollider>().isTrigger = true;
        GetComponent<BoxCollider>().isTrigger = true;

        //I don't want to create the materials for all elements from the beginning,
        //so I only create a material for an element at the first time when I create this element,
        //and then add this material to the dictionary
        //So next time when I need to create this element,
        //I will use the dictionary to get a copy of an existent material.
        if (!GlobalCtrl.Singleton.Dic_AtomMat.ContainsKey(m_data.m_id))
        {
            Material tempMat = Instantiate(GlobalCtrl.Singleton.atomMatPrefab);
            tempMat.color = m_data.m_color;
            GlobalCtrl.Singleton.Dic_AtomMat.Add(m_data.m_id, tempMat);
        }
        if (m_data.m_abbre == "Dummy")
        {
            GetComponent<MeshRenderer>().material = GlobalCtrl.Singleton.dummyMatPrefab;
            m_mat = GetComponent<MeshRenderer>().material;
        }
        else
        {
            GetComponent<MeshRenderer>().material = GlobalCtrl.Singleton.Dic_AtomMat[m_data.m_id];
            m_mat = GetComponent<MeshRenderer>().material;
        }

        frozen_mat = Resources.Load("materials/frozenMaterial") as Material;

        transform.parent = inputMole.transform;
        transform.localPosition = pos;
        transform.localScale = Vector3.one * m_data.m_radius * (GlobalCtrl.scale / GlobalCtrl.u2pm) * GlobalCtrl.atomScale;

        //Debug.Log(string.Format("Added latest {0}:  rad={1}  scale={2}  hyb={3}  nBonds={4}", m_data.m_abbre, m_data.m_radius, GlobalCtrl.Singleton.atomScale, m_data.m_hybridization, m_data.m_bondNum));

        //Initial positions for dummies
        m_posForDummies = new List<Vector3>();
        Vector3 offset = new Vector3(0, 100, 0);
        // TODO: make this dependent on m_nBond and m_hybridization:

        //UnityEngine.Debug.Log($"[Atom:f_init] Hybridization {m_data.m_hybridization}");

        switch (m_data.m_hybridization)
        {
            case (0):
                break;
            case (1): // linear, max 2 bonds
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 120) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                break;
            case (2): // trigonal, max 3 bonds
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 120) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 240) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                break;
            case (3): // tetrahedral
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(70.53f, 60, 180) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(-70.53f, 0, 180) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 3) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(-70.53f, 120, 180) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                break;
            case (4): // trigonal bipyramidal
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 180) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 90) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 3) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(120, 0, 180) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 4) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(240, 0, 180) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                break;
            case (6): // octahedral  (with 4 bonds: quadratic planar)
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 1) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 180) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 2) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 90) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 3) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(180, 0, 90) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 4) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(90, 0, 90) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                if (m_data.m_bondNum > 5) m_posForDummies.Add(transform.localPosition + Quaternion.Euler(270, 0, 90) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                break;
            default:  // fall-back ... we have to see how to do error handling here
                m_posForDummies.Add(transform.localPosition + Quaternion.Euler(0, 0, 0) * offset * (GlobalCtrl.scale / GlobalCtrl.u2pm));
                UnityEngine.Debug.Log("[Atom] InitDummies: Landed in Fallback!");
                break;
        }

    }


    /// <summary>
    /// modify the atom using the info on ElementData
    /// </summary>
    /// <param name="newData"></param>
    public void f_Modify(ElementData newData)
    {
        int numConnected = connectedAtoms().Count;
        m_data = newData;
        uint dummyLimit = m_data.m_bondNum;
        gameObject.name = m_data.m_name;
        if (!GlobalCtrl.Singleton.Dic_AtomMat.ContainsKey(m_data.m_id))
        {
            Material tempMat = Instantiate(GlobalCtrl.Singleton.atomMatPrefab);
            tempMat.color = m_data.m_color;
            GlobalCtrl.Singleton.Dic_AtomMat.Add(m_data.m_id, tempMat);
        }
        GetComponent<MeshRenderer>().material = GlobalCtrl.Singleton.Dic_AtomMat[m_data.m_id];
        m_mat = GetComponent<MeshRenderer>().material;

        transform.localScale = Vector3.one * m_data.m_radius * (GlobalCtrl.scale / GlobalCtrl.u2pm) * GlobalCtrl.atomScale;


        foreach (Atom a in connectedDummys())
        {
            if (numConnected > dummyLimit)
            {
                numConnected--;
                a.m_molecule.atomList.Remove(a);
                Bond b = a.connectedBonds()[0];
                b.m_molecule.bondList.Remove(b);
                Destroy(a.gameObject);
                Destroy(b.gameObject);
            }
        }

        while (dummyLimit > numConnected)
        {
            UnityEngine.Debug.Log($"[Atom:f_modify] Adding dummies. Limit: {dummyLimit}, current connected: {numConnected}");
            addDummy(numConnected);
            numConnected++;
        }

        m_molecule.shrinkAtomIDs();

        foreach (Atom a in connectedAtoms())
        {
            Bond bond = getBond(a);
            bond.setShaderProperties();
        }

        // Debug.Log(string.Format("Modified latest {0}:  rad={1}   scale={2} ", m_data.m_abbre, m_data.m_radius, GlobalCtrl.Singleton.atomScale));
    }

    /// <summary>
    /// Handles the transfer of the movement of a single atom (with 
    /// consequences because of the force field) to the containing molecule.
    /// </summary>
    public void resetMolPositionAfterMove()
    {

        // reset molecule position
        Vector3 molCenter = m_molecule.getCenterInAtomWorld();
        var mol_rot = m_molecule.transform.localRotation;
        m_molecule.transform.localRotation = Quaternion.identity;
        var mol_center_rotated = m_molecule.getCenterInAtomWorld();
        // positions relative to the molecule center
        foreach (Atom a in m_molecule.atomList)
        {
            a.transform.localPosition = (GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(a.transform.position) - mol_center_rotated) * (1f/m_molecule.transform.localScale.x);
        }
        // rotate back
        m_molecule.transform.localRotation = mol_rot;
        m_molecule.transform.localPosition = molCenter;
        // scale, position and orient bonds
        foreach (Bond bond in m_molecule.bondList)
        {
            Atom a1 = m_molecule.atomList.ElementAtOrDefault(bond.atomID1);
            Atom a2 = m_molecule.atomList.ElementAtOrDefault(bond.atomID2);
            var a1_pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(a1.transform.position);
            var a2_pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(a2.transform.position);
            float offset1 = a1.m_data.m_radius * ForceField.scalingfactor*GlobalCtrl.atomScale*GlobalCtrl.scale  * 0.8f * m_molecule.transform.localScale.x;
            float offset2 = a2.m_data.m_radius * ForceField.scalingfactor*GlobalCtrl.atomScale*GlobalCtrl.scale  * 0.8f * m_molecule.transform.localScale.x;
            float distance = (Vector3.Distance(a1_pos, a2_pos) - offset1 - offset2) / m_molecule.transform.localScale.x;
            bond.transform.localScale = new Vector3(bond.transform.localScale.x, bond.transform.localScale.y, distance);
            Vector3 pos1 = Vector3.MoveTowards(a1_pos, a2_pos, offset1);
            Vector3 pos2 = Vector3.MoveTowards(a2_pos, a1_pos, offset2);
            bond.transform.position = (pos1 + pos2) / 2;
            bond.transform.LookAt(a2.transform.position);
        }
    }

    /// <summary>
    /// Adds a dummy to the current atom.
    /// </summary>
    /// <param name="numConnected">The number of already connected atoms</param>
    public void addDummy(int numConnected)
    {
        List<Atom> conAtoms = connectedAtoms();

        Vector3 position = new Vector3();
        Vector3 firstVec = new Vector3();
        Vector3 secondVec = new Vector3();
        Vector3 normalVec = new Vector3();
        switch (numConnected)
        {
            case (0):
                position = transform.localPosition + new Vector3(0, 0, 0.05f);
                GlobalCtrl.Singleton.CreateDummy(m_molecule.getFreshAtomID(), m_molecule, this, position);
                break;
            case (1):
                firstVec = transform.localPosition - conAtoms[0].transform.localPosition;
                position = transform.localPosition + firstVec;
                GlobalCtrl.Singleton.CreateDummy(m_molecule.getFreshAtomID(), m_molecule, this, position);
                break;
            case (2):
                firstVec = transform.localPosition - conAtoms[0].transform.localPosition;
                secondVec = transform.localPosition - conAtoms[1].transform.localPosition;
                position = transform.localPosition + ((firstVec + secondVec) / 2.0f);
                if (position == transform.localPosition)
                    position = Vector3.Cross(firstVec, secondVec);
                GlobalCtrl.Singleton.CreateDummy(m_molecule.getFreshAtomID(), m_molecule, this, position);
                break;
            case (3):
                firstVec = conAtoms[1].transform.localPosition - conAtoms[0].transform.localPosition;
                secondVec = conAtoms[2].transform.localPosition - conAtoms[0].transform.localPosition;
                normalVec = new Vector3(firstVec.y * secondVec.z - firstVec.z * secondVec.y, firstVec.z * secondVec.x - firstVec.x * secondVec.z, firstVec.x * secondVec.y - firstVec.y * secondVec.x);
                position = transform.localPosition + normalVec;

                float sideCheck1 = normalVec.x * transform.localPosition.x + normalVec.y * transform.localPosition.y + normalVec.z * transform.localPosition.z;
                float sideCheck2 = position.x * transform.localPosition.x + position.y * transform.localPosition.y + position.z * transform.localPosition.z;

                if ((sideCheck1 >= 0 && sideCheck2 >= 0) || (sideCheck1 <= 0 && sideCheck2 <= 0))
                {
                    position = transform.localPosition - normalVec;
                }

                GlobalCtrl.Singleton.CreateDummy(m_molecule.getFreshAtomID(), m_molecule, this, position);
                break;
            case (4):
                position = (conAtoms[1].transform.localPosition - conAtoms[0].transform.localPosition) / 2.0f;

                GlobalCtrl.Singleton.CreateDummy(m_molecule.getFreshAtomID(), m_molecule, this, position);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// changes color of selected and deselected atoms
    /// </summary>
    /// <param name="isOn">if this atom is selected</param>
    public void colorSwapSelect(int col)
    {
        var outline_component = GetComponent<OutlinePro>();
        if (col == 1)
        {
            // merging
            GetComponent<Renderer>().material = GlobalCtrl.Singleton.overlapMat;
        }
        else if (col == 2)
        {
            // single component
            //GetComponent<Renderer>().material = GlobalCtrl.Singleton.markedMat;
            outline_component.enabled = true;
            for (int i = 0; i < FocusManager.maxNumOutlines; i++)
            {
                outline_component.OutlineColor[i] = chARpackColors.yellow;
                backupOutlineColor[i] = chARpackColors.yellow;
            }
            previousHighlightType = currentHighlightType;
            currentHighlightType = FocusManager.HighlightType.Select;
            selectHighlightInFormula(outline_component.OutlineColor);
        }
        else if (col == 3)
        {
            // as part of single bond
            outline_component.enabled = true;
            for (int i = 0; i < FocusManager.maxNumOutlines; i++)
            {
                outline_component.OutlineColor[i] = chARpackColors.orange; //orange
                backupOutlineColor[i] = chARpackColors.orange;
            }
            previousHighlightType = currentHighlightType;
            currentHighlightType = FocusManager.HighlightType.Select;
            selectHighlightInFormula(outline_component.OutlineColor);
        }
        else if (col == 4)
        {
            // as part of angle bond
            outline_component.enabled = true;
            for (int i = 0; i < FocusManager.maxNumOutlines; i++)
            {
                outline_component.OutlineColor[i] = chARpackColors.red;
                backupOutlineColor[i] = chARpackColors.red;
            }
            previousHighlightType = currentHighlightType;
            currentHighlightType = FocusManager.HighlightType.Select;
            selectHighlightInFormula(outline_component.OutlineColor);
        }
        else if (col == 5)
        {
            // as part of angle bond
            outline_component.enabled = true;
            for (int i = 0; i < FocusManager.maxNumOutlines; i++)
            {
                outline_component.OutlineColor[i] = chARpackColors.green;
                backupOutlineColor[i] = chARpackColors.green;
            }
            previousHighlightType = currentHighlightType;
            currentHighlightType = FocusManager.HighlightType.Select;
            selectHighlightInFormula(outline_component.OutlineColor);
        }
        else
        {
            // reset or nothing
            outline_component.enabled = false;
            previousHighlightType = currentHighlightType;
            currentHighlightType = FocusManager.HighlightType.None;
            if (previousHighlightType == FocusManager.HighlightType.Select)
            {
                selectHighlightInFormula(new Color[4] { chARpackColors.notEnabledColor, chARpackColors.notEnabledColor, chARpackColors.notEnabledColor, chARpackColors.notEnabledColor });
            }
            if (m_data.m_abbre.ToLower() == "dummy")
            {
                GetComponent<Renderer>().material = GlobalCtrl.Singleton.dummyMatPrefab;
            }
            else
            {
                GetComponent<Renderer>().material = GlobalCtrl.Singleton.Dic_AtomMat[m_data.m_id];
            }
        }

        outline_component.NeedsUpdate();
    }

    #endregion

    private void OnTriggerEnter(Collider collider)
    {
        // Debug.Log($"[Atom] Collision Detected: {collider.name}");
        if (collider.name.StartsWith("Dummy") && name.StartsWith("Dummy") && GlobalCtrl.Singleton.collision == false)
        {

            GlobalCtrl.Singleton.collision = true;
            GlobalCtrl.Singleton.collider1 = collider.GetComponent<Atom>();
            GlobalCtrl.Singleton.collider2 = GetComponent<Atom>();
            GlobalCtrl.Singleton.collider1.colorSwapSelect(1);
            GlobalCtrl.Singleton.collider2.colorSwapSelect(1);
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.name.StartsWith("Dummy") && name.StartsWith("Dummy"))
        {
            if (GlobalCtrl.Singleton.collider1 != null)
            {
                GlobalCtrl.Singleton.collider1.colorSwapSelect(0);
                GlobalCtrl.Singleton.collider1 = null;
            }
            if (GlobalCtrl.Singleton.collider2 != null)
            {
                GlobalCtrl.Singleton.collider2.colorSwapSelect(0);
                GlobalCtrl.Singleton.collider2 = null;
            }
            GlobalCtrl.Singleton.collision = false;
        }
    }

    #region queries

    /// <summary>
    /// this method calculates a list of all connected atoms for a given atom
    /// </summary>
    /// <returns>list of connected atoms</returns>
    public List<Atom> connectedAtoms()
    {
        List<Atom> conAtomList = new List<Atom>();
        foreach (Bond b in m_molecule.bondList)
        {
            if (b.atomID1 == m_id || b.atomID2 == m_id)
            {
                Atom otherAtom = b.findTheOther(this);
                if (!conAtomList.Contains(otherAtom))
                    conAtomList.Add(otherAtom);
            }
        }
        return conAtomList;
    }

    /// <summary>
    /// Calculates a list of all connected atoms for a given atom,
    /// excluding a specific one.
    /// </summary>
    /// <param name="exclude">The atom that should not appear in the list</param>
    /// <returns>list of connected atoms without <c>exclude</c></returns>
    public List<Atom> otherConnectedAtoms(Atom exclude)
    {
        var conList = connectedAtoms();
        if (conList.Contains(exclude))
        {
            conList.Remove(exclude);
        }
        return conList;
    }

    /// <summary>
    /// Calculates a list of all connected atoms for a given atom,
    /// excluding multiple specific ones.
    /// </summary>
    /// <param name="exclude">The atoms that should not appear in the list</param>
    /// <returns>list of connected atoms without <c>exclude</c></returns>
    public HashSet<Atom> otherConnectedAtoms(HashSet<Atom> exclude)
    {
        HashSet<Atom> conAtomList = new HashSet<Atom>();

        // Convert to HashSet
        var conList = connectedAtoms();
        foreach (var atom in conList)
        {
            conAtomList.Add(atom);
        }

        // Do exclude
        foreach (var atom in exclude)
        {
            if (conAtomList.Contains(atom))
            {
                conAtomList.Remove(atom);
            }
        }

        return conAtomList;
    }

    /// <summary>
    /// Computes the chain of atoms from the current one in the direction
    /// of <c>exceptAtom</c>.
    /// </summary>
    /// <param name="exceptAtom">The atom in the direction of the chain</param>
    /// <returns>A list of atoms in the chain</returns>
    public List<Atom> connectedChain(Atom exceptAtom)
    {
        // get start set
        var chainAtomList = otherConnectedAtoms(exceptAtom);

        HashSet<Atom> prevLayer = new HashSet<Atom>();
        prevLayer.Add(exceptAtom);

        bool not_reached_end = true;
        HashSet<Atom> currentLayer = new HashSet<Atom>();
        foreach (var atom in chainAtomList)
        {
            currentLayer.Add(atom);
        }
        while (not_reached_end)
        {
            HashSet<Atom> nextLayer = new HashSet<Atom>();
            foreach (var atom in currentLayer)
            {
                var conAtoms = atom.otherConnectedAtoms(prevLayer);
                foreach (var conAtom in conAtoms)
                {
                    nextLayer.Add(conAtom);
                }
            }
            foreach (var to_add in nextLayer)
            {
                chainAtomList.Add(to_add);
            }

            if (nextLayer.Count == 0)
            {
                not_reached_end = false;
            }
            // Update layers
            foreach (var a in currentLayer)
            {
                prevLayer.Add(a);
            }
            currentLayer = nextLayer;
        }
        return new List<Atom>(new HashSet<Atom>(chainAtomList));
    }

    /// <summary>
    /// Returns all dummies connected to the current atom.
    /// </summary>
    /// <returns>list of connected dummies</returns>
    public List<Atom> connectedDummys()
    {
        List<Atom> allConnected = connectedAtoms();
        List<Atom> conDummys = new List<Atom>();
        foreach (Atom at in allConnected)
        {
            if (at.m_data.m_abbre == "Dummy")
                conDummys.Add(at);
        }

        return conDummys;
    }

    /// <summary>
    /// this method calculates a list of all connected bonds for a given atom
    /// </summary>
    /// <returns>list of connected bonds</returns>
    public List<Bond> connectedBonds()
    {
        List<Bond> conBondList = new List<Bond>();
        foreach (Bond b in m_molecule.bondList)
        {
            if (b.atomID1 == m_id || b.atomID2 == m_id)
            {
                conBondList.Add(b);
            }
        }
        return conBondList;
    }

    /// <summary>
    /// this method returns a bond between two atoms
    /// </summary>
    /// <param name="a1">first atom of the bond</param>
    /// <param name="a2">second atom of the bond</param>
    /// <returns>the bond between the two atoms</returns>
    public Bond getBond(Atom a2)
    {
        foreach (Bond b in m_molecule.bondList)
        {
            if (b.atomID1 == m_id && b.atomID2 == a2.m_id)
                return b;
            else if (b.atomID2 == m_id && b.atomID1 == a2.m_id)
                return b;
        }
        return null;
    }


    /// <summary>
    /// this method returns the main atom for a given dummy atom
    /// </summary>
    /// <param name="dummy">the dummy atom</param>
    /// <returns>the main atom of the dummy</returns>
    public Atom dummyFindMain()
    {
        if (m_data.m_name == "Dummy")
        {
            Bond b = m_molecule.bondList.Find(p => p.atomID1 == m_id || p.atomID2 == m_id);
            Atom a;
            if (m_id == b.atomID1)
            {
                a = m_molecule.atomList.ElementAtOrDefault(b.atomID2);
            }
            else
            {
                a = m_molecule.atomList.ElementAtOrDefault(b.atomID1);
            }
            if (a == default)
            {
                throw new Exception("[Atom:dummyFindMain] Could not find Atom on the other side of the bond.");
            }
            return a;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// this method tests if two atoms are already connected
    /// </summary>
    /// <param name="a1">the first atom</param>
    /// <param name="a2">the second atom</param>
    /// <returns>true or false depending on if the atoms are connected</returns>
    public bool alreadyConnected(Atom a2)
    {
        foreach (Bond b in m_molecule.bondList)
        {
            if (b.findTheOther(this) == a2)
                return true;
        }

        if (this == a2)
            return true;

        return false;
    }

    #endregion

    #region marking
    private void markConnectedBonds(bool mark)
    {
        foreach (var bond in connectedBonds())
        {
            bond.markBond(mark);
        }
    }

    /// <summary>
    /// this method marks the atom in a different color if selected
    /// </summary>
    /// <param name="mark">true or false if the atom should be marked</param>
    public void markAtom(bool mark, ushort mark_case = 2, bool toolTip = false)
    {

        isMarked = mark;

        if (isMarked)
        {
            colorSwapSelect(mark_case);
            if (!toolTipInstance && toolTip)
            {
                if (SceneManager.GetActiveScene().name.Equals("ServerScene")) 
                { 
                    createServerToolTip(this); 
               
                }
                else 
                { 
                    createToolTip(); 
                }
                
            }
            if (!m_molecule.isMarked)
            {
                markedAtoms.Add(this);
            }
            else
            {
                // Remove single marked atoms if whole molecule selected
                markedAtoms.Remove(this);
            }
        }
        else
        {
            if (toolTipInstance)
            {
                Destroy(toolTipInstance);
                toolTipInstance = null;
            }
            colorSwapSelect(0);
            markConnectedBonds(false);
            markedAtoms.Remove(this);
        }
        // destroy tooltip of marked without flag
        if (!toolTip && toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
    }

    /// <summary>
    /// Marks a list of atoms and potentially creates corresponding tool tips,
    /// depending on whether one, two (single bond), three (angle bond)
    /// or four (torsion bond) connected atoms are selected.
    /// </summary>
    /// <param name="toolTip">Whether to spawn a tool tip</param>
    public void markConnections(bool toolTip = false)
    {
        // check for connected atom
        var markedList = new List<Atom>();
        foreach (var atom in m_molecule.atomList)
        {
            if (atom.isMarked)
            {
                 markedList.Add(atom);
            }
        }
        if (markedList.Count == 1)
        {
            Vector3 rectSave = new Vector3(0,0,0);
            if(m_molecule.toolTipInstance != null)
            {
            rectSave = m_molecule.toolTipInstance.GetComponent<RectTransform>().localPosition;
            Destroy(m_molecule.toolTipInstance);
            m_molecule.toolTipInstance = null;
            }
            markedList[0].markAtom(true, 2, toolTip);
            markedList[0].toolTipInstance.GetComponent<ServerAtomTooltip>().localPosition = rectSave;
        }
        else if (markedList.Count == 2)
        {
            foreach (var bond in m_molecule.bondTerms)
            {
                if (bond.Contains(markedList[0].m_id) && bond.Contains(markedList[1].m_id))
                {
                    if (toolTip)
                    {
                        if(SceneManager.GetActiveScene().name == "ServerScene")
                        {
                            m_molecule.createServerBondToolTip(bond);
                        }                        
                        else
                        {
                            m_molecule.createBondToolTip(bond);
                        }
                    }
                    else
                    {
                        m_molecule.markBondTerm(bond, true);
                    }
                }
            }
        }
        else if (markedList.Count == 3)
        {
            var atom1 = markedList[0];
            var atom2 = markedList[1];
            var atom3 = markedList[2];
            foreach (var angle in m_molecule.angleTerms)
            {
                if (angle.Contains(atom1.m_id) && angle.Contains(atom2.m_id) && angle.Contains(atom3.m_id))
                {
                    if (toolTip)
                    {
                        if(!(SceneManager.GetActiveScene().name == "ServerScene"))
                        {
                            m_molecule.createAngleToolTip(angle);
                        }
                        else
                        {
                            m_molecule.createServerAngleToolTip(angle);
                        }
                    }
                    else
                    {
                        m_molecule.markAngleTerm(angle, true);
                    }
                }
            }

        }
        else if (markedList.Count == 4)
        {
            var atom1 = markedList[0];
            var atom2 = markedList[1];
            var atom3 = markedList[2];
            var atom4 = markedList[3];
            foreach (var torsion in m_molecule.torsionTerms)
            {
                if (torsion.Contains(atom1.m_id) && torsion.Contains(atom2.m_id) && torsion.Contains(atom3.m_id) && torsion.Contains(atom4.m_id))
                {
                    if (toolTip)
                    {
                        if(!(SceneManager.GetActiveScene().name == "ServerScene"))
                        {
                            m_molecule.createTorsionToolTip(torsion);
                        }
                        else
                        {
                            m_molecule.createServerTorsionToolTip(torsion);
                        }
                    }
                    else
                    {
                        m_molecule.markTorsionTerm(torsion, true);
                    }
                    break; // TODO Delete when double torsion fixed
                }
            }
        }
        else
        {

        }
        if(toolTipInstance == null && m_molecule.toolTipInstance == null){markedList.RemoveRange(0,markedList.Count);}
    }

    /// <summary>
    /// Marks an atom with networking and differentiates between a
    /// single selected atom and multiple connected ones.
    /// </summary>
    /// <param name="mark">whether the atom should be marked</param>
    /// <param name="toolTip">whether to spawn a tool tip</param>
    public void markAtomUI(bool mark, bool toolTip = true)
    {
        EventManager.Singleton.SelectAtom(m_molecule.m_id, m_id, !isMarked);
        advancedMarkAtom(mark, toolTip);
    }

    public void advancedMarkAtom(bool mark, bool toolTip = false)
    {
        markAtom(mark, 2, toolTip);
        markConnections(toolTip);
    }

    /// <summary>
    /// Helper method to find out if atom is part of angle bond (for tool tip regeneration)
    /// </summary>
    public bool anyConnectedAtomsMarked()
    {
        var conAtoms = connectedAtoms();
        foreach (Atom a in conAtoms)
        {
            if (a.isMarked)
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    #region tooltip

    /// <summary>
    /// Creates a tool tip for a single atom.
    /// This includes a localized tool tip text with data about the atom as 
    /// well as multiple buttons for different user interactions 
    /// (e.g. freezing the atom).
    /// </summary>
    public void createToolTip()
    {
        if (toolTipInstance)
        {
            Destroy(toolTipInstance);
        }
        // create tool tip
        toolTipInstance = Instantiate(myAtomToolTipPrefab);
        // calc position for tool tip
        // first: get position in the bounding box and decide if the tool tip spawns left, right, top or bottom of the box
        Vector3 mol_center = m_molecule.getCenter();
        // project to camera coordnates
        Vector2 mol_center_in_cam = new Vector2(Vector3.Dot(mol_center, GlobalCtrl.Singleton.mainCamera.transform.right), Vector3.Dot(mol_center, GlobalCtrl.Singleton.mainCamera.transform.up));
        Vector2 atom_pos_in_cam = new Vector2(Vector3.Dot(transform.position, GlobalCtrl.Singleton.mainCamera.transform.right), Vector3.Dot(transform.position, GlobalCtrl.Singleton.mainCamera.transform.up));
        // calc diff
        Vector2 diff_mol_atom = atom_pos_in_cam - mol_center_in_cam;
        // enhance diff for final tool tip pos
        Vector3 ttpos = transform.position + toolTipDistanceWeight * diff_mol_atom[0] * GlobalCtrl.Singleton.mainCamera.transform.right + toolTipDistanceWeight * diff_mol_atom[1] * GlobalCtrl.Singleton.mainCamera.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add atom as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = gameObject;
        var con_atoms = connectedAtoms();
        string toolTipText = getToolTipText(m_data.m_name, m_data.m_mass, m_data.m_radius, con_atoms.Count);
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;
        GameObject modifyHybridizationInstance = null;
        if (m_data.m_abbre != "Dummy")
        {

            if (m_data.m_abbre == "H")
            {
                var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
                modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { toolTipHelperChangeAtom("Dummy"); });
                modifyButtonInstance.GetComponent<ButtonConfigHelper>().MainLabelText = "To Dummy";
                toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);
            }

            var delButtonInstance = Instantiate(deleteMeButtonPrefab);
            delButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.deleteAtomUI(this); });
            toolTipInstance.GetComponent<DynamicToolTip>().addContent(delButtonInstance);

            modifyHybridizationInstance = Instantiate(modifyHybridizationPrefab);
            modifyHybridizationInstance.GetComponent<modifyHybridization>().currentAtom = this;
        }
        else
        {
            var modifyButtonInstance = Instantiate(modifyMeButtonPrefab);
            modifyButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { toolTipHelperChangeAtom("H"); });
            modifyButtonInstance.GetComponent<ButtonConfigHelper>().MainLabelText = "To Hydrogen";
            toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyButtonInstance);
        }
        var freezeButtonInstance = Instantiate(freezeMePrefab);
        freezeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { freezeUI(!frozen); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(freezeButtonInstance);
        freezeButton = freezeButtonInstance;

        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markAtomUI(false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);

        // add last
        if (modifyHybridizationInstance != null)
        {
            toolTipInstance.GetComponent<DynamicToolTip>().addContent(modifyHybridizationInstance);
        }

        // Starting color for indicator
        setFrozenVisual(frozen);

    }


    private void toolTipHelperChangeAtom(string chemAbbre)
    {
        GlobalCtrl.Singleton.changeAtomUI(m_molecule.m_id, m_id, chemAbbre);
        markAtomUI(false);
    }


    public void OnDestroy()
    {
        if (toolTipInstance)
        {
            Destroy(toolTipInstance);
            toolTipInstance = null;
        }
        GlobalCtrl.Singleton.deleteMeasurmentsOf(this);
    }

    // Helper methods to generate localized tool tip text
    private string getToolTipText(string name, double mass, double radius, int bondNum)
    {
        //$"Name: {m_data.m_name}\nMass: {m_data.m_mass}\nRadius: {m_data.m_radius}\nNumBonds: {m_data.m_bondNum}"
        string rad = GlobalCtrl.Singleton.GetLocalizedString("RADIUS");
        string numBonds = GlobalCtrl.Singleton.GetLocalizedString("NUM_BONDS");
        string massStr = GlobalCtrl.Singleton.GetLocalizedString("MASS");
        string nameStr = GlobalCtrl.Singleton.GetLocalizedString("NAME");
        name = GetLocalizedElementName(name);
        double radius_in_angstrom = radius * 0.01f;
        string radiusInCorrectUnit = SettingsData.useAngstrom ? $"{radius_in_angstrom:0.00}" : $"{radius:0}";
        string unit = SettingsData.useAngstrom ? "\u00C5" : "pm";
        string toolTipText = $"{nameStr}: {name}\n{massStr}: {mass:0.00}u\n{rad}: {radiusInCorrectUnit}{unit}\n{numBonds}: {bondNum}";
        return toolTipText;
    }

    /// <summary>
    /// Gets the localized version of the atom's element name.
    /// </summary>
    /// <param name="text">the key corresponding to the correct entry in the "Elements" table</param>
    /// <returns>a string with the localized element name</returns>
    public string GetLocalizedElementName(string text)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString("Elements", text);
    }
    public void createServerToolTip(Atom thisAtom)
    {
        if (toolTipInstance)
        {
            Destroy(toolTipInstance);
        }

        toolTipInstance = Instantiate(serverTooltipPrefab);
        toolTipInstance.GetComponent<ServerAtomTooltip>().hybridUp.onClick.AddListener(delegate { toolTipInstance.GetComponent<ServerAtomTooltip>().increase(); });
        toolTipInstance.GetComponent<ServerAtomTooltip>().hybridDown.onClick.AddListener(delegate {  toolTipInstance.GetComponent<ServerAtomTooltip>().decrease(); });
        toolTipInstance.GetComponent<ServerAtomTooltip>().closeButton.onClick.AddListener(delegate { markAtomUI(false);  });
        var con_atoms = connectedAtoms();
        toolTipInstance.GetComponent<ServerAtomTooltip>().TooltipText.text = getToolTipText(m_data.m_name, m_data.m_mass, m_data.m_radius, con_atoms.Count);
        toolTipInstance.GetComponent<ServerAtomTooltip>().deleteButton.onClick.AddListener(delegate { GlobalCtrl.Singleton.deleteAtomUI(this); });
        toolTipInstance.GetComponent<ServerAtomTooltip>().freezeButton.onClick.AddListener(delegate { freezeUI(!frozen); });
        toolTipInstance.GetComponent<ServerAtomTooltip>().linkedAtom = thisAtom;
        
        


    }

        #endregion

        #region focus_highlight

        private void Start()
    {
        var et = GetComponent<EyeTrackingTarget>();
        et.OnLookAtStart.AddListener(delegate { onLookStart(); });
        et.OnLookAway.AddListener(delegate { onLookAway(); });
    }

    // Handle Gaze events
    private void onLookStart()
    {
        if (SettingsData.gazeHighlighting)
        {
            proccessFocusUI(true);
        }
        else
        {
            EventManager.Singleton.FocusHighlight(m_molecule.m_id, m_id, true);
        }
    }

    private void onLookAway()
    {
        if (SettingsData.gazeHighlighting)
        {
            proccessFocusUI(false);
        }
        else
        {
            EventManager.Singleton.FocusHighlight(m_molecule.m_id, m_id, false);
        }
    }

    // Handle mrtk focus events
    void IMixedRealityFocusHandler.OnFocusEnter(FocusEventData eventData)
    {
        OnFocusEnter(eventData);
    }

    void IMixedRealityFocusHandler.OnFocusExit(FocusEventData eventData)
    {
        OnFocusExit(eventData);
    }

    public void OnFocusEnter(FocusEventData eventData)
    {
        if (SettingsData.pointerHighlighting)
        {
            proccessFocusUI(true);

        }
        else
        {
            EventManager.Singleton.FocusHighlight(m_molecule.m_id, m_id, true);
        }
    }

    public void OnFocusExit(FocusEventData eventData)
    {
        if (SettingsData.pointerHighlighting)
        {
            proccessFocusUI(false);
        }
        else
        {
            EventManager.Singleton.FocusHighlight(m_molecule.m_id, m_id, false);
        }
    }

    public void networkSetFocus(bool focus, int focus_id)
    {
        if (NetworkManagerServer.Singleton != null || SettingsData.showAllHighlightsOnClients)
        {
            proccessFocus(focus, focus_id);
            focus_id_tracker = focus_id;
            this.m_molecule.focus_id_tracker = focus_id;
        }
    }

    /// <summary>
    /// Outlines the current atom in focusColor; is used when a pointer from the index finger gets close to the atom.
    /// </summary>
    /// <param name="active">Whether to activate or deactivate the focusColor outline</param>
    public void updateFocusHighlight()
    {
        if (!focus_alpha.AllZero())
        {
            var outline_component = GetComponent<OutlinePro>();
            // if(active) { 
            if (currentHighlightType == FocusManager.HighlightType.Grab ||
                currentHighlightType == FocusManager.HighlightType.Select ||
                currentHighlightType == FocusManager.HighlightType.ServerFocus)
            {
                return;
            }
            else if (currentHighlightType == FocusManager.HighlightType.None)
            {
                for (int i = 0; i < FocusManager.maxNumOutlines; i++)
                {
                    backupOutlineColor[i] = outline_component.OutlineColor[i];
                }
                previousHighlightType = currentHighlightType;
                currentHighlightType = FocusManager.HighlightType.Focus;
                outline_component.enabled = true;
            }

            foreach (var focus_id in FocusManager.getForcusIDsInUse())
            {
                var pos = FocusManager.getPosInArray(focus_id);
                var col = FocusColors.getColor(focus_id);
                col.a = focus_alpha[pos];
                outline_component.OutlineColor[pos] = col;
                outline_component.OutlineWidth[pos] = outline_radius_current[pos];
            }
            outline_component.NeedsUpdate();
            focusHighlightInFormula(focused, GetComponent<OutlinePro>().OutlineColor);
        }
    }

    public void proccessFocusUI(bool value, int? f_id = null)
    {
        var focus_id = f_id.HasValue ? f_id.Value : FocusManager.getMyFocusID();
        if (focus_id >= 0)
        {
            var pos = FocusManager.getPosInArray(focus_id);
            focused[pos] = value;
            focusHighlightInFormula(focused, null);
            EventManager.Singleton.FocusHighlight(m_molecule.m_id, m_id, value);
        }
        else
        {
            serverFocusHighlightUI(value);
        }
    }

    private void proccessFocus(bool value, int? f_id = null)
    {
        var focus_id = f_id.HasValue ? f_id.Value : FocusManager.getMyFocusID();
        if (focus_id >= 0)
        {
            var pos = FocusManager.getPosInArray(focus_id);
            focused[pos] = value;
            focusHighlightInFormula(focused, null);

        }
        else
        {
            serverFocusHighlight(value);
        }
    }

    public void serverFocusHighlightUI(bool value)
    {

        serverFocusHighlight(value);
        EventManager.Singleton.ServerFocusHighlight(m_molecule.m_id, m_id, value);

    }

    public void serverFocusHighlight(bool active)
    {
        serverFocus = active;
        var outline_component = GetComponent<OutlinePro>();
        if (active)
        {
            if (currentHighlightType == FocusManager.HighlightType.Grab)
            {
                return;
            }
            else if (currentHighlightType == FocusManager.HighlightType.None || currentHighlightType == FocusManager.HighlightType.Select)
            {
                outline_component.enabled = true;
                for (int i = 0; i < FocusManager.maxNumOutlines; i++)
                {
                    backupOutlineColor[i] = outline_component.OutlineColor[i];
                }
                previousHighlightType = currentHighlightType;
                currentHighlightType = FocusManager.HighlightType.ServerFocus;
            }

            for (int i = 0; i < FocusManager.maxNumOutlines; i++)
            {
                outline_component.OutlineColor[i] = FocusColors.getColor(-1);
            }
            serverFocusHighlightInFormula(outline_component.OutlineColor);
            outline_component.NeedsUpdate();
        }
        else
        {
            if (previousHighlightType == FocusManager.HighlightType.None)
            {
                outline_component.enabled = false;
                previousHighlightType = currentHighlightType;
                currentHighlightType = FocusManager.HighlightType.None;
                serverFocusHighlightInFormula(new Color[4] { chARpackColors.notEnabledColor, chARpackColors.notEnabledColor , chARpackColors.notEnabledColor , chARpackColors.notEnabledColor });
            }
            else
            {
                for (int i = 0; i < FocusManager.maxNumOutlines; i++)
                {
                    outline_component.OutlineColor[i] = backupOutlineColor[i];
                }
                currentHighlightType = previousHighlightType;
                previousHighlightType = FocusManager.HighlightType.ServerFocus;
                serverFocusHighlightInFormula(backupOutlineColor);
                outline_component.NeedsUpdate();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!serverFocus)
        {
            foreach (var focus_id in FocusManager.getForcusIDsInUse())
            {
                var pos = FocusManager.getPosInArray(focus_id);
                if (focused[pos])
                {
                    focus_alpha[pos] += focus_ramping_constant;
                    outline_radius_current[pos] += 3 * focus_ramping_constant;
                    if (focus_alpha[pos] > 1f) focus_alpha[pos] = 1f;
                    if (outline_radius_current[pos] > outline_radius_max) outline_radius_current[pos] = outline_radius_max;
                }
                else if (focus_alpha[pos] != 0f)
                {
                    focus_alpha[pos] -= focus_ramping_constant;
                    outline_radius_current[pos] -= 3 * focus_ramping_constant;
                    if (focus_alpha[pos] <= 0f)
                    {
                        outline_radius_current[pos] = outline_radius_min;
                        focus_alpha[pos] = 0f;
                    }
                }
            }
            updateFocusHighlight();
        }
    }

    #endregion

    #region freezing

    /// <summary>
    /// Freezes/unfreezes the current atom and triggers
    /// a network event for this action.
    /// </summary>
    /// <param name="value">Whether to freeze or unfreeze the atom</param>
    public void freezeUI(bool value)
    {
        if (value == frozen) return;
        freeze(value);
        EventManager.Singleton.FreezeAtom(m_molecule.m_id, m_id, value);
    }

    /// <summary>
    /// Freezes/unfreezes the current atom.
    /// This changes its appearance and makes it non-interactable.
    /// </summary>
    /// <param name="value"></param>
    public void freeze(bool value)
    {
        GetComponent<NearInteractionGrabbable>().enabled = !value;
        GetComponent<ObjectManipulator>().enabled = !value;
        if (value)
        {
            m_data.m_mass = -1f;
            // Append frozen material to end of list
            Material[] frozen = GetComponent<MeshRenderer>().sharedMaterials.ToList().Append(frozen_mat).ToArray();
            GetComponent<MeshRenderer>().sharedMaterials = frozen;
        }
        else
        {
            ElementData tempData = GlobalCtrl.Singleton.Dic_ElementData[m_data.m_abbre];
            m_data.m_mass = tempData.m_mass;
            // Remove frozen material
            List<Material> unfrozen = GetComponent<MeshRenderer>().sharedMaterials.ToList();
            unfrozen.Remove(frozen_mat);
            GetComponent<MeshRenderer>().sharedMaterials = unfrozen.ToArray();
        }

        frozen = value;
        if (freezeButton)
        {
            setFrozenVisual(frozen);
        }
    }

    /// <summary>
    /// Set the color of the indicator on the frozen button.
    /// </summary>
    /// <param name="value">Whether the atom is frozen</param>
    public void setFrozenVisual(bool value)
    {
        var FrozenIndicator = freezeButton.transform.Find("IconAndText").gameObject.transform.Find("Indicator").gameObject;
        if (value)
        {
            FrozenIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.orange;
        }
        else
        {
            FrozenIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.gray;
        }
    }
    #endregion

}