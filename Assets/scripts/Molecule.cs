using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class Molecule : MonoBehaviour, IMixedRealityPointerHandler
{
    private Stopwatch stopwatch;
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        stopwatch = Stopwatch.StartNew();
        // change material of grabbed object
        var bbox = gameObject.GetComponent<myBoundingBox>();
        if (bbox.myHandleGrabbedMaterial != null)
        {
            foreach (var handle in bbox.cornerHandles)
            {
                Renderer[] renderers = handle.GetComponentsInChildren<Renderer>();

                for (int j = 0; j < renderers.Length; ++j)
                {
                    renderers[j].material = bbox.myHandleGrabbedMaterial;
                }
            }
        }
        if (bbox.myLineGrabbedMaterial != null)
        {
            bbox.myLR.material = bbox.myLineGrabbedMaterial;
        }
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
            EventManager.Singleton.SelectMolecule(m_id, !isMarked);
            markMolecule(!isMarked, true);
        }
        if (GlobalCtrl.Singleton.collision)
        {
            Atom d1 = GlobalCtrl.Singleton.collider1;
            Atom d2 = GlobalCtrl.Singleton.collider2;

            Atom a1 = Atom.Instance.dummyFindMain(d1);
            Atom a2 = Atom.Instance.dummyFindMain(d2);

            if (!Atom.Instance.alreadyConnected(a1, a2))
            {
                GlobalCtrl.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1, GlobalCtrl.Singleton.collider2);
                EventManager.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1.m_id, GlobalCtrl.Singleton.collider2.m_id);
            }

        }
        // change material back to normal
        var bbox = gameObject.GetComponent<myBoundingBox>();
        if (bbox.myHandleMaterial != null)
        {
            foreach (var handle in bbox.cornerHandles)
            {
                Renderer[] renderers = handle.GetComponentsInChildren<Renderer>();

                for (int j = 0; j < renderers.Length; ++j)
                {
                    renderers[j].material = bbox.myHandleMaterial;
                }
            }
        }
        if (bbox.myLineMaterial != null)
        {
            bbox.myLR.material = bbox.myLineMaterial;
        }
    }

    private GameObject myToolTipPrefab;
    private GameObject deleteMeButtonPrefab;
    private GameObject closeMeButtonPrefab;
    private GameObject toolTipInstance;
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
    public void f_Init(ushort idInScene, Transform inputParent)
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
        gameObject.AddComponent<ObjectManipulator>();
        gameObject.AddComponent<NearInteractionGrabbable>();

        // load prefabs
        myToolTipPrefab = (GameObject)Resources.Load("prefabs/MRTKAtomToolTip");
        if (myToolTipPrefab == null)
        {
            throw new FileNotFoundException("[Molecule] MRTKAtomToolTip prefab not found - please check the configuration");
        }
        deleteMeButtonPrefab = (GameObject)Resources.Load("prefabs/DeleteMeButton");
        if (deleteMeButtonPrefab == null)
        {
            throw new FileNotFoundException("[Molecule] DeleteMeButton prefab not found - please check the configuration");
        }
        closeMeButtonPrefab = (GameObject)Resources.Load("prefabs/CloseMeButton");
        if (closeMeButtonPrefab == null)
        {
            throw new FileNotFoundException("[Molecule] CloseMeButton prefab not found - please check the configuration");
        }
        

    }

    /// <summary>
    /// if two molecules are merged, all atoms from the old molecule need to be transferred to the new molecule
    /// </summary>
    /// <param name="newParent"> the molecule which is the new parent to all atoms</param>
    public void givingOrphans(Molecule newParent, Molecule oldParent)
    {
        ushort maxID = newParent.getFreshAtomID();
        foreach(Atom a in atomList)
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
        Destroy(gameObject);
    }


    public void markMolecule(bool mark, bool showToolTip = false)
    {

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
            if (toolTipInstance != null)
            {
                Destroy(toolTipInstance);
            }
        } 
        else
        {
            if (toolTipInstance == null && showToolTip)
            {
                createToolTip();
            }
        }
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

    private void createToolTip()
    {
        // create tool tip
        toolTipInstance = Instantiate(myToolTipPrefab);
        // put tool top to the right 
        Vector3 ttpos = transform.position + toolTipDistanceWeight * Camera.main.transform.right + toolTipDistanceWeight * Camera.main.transform.up;
        toolTipInstance.transform.position = ttpos;
        // add atom as connector
        toolTipInstance.GetComponent<myToolTipConnector>().Target = gameObject;
        // calc some meta data to show
        float tot_mass = 0.0f;
        calcMetaData(ref tot_mass);
        var mol_center = getCenter();
        var max_dist = getMaxDistFromCenter(mol_center);
        string toolTipText = $"NumAtoms: {atomList.Count}\nNumBonds: {bondList.Count}\nTotMass: {tot_mass.ToString("0.00")}\nMaxRadius: {max_dist.ToString("0.00")}";
        toolTipInstance.GetComponent<DynamicToolTip>().ToolTipText = toolTipText;
        var delButtonInstance = Instantiate(deleteMeButtonPrefab);
        delButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.deleteMoleculeUI(this); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(delButtonInstance);
        var closeButtonInstance = Instantiate(closeMeButtonPrefab);
        closeButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { markMolecule(false); });
        toolTipInstance.GetComponent<DynamicToolTip>().addContent(closeButtonInstance);

    }

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
        var bondList = new List<Bond>();
        for (ushort i = 0; i < atomList.Count; i++)
        {
            // also change ids in bond
            if (atomList[i].m_id != i)
            {
                from.Add(atomList[i].m_id);
                to.Add(i);
                foreach (var bond in atomList[i].connectedBonds())
                {
                    if (!bondList.Contains(bond))
                    {
                        bondList.Add(bond);
                    }
                }

            }
            atomList[i].m_id = i;
        }
        foreach (var bond in bondList)
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


    public void OnDestroy()
    {
        if (toolTipInstance != null)
        {
            Destroy(toolTipInstance);
        }
        GlobalCtrl.Singleton.List_curMolecules.Remove(this);
    }
}
