using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using chARpack.Structs;

namespace chARpack
{
    public static class chARpackExtensions
    {
        public static string AsCommaSeparatedString<T>(this T[] list)
        {
            var output = "";
            for (int i = 0; i < list.Length; i++)
            {
                if (i == list.Length - 1)
                {
                    output += $"{list[i]}";
                }
                else
                {
                    output += $"{list[i]},";
                }
            }
            return output;
        }


        public static bool Contains(this ForceField.BondTerm term, ushort id)
        {
            return (term.Atom1 == id || term.Atom2 == id);
        }

        public static bool Contains(this ForceField.BondTerm term, ushort id1, ushort id2)
        {
            return (term.Atom1 == id1 && term.Atom2 == id2 || term.Atom1 == id2 && term.Atom2 == id1);
        }

        public static bool Contains(this ForceField.AngleTerm term, ushort id)
        {
            return (term.Atom1 == id || term.Atom2 == id || term.Atom3 == id);
        }

        public static bool Contains(this ForceField.TorsionTerm term, ushort id)
        {
            return (term.Atom1 == id || term.Atom2 == id || term.Atom3 == id || term.Atom4 == id);
        }

        public static bool approx(this float f1, float f2, float precision = 0.0001f)
        {
            return (Mathf.Abs(f1 - f2) <= precision);
        }

        public static bool approx(this double d1, double d2, double precision = 0.0001)
        {
            return (System.Math.Abs(d1 - d2) <= precision);
        }

        public static Vector3 multiply(this Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z);
        }

        public static Vector3 divide(this Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z);
        }

        public static Vector3 abs(this Vector3 lhs)
        {
            return new Vector3(Mathf.Abs(lhs.x), Mathf.Abs(lhs.y), Mathf.Abs(lhs.z));
        }

        public static Vector3 sqrt(this Vector3 lhs)
        {
            return new Vector3(Mathf.Sqrt(lhs.x), Mathf.Sqrt(lhs.y), Mathf.Sqrt(lhs.z));
        }

        public static Vector3 pow(this Vector3 lhs, float exponent)
        {
            return new Vector3(Mathf.Pow(lhs.x, exponent), Mathf.Pow(lhs.y, exponent), Mathf.Pow(lhs.z, exponent));
        }

        public static Vector3 max(this Vector3 lhs, float other)
        {
            return new Vector3(Mathf.Max(lhs.x, other), Mathf.Max(lhs.y, other), Mathf.Max(lhs.z, other));
        }

        public static Vector3 min(this Vector3 lhs, float other)
        {
            return new Vector3(Mathf.Min(lhs.x, other), Mathf.Min(lhs.y, other), Mathf.Min(lhs.z, other));
        }

        public static Vector3 limit(this Vector3 lhs, float other)
        {
            return new Vector3((lhs.x) * Mathf.Min(lhs.x, other), Mathf.Min(lhs.y, other), Mathf.Min(lhs.z, other));
        }

        public static float maxDimValue(this Vector3 lhs)
        {
            return Mathf.Max(lhs.x, Mathf.Max(lhs.y, lhs.z));
        }

        public static int maxElementIndex(this List<float> lhs)
        {
            return lhs.IndexOf(lhs.Max());
        }

        public static int minElementIndex(this List<float> lhs)
        {
            return lhs.IndexOf(lhs.Min());
        }

        public static Camera getWrapElement(this List<Camera> lhs, int index)
        {
            if (index < 0)
            {
                return lhs.Last();
            }
            var n = lhs.Count;
            return lhs[((index % n) + n) % n];
        }

        public static T ElementAtOrNull<T>(this IList<T> list, int index, T @default)
        {
            return index >= 0 && index < list.Count ? list[index] : @default;
        }

        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
                                                      //BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            BindingFlags flags = BindingFlags.Public | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }

        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            return go.AddComponent<T>().GetCopyOf(toAdd) as T;
        }

        public static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        public static string printComponents(this GameObject go)
        {
            Component[] components = go.GetComponents(typeof(Component));
            string list = "";
            foreach (var component in components)
            {
                list += component.ToString() + ", ";
            }
            return list;
        }

        public static float CalculateCosineSimilarity(Vector3 vecA, Vector3 vecB)
        {
            var dotProduct = Vector3.Dot(vecA, vecB);
            var magnitudeOfA = vecA.magnitude;
            var magnitudeOfB = vecB.magnitude;

            return dotProduct / (magnitudeOfA * magnitudeOfB);
        }

        public static bool AnyTrue(this bool[] list)
        {
            foreach (var entry in list)
            {
                if (entry) return true;
            }
            return false;
        }

        public static bool AllZero(this float[] list)
        {
            foreach (var entry in list)
            {
                if (entry != 0f) return false;
            }
            return true;
        }

        public static void RemoveValue<TKey>(this Dictionary<TKey, Molecule> dictionary,
        Molecule mol, params object[] argList)
        {
            dictionary.Remove(dictionary.FirstOrDefault(x => x.Value == mol).Key);
        }

        public static Molecule ElementAtOrNull<TKey>(this Dictionary<TKey, Molecule> dictionary,
    TKey mol_id, params object[] argList)
        {
            if (!dictionary.ContainsKey(mol_id)) return null;
            return dictionary[mol_id];
        }

        public static Molecule GetFirst<TKey>(this Dictionary<TKey, Molecule> dictionary, params object[] argList)
        {
            var keys = dictionary.Keys.ToList();
            return dictionary[keys.First()];
        }

        public static string Print<T>(this T[] list)
        {
            string print = "";
            foreach (var entry in list)
            {
                print += $"{entry} ";
            }
            return print;
        }

        public static string Print<T, S>(this Dictionary<T, S> dict)
        {
            string print = "";
            foreach (var entry in dict)
            {
                print += $"{entry.Key}:{entry.Value} ";
            }
            return print;
        }

        public static SaveableQuaternion ToSaveableQuaternion(this Quaternion q)
        {
            return new SaveableQuaternion(q.x, q.y, q.z, q.w);
        }

        public static SaveableVector2[] ToSaveableVector2Array(this List<Vector2> v2l)
        {
            SaveableVector2[] sva = new SaveableVector2[v2l.Count];
            for (int i = 0; i < v2l.Count; i++)
            {
                sva[i] = v2l[i];
            }
            return sva;
        }

        public static List<SaveableVector2> ToSaveableVector2List(this List<Vector2> v2l)
        {
            var sva = new List<SaveableVector2>();
            foreach (var v2 in v2l)
            {
                sva.Add(v2);
            }
            return sva;
        }

        public static Vector3[] GetCorners(this Bounds b)
        {
            Vector3[] boundPoints = new Vector3[8];
            boundPoints[0] = b.min;
            boundPoints[1] = b.max;
            boundPoints[2] = new Vector3(boundPoints[0].x, boundPoints[0].y, boundPoints[1].z);
            boundPoints[3] = new Vector3(boundPoints[0].x, boundPoints[1].y, boundPoints[0].z);
            boundPoints[4] = new Vector3(boundPoints[1].x, boundPoints[0].y, boundPoints[0].z);
            boundPoints[5] = new Vector3(boundPoints[0].x, boundPoints[1].y, boundPoints[1].z);
            boundPoints[6] = new Vector3(boundPoints[1].x, boundPoints[0].y, boundPoints[1].z);
            boundPoints[7] = new Vector3(boundPoints[1].x, boundPoints[1].y, boundPoints[0].z);
            return boundPoints;
        }

        public static sGenericObject AsSerializable(this GenericObject go)
        {
            return new sGenericObject(go.name, go.id, go.transform.localPosition, go.transform.localScale, go.transform.localRotation);
        }


        /// <summary>
        /// Shuffles the element order of the specified list.
        /// </summary>
        public static void Shuffle<T>(this List<T> ts)
        {
            var count = ts.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self) => self.Select((item, index) => (item, index));


        public static List<int> GetAllIndicesOf<T>(this List<T> self, T item)
        {
            var return_list = new List<int>();
            foreach (var (entry, i) in self.WithIndex())
            {
                if (entry.Equals(item))
                {
                    return_list.Add(i);
                }
            }
            return return_list;
        }

        public static bool ContainedInChildren(this List<GameObject> self, GameObject obj)
        {
            bool found_match = false;
            foreach (var go in self)
            {
                foreach (Transform child in go.transform)
                {
                    if (child.gameObject == obj)
                    {
                        found_match = true;
                        return found_match;
                    }
                }
            }
            return found_match;
        }

        public static bool ContainedInChildren(this List<Transform> self, Transform obj)
        {
            bool found_match = false;
            foreach (var go in self)
            {
                foreach (Transform child in go)
                {
                    if (child == obj)
                    {
                        found_match = true;
                        return found_match;
                    }
                }
            }
            return found_match;
        }
    }

    public static class TaskExtensions
    {
        public static IEnumerator AsCoroutine(this Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }
    }

    public static class CoroutineWrapper
    {
        public static IEnumerator WrapAction(Action action)
        {
            action();
            yield return null;
        }
    }
}
