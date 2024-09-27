using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Linq;
using Unity.VisualScripting;

namespace chARpackStructs
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
        public ushort m_bondNum;
        public ushort m_hybridization;
        public Color m_color;
        public ElementData(ushort id,string name, string abbre, ElementType type, float mass, float radius, ushort b_count, ushort hyb, float red, float green, float blue)                  
        {
            m_id = id;
            m_name = name;
            m_abbre= abbre;
            m_type = type;
            m_mass = mass;
            m_radius = radius;
            m_bondNum = b_count;
            m_hybridization = hyb;
            m_color = new Color(red,green,blue);
        }

        public ElementData(ushort id, string name, string abbre, ElementType type, float mass, float radius, ushort b_count, ushort hyb, Color color)
        {
            m_id = id;
            m_name = name;
            m_abbre = abbre;
            m_type = type;
            m_mass = mass;
            m_radius = radius;
            m_bondNum = b_count;
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
        public bool keepConfig;

        public cmlAtom(ushort _id,string name, ushort hybridisation, SaveableVector3 _pos, bool keep_config)
        {
            id = _id;
            abbre = name;
            hybrid = hybridisation;
            pos = _pos;
            keepConfig = keep_config;
        }
    }

    //Bindungsl�nge

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
    [XmlRoot("cmlData")]
    [Serializable]
    public struct cmlData
    {
        public string name;
        public SaveableVector3 molePos;
        public SaveableVector2 ssPos;
        public SaveableVector4 ssBounds;
        public SaveableVector3 moleScale;
        public bool moleTransitioned;
        public int transitionTriggeredBy;
        public SaveableQuaternion moleQuat;
        public SaveableQuaternion relQuat;
        public Guid moleID;
        public bool keepConfig;
        public cmlAtom[] atomArray;
        public cmlBond[] bondArray;
        [XmlArray, DefaultValue(null)]
        public cmlAngle[] angleArray;
        [XmlArray, DefaultValue(null)]
        public cmlTorsion[] torsionArray;
        public bool frozen;
        public int TransitionTriggeredFromId;

        public cmlData(SaveableVector3 pos, SaveableVector3 scale, SaveableQuaternion quat, Guid id, List<cmlAtom> a, List<cmlBond> b, List<cmlAngle> ang = null, List<cmlTorsion> tor = null, bool keepConfig_ = false)
        {
            molePos = pos;
            moleScale = scale;
            moleQuat = quat;
            moleID = id;
            keepConfig = keepConfig_;
            atomArray = a.ToArray();
            bondArray = b.ToArray();
            angleArray = ang?.ToArray();
            torsionArray = tor?.ToArray();
            relQuat = Quaternion.identity;
            ssPos = Vector2.zero;
            ssBounds = Vector4.zero;
            moleTransitioned = false;
            transitionTriggeredBy = -1;
            name = "";
            frozen = false;
            TransitionTriggeredFromId = -1;
        }

        public void assignRelativeQuaternion(Quaternion q)
        {
            relQuat = q;
        }

        public void assignSSPos(Vector2 ss_coords)
        {
            ssPos = ss_coords;
        }

        public void assignSSBounds(Vector4 ss_bounds)
        {
            ssBounds = ss_bounds;
        }

        public void setTransitionFlag()
        {
            moleTransitioned = true;
        }

        public void setTransitionTriggeredBy(TransitionManager.InteractionType triggered_by)
        {
            transitionTriggeredBy = (int)triggered_by;
        }

        public void assignName(string name_)
        {
            name = name_;
        }

        public void setFrozen(bool value)
        {
            frozen = value;
        }

        public void setTriggeredFromId(int id)
        {
            TransitionTriggeredFromId = id;
        }
    }

    /// <summary>
    /// data that combines attributes of a generic object for saving or sending data via network
    /// </summary>
    [Serializable]
    public struct sGenericObject
    {
        public string obj_name;
        public SaveableVector3 pos;
        public SaveableVector3 scale;
        public SaveableQuaternion quat;
        public SaveableVector2 ssPos;
        public SaveableVector4 ssBounds;
        public bool transitioned;
        public int transitionTriggeredBy;
        public SaveableQuaternion relQuat;
        public Guid ID;
        public int TransitionTriggeredFromId;

        public sGenericObject(string obj_name_, Guid id_, SaveableVector3 pos_, SaveableVector3 scale_, SaveableQuaternion quat_)
        {
            obj_name = obj_name_;
            pos = pos_;
            scale = scale_;
            quat = quat_;
            ID = id_;
            relQuat = Quaternion.identity;
            ssPos = Vector2.zero;
            ssBounds = Vector4.zero;
            transitioned = false;
            transitionTriggeredBy = -1;
            TransitionTriggeredFromId = -1;
        }

        public void assignRelativeQuaternion(Quaternion q)
        {
            relQuat = q;
        }

        public void assignSSPos(Vector2 ss_coords)
        {
            ssPos = ss_coords;
        }

        public void assignSSBounds(Vector4 ss_bounds)
        {
            ssBounds = ss_bounds;
        }

        public void setTransitionFlag()
        {
            transitioned = true;
        }
        public void setTransitionTriggeredBy(TransitionManager.InteractionType triggered_by)
        {
            transitionTriggeredBy = (int)triggered_by;
        }

        public void setTranstionTriggeredFromId(int id)
        {
            TransitionTriggeredFromId = id;
        }
    }


    [Serializable]
    public struct SaveableVector2
    {
        public float x;
        public float y;

        public SaveableVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SaveableVector2))
            {
                return false;
            }

            var s = (SaveableVector2)obj;
            return x == s.x && y == s.y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }

        public static bool operator ==(SaveableVector2 a, SaveableVector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(SaveableVector2 a, SaveableVector2 b)
        {
            return a.x != b.x || a.y != b.y;
        }

        public static bool operator ==(Vector2 a, SaveableVector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator ==(SaveableVector2 a, Vector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Vector2 a, SaveableVector2 b)
        {
            return a.x != b.x || a.y != b.y;
        }

        public static bool operator !=(SaveableVector2 a, Vector2 b)
        {
            return a.x != b.x || a.y != b.y;
        }

        public static implicit operator Vector2(SaveableVector2 x)
        {
            return new Vector2(x.x, x.y);
        }

        public static implicit operator SaveableVector2(Vector2 x)
        {
            return new SaveableVector2(x.x, x.y);
        }
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
            return a.x != b.x || a.y != b.y || a.z != b.z;
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
    public struct SaveableVector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SaveableVector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;

        }

        public override bool Equals(object obj)
        {
            if (!(obj is SaveableVector4))
            {
                return false;
            }

            var s = (SaveableVector4)obj;
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
            return hashCode;
        }

        public Vector4 ToVector4()
        {
            return new Vector4(x, y, z, w);
        }

        public static bool operator ==(SaveableVector4 a, SaveableVector4 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        public static bool operator !=(SaveableVector4 a, SaveableVector4 b)
        {
            return a.x != b.x && a.y != b.y && a.z != b.z && a.w != b.w;
        }

        public static implicit operator Vector4(SaveableVector4 x)
        {
            return new Vector4(x.x, x.y, x.z, x.w);
        }

        public static implicit operator SaveableVector4(Vector4 x)
        {
            return new SaveableVector4(x.x, x.y, x.z, x.w);
        }

        public static bool operator ==(Vector4 a, SaveableVector4 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        public static bool operator ==(SaveableVector4 a, Vector4 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        public static bool operator !=(Vector4 a, SaveableVector4 b)
        {
            return a.x != b.x || a.y != b.y || a.z != b.z || a.w != b.w;
        }

        public static bool operator !=(SaveableVector4 a, Vector4 b)
        {
            return a.x != b.x || a.y != b.y || a.z != b.z || a.w != b.w;
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
            return a.x != b.x || a.y != b.y || a.z != b.z || a.w != b.w;
        }

        public static bool operator ==(SaveableQuaternion a, Quaternion b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        public static bool operator !=(SaveableQuaternion a, Quaternion b)
        {
            return a.x != b.x || a.y != b.y || a.z != b.z || a.w != b.w;
        }

        public static bool operator ==(Quaternion a, SaveableQuaternion b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        public static bool operator !=(Quaternion a, SaveableQuaternion b)
        {
            return a.x != b.x || a.y != b.y || a.z != b.z || a.w != b.w;
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

    public struct AtomList
    {
        private List<Guid> ids;
        private List<Atom> atoms;
        private List<bool> in_scene;

        public void Add(Atom a)
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            atoms.Add(a);
            in_scene.Add(true);
        }

        public void Add(Atom a, Guid id)
        {
            ids.Add(id);
            atoms.Add(a);
            in_scene.Add(true);
        }

        public Atom GetAtom(Guid id)
        {
            var list_id = ids.IndexOf(id);
            if (in_scene[list_id])
            {
                return atoms[list_id];
            }
            else
            {
                return null;
            }
        }

        public List<Atom> GetAtoms()
        {
            var local_inscene = in_scene;
            return atoms.Where((atom, index) => local_inscene[index]).ToList();
        }
    

    }
}