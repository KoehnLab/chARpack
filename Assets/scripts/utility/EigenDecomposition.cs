using Unity.Mathematics;
using UnityEngine;
public class EigenDecomposition
{
    // Compute the eigenvalues of a 3x3 symmetric matrix
    public static float3 ComputeEigenvalues(float3x3 A)
    {
        // Coefficients for the characteristic equation det(A - lambda*I) = 0 (cubic equation)
        float p1 = A.c0.y * A.c0.y + A.c0.z * A.c0.z + A.c1.z * A.c1.z;

        if (p1 == 0.0f)
        {
            // A is diagonal, the eigenvalues are the diagonal elements
            return new float3(A.c0.x, A.c1.y, A.c2.z);
        }
        else
        {
            float q = Trace(A) / 3.0f;
            float p2 = (A.c0.x - q) * (A.c0.x - q) + (A.c1.y - q) * (A.c1.y - q) + (A.c2.z - q) * (A.c2.z - q) + 2.0f * p1;
            float p = math.sqrt(p2 / 6.0f);

            // B = (1 / p) * (A - q * I)
            float3x3 I = float3x3.identity;
            float3x3 B = (1.0f / p) * (A - q * I);

            float r = Determinant(B) / 2.0f;

            // Avoid floating-point precision errors
            r = math.clamp(r, -1.0f, 1.0f);

            // Compute the angles
            float phi = math.acos(r) / 3.0f;

            // Eigenvalues
            float eig1 = q + 2.0f * p * math.cos(phi);
            float eig2 = q + 2.0f * p * math.cos(phi + 2.0f * math.PI / 3.0f);
            float eig3 = 3.0f * q - eig1 - eig2; // Since sum of eigenvalues equals the trace

            return new float3(eig1, eig2, eig3);
        }
    }

    // Helper function to compute the trace of a 3x3 matrix (sum of diagonal elements)
    public static float Trace(float3x3 A)
    {
        return A.c0.x + A.c1.y + A.c2.z;
    }

    // Helper function to compute the determinant of a 3x3 matrix
    public static float Determinant(float3x3 A)
    {
        return A.c0.x * (A.c1.y * A.c2.z - A.c1.z * A.c2.y)
             - A.c0.y * (A.c1.x * A.c2.z - A.c1.z * A.c2.x)
             + A.c0.z * (A.c1.x * A.c2.y - A.c1.y * A.c2.x);
    }

    // Helper function to normalize a vector
    public static float3 Normalize(float3 v)
    {
        float length = math.length(v);
        if (length > math.EPSILON)
            return v / length;
        return float3.zero;
    }


    public static float3x3 CalculateCovarianceMatrix(float3[] vertices)
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

    // Function to compute the eigenvectors of a 3x3 matrix given its eigenvalues
    public static float3x3 GetEigenvectors(float3x3 A, float3 eigenvalues)
    {
        float3x3 eigenvectors = new float3x3();

        // Eigenvector corresponding to the largest eigenvalue
        eigenvectors.c0 = ComputeEigenvector(A, eigenvalues.x);
        eigenvectors.c0 = Normalize(eigenvectors.c0);  // Normalize the eigenvector

        // Eigenvector corresponding to the second largest eigenvalue
        eigenvectors.c1 = ComputeEigenvector(A, eigenvalues.y);
        eigenvectors.c1 = Normalize(eigenvectors.c1);  // Normalize the eigenvector

        // Eigenvector corresponding to the smallest eigenvalue
        eigenvectors.c2 = ComputeEigenvector(A, eigenvalues.z);
        eigenvectors.c2 = Normalize(eigenvectors.c2);  // Normalize the eigenvector

        return eigenvectors;
    }

    // Helper function to compute the eigenvector for a given eigenvalue
    public static float3 ComputeEigenvector(float3x3 A, float eigenvalue)
    {
        // Solve (A - lambda*I)v = 0 for v (eigenvector)
        float3x3 shiftedMatrix = A - eigenvalue * float3x3.identity;

        // We use a simple cross-product based solution to find the eigenvector
        float3 row0 = new float3(shiftedMatrix.c0.x, shiftedMatrix.c0.y, shiftedMatrix.c0.z);
        float3 row1 = new float3(shiftedMatrix.c1.x, shiftedMatrix.c1.y, shiftedMatrix.c1.z);
        float3 row2 = new float3(shiftedMatrix.c2.x, shiftedMatrix.c2.y, shiftedMatrix.c2.z);

        // Cross products of any two non-collinear rows give the eigenvector
        float3 v = math.cross(row0, row1);
        if (math.length(v) < math.EPSILON)
        {
            v = math.cross(row0, row2);
        }
        if (math.length(v) < math.EPSILON)
        {
            v = math.cross(row1, row2);
        }

        return v;
    }

    public static void SortAndClampEigenVectors(ref float3 eigenvalues, ref float3x3 eigenvectors, float threshold = 1e-4f)
    {
        // Create array of eigenvalue, eigenvector pairs
        (float value, float3 vector)[] eigenPairs = new (float, float3)[3];
        eigenPairs[0] = (eigenvalues.x, eigenvectors.c0);
        eigenPairs[1] = (eigenvalues.y, eigenvectors.c1);
        eigenPairs[2] = (eigenvalues.z, eigenvectors.c2);

        // Sort by eigenvalues in descending order
        System.Array.Sort(eigenPairs, (a, b) => b.value.CompareTo(a.value));

        // Clamp small eigenvalues (near zero) to avoid instability
        for (int i = 0; i < eigenPairs.Length; i++)
        {
            if (math.abs(eigenPairs[i].value) < threshold)
            {
                eigenPairs[i].value = 0; // Treat it as zero variance
            }
        }

        // Assign the sorted and clamped values back
        eigenvalues = new float3(eigenPairs[0].value, eigenPairs[1].value, eigenPairs[2].value);
        eigenvectors.c0 = eigenPairs[0].vector;
        eigenvectors.c1 = eigenPairs[1].vector;
        eigenvectors.c2 = eigenPairs[2].vector;
    }

    public static bool AreEigenvectorsSimilar(float3 v1, float3 v2, float threshold = 0.99f)
    {
        // Calculate the dot product between the two vectors
        float dotProduct = math.dot(math.normalize(v1), math.normalize(v2));

        // Check if the absolute value of the dot product is close to 1 (indicating similarity)
        return math.abs(dotProduct) > threshold;
    }

    public static Vector3[] ProjectOntoDominantAxis(float3[] vertices, float3 primaryAxis)
    {
        // Find min and max projections along the dominant axis
        float minProjection = float.MaxValue, maxProjection = float.MinValue;
        float3 minPoint = float3.zero, maxPoint = float3.zero;

        foreach (float3 vertex in vertices)
        {
            float projection = math.dot(primaryAxis, vertex);

            if (projection < minProjection)
            {
                minProjection = projection;
                minPoint = vertex;
            }
            if (projection > maxProjection)
            {
                maxProjection = projection;
                maxPoint = vertex;
            }
        }

        return new Vector3[] { (Vector3)minPoint, (Vector3)maxPoint };
    }

    public static Vector3[] ProjectOntoPrincipalAxes(float3[] vertices, float3x3 eigenvectors)
    {
        // These will hold the extreme projections along each axis
        float minProjectionX = float.MaxValue, maxProjectionX = float.MinValue;
        float minProjectionY = float.MaxValue, maxProjectionY = float.MinValue;
        float minProjectionZ = float.MaxValue, maxProjectionZ = float.MinValue;

        // These will hold the actual points on the mesh corresponding to the extreme projections
        float3 minPointX = float3.zero, maxPointX = float3.zero;
        float3 minPointY = float3.zero, maxPointY = float3.zero;
        float3 minPointZ = float3.zero, maxPointZ = float3.zero;

        // Use centroid as reference
        float3 centroid = float3.zero;

        foreach (float3 vertex in vertices)
        {
            // Project the vertex onto each of the three principal axes (eigenvectors)
            float projectionX = math.dot(eigenvectors.c0, vertex);
            float projectionY = math.dot(eigenvectors.c1, vertex);
            float projectionZ = math.dot(eigenvectors.c2, vertex);

            // Find extremes along X axis
            if (projectionX < minProjectionX)
            {
                minProjectionX = projectionX;
                minPointX = vertex;
            }
            if (projectionX > maxProjectionX)
            {
                maxProjectionX = projectionX;
                maxPointX = vertex;
            }

            // Find extremes along Y axis
            if (projectionY < minProjectionY)
            {
                minProjectionY = projectionY;
                minPointY = vertex;
            }
            if (projectionY > maxProjectionY)
            {
                maxProjectionY = projectionY;
                maxPointY = vertex;
            }

            // Find extremes along Z axis
            if (projectionZ < minProjectionZ)
            {
                minProjectionZ = projectionZ;
                minPointZ = vertex;
            }
            if (projectionZ > maxProjectionZ)
            {
                maxProjectionZ = projectionZ;
                maxPointZ = vertex;
            }

            centroid += vertex;
        }

        centroid /= vertices.Length;  // Calculate the centroid

        // Step 5: Calculate min and max points along each principal axis
        Vector3 finalMinPointX = (Vector3)(centroid + minProjectionX * eigenvectors.c0);
        Vector3 finalMaxPointX = (Vector3)(centroid + maxProjectionX * eigenvectors.c0);

        Vector3 finalMinPointY = (Vector3)(centroid + minProjectionY * eigenvectors.c1);
        Vector3 finalMaxPointY = (Vector3)(centroid + maxProjectionY * eigenvectors.c1);

        Vector3 finalMinPointZ = (Vector3)(centroid + minProjectionZ * eigenvectors.c2);
        Vector3 finalMaxPointZ = (Vector3)(centroid + maxProjectionZ * eigenvectors.c2);

        return new Vector3[] { finalMinPointX, finalMaxPointX, finalMinPointY, finalMaxPointY, finalMinPointZ, finalMaxPointZ };
    }

}
