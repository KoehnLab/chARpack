using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DihedralAngleMeasurement : MonoBehaviour
{
    [HideInInspector] public List<Atom> atoms = new List<Atom>();

    private double dihedralAngle = 0f;

    // Update is called once per frame
    void Update()
    {
        if(atoms.Count == 4 && !atoms.Contains(null))
        {
            // TODO: implement checking whether the atoms are connected
            dihedralAngle = computeDihedralAngle();
        }
    }

    private double computeDihedralAngle()
    {
        Vector3[] distances = new Vector3[3];
        for(var i=0; i<3; i++)
        {
            distances[i] = atoms[i + 1].gameObject.transform.position - atoms[i].gameObject.transform.position;
        }

        Vector3 n1 = Vector3.Cross(distances[0], distances[1]).normalized;
        Vector3 n2 = Vector3.Cross(distances[1], distances[2]).normalized;

        var phi = Vector3.Angle(n1, n2);
        return phi;
    }

    public double getAngle()
    {
        return dihedralAngle;
    }
}
