using System;
using System.Collections.Generic;
using UnityEngine;

namespace chARpack
{
    public class Molecule2D : MonoBehaviour
    {
        public static List<Molecule2D> molecules = new List<Molecule2D>();

        public Molecule molReference;

        private List<Atom2D> atoms;

        public List<Bond2D> bonds = new List<Bond2D>();

        public bool initialized = false;

        public Vector3 molCenter = Vector3.zero;

        public List<Atom2D> Atoms { get => atoms; set { atoms = value; calcCenter(); } }

        private void calcCenter()
        {
            var accum = Vector3.zero;
            foreach (Transform child in transform)
            {
                accum = child.position;
            }
            accum /= transform.childCount;
            molCenter = accum;
            var debug_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debug_sphere.transform.parent = transform;
            debug_sphere.transform.position = molCenter;

            var debug_sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debug_sphere2.transform.parent = transform;
            debug_sphere2.transform.position = transform.position;
        }

        private void Update()
        {
            if (initialized)
            {
                transform.position += molReference.transform.position - molCenter;
                foreach (var bond in bonds)
                {
                    //bond.atom1.transform.position = bond.atom1.atomReference.transform.position;
                    //bond.atom2.transform.position = bond.atom2.atomReference.transform.position;

                    var a1_pos = bond.atom1.transform.position;
                    var a2_pos = bond.atom2.transform.position;
                    var offset1 = bond.atom1ConnectionOffset * transform.localScale.x;
                    var offset2 = bond.atom2ConnectionOffset * transform.localScale.x;
                    var direction = a1_pos - a2_pos;
                    var pos1 = a1_pos - direction.normalized * offset1;
                    var pos2 = a2_pos + direction.normalized * offset2;
                    var distance = Vector3.Distance(pos1, pos2);

                    //var pos1 = Vector3.MoveTowards(a1_pos, a2_pos, offset1);
                    //var pos2 = Vector3.MoveTowards(a2_pos, a1_pos, offset2);
                    //var pos1 = bond.atom1.transform.position + transform.TransformVector(bond.atom1ConnectionOffset);
                    //var pos2 = bond.atom2.transform.position + transform.TransformVector(bond.atom2ConnectionOffset);
                    //float distance = (Vector3.Distance(a1_pos, a2_pos) - offset1 - offset2);
                    // Calculate the direction and distance between the two points
                    //Vector3 direction = endPoint - startPoint;
                    //float distance = direction.magnitude;

                    // Calculate the midpoint between the two points
                    Vector3 midpoint = (pos1 + pos2) / 2.0f;

                    // Set the position of the GameObject to the midpoint
                    bond.transform.position = midpoint;

                    // Rotate the GameObject to align with the direction
                    if (bond.initialLookAt == bond.atom1)
                    {
                        bond.transform.LookAt(pos1);
                    }
                    else
                    {
                        bond.transform.LookAt(pos2);
                    }


                    // Scale the GameObject along the X-axis to match the distance between the two points
                    // weighted with the inital length of the object and corrected for the molecule's current scale
                    bond.transform.localScale = new Vector3(
                        bond.transform.localScale.x,
                        bond.transform.localScale.y,
                        distance / (bond.initialLength * transform.localScale.x));
                }
            }
        }

        public void setOpacity(float value)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.materials)
                {
                    var col = new Color(mat.color.r, mat.color.g, mat.color.b, value);
                    mat.color = col;
                }
            }
        }
    }
}