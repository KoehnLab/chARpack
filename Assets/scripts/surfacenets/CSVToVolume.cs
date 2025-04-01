using RuntimeGizmos;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class CSVToVolume
{
    public string filePath; // Assign this in the Unity Inspector


    public static ScalarVolume LoadCSV(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return new ScalarVolume();
        }

        List<Vector3> positions = new List<Vector3>();
        List<float> values = new List<float>();
        float spacingX, spacingY, spacingZ;

        string[] lines = File.ReadAllLines(path);
        bool isHeader = true;
        HashSet<float> xPositions = new HashSet<float>();
        HashSet<float> yPositions = new HashSet<float>();
        HashSet<float> zPositions = new HashSet<float>();
        var bounds_min = Vector3.one * float.MaxValue;
        var bounds_max = Vector3.one * float.MinValue;
        foreach (string line in lines)
        {
            if (isHeader) // Skip header line
            {
                isHeader = false;
                continue;
            }

            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6) continue;

            float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
            float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float z = float.Parse(parts[2], CultureInfo.InvariantCulture);
            float sx = float.Parse(parts[3], CultureInfo.InvariantCulture);
            float sy = float.Parse(parts[4], CultureInfo.InvariantCulture);
            float sz = float.Parse(parts[5], CultureInfo.InvariantCulture);

            bounds_min.x = Mathf.Min(bounds_min.x, x);
            bounds_min.y = Mathf.Min(bounds_min.y, y);
            bounds_min.z = Mathf.Min(bounds_min.z, z);
            bounds_max.x = Mathf.Min(bounds_max.x, x);
            bounds_max.y = Mathf.Min(bounds_max.y, y);
            bounds_max.z = Mathf.Min(bounds_max.z, z);
            positions.Add(new Vector3(x, y, z));
            values.Add(Vector3.Magnitude(new Vector3(sx,sy,sz)));
            xPositions.Add(x);
            yPositions.Add(y);
            zPositions.Add(z);
        }

        Debug.Log($"[CSVToVolume] {xPositions.Count} lines loaded.");

        spacingX = xPositions.Count > 1 ? (xPositions.ElementAt(1) - xPositions.ElementAt(0)) : 1.0f;
        spacingY = yPositions.Count > 1 ? (yPositions.ElementAt(1) - yPositions.ElementAt(0)) : 1.0f;
        spacingZ = zPositions.Count > 1 ? (zPositions.ElementAt(1) - zPositions.ElementAt(0)) : 1.0f;

        int dimX = Mathf.RoundToInt((xPositions.Last() - xPositions.First()) / spacingX) + 1;
        int dimY = Mathf.RoundToInt((yPositions.Last() - yPositions.First()) / spacingY) + 1;
        int dimZ = Mathf.RoundToInt((zPositions.Last() - zPositions.First()) / spacingZ) + 1;

        Debug.Log($"Volume Dimensions: {dimX} x {dimY} x {dimZ}");
        Debug.Log("Voxel Count: " + positions.Count);
        Debug.Log($"Spacing: {spacingX}, {spacingY}, {spacingZ}");
        Debug.Log($"Box: ({bounds_min.x}, {bounds_min.y}, {bounds_min.z}); ({bounds_max.x}, {bounds_max.y}, {bounds_max.z})");

        //var resorted_values = resort3DList(values, dimX, dimY, dimZ, "YXZ");

        var vol = new ScalarVolume();
        vol.dim = new Vector3Int(dimX, dimY, dimZ);
        vol.values = values;
        vol.spacing = new Vector3(spacingX, spacingY, spacingZ);
        vol.bounds_min = bounds_min;
        vol.bounds_max = bounds_max;

        return vol;
    }

    static List<float> resort3DList(List<float> data, int dimX, int dimY, int dimZ, string order)
    {
        if (order.Length != 3 || !order.Contains("X") || !order.Contains("Y") || !order.Contains("Z"))
            throw new ArgumentException("Order must be a permutation of 'XYZ'");

        List<float> sortedData = new List<float>(new float[data.Count]);

        for (int x = 0; x < dimX; x++)
        {
            for (int y = 0; y < dimY; y++)
            {
                for (int z = 0; z < dimZ; z++)
                {
                    int oldIndex = x + dimX * (y + dimY * z);
                    int newX = 0, newY = 0, newZ = 0;

                    switch (order.IndexOf('X')) { case 0: newX = x; break; case 1: newY = x; break; case 2: newZ = x; break; }
                    switch (order.IndexOf('Y')) { case 0: newX = y; break; case 1: newY = y; break; case 2: newZ = y; break; }
                    switch (order.IndexOf('Z')) { case 0: newX = z; break; case 1: newY = z; break; case 2: newZ = z; break; }

                    int newIndex = newX + dimX * (newY + dimY * newZ);
                    sortedData[newIndex] = data[oldIndex];
                }
            }
        }

        return sortedData;
    }


    public static Texture3D GenerateTexture3D(List<Vector3> positions, ScalarVolume vol)
    {
        var volumeTexture = new Texture3D(vol.dim.x, vol.dim.y, vol.dim.z, TextureFormat.RFloat, false);
        Color[] colorArray = new Color[vol.dim.x * vol.dim.y * vol.dim.z];

        for (int i = 0; i < positions.Count; i++)
        {
            int x = Mathf.RoundToInt((positions[i].x - positions[0].x) / vol.spacing.x);
            int y = Mathf.RoundToInt((positions[i].y - positions[0].y) / vol.spacing.y);
            int z = Mathf.RoundToInt((positions[i].z - positions[0].z) / vol.spacing.z);

            int index = x + vol.dim.x * (y + vol.dim.y * z);
            colorArray[index] = new Color(vol.values[i], 0f, 0f);
        }

        volumeTexture.SetPixels(colorArray);
        volumeTexture.Apply();

        Debug.Log("3D Texture generated successfully");
        return volumeTexture;
    }
}
