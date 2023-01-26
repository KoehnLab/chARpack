using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace StructClass
{
    /// <summary>
    /// elements with their order are saved here
    /// </summary>
    public enum ElementType
    {
        s,p,d,f
    }

    /// <summary>
    /// structure of the element, includes: ID, name, abbrevation, type, mass, radius, number of bonds, color
    /// </summary>
    [Serializable]
    public struct ElementData
    {
        public ushort m_id;
        public string m_name;
        public string m_abbre;
        public ElementType m_type;
        public float m_mass;
        public float m_radius;
        public uint m_bondNum;
        public ushort m_hybridization;
        public Color m_color;
        public ElementData(ushort id,string name, string abbre, ElementType type, float mass, float radius, uint count, ushort hyb, float red, float green, float blue)                  
        {
            m_id = id;
            m_name = name;
            m_abbre= abbre;
            m_type = type;
            m_mass = mass;
            m_radius = radius;
            m_bondNum = count;
            m_hybridization = hyb;
            m_color = new Color(red,green,blue);
        }

        public ElementData(ushort id, string name, string abbre, ElementType type, float mass, float radius, uint count, ushort hyb, Color color)
        {
            m_id = id;
            m_name = name;
            m_abbre = abbre;
            m_type = type;
            m_mass = mass;
            m_radius = radius;
            m_bondNum = count;
            m_hybridization = hyb;
            m_color = color;
        }
    }

    /// <summary>
    /// structure of an atom in cml
    /// </summary>
    [Serializable]
    public struct cmlAtom
    {
        public ushort id;
        public string abbre;
        public ushort hybrid;
        public SaveableVector3 pos;
        public cmlAtom(ushort _id,string name, ushort hybridisation, SaveableVector3 _pos)
        {
            id = _id;
            abbre = name;
            hybrid = hybridisation;
            pos = _pos;
        }
    }

    //Bindungslänge

    /// <summary>
    /// structure of a bond in cml
    /// </summary>
    [Serializable]
    public struct cmlBond
    {
        public ushort id1;
        public ushort id2;
        public float order;
        public float eqDist;
        public float kb;

        public cmlBond(ushort atom1, ushort atom2, float order_, float eqDist_ = -1.0f, float k = -1.0f)
        {
            id1 = atom1;
            id2 = atom2;
            order = order_;
            eqDist = eqDist_;
            kb = k;
        }
    }

    /// <summary>
    /// structure of an angle bond in cml
    /// </summary>
    [Serializable]
    public struct cmlAngle
    {
        public ushort id1;
        public ushort id2;
        public ushort id3;
        public float angle;
        public float ka;

        public cmlAngle(ushort a, ushort b, ushort c, float ang, float k = -1.0f)
        {
            id1 = a;
            id2 = b;
            id3 = c;
            angle = ang;
            ka = k;
        }
    }

    /// <summary>
    /// structure of an torsion bond in cml
    /// </summary>
    [Serializable]
    public struct cmlTorsion
    {
        public ushort id1;
        public ushort id2;
        public ushort id3;
        public ushort id4;
        public float angle;
        public float k0;
        public ushort nn;
        public cmlTorsion(ushort a, ushort b, ushort c, ushort d, float ang, float k = -1.0f, ushort nn_ = 1)
        {
            id1 = a;
            id2 = b;
            id3 = c;
            id4 = d;
            angle = ang;
            k0 = k;
            nn = nn_;
        }
    }

    /// <summary>
    /// cmlData combines the list of atoms and bonds in cml format
    /// </summary>
    [Serializable]
    public struct cmlData
    {
        public SaveableVector3 molePos;
        public SaveableQuaternion moleQuat;
        public ushort moleID;
        public bool keepConfig;
        public cmlAtom[] atomArray;
        public cmlBond[] bondArray;
        public cmlAngle[] angleArray;
        public cmlTorsion[] torsionArray;

        public cmlData(SaveableVector3 pos, SaveableQuaternion quat, ushort id, List<cmlAtom> a, List<cmlBond> b, List<cmlAngle> ang = null, List<cmlTorsion> tor = null, bool keepConfig_ = false)
        {
            molePos = pos;
            moleQuat = quat;
            moleID = id;
            keepConfig = keepConfig_;
            atomArray = a.ToArray();
            bondArray = b.ToArray();
            angleArray = ang?.ToArray();
            torsionArray = tor?.ToArray();
        }
    }
    /// <summary>
    /// contain the status of each buttons of a controller
    /// </summary>
    public struct ControllerStatus
    {
        public float TriggerPress;
        public float Primary2D;
        public float Secondary2D;
        public float GripPress;
        public void SetValue(float triggerPress, float primary2D, float secondary2D, float grip)
        {
            TriggerPress = triggerPress;
            Primary2D = primary2D;
            Secondary2D = secondary2D;
            GripPress = grip;
        }
    }
    /// <summary>
    /// which hand
    /// </summary>
    public enum WhichHand
    {
        Any = 0,
        left = 1,
        right = 2
    }

    [Serializable]
    public struct SaveableVector3
    {
        public float x;
        public float y;
        public float z;

        public SaveableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SaveableVector3))
            {
                return false;
            }

            var s = (SaveableVector3)obj;
            return x == s.x &&
                   y == s.y &&
                   z == s.z;
        }

        public override int GetHashCode()
        {
            var hashCode = 373119288;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static bool operator ==(SaveableVector3 a, SaveableVector3 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        public static bool operator !=(SaveableVector3 a, SaveableVector3 b)
        {
            return a.x != b.x && a.y != b.y && a.z != b.z;
        }

        public static implicit operator Vector3(SaveableVector3 x)
        {
            return new Vector3(x.x, x.y, x.z);
        }

        public static implicit operator SaveableVector3(Vector3 x)
        {
            return new SaveableVector3(x.x, x.y, x.z);
        }
    }

    [Serializable]
    public struct SaveableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SaveableQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SaveableQuaternion))
            {
                return false;
            }

            var s = (SaveableQuaternion)obj;
            return x == s.x &&
                   y == s.y &&
                   z == s.z &&
                   w == s.w;
        }

        public override int GetHashCode()
        {
            var hashCode = 373119288;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            hashCode = hashCode * -1521134295 + w.GetHashCode();
            return hashCode;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        public static bool operator ==(SaveableQuaternion a, SaveableQuaternion b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        public static bool operator !=(SaveableQuaternion a, SaveableQuaternion b)
        {
            return a.x != b.x && a.y != b.y && a.z != b.z && a.w != b.w;
        }

        public static implicit operator Quaternion(SaveableQuaternion q)
        {
            return new Quaternion(q.x, q.y, q.z, q.w);
        }

        public static implicit operator SaveableQuaternion(Quaternion q)
        {
            return new SaveableQuaternion(q.x, q.y, q.z, q.w);
        }
    }
}