using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class chARpackUtility
{

    public static void setObjectGrabbed(Transform obj, bool value)
    {
        var mol = obj.GetComponent<Molecule>();
        if (mol != null)
        {
            mol.isGrabbed = value;
            return;
        }
        var go = obj.GetComponent<GenericObject>();
        if (go != null)
        {
            go.isGrabbed = value;
        }
    }
}
