using UnityEngine;
using Unity.Mathematics;
using NUnit.Framework.Internal;
using NUnit.Framework;

public class MeshLine
{

    //public static void GetStartAndEndPoints(Transform trans, out Vector3 startPoint, out Vector3 endPoint)
    //{


    //}

    public static Vector3[] ExtractLineEndpointsWithDegeneracy(Transform trans)
    {
        // Get the MeshFilter component
        var meshFilter = trans.GetComponent<MeshFilter>();

        // Get the mesh vertices in local space
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        // Convert vertices to float3 array for math compatibility
        float3[] floatVertices = new float3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            floatVertices[i] = (float3)vertices[i];
        }

        // Step 1: Calculate covariance matrix
        float3x3 covarianceMatrix = EigenDecomposition.CalculateCovarianceMatrix(floatVertices);

        // Step 2: Perform eigenvalue decomposition to find eigenvectors and eigenvalues
        float3 eigenvalues = EigenDecomposition.ComputeEigenvalues(covarianceMatrix);
        float3x3 eigenvectors = EigenDecomposition.GetEigenvectors(covarianceMatrix, eigenvalues);

        // Project onto the principal axis
        float3 primaryAxis = eigenvectors.c0;
        float3 secondaryAxis = eigenvectors.c1;

        // If the two eigenvectors are nearly identical, treat it as 2D in the plane
        if (EigenDecomposition.AreEigenvectorsSimilar(eigenvectors.c0, eigenvectors.c1))
        {
            // Treat the mesh as planar
            Debug.Log("Eigenvectors are similar, treating mesh as 2D in the plane of similar axes.");

            // Project onto the dominant axis and calculate min/max along that axis
            return EigenDecomposition.ProjectOntoDominantAxis(floatVertices, primaryAxis);
        }
        else
        {
            // Project onto both axes and calculate min/max in 3D space
            return EigenDecomposition.ProjectOntoPrincipalAxes(floatVertices, eigenvectors);
        }
    }

    public static Vector3[] ExtractPrincipalAxisEndpoints(Transform trans)
    {
        // Get the MeshFilter component
        var meshFilter = trans.GetComponent<MeshFilter>();

        // Get the mesh vertices in local space
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        // Convert vertices to float3 array for math compatibility
        float3[] floatVertices = new float3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            floatVertices[i] = (float3)vertices[i];
        }

        // Step 1: Calculate covariance matrix
        float3x3 covarianceMatrix = EigenDecomposition.CalculateCovarianceMatrix(floatVertices);

        // Step 2: Perform eigenvalue decomposition to find eigenvectors and eigenvalues
        float3 eigenvalues = EigenDecomposition.ComputeEigenvalues(covarianceMatrix);
        float3x3 eigenvectors = EigenDecomposition.GetEigenvectors(covarianceMatrix, eigenvalues);

        // Step 3: Sort eigenvectors by eigenvalues (largest eigenvalue first)
        EigenDecomposition.SortAndClampEigenVectors(ref eigenvalues, ref eigenvectors);

        Debug.Log($"[MeshLine] Eigenvectors {eigenvectors.c0} {eigenvectors.c1} {eigenvectors.c2}");

        // Step 4: Project vertices onto each principal axis and find min/max projections
        return EigenDecomposition.ProjectOntoPrincipalAxes(floatVertices, eigenvectors);
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
        float3x3 covarianceMatrix = EigenDecomposition.CalculateCovarianceMatrix(floatVertices);

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

}
