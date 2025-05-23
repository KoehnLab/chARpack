using UnityEngine;

namespace chARpack
{
    public class Bond2D : MonoBehaviour
    {
        public Atom2D atom1;
        public Atom2D atom2;

        public Atom atom1ref;
        public Atom atom2ref;

        public Bond bondReference;

        public float atom1ConnectionOffset;
        public float atom2ConnectionOffset;

        public Vector3 end1;
        public Vector3 end2;

        public float initialLength;

        public Atom2D initialLookAt;
    }
}
