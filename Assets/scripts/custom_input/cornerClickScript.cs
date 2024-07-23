using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This script allows for mouse interaction with molecules and their bounding boxes.
/// </summary>
public class cornerClickScript : MonoBehaviour
{
#if UNITY_STANDALONE || UNITY_EDITOR

    private Transform trans;
    private Molecule mol = null;
    private GenericObject go = null;
    private myBoundingBox box;
    private Vector3 pickupPos = Vector3.zero;
    private Quaternion pickupRot = Quaternion.identity;

    private void Start()
    {
        trans = transform.parent.transform.parent;
        mol = trans.GetComponent<Molecule>();
        go = trans.GetComponent<GenericObject>();
        box = transform.parent.transform.parent.GetComponent<myBoundingBox>();
    }

    private Stopwatch stopwatch;
    // offset for mouse interaction
    private Vector3 offset = Vector3.zero;
    void OnMouseDown()
    {
        // Handle server GUI interactions
        if (EventSystem.current.IsPointerOverGameObject()) { return; }
        
        pickupPos = trans.localPosition;
        pickupRot = trans.localRotation;

        offset = trans.position -
        GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f));
        //
        stopwatch = Stopwatch.StartNew();
        box.setGrabbed(true);

        if (mol != null)
        {
            mol.isGrabbed = true;
        }
        if (go != null)
        {
            go.isGrabbed = true;
            go.processHighlights();
        }
    }

    void OnMouseDrag()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f);
        trans.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(newPosition) + offset;
        // position relative to molecule position
        if (mol != null)
        {
            EventManager.Singleton.MoveMolecule(mol.m_id, trans.localPosition, trans.localRotation);
        }
    }

    private void OnMouseUp()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        stopwatch?.Stop();
        if (stopwatch?.ElapsedMilliseconds < 200)
        {
            trans.localPosition = pickupPos;
            trans.localRotation = pickupRot;
            if (mol != null)
            {
                EventManager.Singleton.MoveMolecule(mol.m_id, trans.localPosition, trans.localRotation);
                mol.markMoleculeUI(!mol.isMarked, true);
            }
            else if (go != null)
            {
                go.toggleMarkObject();
            }
        }
        else
        {
            // check for potential merge
            GlobalCtrl.Singleton.checkForCollisionsAndMerge(mol);
        }

        // change material back to normal
        box.setGrabbed(false);

        if (mol != null)
        {
            mol.isGrabbed = false;
        }
        if (go != null)
        {
            go.isGrabbed = false;
            go.processHighlights();
        }
    }
#endif
}

