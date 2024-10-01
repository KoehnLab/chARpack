using Unity.Mathematics;
using UnityEngine;

public class EigenDecomposition
{
    // Compute the eigenvalues of a 3x3 symmetric matrix
    public static float3 ComputeEigenvalues(float3x3 A)
    {
        // Coefficients for the characteristic equation det(A - AI) = 0 (cubic equation)
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

            // Compute the angles (trigonometry)
            float phi = math.acos(r) / 3.0f;

            // Eigenvalues
            float eig1 = q + 2.0f * p * math.cos(phi);
            float eig2 = q + 2.0f * p * math.cos(phi + 2.0f * math.PI / 3.0f);
            float eig3 = 3.0f * q - eig1 - eig2; // Since sum of eigenvalues equals the trace

            return new float3(eig1, eig2, eig3);
        }
    }

    // Helper function to compute the trace of a 3x3 matrix (sum of diagonal elements)
    private static float Trace(float3x3 A)
    {
        return A.c0.x + A.c1.y + A.c2.z;
    }

    // Helper function to compute the determinant of a 3x3 matrix
    private static float Determinant(float3x3 A)
    {
        return A.c0.x * (A.c1.y * A.c2.z - A.c1.z * A.c2.y)
             - A.c0.y * (A.c1.x * A.c2.z - A.c1.z * A.c2.x)
             + A.c0.z * (A.c1.x * A.c2.y - A.c1.y * A.c2.x);
    }
}
