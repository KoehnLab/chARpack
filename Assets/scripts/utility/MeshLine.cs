using UnityEngine;
using Unity.Mathematics;

public class MeshLine
{



    public static void GetStartAndEndPoints(Transform trans, out Vector3 startPoint, out Vector3 endPoint)
    {
        // Get the MeshFilter component
        var meshFilter = trans.GetComponent<MeshFilter>();

        // Get the mesh and its bounds
        Mesh mesh = meshFilter.sharedMesh;
        Bounds bounds = mesh.bounds;

        // Assuming the line stretches along the Z-axis in local space
        // Start point is at the minimum Z of the bounds
        // End point is at the maximum Z of the bounds

        Vector3 localStartPoint = new Vector3(0, 0, bounds.min.z);
        Vector3 localEndPoint = new Vector3(0, 0, bounds.max.z);

        // Transform the points from local space to world space
        startPoint = trans.TransformPoint(localStartPoint);
        endPoint = trans.TransformPoint(localEndPoint);
    }


    public static void SetStartAndEndPoint(Transform trans, Vector3 startPoint, Vector3 endPoint)
    {
        // Step 1: Calculate midpoint and position the mesh
        Vector3 midpoint = (startPoint + endPoint) / 2;
        trans.position = midpoint;

        // Step 2: Scale the mesh along the z-axis based on the distance
        float distance = Vector3.Distance(startPoint, endPoint);
        trans.localScale = new Vector3(trans.localScale.x, trans.localScale.y, distance);

        // Step 3: Rotate the mesh to align with the direction between start and end points
        Vector3 direction = (endPoint - startPoint).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);
        trans.rotation = rotation;
    }


    public static bool IsMeshALine(MeshFilter meshFilter, float lengthRatioThreshold = 5.0f, bool flat = true)
    {
        // Get the mesh vertices in local space
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        // Convert vertices to float3 array for math compatibility
        float3[] floatVertices = new float3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            floatVertices[i] = (float3)vertices[i];
        }

        // Calculate the covariance matrix for the vertices
        float3x3 covarianceMatrix = CalculateCovarianceMatrix(floatVertices);

        // Perform eigenvalue decomposition to find eigenvalues
        float3 eigenvalues = EigenDecomposition.ComputeEigenvalues(covarianceMatrix);

        // Sort eigenvalues to identify the largest and smallest
        float[] sortedEigenvalues = { eigenvalues.x, eigenvalues.y, eigenvalues.z };
        System.Array.Sort(sortedEigenvalues);

        // Get the largest and smallest eigenvalues
        float largestEigenvalue = sortedEigenvalues[2];
        float smallestEigenvalue = flat ? sortedEigenvalues[1] : sortedEigenvalues[0]; // if flat the last entry is always the thickness

        // Check if the largest eigenvalue is much larger than the others
        if (largestEigenvalue / smallestEigenvalue >= lengthRatioThreshold)
        {
            return true;  // The mesh is likely a line
        }

        return false;  // The mesh is not a line
    }

    static float3x3 CalculateCovarianceMatrix(float3[] vertices)
    {
        // Calculate the mean (centroid) of the vertices
        float3 mean = float3.zero;
        foreach (float3 vertex in vertices)
        {
            mean += vertex;
        }
        mean /= vertices.Length;

        // Initialize covariance matrix
        float3x3 covariance = new float3x3();

        // Calculate covariance matrix
        foreach (float3 vertex in vertices)
        {
            float3 v = vertex - mean;
            covariance.c0.x += v.x * v.x;
            covariance.c0.y += v.x * v.y;
            covariance.c0.z += v.x * v.z;

            covariance.c1.x += v.y * v.x;
            covariance.c1.y += v.y * v.y;
            covariance.c1.z += v.y * v.z;

            covariance.c2.x += v.z * v.x;
            covariance.c2.y += v.z * v.y;
            covariance.c2.z += v.z * v.z;
        }

        return covariance;
    }

}
