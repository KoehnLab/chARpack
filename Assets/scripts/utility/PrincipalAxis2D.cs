using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PrincipalAxis2D
{
    public MeshFilter meshFilter;

    public static (Tuple<float, float>, Tuple<Vector2, Vector2>) ComputPrincipalAxis(Transform trans)
    {
        var meshFilter = trans.GetComponent<MeshFilter>();
        return ComputPrincipalAxis(meshFilter);
    }

    public static (Tuple<float,float>,Tuple<Vector2, Vector2>) ComputPrincipalAxis(MeshFilter meshFilter)
    {
        // Assuming the mesh is 2D in the xy-plane, we extract the vertices.
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles; // Mesh triangles

        // Compute the weighted centroid of the mesh based on triangle areas
        var centroid = ComputeAreaWeightedCentroid(vertices, triangles, meshFilter.transform);

        // Convert vertices to 2D points (ignoring the z-coordinate) in world space
        List<Vector2> points2D = new List<Vector2>();
        foreach (Vector3 vertex in vertices)
        {
            Vector3 worldVertex = meshFilter.transform.TransformPoint(vertex);
            points2D.Add(new Vector2(worldVertex.x, worldVertex.y));
        }

        // Translate points to the origin (centroid becomes the origin)
        List<Vector2> centeredPoints = new List<Vector2>();
        foreach (Vector2 point in points2D)
        {
            centeredPoints.Add(point - centroid);
        }

        // Compute the covariance matrix
        Matrix2x2 covarianceMatrix = ComputeCovarianceMatrix(centeredPoints);

        // Compute the eigenvalues and eigenvectors (principal components)
        Vector2 eigenvector1, eigenvector2;
        float eigenvalue1, eigenvalue2;
        ComputeEigenDecomposition(covarianceMatrix, out eigenvalue1, out eigenvalue2, out eigenvector1, out eigenvector2);

        // Output the results
        Debug.Log($"Principal Axis 1: {eigenvector1}, Eigenvalue: {eigenvalue1}");
        Debug.Log($"Principal Axis 2: {eigenvector2}, Eigenvalue: {eigenvalue2}");

        return (new Tuple<float,float>(eigenvalue1,eigenvalue2),new Tuple<Vector2,Vector2>(eigenvector1, eigenvector2));
    }

    public static void GetEigenCenterEndpoints(Transform trans, out float eigenvalue1, out float eigenvalue2, out Vector2 eigenvector1, out Vector2 eigenvector2, out Vector2 centroid, out Vector2 startPoint, out Vector2 endPoint)
    {
        var meshFilter = trans.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles; // Mesh triangles

        // Compute the weighted centroid of the mesh based on triangle areas
        centroid = ComputeAreaWeightedCentroid(vertices, triangles, trans);

        // Convert vertices to 2D points (ignoring the z-coordinate) in world space
        List<Vector2> points2D = new List<Vector2>();
        foreach (Vector3 vertex in vertices)
        {
            Vector3 worldVertex = meshFilter.transform.TransformPoint(vertex);
            points2D.Add(new Vector2(worldVertex.x, worldVertex.y));
        }

        // Translate points to the origin (centroid becomes the origin)
        List<Vector2> centeredPoints = new List<Vector2>();
        foreach (Vector2 point in points2D)
        {
            centeredPoints.Add(point - centroid);
        }

        // Compute the covariance matrix
        Matrix2x2 covarianceMatrix = ComputeCovarianceMatrix(centeredPoints);

        // Compute the eigenvalues and eigenvectors (principal components)
        ComputeEigenDecomposition(covarianceMatrix, out eigenvalue1, out eigenvalue2, out eigenvector1, out eigenvector2);

        // Calculate the start and endpoint of the line segment using the mesh's projection onto the principal axis
        CalculateLineEndpointsFromProjections(points2D, centroid, eigenvector1, out startPoint, out endPoint);
    }

    public static void GetEigenValuesAndEigenVectors(Transform trans, out float eigenvalue1, out float eigenvalue2, out Vector2 eigenvector1, out Vector2 eigenvector2, out Vector2 centroid)
    {
        var meshFilter = trans.GetComponent<MeshFilter>();

        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles; // Mesh triangles

        // Compute the weighted centroid of the mesh based on triangle areas
        centroid = ComputeAreaWeightedCentroid(vertices, triangles, meshFilter.transform);

        // Convert vertices to 2D points (ignoring the z-coordinate) in world space
        List<Vector2> points2D = new List<Vector2>();
        foreach (Vector3 vertex in vertices)
        {
            Vector3 worldVertex = meshFilter.transform.TransformPoint(vertex);
            points2D.Add(new Vector2(worldVertex.x, worldVertex.y));
        }

        // Translate points to the origin (centroid becomes the origin)
        List<Vector2> centeredPoints = new List<Vector2>();
        foreach (Vector2 point in points2D)
        {
            centeredPoints.Add(point - centroid);
        }

        // Compute the covariance matrix
        Matrix2x2 covarianceMatrix = ComputeCovarianceMatrix(centeredPoints);

        // Compute the eigenvalues and eigenvectors (principal components)
        ComputeEigenDecomposition(covarianceMatrix, out eigenvalue1, out eigenvalue2, out eigenvector1, out eigenvector2);
    }

    // Function to determine if the mesh can be approximated as a line
    public static bool IsLine(float eigenvalue1, float eigenvalue2, float threshold)
    {
        float ratio = Mathf.Max(eigenvalue1, eigenvalue2) / Mathf.Min(eigenvalue1, eigenvalue2);
        return ratio > threshold;
    }

    // Function to calculate the start and endpoints of the line segment
    public static void CalculateLineEndpoints(Vector2 centroid, Vector2 direction, float eigenvalue1, float lengthFactor, out Vector2 startPoint, out Vector2 endPoint)
    {
        // Calculate the length based on the principal axis eigenvalue
        float length = lengthFactor * Mathf.Sqrt(eigenvalue1); // Adjust the scaling factor as needed

        // Start and end points based on the direction vector
        startPoint = centroid - direction * (length / 2);
        endPoint = centroid + direction * (length / 2);
    }

    // Function to calculate the start and endpoints of the line segment from projections
    public static void CalculateLineEndpointsFromProjections(List<Vector2> points, Vector2 centroid, Vector2 direction, out Vector2 startPoint, out Vector2 endPoint)
    {
        float minProjection = float.MaxValue;
        float maxProjection = float.MinValue;

        // Project all points onto the direction of the principal axis
        foreach (Vector2 point in points)
        {
            float projection = Vector2.Dot(point - centroid, direction);

            // Update min and max projection
            if (projection < minProjection)
                minProjection = projection;
            if (projection > maxProjection)
                maxProjection = projection;
        }

        // Calculate start and end points along the principal axis
        startPoint = centroid + direction * minProjection;
        endPoint = centroid + direction * maxProjection;
    }

    // Function to compute the centroid of the mesh points
    static Vector2 ComputeCentroid(List<Vector2> points)
    {
        //Vector2 sum = Vector2.zero;
        //foreach (Vector2 point in points)
        //{
        //    sum += point;
        //}
        //return sum / points.Count;
        float min_x = float.MaxValue;
        float max_x = float.MinValue;
        float min_y = float.MaxValue;
        float max_y = float.MinValue;
        foreach (Vector2 point in points)
        {
            min_x = math.min(min_x, point.x);
            max_x = math.max(max_y, point.x);
            min_y = math.min(min_y, point.y);
            max_y = math.max(max_y, point.y);
        }
        var center = new Vector2(min_x + 0.5f * (max_x - min_x), min_y + 0.5f * (max_y - min_y));
        return center;
    }

    // Function to compute the centroid of the mesh weighted by triangle areas
    public static Vector2 ComputeAreaWeightedCentroid(Vector3[] vertices, int[] triangles, Transform meshTransform = null)
    {
        Vector2 weightedCentroid = Vector2.zero;
        float totalArea = 0f;

        for (int i = 0; i < triangles.Length; i += 3)
        {

            Vector3 v0, v1, v2;
            // Get triangle vertices in world space
            if (meshTransform == null)
            {
                v0 = vertices[triangles[i]];
                v1 = vertices[triangles[i + 1]];
                v2 = vertices[triangles[i + 2]];
            }
            else
            {
                v0 = meshTransform.TransformPoint(vertices[triangles[i]]);
                v1 = meshTransform.TransformPoint(vertices[triangles[i + 1]]);
                v2 = meshTransform.TransformPoint(vertices[triangles[i + 2]]);
            }

            // Calculate the area of the triangle (cross product magnitude in 2D)
            float area = Mathf.Abs((v0.x * (v1.y - v2.y) + v1.x * (v2.y - v0.y) + v2.x * (v0.y - v1.y)) / 2f);

            // Calculate the centroid of the triangle
            Vector2 triangleCentroid = new Vector2((v0.x + v1.x + v2.x) / 3f, (v0.y + v1.y + v2.y) / 3f);

            // Weight the centroid by the area
            weightedCentroid += area * triangleCentroid;
            totalArea += area;
        }

        // Divide by the total area to get the final centroid
        return weightedCentroid / totalArea;
    }

    // Function to compute the covariance matrix
    static Matrix2x2 ComputeCovarianceMatrix(List<Vector2> points)
    {
        float sumXX = 0, sumXY = 0, sumYY = 0;
        foreach (Vector2 point in points)
        {
            sumXX += point.x * point.x;
            sumXY += point.x * point.y;
            sumYY += point.y * point.y;
        }

        float n = points.Count;
        return new Matrix2x2(sumXX / n, sumXY / n, sumXY / n, sumYY / n);
    }

    // Function to compute the eigenvalue decomposition of the covariance matrix
    static void ComputeEigenDecomposition(Matrix2x2 matrix, out float eigenvalue1, out float eigenvalue2, out Vector2 eigenvector1, out Vector2 eigenvector2)
    {
        // Compute the trace and determinant of the 2x2 matrix
        float trace = matrix.m00 + matrix.m11;
        float determinant = matrix.m00 * matrix.m11 - matrix.m01 * matrix.m10;

        // Compute the eigenvalues
        float discriminant = Mathf.Sqrt(trace * trace - 4 * determinant);
        eigenvalue1 = (trace + discriminant) / 2;
        eigenvalue2 = (trace - discriminant) / 2;

        // Compute the eigenvectors (for simplicity, assuming the matrix is symmetric)
        eigenvector1 = new Vector2(matrix.m01, eigenvalue1 - matrix.m00).normalized;
        eigenvector2 = new Vector2(matrix.m01, eigenvalue2 - matrix.m00).normalized;
    }


}

public struct Matrix2x2
{
    public float m00, m01, m10, m11;

    public Matrix2x2(float m00, float m01, float m10, float m11)
    {
        this.m00 = m00;
        this.m01 = m01;
        this.m10 = m10;
        this.m11 = m11;
    }
}