using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Molecule : MonoBehaviour, IMixedRealityPointerHandler
{
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer is SpherePointer)
        {
            Debug.Log($"Grab start from {eventData.Pointer.PointerName}");
        }
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
        // Intentionally empty
    }

    // This function is triggered when a grabbed object is dropped
    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (GlobalCtrl.Instance.collision)
        {
            Atom d1 = GlobalCtrl.Instance.collider1;
            Atom d2 = GlobalCtrl.Instance.collider2;

            Atom a1 = Atom.Instance.dummyFindMain(d1);
            Atom a2 = Atom.Instance.dummyFindMain(d2);

            if (!Atom.Instance.alreadyConnected(a1, a2))
                GlobalCtrl.Instance.MergeMolecule(GlobalCtrl.Instance.collider1, GlobalCtrl.Instance.collider2);

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


    /// <summary>
    /// molecule id
    /// </summary>
    public int m_id;


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
    public void f_Init(int idInScene, Transform inputParent)
    {
        m_id = idInScene;
        isMarked = false;
        this.name = "molecule_" + m_id;
        this.transform.parent = inputParent;
        atomList = new List<Atom>();
        bondList = new List<Bond>();
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.001f,0.001f,0.001f);
        // these objects take input from corner colliders and manipulate the moluecule
        gameObject.AddComponent<ObjectManipulator>();
        gameObject.AddComponent<NearInteractionGrabbable>();

    }

    /// <summary>
    /// if two molecules are merged, all atoms from the old molecule need to be transferred to the new molecule
    /// </summary>
    /// <param name="newParent"> the molecule which is the new parent to all atoms</param>
    public void givingOrphans(Molecule newParent, Molecule oldParent)
    {
        foreach(Atom a in atomList)
        {
            a.transform.parent = newParent.transform;
            a.m_molecule = newParent;
            newParent.atomList.Add(a);
        }
        foreach (Bond b in bondList)
        {
            b.transform.parent = newParent.transform;
            b.m_molecule = newParent;
            newParent.bondList.Add(b);
        }
        GlobalCtrl.Instance.List_curMolecules.Remove(oldParent);
        //GlobalCtrl.Instance.Dic_curMolecules.Remove(m_id);
        Destroy(this.gameObject);
    }


    public void markMolecule(bool mark)
    {
        foreach (Atom a in this.atomList)
        {
            a.markAtom(mark);
        }

        foreach (Bond b in this.bondList)
        {
            b.markBond(mark);
        }
        isMarked = mark;
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

    // Update is called once per frame
    void Update()
    {
        //BoxCollider bc = gameObject.GetComponent<BoxCollider>();
        //Vector3 center = getCenter();
        //bc.transform.position = center;
        //float max_dist = getMaxDistFromCenter(center);
        //bc.size = new Vector3(max_dist / 4.0f, max_dist / 4.0f, max_dist / 4.0f);
    }
}
