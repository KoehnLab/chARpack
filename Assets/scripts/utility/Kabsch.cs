using System;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using UnityEngine;

public class Kabsch
{
    private static Matrix<float> covariance(float[,] P, float[,] Q)
    {
        // Assuming the same number of positions/atoms for both molecules
        Matrix<float> P_Matrix = CreateMatrix.DenseOfArray(P);
        Matrix<float> Q_Matrix = CreateMatrix.DenseOfArray(Q);
        var cov = P_Matrix.TransposeThisAndMultiply(Q_Matrix);
        return cov;
    }

    private static Matrix<float> optimalRotation(Matrix<float> covariance)
    {
        Svd<float> svd = covariance.Svd();
        var d = Math.Sign(svd.U.Determinant() * svd.VT.Transpose().Determinant());
        var reflected = CreateMatrix.DenseOfDiagonalArray(new float[] { 1, 1, d });
        var rotation = svd.U.Multiply(reflected.Multiply(svd.VT));
        return rotation;
    }

    public static Matrix4x4 kabschRotationMatrix(Vector3[] positions1, Vector3[] positions2)
    {
        // Translate positions to origin (center of second molecule)
        var sum = new Vector3();
        foreach (var pos in positions2) { sum += pos; }
        var meanPos = sum / (positions2.Count());
        for (var i = 0; i < positions1.Count(); i++) positions1[i] -= meanPos;
        for (var i = 0; i < positions2.Count(); i++) positions2[i] -= meanPos;

        // Convert positions to array
        var P = new float[positions1.Count(), 3];
        var Q = new float[positions2.Count(), 3];
        for (var i = 0; i < positions1.Count(); i++) { P[i, 0] = positions1[i][0]; P[i, 1] = positions1[i][1]; P[i, 2] = positions1[i][2]; }
        for (var i = 0; i < positions2.Count(); i++) { Q[i, 0] = positions2[i][0]; Q[i, 1] = positions2[i][1]; Q[i, 2] = positions2[i][2]; }
        var cov = covariance(P, Q);
        var rotation = optimalRotation(cov);
        var matrix = Matrix4x4.identity;
        for (var i = 0; i < 3; i++) { matrix[i, 0] = rotation[i, 0]; matrix[i, 1] = rotation[i, 1]; matrix[i, 2] = rotation[i, 2]; }
        return matrix;
    }

    private static void rotateAtomsByMatrix(Matrix4x4 rotationMatrix)
    {
        //var quat = Quaternion.LookRotation(rotationMatrix.GetRow(2), rotationMatrix.GetRow(1));
        //var angle = 0.0f; var axis = Vector3.zero;
        //quat.ToAngleAxis(out angle, out axis);
        //foreach (var atom in atomList) { atom.transform.RotateAround(getCenter(), axis, angle); }
    }
}
