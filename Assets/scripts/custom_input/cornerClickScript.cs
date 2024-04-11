using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This script allows for mouse interaction with molecules and their bounding boxes.
/// </summary>
public class cornerClickScript : MonoBehaviour
{
#if !WINDOWS_UWP

    private Molecule mol;
    private myBoundingBox box;
    private Vector3 pickupPos = Vector3.zero;
    private Quaternion pickupRot = Quaternion.identity;

    private void Start()
    {
        mol = transform.parent.transform.parent.GetComponent<Molecule>();
        box = transform.parent.transform.parent.GetComponent<myBoundingBox>();
    }

    private Stopwatch stopwatch;
    // offset for mouse interaction
    private Vector3 offset = Vector3.zero;
    void OnMouseDown()
    {
        // Handle server GUI interactions
        if (EventSystem.current.IsPointerOverGameObject()) { return; }
        
        pickupPos = mol.transform.localPosition;
        pickupRot = mol.transform.localRotation;

        offset = mol.transform.position -
        GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f));
        //
        stopwatch = Stopwatch.StartNew();
        box.setGrabbed(true);
    }

    void OnMouseDrag()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f);
        mol.transform.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(newPosition) + offset;
        // position relative to molecule position
        EventManager.Singleton.MoveMolecule(mol.m_id, mol.transform.localPosition, mol.transform.localRotation);
    }

    private void OnMouseUp()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        stopwatch?.Stop();
        if (stopwatch?.ElapsedMilliseconds < 200)
        {
            mol.transform.localPosition = pickupPos;
            mol.transform.localRotation = pickupRot;
            EventManager.Singleton.MoveMolecule(mol.m_id, mol.transform.localPosition, mol.transform.localRotation);
            mol.markMoleculeUI(!mol.isMarked, true);
        }
        else
        {
            // check for potential merge
            if (GlobalCtrl.Singleton.collisions.Count > 0)
            {
                var collisions = new Dictionary<Atom, Atom>();
                foreach(Atom a in mol.atomList)
                {
                    if (GlobalCtrl.Singleton.collisions.ContainsKey(a)) collisions.Add(a, GlobalCtrl.Singleton.collisions[a]);
                    if (GlobalCtrl.Singleton.collisions.ContainsValue(a)) collisions.Add(GlobalCtrl.Singleton.collisions.First(x => x.Value.Equals(a)).Key, a);
                }

                if (collisions.Count>0)
                {
                    foreach (Atom d1 in collisions.Keys)
                    {
                        Atom d2 = collisions[d1];
                        Atom a1 = d1.dummyFindMain();
                        Atom a2 = d2.dummyFindMain();

                        if (!a1.alreadyConnected(a2))
                        {
                            if (mol.atomList.Contains(d1))
                            {
                                EventManager.Singleton.MergeMolecule(d1.m_molecule.m_id, d1.m_id, d2.m_molecule.m_id, d2.m_id);
                                GlobalCtrl.Singleton.MergeMolecule(d1, d2);
                            }
                            else
                            {
                                EventManager.Singleton.MergeMolecule(d2.m_molecule.m_id, d2.m_id, d1.m_molecule.m_id, d1.m_id);
                                GlobalCtrl.Singleton.MergeMolecule(d2, d1);
                            }
                        }
                    }
                }
            }
        }

        // change material back to normal
        box.setGrabbed(false);
    }
#endif
}

