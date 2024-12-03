#if CHARPACK_MRTK_2_8
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
#endif
using chARpack.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using chARpack.ColorPalette;
using UnityEngine.SceneManagement;


namespace chARpack
{
    public class Molecule : MonoBehaviour
#if CHARPACK_MRTK_2_8
        , IMixedRealityPointerHandler
#endif
    {
        private Stopwatch stopwatch;
        [HideInInspector] public bool isGrabbed = false;
        [HideInInspector] public bool isServerFocused = false;
        private cmlData before;
        private Vector3 pickupPos = Vector3.zero;
        private Quaternion pickupRot = Quaternion.identity;
        [HideInInspector] public float initial_scale = SettingsData.defaultMoleculeSize;

        private List<Tuple<ushort, Vector3>> atomState = new List<Tuple<ushort, Vector3>>();
        public string svgFormula = string.Empty;


#if CHARPACK_MRTK_2_8
        /// <summary>
        /// This method is triggered when a grab/select gesture is started.
        /// Sets the molecule to grabbed unless measurement mode is active.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (isInteractable)
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
                before = this.AsCML();
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
            if (!frozen && isInteractable)
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
            if (isInteractable)
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
                            cmlData after = this.AsCML();
                            GlobalCtrl.Singleton.undoStack.AddChange(new MoveMoleculeAction(before, after));
                            var merge_occured = GlobalCtrl.Singleton.checkForCollisionsAndMerge(this);
                            if (SettingsData.allowThrowing && !merge_occured)
                            {
                                // continue movement
                                StartCoroutine(continueMovement(HandTracking.Singleton.getHandVelocity()));
                            }
                        }
                        // change material back to normal
                        GetComponent<myBoundingBox>().setGrabbed(false);
                    }
                }
            }
        }
#endif

        private IEnumerator continueMovement(Vector3 initial_velocity)
        {
            isGrabbed = true;
            var current_velocity = initial_velocity * 0.9f;
            var damping_coefficient = 0.98f;
            while (!current_velocity.magnitude.approx(0f, 0.0001f))
            {
                transform.position += current_velocity;
                current_velocity = current_velocity.multiply(damping_coefficient * Vector3.one);
                if (current_velocity.magnitude < 0.005f)
                {
                    damping_coefficient *= damping_coefficient;
                }
                EventManager.Singleton.MoveMolecule(m_id, transform.localPosition, transform.localRotation);
                yield return null;
            }
            isGrabbed = false;
        }

        public void Hover(bool value)
        {
            if (isInteractable)
            {
                isHovered = value;
                GetComponent<myBoundingBox>().setHovering(value);
            }
        }

        public void OnServerSliderUpdated()
        {
            cmlData before = this.AsCML();
            before.moleScale = new SaveableVector3(oldScale, oldScale, oldScale);
            oldScale = transform.localScale.x; // / startingScale.x;
            transform.localScale = scalingSliderInstance.GetComponentInChildren<Slider>().value * Vector3.one; // * startingScale;
            GlobalCtrl.Singleton.undoStack.AddChange(new ScaleMoleculeAction(before, this.AsCML()));
            // networking
            EventManager.Singleton.ChangeMoleculeScale(m_id, transform.localScale.x);
        }

#if CHARPACK_MRTK_2_8
        /// <summary>
        /// Scales the molecule based on the slider value and invokes a 
        /// change molecule scale event.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnSliderUpdated(mySliderEventData eventData)
        {
            if (eventData.Pointer != null) // exclude slider update on startup
            {
                cmlData before = this.AsCML();
                before.moleScale = eventData.OldValue * transform.localScale / eventData.NewValue;
                GlobalCtrl.Singleton.undoStack.AddChange(new ScaleMoleculeAction(before, this.AsCML()));
            }
            transform.localScale = eventData.NewValue * Vector3.one;// * startingScale;
                                                                    // networking
            EventManager.Singleton.ChangeMoleculeScale(m_id, gameObject.transform.localScale.x);
        }
#endif

        public void Update()
        {
            if (toolTipInstance)
            {
                if (!LoginData.isServer)
                {
#if CHARPACK_MRTK_2_8
                    if (type == toolTipType.SINGLE)
                    {
                        string[] text = toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText.Split("\n");
                        string[] distance = text[2].Split(": ");
                        double dist = SettingsData.useAngstrom ? toolTipInstance.transform.Find("Distance Measurement").GetComponent<DistanceMeasurement>().getDistanceInAngstrom()
                            : toolTipInstance.transform.Find("Distance Measurement").GetComponent<DistanceMeasurement>().getDistanceInAngstrom() * 100;
                        string distanceString = SettingsData.useAngstrom ? $"{dist:0.00}\u00C5" : $"{dist:0}pm";
                        string newDistance = string.Concat(distance[0], ": ", distanceString);
                        text[2] = newDistance;

                        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = string.Join("\n", text);

                    }
                    else if (type == toolTipType.ANGLE)
                    {
                        string[] text = toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText.Split("\n");
                        string[] ang = text[3].Split(": ");
                        double angle = toolTipInstance.transform.Find("Angle Measurement").GetComponent<AngleMeasurement>().getAngle();
                        string newAng = string.Concat(ang[0], ": ", $"{angle:0.00}°");
                        text[3] = newAng;

                        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = string.Join("\n", text);
                    }
                    else if (type == toolTipType.TORSION)
                    {
                        string[] text = toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText.Split("\n");
                        string[] ang = text[2].Split(": ");
                        double angle = toolTipInstance.transform.Find("Dihedral Angle Measurement").GetComponent<DihedralAngleMeasurement>().getAngle();
                        string newAng = string.Concat(ang[0], ": ", $"{angle:0.00}°");
                        text[2] = newAng;

                        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = string.Join("\n", text);
                    }
#endif
                }
                else
                {
                    if (type == toolTipType.SINGLE)
                    {
                        string[] text = toolTipInstance.GetComponent<ServerBondTooltip>().ToolTipText.text.Split("\n");
                        string[] distance = text[2].Split(": ");
                        double dist = SettingsData.useAngstrom ? toolTipInstance.transform.Find("Distance Measurement").GetComponent<DistanceMeasurement>().getDistanceInAngstrom()
                            : toolTipInstance.transform.Find("Distance Measurement").GetComponent<DistanceMeasurement>().getDistanceInAngstrom() * 100;
                        string distanceString = SettingsData.useAngstrom ? $"{dist:0.00}\u00C5" : $"{dist:0}pm";
                        string newDistance = string.Concat(distance[0], ": ", distanceString);
                        text[2] = newDistance;

                        toolTipInstance.GetComponent<ServerBondTooltip>().ToolTipText.text = string.Join("\n", text);

                    }
                    else if (type == toolTipType.ANGLE)
                    {
                        string[] text = toolTipInstance.GetComponent<ServerAngleTooltip>().ToolTipText.text.Split("\n");
                        string[] ang = text[3].Split(": ");
                        double angle = toolTipInstance.transform.Find("Angle Measurement").GetComponent<AngleMeasurement>().getAngle();
                        string newAng = string.Concat(ang[0], ": ", $"{angle:0.00}°");
                        text[3] = newAng;

                        toolTipInstance.GetComponent<ServerAngleTooltip>().ToolTipText.text = string.Join("\n", text);
                    }
                    else if (type == toolTipType.TORSION)
                    {
                        string[] text = toolTipInstance.GetComponent<ServerTorsionTooltip>().ToolTipText.text.Split("\n");
                        string[] ang = text[2].Split(": ");
                        double angle = toolTipInstance.transform.Find("Dihedral Angle Measurement").GetComponent<DihedralAngleMeasurement>().getAngle();
                        string newAng = string.Concat(ang[0], ": ", $"{angle:0.00}°");
                        text[2] = newAng;

                        toolTipInstance.GetComponent<ServerTorsionTooltip>().ToolTipText.text = string.Join("\n", text);
                    }
                }
            }
        }


        [HideInInspector] public static GameObject myToolTipPrefab;
        [HideInInspector] public static GameObject mySnapToolTipPrefab;
        [HideInInspector] public static GameObject deleteMeButtonPrefab;
        [HideInInspector] public static GameObject closeMeButtonPrefab;
        [HideInInspector] public static GameObject modifyMeButtonPrefab;
        [HideInInspector] public static GameObject toggleDummiesButtonPrefab;
        [HideInInspector] public static GameObject undoButtonPrefab;
        [HideInInspector] public static GameObject changeBondWindowPrefab;
        [HideInInspector] public static GameObject changeServerBondWindowPrefab;
        [HideInInspector] public static GameObject copyButtonPrefab;
        [HideInInspector] public static GameObject scaleMoleculeButtonPrefab;
        [HideInInspector] public static GameObject scalingSliderPrefab;
        [HideInInspector] public static GameObject serverScalingSliderPrefab;
        [HideInInspector] public static GameObject freezeMeButtonPrefab;
        [HideInInspector] public static GameObject snapMeButtonPrefab;
        [HideInInspector] public static GameObject mergeButtonPrefab;
        [HideInInspector] public static GameObject distanceMeasurementPrefab;
        [HideInInspector] public static GameObject angleMeasurementPrefab;
        [HideInInspector] public static GameObject serverMoleculeTooltipPrefab;
        [HideInInspector] public static GameObject serverBondTooltipPrefab;
        [HideInInspector] public static GameObject serverAngleTooltipPrefab;
        [HideInInspector] public static GameObject serverTorsionTooltipPrefab;
        [HideInInspector] public static GameObject serverSnapTooltipPrefab;

        [HideInInspector] public static Material compMaterialA;
        [HideInInspector] public static Material compMaterialB;

        public GameObject toolTipInstance;
        private GameObject freezeButton;
        public GameObject scalingSliderInstance;
        public GameObject changeBondWindowInstance;
        private float toolTipDistanceWeight = 0.01f;
        private Vector3 startingScale;
        private float previousScale;
        public bool frozen = false;
        private Material frozen_bond_mat;
        private float oldScale = 1.0f;

        public enum toolTipType
        {
            MOLECULE,
            SINGLE,
            ANGLE,
            TORSION
        }
        public toolTipType type;

        /// <summary>
        /// molecule id
        /// </summary>
        private Guid _id;
        public Guid m_id { get { return _id; } set { _id = value; } }


        public bool isMarked;
        public bool isHovered;
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
        public void f_Init(Guid idInScene, Transform inputParent, cmlData mol_data = new cmlData())
        {
            m_id = idInScene;
            isMarked = false;
            transform.parent = inputParent;
            transform.localScale = SettingsData.defaultMoleculeSize * Vector3.one;
            startingScale = transform.localScale;
            atomList = new List<Atom>();
            bondList = new List<Bond>();
            // TODO: we need the collider but we dont want it
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.001f, 0.001f, 0.001f);
            //collider.center = GetComponent<myBoundingBox>().cornerHandles[1].transform.position;

            compMaterialA = Resources.Load("materials/ComparisonMaterialA") as Material;
            compMaterialB = Resources.Load("materials/ComparisonMaterialB") as Material;

            frozen_bond_mat = Resources.Load("materials/frozenBondMaterial") as Material;

            if (mol_data.name == null || mol_data.name == "")
            {
                gameObject.name = "mol_" + m_id.ToString().Substring(0, 5);
            }
            else
            {
                gameObject.name = mol_data.name;
            }


#if CHARPACK_MRTK_2_8
            // these objects take input from corner colliders and manipulate the moluecule
            var om = gameObject.AddComponent<ObjectManipulator>();
            gameObject.AddComponent<NearInteractionGrabbable>();

            // add object manipulation methods to check for scaling using two hands
            // Subscribe to the Manipulation events
            om.OnManipulationStarted.AddListener(OnManipulationStarted);
            om.OnManipulationEnded.AddListener(OnManipulationEnded);
#endif

            // Initialize the previous scale to the object's initial scale
            previousScale = transform.localScale.x;


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
            EventManager.Singleton.OnMoleculeLoaded += triggerGenerateFF;
            EventManager.Singleton.OnMolDataChanged += triggerGenerateFF;
            EventManager.Singleton.OnMoleculeLoaded += adjustBBox;
            EventManager.Singleton.OnMolDataChanged += adjustBBox;
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.OnMiddleFingerGrab.AddListener(OnTransitionGrab, IsMiddleFingerInTransitionGrabBounds);
                HandTracking.Singleton.OnFlick.AddListener(OnTransitionFlick, IsIndexFingerInTransitionGrabBounds);
                HandTracking.Singleton.OnMiddleFingerGrabRelease += OnTransitionGrabRelease;
                HandTracking.Singleton.OnEmptyIndexFingerGrab.AddListener(OnNormalGrab, IsIndexFingerInTransitionGrabBounds);
                HandTracking.Singleton.OnEmptyCloseIndexFingerGrab.AddListener(OnNormalGrab, IsIndexFingerInTransitionGrabBounds);
            }
        }

        bool isManipulating = false;
#if CHARPACK_MRTK_2_8
        private void OnManipulationStarted(ManipulationEventData eventData)
        {
            isManipulating = true;
            StartCoroutine(HandleScaleChanges());
        }
#endif

        private IEnumerator HandleScaleChanges()
        {
            // Reset the previous scale when manipulation starts
            previousScale = transform.localScale.x;
            while (isManipulating)
            {
                if (transform.localScale.x != previousScale)
                {
                    EventManager.Singleton.ChangeMoleculeScale(m_id, transform.localScale.x);
                    previousScale = transform.localScale.x;
                }
                yield return new WaitForSeconds(0.1f); // update 10 times per second
            }
        }

#if CHARPACK_MRTK_2_8
        private void OnManipulationEnded(ManipulationEventData eventData)
        {
            isManipulating = false;
        }
#endif

        public bool containedInAtoms(Vector3 pos)
        {
            foreach (var atom in atomList)
            {
                if (atom.GetComponent<BoxCollider>().bounds.Contains(pos)) return true;
            }
            return false;
        }

        bool emptyGrab = false;
        private void OnNormalGrab()
        {
            // blocks default case of event
        }

        private void OnNormalGrabRelease()
        {
#if CHARPACK_MRTK_2_8
            if (screenAlignment.Singleton == null) return;
            if (emptyGrab)
            {
                screenAlignment.Singleton.OnDistantTransitionGrabRelease();
                emptyGrab = false;
            }
#endif
        }

        private void OnTransitionGrab()
        {
            if (SettingsData.syncMode == TransitionManager.SyncMode.Async)
            {
                if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.DISTANT_GRAB))
                {
                    TransitionManager.Singleton.initializeTransitionClient(transform, TransitionManager.InteractionType.DISTANT_GRAB);
                }
            }
        }

        private void OnTransitionFlick()
        {
            if (SettingsData.syncMode == TransitionManager.SyncMode.Async)
            {
                if (SettingsData.allowedTransitionInteractions.HasFlag(TransitionManager.InteractionType.FLICK))
                {
                    TransitionManager.Singleton.initializeTransitionClient(transform, TransitionManager.InteractionType.FLICK);
                }
            }
        }

        private bool IsIndexFingerInTransitionGrabBounds()
        {
            if (isInteractable)
            {
                var bounds = GetComponent<myBoundingBox>().getCopyOfBounds();
                bounds.Expand(0.05f); // take pointer size into account
                if (bounds.Contains(HandTracking.Singleton.getIndexTip()))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsMiddleFingerInTransitionGrabBounds()
        {
            if (isInteractable)
            {
                var bounds = GetComponent<myBoundingBox>().getCopyOfBounds();
                bounds.Expand(0.05f); // take pointer size into account
                if (bounds.Contains(HandTracking.Singleton.getMiddleTip()))
                {
                    return true;
                }
            }
            return false;
        }

        public Quaternion relQuatBeforeTransition = Quaternion.identity;

        private void OnTransitionGrabRelease()
        {

        }

        bool isInteractable = true;
        public void setIntractable(bool value)
        {
            if (isInteractable != value)
            {
                isInteractable = value;
#if CHARPACK_MRTK_2_8
                GetComponent<ObjectManipulator>().enabled = value;
                GetComponent<NearInteractionGrabbable>().enabled = value;
                foreach (var nag in GetComponentsInChildren<NearInteractionGrabbable>())
                {
                    nag.enabled = value;
                }
#endif
                GetComponent<myBoundingBox>().show(value);
                GetComponent<myBoundingBox>().enabled = value;
            }
        }

        public bool getIsInteractable()
        {
            return isInteractable;
        }

        public float getOpactiy()
        {
            return currentOpacity;
        }

        float currentOpacity = 1f;
        public void setOpacity(float value)
        {
            if (currentOpacity != value)
            {
                currentOpacity = value;
                foreach (var atom in atomList)
                {
                    var renderer = atom.GetComponent<Renderer>();
                    foreach (var mat in renderer.materials)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            var col = new Color(mat.color.r, mat.color.g, mat.color.b, value);
                            mat.color = col;
                        }
                    }
                }
                foreach (var bond in bondList)
                {
                    var renderer = bond.GetComponentInChildren<Renderer>();
                    renderer.material.SetFloat("_Alpha", value);
                }
                //foreach (var renderer in GetComponentsInChildren<Renderer>())
                //{
                //    foreach (var mat in renderer.materials)
                //    {
                //        var col = new Color(mat.color.r, mat.color.g, mat.color.b, value);
                //        mat.color = col;
                //    }
                //}
            }
        }

        private void adjustBBox(Molecule mol)
        {
            if (mol == this)
            {
#if UNITY_STANDALONE || UNITY_EDITOR
                GetComponent<myBoundingBox>().setNormalMaterial(false);
#else
            if (GlobalCtrl.Singleton.List_curMolecules.ContainsValue(mol))
            {
                StartCoroutine(adjustBBoxCoroutine());
            }
#endif
            }

        }

        // Need coroutine to use sleep
        private IEnumerator adjustBBoxCoroutine()
        {
            yield return new WaitForSeconds(0.1f);
            var current_size = getLongestBBoxEdge();
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

            foreach (var bond in newParent.bondList)
            {
                UnityEngine.Debug.Log($"Bond atom ids: {bond.atomID1} {bond.atomID2}");
            }

            GlobalCtrl.Singleton.List_curMolecules.RemoveValue(this);
            Destroy(gameObject);
        }

        public void setServerFocus(bool focus, bool useBlinkAnimation = false)
        {
            if (isServerFocused != focus)
            {
                isServerFocused = focus;
                if (!useBlinkAnimation)
                {
                    foreach (Atom a in atomList)
                    {
                        a.serverFocusHighlightUI(focus);
                    }
                }
                else
                {
                    StartCoroutine(serverFocusBlinkAnimation());
                }

            }
        }

        private IEnumerator serverFocusBlinkAnimation()
        {
            bool current_state = true;
            while (isServerFocused)
            {
                foreach (Atom a in atomList)
                {
                    a.serverFocusHighlightUI(current_state);
                }
                current_state = !current_state;
                yield return new WaitForSeconds(0.1f);
            }
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
                    if (LoginData.isServer)
                    {
                        createServerToolTip();
                    }
                    else
                    {
                        createToolTip();
                    }
                }
            }

            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
            {
                if (mol != this && mol.isMarked)
                {
                    if (!GlobalCtrl.Singleton.snapToolTipInstances.ContainsKey(new Tuple<Guid, Guid>(m_id, mol.m_id)) &&
                        !GlobalCtrl.Singleton.snapToolTipInstances.ContainsKey(new Tuple<Guid, Guid>(mol.m_id, m_id)))
                    {
                        if (mark)
                        {
                            if (LoginData.isServer)
                            {
                                createServerSnapToolTip(mol.m_id);
                            }
                            else
                            {
                                createSnapToolTip(mol.m_id);
                            }
                        }
                    }
                    else
                    {
                        if (mark == false)
                        {
                            if (GlobalCtrl.Singleton.snapToolTipInstances.ContainsKey(new Tuple<Guid, Guid>(m_id, mol.m_id)))
                            {
                                Destroy(GlobalCtrl.Singleton.snapToolTipInstances[new Tuple<Guid, Guid>(m_id, mol.m_id)]);
                                GlobalCtrl.Singleton.snapToolTipInstances.Remove(new Tuple<Guid, Guid>(m_id, mol.m_id));
                            }
                            else
                            {
                                Destroy(GlobalCtrl.Singleton.snapToolTipInstances[new Tuple<Guid, Guid>(mol.m_id, m_id)]);
                                GlobalCtrl.Singleton.snapToolTipInstances.Remove(new Tuple<Guid, Guid>(mol.m_id, m_id));
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
            if (currentOpacity != 0f)
            {
                EventManager.Singleton.SelectMolecule(m_id, mark);
                markMolecule(mark, showToolTip);
            }
        }

        /// <summary>
        /// Creates a snap tool tip connected to the current molecule and the
        /// other selected molecule.
        /// It contains information about the molecules and a button that provides
        /// the option to perform the snap.
        /// </summary>
        /// <param name="otherMolID">ID of the other selected molecule</param>
        public void createSnapToolTip(Guid otherMolID)
        {
#if CHARPACK_MRTK_2_8
            // create tool tip
            var snapToolTip = Instantiate(mySnapToolTipPrefab);

            // put tool top to the right 
            snapToolTip.transform.position = (GlobalCtrl.Singleton.List_curMolecules[otherMolID].transform.position - transform.position) / 2f + transform.position - 0.25f * Vector3.up;
            // add atom as connector
            snapToolTip.GetComponent<myDoubleLineToolTipConnector>().Target1 = gameObject;
            snapToolTip.GetComponent<myDoubleLineToolTipConnector>().Target2 = GlobalCtrl.Singleton.List_curMolecules[otherMolID].gameObject;
            string toolTipText = $"Molecule1 ID: {m_id}\nMolecule2 ID: {otherMolID}";
            snapToolTip.GetComponent<DoubleLineDynamicToolTip>().ToolTipText = toolTipText;

            var snapMoleculeButtonInstance = Instantiate(snapMeButtonPrefab);
            snapMoleculeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { snapUI(otherMolID); });
            snapToolTip.GetComponent<DoubleLineDynamicToolTip>().addContent(snapMoleculeButtonInstance);

            var mergeMoleculeButtonInstance = Instantiate(mergeButtonPrefab);
            mergeMoleculeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.MergeUnconnectedMolecules(m_id, otherMolID); });
            snapToolTip.GetComponent<DoubleLineDynamicToolTip>().addContent(mergeMoleculeButtonInstance);

            var closeSnapButtonInstance = Instantiate(closeMeButtonPrefab);
            closeSnapButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { closeSnapUI(otherMolID); });
            snapToolTip.GetComponent<DoubleLineDynamicToolTip>().addContent(closeSnapButtonInstance);

            GlobalCtrl.Singleton.snapToolTipInstances[new Tuple<Guid, Guid>(m_id, otherMolID)] = snapToolTip;
#endif
        }

        /// <summary>
        /// Creates a snap tool tip connected to the current molecule and the
        /// other selected molecule.
        /// It contains information about the molecules and a button that provides
        /// the option to perform the snap.
        /// </summary>
        /// <param name="otherMolID">ID of the other selected molecule</param>
        public void createServerSnapToolTip(Guid otherMolID, int focus_id = -1)
        {
            // create tool tip
            var snapToolTip = Instantiate(serverSnapTooltipPrefab);

            snapToolTip.GetComponent<ServerSnapTooltip>().mol1 = this;
            snapToolTip.GetComponent<ServerSnapTooltip>().mol2 = GlobalCtrl.Singleton.List_curMolecules[otherMolID];
            string toolTipText = $"Molecule1 ID: {m_id}\nMolecule2 ID: {otherMolID}";
            snapToolTip.GetComponent<ServerSnapTooltip>().ToolTipText.text = toolTipText;

            snapToolTip.GetComponent<ServerSnapTooltip>().snapButton.onClick.AddListener(delegate { snapUI(otherMolID); });
            snapToolTip.GetComponent<ServerSnapTooltip>().mergeButton.onClick.AddListener(delegate { GlobalCtrl.Singleton.MergeUnconnectedMolecules(m_id, otherMolID); });
            snapToolTip.GetComponent<ServerSnapTooltip>().closeButton.onClick.AddListener(delegate { closeSnapUI(otherMolID); });
            if (toolTipInstance != null)
            {
                toolTipInstance.GetComponent<ServerMoleculeTooltip>().focus_id = focus_id;
            }

            GlobalCtrl.Singleton.snapToolTipInstances[new Tuple<Guid, Guid>(m_id, otherMolID)] = snapToolTip;
        }

        private void snapUI(Guid otherMolID)
        {

            if (!GlobalCtrl.Singleton.List_curMolecules.ContainsKey(otherMolID))
            {
                UnityEngine.Debug.LogError($"[Molecule:snapUI] Could not find Molecule with ID {otherMolID}");
                return;
            }
            var otherMol = GlobalCtrl.Singleton.List_curMolecules[otherMolID];
            snap(otherMolID);
            markMolecule(false);
            otherMol.markMolecule(false);
            EventManager.Singleton.MoveMolecule(m_id, otherMol.transform.localPosition, otherMol.transform.localRotation);
            EventManager.Singleton.SelectMolecule(m_id, false);
            EventManager.Singleton.SelectMolecule(otherMolID, false);
            EventManager.Singleton.SetSnapColors(m_id, otherMolID);
        }

        private bool snap(Guid otherMolID)
        {
            if (!GlobalCtrl.Singleton.List_curMolecules.ContainsKey(otherMolID))
            {
                return false;
            }
            var otherMol = GlobalCtrl.Singleton.List_curMolecules[otherMolID];
            // apply transformation
            if (SettingsData.UseKabsch)
            {
                try
                {
                    var rotationMatrix = Kabsch.kabschRotationMatrix(getAtomPositions(), otherMol.getAtomPositions());
                    transform.localRotation = Quaternion.LookRotation(rotationMatrix.GetRow(2), rotationMatrix.GetRow(1)) * transform.localRotation;

                    resetMolRotation();

                    transform.localPosition = otherMol.transform.localPosition;
                }
                catch (Exception e)
                {
                    // keep rotation if algorithm doesn't converge
                    UnityEngine.Debug.Log(e.Message);
                    transform.localPosition = otherMol.transform.localPosition;
                }
            }
            else
            {
                transform.localScale = otherMol.transform.localScale;
                transform.localPosition = otherMol.transform.localPosition;
                transform.localRotation = otherMol.transform.localRotation;
            }
            // TODO: Add advanced alignment mode
            // add coloring
            setSnapColors(otherMol);

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

        public void setSnapColors(Molecule otherMol)
        {
            addSnapColor(ref compMaterialA);
            otherMol.addSnapColor(ref compMaterialB);
        }

        private void closeSnapUI(Guid otherMolID)
        {
            if (GlobalCtrl.Singleton.List_curMolecules.ContainsKey(otherMolID))
            {
                UnityEngine.Debug.LogError($"[Molecule:closeSnapUI] Could not find Molecule with ID {otherMolID}");
                return;
            }
            var otherMol = GlobalCtrl.Singleton.List_curMolecules[otherMolID];
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
            cmlData before_ = this.AsCML();
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
            cmlData after = this.AsCML();
            GlobalCtrl.Singleton.undoStack.AddChange(new ToggleDummiesAction(before_, after));
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
                dists.Add(Mathf.Sqrt(center[0] * atom_pos[0] + center[1] * atom_pos[1] + center[2] * atom_pos[2]));
            }

            float max_dist = 0.0f;
            foreach (float dist in dists)
            {
                max_dist = Mathf.Max(max_dist, dist);
            }

            return max_dist;
        }

        /// <summary>
        /// Gets the length of the longest bounding box edge.
        /// </summary>
        /// <returns>the length of the longest edge of the bounding box</returns>
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

        /// <summary>
        /// Handles the transfer of the movement of a single atom (with 
        /// consequences because of the force field) to the containing molecule.
        /// </summary>
        public void resetMolPositionAfterMove()
        {

            // reset molecule position
            Vector3 molCenter = getCenterInAtomWorld();
            var mol_rot = transform.localRotation;
            transform.localRotation = Quaternion.identity;
            var mol_center_rotated = getCenterInAtomWorld();
            // positions relative to the molecule center
            foreach (Atom a in atomList)
            {
                a.transform.localPosition = (GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(a.transform.position) - mol_center_rotated) * (1f / transform.localScale.x);
            }
            // rotate back
            transform.localRotation = mol_rot;
            transform.localPosition = molCenter;
            // scale, position and orient bonds
            foreach (Bond bond in bondList)
            {
                Atom a1 = atomList.ElementAtOrDefault(bond.atomID1);
                Atom a2 = atomList.ElementAtOrDefault(bond.atomID2);
                var a1_pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(a1.transform.position);
                var a2_pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(a2.transform.position);
                //float offset1 = a1.m_data.m_radius * ForceField.scalingfactor * GlobalCtrl.atomScale * GlobalCtrl.scale * 0.8f * transform.localScale.x;
                //float offset2 = a2.m_data.m_radius * ForceField.scalingfactor * GlobalCtrl.atomScale * GlobalCtrl.scale * 0.8f * transform.localScale.x;
                float offset1 = SettingsData.licoriceRendering ? 0f : a1.m_data.m_radius * ForceField.scalingfactor * GlobalCtrl.atomScale * GlobalCtrl.scale * 0.8f * transform.localScale.x;
                float offset2 = SettingsData.licoriceRendering ? 0f : a2.m_data.m_radius * ForceField.scalingfactor * GlobalCtrl.atomScale * GlobalCtrl.scale * 0.8f * transform.localScale.x;
                float distance = (Vector3.Distance(a1_pos, a2_pos) - offset1 - offset2) / transform.localScale.x;
                bond.transform.localScale = new Vector3(bond.transform.localScale.x, bond.transform.localScale.y, distance);
                Vector3 pos1 = Vector3.MoveTowards(a1_pos, a2_pos, offset1);
                Vector3 pos2 = Vector3.MoveTowards(a2_pos, a1_pos, offset2);
                bond.transform.position = (pos1 + pos2) / 2;
                bond.transform.LookAt(a2.transform.position);
            }
        }

        #region ToolTips
        /// <summary>
        /// Creates a molecule tool tip with information about the molecule as well
        /// as buttons to provide interactions like copying and toggling dummies.
        /// </summary>
        public void createToolTip()
        {
#if CHARPACK_MRTK_2_8
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
            string toolTipText = getAtomToolTipText(tot_mass, max_dist);
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
#endif
        }

        public void createServerToolTip(int focus_id = -1)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            Vector2? oldPos = null;
            if (toolTipInstance)
            {
                oldPos = toolTipInstance.GetComponent<RectTransform>().localPosition;
                Destroy(toolTipInstance);
            }
            toolTipInstance = Instantiate(serverMoleculeTooltipPrefab);
            if (oldPos != null) toolTipInstance.GetComponent<ServerMoleculeTooltip>().localPosition = (Vector2)oldPos;
            type = toolTipType.MOLECULE;
            float tot_mass = 0.0f;
            calcMetaData(ref tot_mass);
            var mol_center = getCenter();
            var max_dist = getMaxDistFromCenter(mol_center);
            string toolTipText = getAtomToolTipText(tot_mass, max_dist);
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().focus_id = focus_id;
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().ToolTipText.text = toolTipText;
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().closeButton.onClick.AddListener(delegate { markMoleculeUI(false); });
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().freezeButton.onClick.AddListener(delegate { freezeUI(!frozen); });
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().deleteButton.onClick.AddListener(delegate { GlobalCtrl.Singleton.deleteMoleculeUI(this); });
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().toggleDummiesButton.onClick.AddListener(delegate { toggleDummiesUI(); });
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().scaleButton.onClick.AddListener(delegate { toggleScalingSlider(); });
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().copyButton.onClick.AddListener(delegate { GlobalCtrl.Singleton.copyMolecule(this); });
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().structureFormulaButton.onClick.AddListener(delegate { StructureFormulaGenerator.Singleton.immediateRequestStructureFormula(this); });
            toolTipInstance.GetComponent<ServerMoleculeTooltip>().linkedMolecule = this;
#endif
        }

        public void toggleScalingSlider()
        {
            if (!scalingSliderInstance)
            {
                if (LoginData.isServer)
                {
                    scalingSliderInstance = Instantiate(serverScalingSliderPrefab);
                    scalingSliderInstance.GetComponentInChildren<Slider>().maxValue = 2;
                    scalingSliderInstance.GetComponentInChildren<Slider>().minValue = 0.1f;
                    var currentScale = transform.localScale.x;// / startingScale.x;
                    scalingSliderInstance.GetComponentInChildren<Slider>().normalizedValue = (currentScale - scalingSliderInstance.GetComponentInChildren<Slider>().minValue) / (scalingSliderInstance.GetComponentInChildren<Slider>().maxValue - scalingSliderInstance.GetComponentInChildren<Slider>().minValue);
                    scalingSliderInstance.GetComponentInChildren<Slider>().onValueChanged.AddListener(delegate { OnServerSliderUpdated(); });
                    scalingSliderInstance.GetComponentInChildren<UpdateSliderLabel>().updateLabel();
                }
                else
                {
#if CHARPACK_MRTK_2_8
                    // position needs to be optimized
                    scalingSliderInstance = Instantiate(scalingSliderPrefab, gameObject.transform.position - 0.17f * GlobalCtrl.Singleton.currentCamera.transform.forward - 0.05f * Vector3.up, GlobalCtrl.Singleton.currentCamera.transform.rotation);
                    scalingSliderInstance.GetComponent<mySlider>().maxVal = 2;
                    scalingSliderInstance.GetComponent<mySlider>().minVal = 0.1f;
                    var currentScale = transform.localScale.x; // / startingScale.x;
                                                               // Set effective starting value and default to 1
                    scalingSliderInstance.GetComponent<mySlider>().SliderValue = (currentScale - scalingSliderInstance.GetComponent<mySlider>().minVal) / (scalingSliderInstance.GetComponent<mySlider>().maxVal - scalingSliderInstance.GetComponent<mySlider>().minVal);
                    scalingSliderInstance.GetComponent<mySlider>().defaultVal = (1 - scalingSliderInstance.GetComponent<mySlider>().minVal) / (scalingSliderInstance.GetComponent<mySlider>().maxVal - scalingSliderInstance.GetComponent<mySlider>().minVal);
                    //startingScale = gameObject.transform.localScale;
                    scalingSliderInstance.GetComponent<mySlider>().OnValueUpdated.AddListener(OnSliderUpdated);
#endif
                }
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
#if CHARPACK_MRTK_2_8
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
            string toolTipText = getBondToolTipText(term.eqDist / 100, dist.getDistanceInAngstrom(), term.kBond, term.order);
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
#endif
        }

        /// <summary>
        /// Creates a server tool tip (on the UI canvas) for a single bond that contains both static and dynamic information about
        /// its length and buttons, including the option to change the bonds equilibrium parameters.
        /// </summary>
        /// <param name="term"></param>
        /// <param name="focus_id"></param>
        public void createServerBondToolTip(ForceField.BondTerm term, int focus_id = -1)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            //TODO: Make spawn location conistant with first atom tooltip
            Vector3 rectSave = new Vector3(0, 0, 0);
            foreach (var atom in atomList)
            {
                if (atom.toolTipInstance != null && atom.toolTipInstance.activeInHierarchy)
                {
                    rectSave = atom.toolTipInstance.GetComponent<RectTransform>().localPosition;
                    Destroy(atom.toolTipInstance);
                    atom.toolTipInstance = null;
                }
            }
            if (toolTipInstance != null)
            {
                if (toolTipInstance.activeInHierarchy)
                {
                    rectSave = toolTipInstance.GetComponent<RectTransform>().localPosition;
                    Destroy(toolTipInstance);
                }
            }
            toolTipInstance = Instantiate(serverBondTooltipPrefab);
            type = toolTipType.SINGLE;

            var distGO = Instantiate(distanceMeasurementPrefab);
            distGO.transform.parent = toolTipInstance.transform;
            distGO.name = "Distance Measurement";
            distGO.transform.Find("Line").gameObject.SetActive(false);
            var atom1 = atomList.ElementAtOrDefault(term.Atom1);
            var atom2 = atomList.ElementAtOrDefault(term.Atom2);
            DistanceMeasurement dist = distGO.GetComponent<DistanceMeasurement>();
            dist.StartAtom = atom1;
            dist.EndAtom = atom2;

            var bond = atomList[term.Atom1].getBond(atomList[term.Atom2]);

            string toolTipText = getBondToolTipText(term.eqDist / 100, dist.getDistanceInAngstrom(), term.kBond, term.order);
            toolTipInstance.GetComponent<ServerBondTooltip>().focus_id = focus_id;
            toolTipInstance.GetComponent<ServerBondTooltip>().ToolTipText.text = toolTipText;
            toolTipInstance.GetComponent<ServerBondTooltip>().closeButton.onClick.AddListener(delegate { markBondTermUI(term, false); });
            toolTipInstance.GetComponent<ServerBondTooltip>().deleteButton.onClick.AddListener(delegate { GlobalCtrl.Singleton.deleteBondUI(bond); });
            toolTipInstance.GetComponent<ServerBondTooltip>().modifyButton.onClick.AddListener(delegate { createServerChangeBondWindow(term); });
            toolTipInstance.GetComponent<ServerBondTooltip>().localPosition = rectSave;
            toolTipInstance.GetComponent<ServerBondTooltip>().linkedBond = bond;
            if (atom1.m_data.m_abbre == "Dummy" || atom2.m_data.m_abbre == "Dummy")
            {
                toolTipInstance.GetComponent<ServerBondTooltip>().deleteButton.gameObject.SetActive(false);
                toolTipInstance.GetComponent<RectTransform>().sizeDelta = new Vector2(toolTipInstance.GetComponent<RectTransform>().sizeDelta.x, toolTipInstance.GetComponent<RectTransform>().sizeDelta.y - 30);
                markBondTermServer(term, true);
            }
            else
            {
                markBondTermServer(term, true);
            }
#endif
        }
        private void createChangeBondWindow(ForceField.BondTerm bond)
        {
#if CHARPACK_MRTK_2_8
            changeBondWindowInstance = Instantiate(changeBondWindowPrefab);
            var cb = changeBondWindowInstance.GetComponent<ManipulateBondTerm>();
            cb.bt = bond;
            var id = bondTerms.IndexOf(bond);
            cb.okButton.GetComponent<Button>().onClick.AddListener(delegate { changeBondParametersUI(changeBondWindowInstance, id); });
#endif
        }
        private void createServerChangeBondWindow(ForceField.BondTerm bond)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (changeBondWindowInstance != null)
            {
                Destroy(changeBondWindowInstance);
                changeBondWindowInstance = null;
            }
            changeBondWindowInstance = Instantiate(changeServerBondWindowPrefab);

            var cb = changeBondWindowInstance.GetComponent<BondParametersServer>();
            cb.bt = bond;
            var id = bondTerms.IndexOf(bond);
            cb.saveButton.GetComponent<Button>().onClick.AddListener(delegate { changeBondParametersUI(changeBondWindowInstance, id); });
#endif
        }

        private void changeBondParametersUI(GameObject windowInstance, int id)
        {
            ForceField.BondTerm bt;
            cmlData before = this.AsCML();
            if (!LoginData.isServer)
            {
#if CHARPACK_MRTK_2_8
                var cb = windowInstance.GetComponent<ManipulateBondTerm>();
                cb.changeBondParametersBT();
                bt = cb.bt;
                var dist = toolTipInstance.transform.Find("Distance Measurement").GetComponent<DistanceMeasurement>();
                string toolTipText = getBondToolTipText(bt.eqDist, dist.getDistanceInAngstrom(), bt.kBond, bt.order);
                toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;
#else
                return;
#endif
            }
            else
            {
                var cb = windowInstance.GetComponent<BondParametersServer>();
                cb.changeBondParametersBT();
                bt = cb.bt;
                var dist = toolTipInstance.transform.Find("Distance Measurement").GetComponent<DistanceMeasurement>();
                string toolTipText = getBondToolTipText(bt.eqDist, dist.getDistanceInAngstrom(), bt.kBond, bt.order);
                toolTipInstance.GetComponent<ServerBondTooltip>().ToolTipText.text = toolTipText;
            }
            // Update tool tip

            changeBondParameters(bt, id);
            EventManager.Singleton.ChangeBondTerm(bt, m_id, (ushort)id);

            cmlData after = this.AsCML();
            GlobalCtrl.Singleton.undoStack.AddChange(new ChangeBondAction(before, after));

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
            atomList[term.Atom1].markAtom(mark, 3);
            atomList[term.Atom2].markAtom(mark, 3);
            atomList[term.Atom1].getBond(atomList[term.Atom2])?.markBond(mark, 3);

            if (toolTipInstance)
            {
                Destroy(toolTipInstance);
                toolTipInstance = null;
            }
        }
        public void markBondTermServer(ForceField.BondTerm term, bool mark)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            atomList[term.Atom1].markAtom(mark, 3);
            atomList[term.Atom2].markAtom(mark, 3);
            atomList[term.Atom1].getBond(atomList[term.Atom2])?.markBond(mark, 3);
#endif
        }

        public void resetMolRotation()
        {

            // reset molecule position
            Vector3 molCenter = getCenterInAtomWorld();
            // positions relative to the molecule center
            foreach (Atom a in atomList)
            {
                a.transform.localPosition = (GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(a.transform.position) - molCenter) * (1f / transform.localScale.x);
            }
            // rotate back
            transform.localRotation = Quaternion.identity;
            transform.localPosition = molCenter;
            // scale, position and orient bonds
            foreach (Bond bond in bondList)
            {
                Atom a1 = atomList.ElementAtOrDefault(bond.atomID1);
                Atom a2 = atomList.ElementAtOrDefault(bond.atomID2);
                var a1_pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(a1.transform.position);
                var a2_pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(a2.transform.position);
                float offset1 = SettingsData.licoriceRendering ? 0f : a1.m_data.m_radius * ForceField.scalingfactor * GlobalCtrl.atomScale * GlobalCtrl.scale * 0.8f * transform.localScale.x;
                float offset2 = SettingsData.licoriceRendering ? 0f : a2.m_data.m_radius * ForceField.scalingfactor * GlobalCtrl.atomScale * GlobalCtrl.scale * 0.8f * transform.localScale.x;
                float distance = (Vector3.Distance(a1_pos, a2_pos) - offset1 - offset2) / transform.localScale.x;
                bond.transform.localScale = new Vector3(bond.transform.localScale.x, bond.transform.localScale.y, distance);
                Vector3 pos1 = Vector3.MoveTowards(a1_pos, a2_pos, offset1);
                Vector3 pos2 = Vector3.MoveTowards(a2_pos, a1_pos, offset2);
                bond.transform.position = (pos1 + pos2) / 2;
                bond.transform.LookAt(a2.transform.position);
            }
        }

        public Vector3[] getAtomPositions()
        {
            var positions = new Vector3[atomList.Count()];
            for (var i = 0; i < atomList.Count(); i++) { positions[i] = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(atomList[i].transform.position); }
            return positions;
        }


        /// <summary>
        /// Creates a tool tip for an angle bond that contains both static and dynamic information about
        /// its size and buttons, including the option to change the bonds equilibrium parameters.
        /// </summary>
        /// <param name="term"></param>
        public void createAngleToolTip(ForceField.AngleTerm term)
        {
            markAngleTerm(term, true);
#if CHARPACK_MRTK_2_8
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
#endif
        }

        public void createServerAngleToolTip(ForceField.AngleTerm term, int focus_id = -1)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            Vector3 rectSave = new Vector3(0, 0, 0);
            foreach (var atom in atomList)
            {
                if (atom.toolTipInstance != null)
                {
                    rectSave = atom.toolTipInstance.GetComponent<RectTransform>().localPosition;
                    Destroy(atom.toolTipInstance);
                    atom.toolTipInstance = null;
                }
            }
            if (toolTipInstance != null)
            {
                rectSave = toolTipInstance.GetComponent<RectTransform>().localPosition;
                Destroy(toolTipInstance);
            }
            toolTipInstance = Instantiate(serverAngleTooltipPrefab);
            type = toolTipType.ANGLE;
            AngleMeasurement angle = getMeasurements(term);
            string toolTipText = getAngleToolTipText(term.eqAngle, term.kAngle, angle.getAngle());
            toolTipInstance.GetComponent<ServerAngleTooltip>().focus_id = focus_id;
            toolTipInstance.GetComponent<ServerAngleTooltip>().ToolTipText.text = toolTipText;
            toolTipInstance.GetComponent<ServerAngleTooltip>().closeButton.onClick.AddListener(delegate { markAngleTermUI(term, false); });
            toolTipInstance.GetComponent<ServerAngleTooltip>().modifyButton.onClick.AddListener(delegate { createServerChangeAngleWindow(term); });
            toolTipInstance.GetComponent<ServerAngleTooltip>().localPosition = rectSave;
            toolTipInstance.GetComponent<ServerAngleTooltip>().linkedAtom = atomList[term.Atom2];
            markAngleTermServer(term, true);
#endif
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
#if CHARPACK_MRTK_2_8
            changeBondWindowInstance = Instantiate(changeBondWindowPrefab);
            var cb = changeBondWindowInstance.GetComponent<ManipulateBondTerm>();
            cb.at = bond;
            var id = angleTerms.IndexOf(bond);
            cb.okButton.GetComponent<Button>().onClick.AddListener(delegate { changeAngleParametersUI(changeBondWindowInstance, id); });
#endif
        }
        private void createServerChangeAngleWindow(ForceField.AngleTerm bond)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (changeBondWindowInstance != null) { Destroy(changeBondWindowInstance); }
            changeBondWindowInstance = Instantiate(changeServerBondWindowPrefab);

            var cb = changeBondWindowInstance.GetComponent<BondParametersServer>();
            cb.at = bond;
            var id = angleTerms.IndexOf(bond);
            cb.saveButton.GetComponent<Button>().onClick.AddListener(delegate { changeAngleParametersUI(changeBondWindowInstance, id); });
#endif
        }

        private void changeAngleParametersUI(GameObject windowInstance, int id)
        {
            cmlData before = this.AsCML();
            ForceField.AngleTerm at;
            if (!LoginData.isServer)
            {
#if CHARPACK_MRTK_2_8
                var cb = windowInstance.GetComponent<ManipulateBondTerm>();
                cb.changeBondParametersAT();
                at = cb.at;
                string toolTipText = getAngleToolTipText(at.eqAngle, at.kAngle);
                toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;
#else
                return;
#endif
            }
            else
            {
                var cb = windowInstance.GetComponent<BondParametersServer>();
                cb.changeBondParametersAT();
                at = cb.at;
                string toolTipText = getAngleToolTipText(at.eqAngle, at.kAngle);
                toolTipInstance.GetComponent<ServerAngleTooltip>().ToolTipText.text = toolTipText;
            }

            // Update tool tip
            changeAngleParameters(at, id);
            EventManager.Singleton.ChangeAngleTerm(at, m_id, (ushort)id);

            cmlData after = this.AsCML();
            GlobalCtrl.Singleton.undoStack.AddChange(new ChangeBondAction(before, after));

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
            atomList[term.Atom1].markAtom(mark, 4);
            atomList[term.Atom2].markAtom(mark, 4);
            atomList[term.Atom3].markAtom(mark, 4);
            atomList[term.Atom1].getBond(atomList[term.Atom2])?.markBond(mark, 4);
            atomList[term.Atom2].getBond(atomList[term.Atom3])?.markBond(mark, 4);

            if (toolTipInstance)
            {
                Destroy(toolTipInstance);
                toolTipInstance = null;
            }
        }
        public void markAngleTermServer(ForceField.AngleTerm term, bool mark)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            atomList[term.Atom1].markAtom(mark, 4);
            atomList[term.Atom2].markAtom(mark, 4);
            atomList[term.Atom3].markAtom(mark, 4);
            atomList[term.Atom1].getBond(atomList[term.Atom2])?.markBond(mark, 4);
            atomList[term.Atom2].getBond(atomList[term.Atom3])?.markBond(mark, 4);
#endif
        }

        /// <summary>
        /// Creates a tool tip for a torsion bond that contains both static and dynamic information about
        /// its angle and buttons, including the option to change the bonds equilibrium parameters.
        /// </summary>
        /// <param name="term"></param>
        public void createTorsionToolTip(ForceField.TorsionTerm term)
        {
            markTorsionTerm(term, true);
#if CHARPACK_MRTK_2_8
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
#endif
        }

        public void createServerTorsionToolTip(ForceField.TorsionTerm term, int focus_id = -1)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            Vector3 rectSave = new Vector3(0, 0, 0);

            type = toolTipType.TORSION;
            if (toolTipInstance != null)
            {
                rectSave = toolTipInstance.GetComponent<RectTransform>().localPosition;
            }
            foreach (var atom in atomList)
            {
                if (atom.toolTipInstance != null)
                {
                    if (rectSave == new Vector3(0, 0, 0))
                    {
                        rectSave = atom.toolTipInstance.GetComponent<RectTransform>().localPosition;
                    }
                    Destroy(atom.toolTipInstance);
                    atom.toolTipInstance = null;
                }

                var bond = atomList[term.Atom2].getBond(atomList[term.Atom3]);

                Destroy(toolTipInstance);
                toolTipInstance = Instantiate(serverTorsionTooltipPrefab);
                var curAngle = getDihedralAngle(term.Atom1, term.Atom2, term.Atom3, term.Atom4);
                string toolTipText = getTorsionToolTipText(term.eqAngle, term.vk, term.nn, curAngle);
                toolTipInstance.GetComponent<ServerTorsionTooltip>().focus_id = focus_id;
                toolTipInstance.GetComponent<ServerTorsionTooltip>().ToolTipText.text = toolTipText;
                toolTipInstance.GetComponent<ServerTorsionTooltip>().closeButton.onClick.AddListener(delegate { markMoleculeUI(false); });
                toolTipInstance.GetComponent<ServerTorsionTooltip>().modifyButton.onClick.AddListener(delegate { createServerChangeTorsionWindow(term); });
                toolTipInstance.GetComponent<ServerTorsionTooltip>().localPosition = rectSave;
                toolTipInstance.GetComponent<ServerTorsionTooltip>().linkedBond = bond;
            }
            markTorsionTermServer(term, true);
#endif
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
#if CHARPACK_MRTK_2_8
            changeBondWindowInstance = Instantiate(changeBondWindowPrefab);
            var cb = changeBondWindowInstance.GetComponent<ManipulateBondTerm>();
            cb.tt = bond;
            var id = torsionTerms.IndexOf(bond);
            cb.okButton.GetComponent<Button>().onClick.AddListener(delegate { changeTorsionParametersUI(changeBondWindowInstance, id); });
#endif
        }

        private void createServerChangeTorsionWindow(ForceField.TorsionTerm bond)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (changeBondWindowInstance != null) { Destroy(changeBondWindowInstance); changeBondWindowInstance = null; }
            changeBondWindowInstance = Instantiate(changeServerBondWindowPrefab);

            var cb = changeBondWindowInstance.GetComponent<BondParametersServer>();
            cb.tt = bond;
            var id = torsionTerms.IndexOf(bond);
            cb.saveButton.GetComponent<Button>().onClick.AddListener(delegate { changeTorsionParametersUI(changeBondWindowInstance, id); });
#endif
        }

        private void changeTorsionParametersUI(GameObject windowInstance, int id)
        {
            cmlData before = this.AsCML();

            ForceField.TorsionTerm tt;
            if (!LoginData.isServer)
            {
#if CHARPACK_MRTK_2_8
                var cb = windowInstance.GetComponent<ManipulateBondTerm>();
                cb.changeBondParametersTT();
                tt = cb.tt;
                // Update tool tip
                string toolTipText = getTorsionToolTipText(tt.eqAngle, tt.vk, tt.nn);
                toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;
#else
                return;
#endif
            }
            else
            {
                var cb = windowInstance.GetComponent<BondParametersServer>();
                cb.changeBondParametersTT();
                tt = cb.tt;
                // Update tool tip
                string toolTipText = getTorsionToolTipText(tt.eqAngle, tt.vk, tt.nn);
                toolTipInstance.GetComponent<ServerTorsionTooltip>().ToolTipText.text = toolTipText;
            }

            changeTorsionParameters(tt, id);
            EventManager.Singleton.ChangeTorsionTerm(tt, m_id, (ushort)id);

            cmlData after = this.AsCML();
            GlobalCtrl.Singleton.undoStack.AddChange(new ChangeBondAction(before, after));

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
            atomList[term.Atom1].markAtom(mark, 5);
            atomList[term.Atom2].markAtom(mark, 5);
            atomList[term.Atom3].markAtom(mark, 5);
            atomList[term.Atom4].markAtom(mark, 5);
            atomList[term.Atom1].getBond(atomList[term.Atom2])?.markBond(mark, 5);
            atomList[term.Atom2].getBond(atomList[term.Atom3])?.markBond(mark, 5);
            atomList[term.Atom3].getBond(atomList[term.Atom4])?.markBond(mark, 5);

            if (toolTipInstance)
            {
                Destroy(toolTipInstance);
                toolTipInstance = null;
            }
        }
        public void markTorsionTermServer(ForceField.TorsionTerm term, bool mark)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            atomList[term.Atom1].markAtom(mark, 5);
            atomList[term.Atom2].markAtom(mark, 5);
            atomList[term.Atom3].markAtom(mark, 5);
            atomList[term.Atom4].markAtom(mark, 5);
            atomList[term.Atom1].getBond(atomList[term.Atom2])?.markBond(mark, 5);
            atomList[term.Atom2].getBond(atomList[term.Atom3])?.markBond(mark, 5);
            atomList[term.Atom3].getBond(atomList[term.Atom4])?.markBond(mark, 5);
#endif
        }
        // Helper methods to generate localized tool tip text
        private string getAtomToolTipText(double totMass, double maxDist)
        {
            string numAtoms = localizationManager.GetLocalizedString("NUM_ATOMS");
            string numBonds = localizationManager.GetLocalizedString("NUM_BONDS");
            string mass = localizationManager.GetLocalizedString("TOT_MASS");
            string toolTipText = $"{numAtoms}: {atomList.Count}\n{numBonds}: {bondList.Count}\n{mass}: {totMass:0.00}\nMaxRadius: {maxDist:0.00}";
            return toolTipText;
        }

        private string getBondToolTipText(double eqDist, double curDist, double kBond, double order)
        {
            string dist = localizationManager.GetLocalizedString("EQ_DIST");
            string singleBond = localizationManager.GetLocalizedString("SINGLE_BOND");
            string current = localizationManager.GetLocalizedString("CURRENT");
            string ord = localizationManager.GetLocalizedString("ORDER");
            string distanceInCorrectUnit = SettingsData.useAngstrom ? $"{dist}: {eqDist: 0.00}\u00C5" : $"{dist}: {eqDist * 100:0}pm";
            string curDistanceInCorrectUnit = SettingsData.useAngstrom ? $"{current}: {curDist: 0.00}\u00C5" : $"{current}: {curDist * 100:0}pm";
            string toolTipText = $"{singleBond}\n{distanceInCorrectUnit}\n{curDistanceInCorrectUnit}\nk: {kBond:0.00}\n{ord}: {order:0.00}";
            return toolTipText;
        }

        private string getAngleToolTipText(double eqAngle, double kAngle, double curAngle = 0)
        {
            string angleBond = localizationManager.GetLocalizedString("ANGLE_BOND");
            string eqAngleStr = localizationManager.GetLocalizedString("EQUI_ANGLE");
            string kAngleStr = localizationManager.GetLocalizedString("K_ANGLE");
            string current = localizationManager.GetLocalizedString("CURRENT");
            string toolTipText = $"{angleBond}\n{kAngleStr}: {kAngle:0.00}\n{eqAngleStr}: {eqAngle:0.00}\u00B0\n{current}: {curAngle:0.00}\u00B0";
            return toolTipText;
        }

        private string getTorsionToolTipText(double eqAngle, double vk, double nn, double curAngle = 0f)
        {
            //$"Torsion Bond\nEqui. Angle: {term.eqAngle}\nvk: {term.vk}\nnn: {term.nn}"
            string torsionBond = localizationManager.GetLocalizedString("TORSION_BOND");
            string eqAngleStr = localizationManager.GetLocalizedString("EQUI_ANGLE");
            string current = localizationManager.GetLocalizedString("CURRENT");
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
            if (SettingsData.licoriceRendering) // add frozen visual to bonds in licorice mode
            {
                foreach (var bond in bondList)
                {
                    setFrozenMaterialOnBond(bond, value);
                }
            }
#if CHARPACK_MRTK_2_8
            GetComponent<NearInteractionGrabbable>().enabled = !value;
            GetComponent<ObjectManipulator>().enabled = !value;
#endif
            frozen = value;
            if (freezeButton)
            {
                setFrozenVisual(frozen);
            }
        }

        public void setFrozenMaterialOnBond(Bond bond, bool value)
        {
            if (value)
            {
                // Append frozen material to end of list
                Material[] frozen = bond.GetComponentInChildren<MeshRenderer>().sharedMaterials.ToList().Append(frozen_bond_mat).ToArray();
                bond.GetComponentInChildren<MeshRenderer>().sharedMaterials = frozen;
            }
            else
            {
                // Remove frozen material
                List<Material> unfrozen = bond.GetComponentInChildren<MeshRenderer>().sharedMaterials.ToList();
                unfrozen.Remove(frozen_bond_mat);
                bond.GetComponentInChildren<MeshRenderer>().sharedMaterials = unfrozen.ToArray();
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
                FrozenIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.orange;
            }
            else
            {
                FrozenIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.gray;
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
            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
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
                            var currentDist = (FFposition[iAtom] - FFposition[jAtom]).magnitude / transform.localScale.x;
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
                                if (atomList[jdx].m_data.m_hybridization == 3) // tetraeder term
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
            var mol2d = Molecule2D.molecules.Find(x => x.molReference == this);
            if (mol2d != null)
            {
                mol2d.initialized = false;
                Destroy(mol2d.gameObject);
            }
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
            EventManager.Singleton.OnMoleculeLoaded -= triggerGenerateFF;
            EventManager.Singleton.OnMolDataChanged -= triggerGenerateFF;
            EventManager.Singleton.OnMoleculeLoaded -= adjustBBox;
            EventManager.Singleton.OnMolDataChanged -= adjustBBox;
#if CHARPACK_MRTK_2_8
            var om = GetComponent<ObjectManipulator>();
            om.OnManipulationStarted.RemoveListener(OnManipulationStarted);
            om.OnManipulationEnded.RemoveListener(OnManipulationEnded);
#endif
            if (HandTracking.Singleton)
            {
                HandTracking.Singleton.OnMiddleFingerGrab.RemoveListener(OnTransitionGrab);
                HandTracking.Singleton.OnFlick.RemoveListener(OnTransitionFlick);
                HandTracking.Singleton.OnMiddleFingerGrabRelease -= OnTransitionGrabRelease;
                HandTracking.Singleton.OnEmptyIndexFingerGrab.RemoveListener(OnNormalGrab);
                HandTracking.Singleton.OnEmptyCloseIndexFingerGrab.RemoveListener(OnNormalGrab);
                //HandTracking.Singleton.OnIndexFingerGrab -= OnNormalGrab;
                //HandTracking.Singleton.OnIndexFingerGrabRelease -= OnNormalGrabRelease;
            }
#if UNITY_STANDALONE || UNITY_EDITOR
            if (LoginData.isServer)
            {
                StructureFormulaManager.Singleton.removeContent(m_id);
            }
#endif
        }
    }
}
