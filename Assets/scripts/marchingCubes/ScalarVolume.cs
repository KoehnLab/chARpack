using System.Collections.Generic;
using UnityEngine;

public struct ScalarVolume
{
    public List<float> values;
    public Vector3 spacing;
    public Vector3Int dim;
    public Vector3 bounds_min;
    public Vector3 bounds_max;
}