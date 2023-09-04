using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using TMPro;

public class cornerClickScript : MonoBehaviour
{
#if !WINDOWS_UWP

    private Molecule mol;
    private myBoundingBox box;
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
        offset = mol.transform.position -
        GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f));
        //
        stopwatch = Stopwatch.StartNew();
        box.setGrabbed(true);
    }

    void OnMouseDrag()
    {
        Vector3 newPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.5f);
        mol.transform.position = GlobalCtrl.Singleton.currentCamera.ScreenToWorldPoint(newPosition) + offset;
        // position relative to molecule position
        EventManager.Singleton.MoveMolecule(mol.m_id, mol.transform.localPosition, mol.transform.localRotation);
    }

    private void OnMouseUp()
    {
        stopwatch?.Stop();
        if (stopwatch?.ElapsedMilliseconds < 200)
        {
            mol.markMoleculeUI(!mol.isMarked, true);
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
                    EventManager.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1.m_molecule.m_id, GlobalCtrl.Singleton.collider1.m_id, GlobalCtrl.Singleton.collider2.m_molecule.m_id, GlobalCtrl.Singleton.collider2.m_id);
                    GlobalCtrl.Singleton.MergeMolecule(GlobalCtrl.Singleton.collider1, GlobalCtrl.Singleton.collider2);
                }

            }
        }

        // change material back to normal
        box.setGrabbed(false);
    }
#endif
}

