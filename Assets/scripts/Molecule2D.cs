using chARpackStructs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Molecule2D : MonoBehaviour
{
    public static List<Molecule2D> molecules = new List<Molecule2D>();

    public Molecule molReference;

    public List<Atom2D> atoms = new List<Atom2D>();
    public List<Bond2D> bonds = new List<Bond2D>();

    public bool initialized = false;


    private void Update()
    {
        if (initialized)
        {
            foreach (var bond in bonds)
            {

                var pos1 = bond.atom1.transform.position;// + transform.TransformVector(bond.atom1ConnectionOffset);
                var pos2 = bond.atom2.transform.position;// + transform.TransformVector(bond.atom2ConnectionOffset);

                MeshLine.SetStartAndEndPoint(bond.transform, pos1, pos2);
            }
        }
    }



}
