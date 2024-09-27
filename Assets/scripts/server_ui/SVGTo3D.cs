using UnityEngine;
using Unity.VectorGraphics;
using System.Collections.Generic;
using System.IO;

public class SVGTo3D : MonoBehaviour
{
    public Material meshMaterial; // Material for the 3D mesh

    public static void generate3DRepresentation(List<VectorUtils.Geometry> geometry)
    {
        //// Parse the SVG
        //var svgScene = SVGParser.ImportSVG(new StringReader(svg_content));

        //// Tessellate the entire SVG scene
        //var tessellationOptions = new VectorUtils.TessellationOptions()
        //{
        //    StepDistance = 10f,         // Controls curve approximation (higher = rougher, lower = smoother)
        //    MaxCordDeviation = 0.5f,    // Maximum deviation allowed for curves
        //    MaxTanAngleDeviation = 0.1f,// Controls smoothness of curves
        //    SamplingStepSize = 0.01f    // Sampling step size (affects precision)
        //};

        //// Get geometry from the SVG scene
        //var geometry = VectorUtils.TessellateScene(svgScene.Scene, tessellationOptions);

        Debug.Log("[SVGTo3D] Extruding SVG...");

        // Extrude the geometry into 3D
        Mesh extrudedMesh = Create3DMeshFromGeometry(geometry, 0.1f); // 0.1f is the extrusion depth

        // Create a GameObject to display the extruded mesh
        var meshObj = new GameObject("Extruded SVG Mesh");
        var meshFilter = meshObj.AddComponent<MeshFilter>();
        var meshRenderer = meshObj.AddComponent<MeshRenderer>();

        meshFilter.mesh = extrudedMesh;
        //meshRenderer.material = meshMaterial; // Assign material to the extruded mesh
    }

    static Mesh Create3DMeshFromGeometry(List<VectorUtils.Geometry> geometries, float depth)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        foreach (var geom in geometries)
        {
            int vertexOffset = vertices.Count;

            // Add front face vertices (Z = 0)
            foreach (var vertex in geom.Vertices)
            {
                vertices.Add(new Vector3(vertex.x, vertex.y, 0));
            }

            // Add back face vertices (Z = depth)
            foreach (var vertex in geom.Vertices)
            {
                vertices.Add(new Vector3(vertex.x, vertex.y, depth));
            }

            // Create front and back triangles
            for (int i = 0; i < geom.Indices.Length; i += 3)
            {
                // Front face
                triangles.Add(geom.Indices[i] + vertexOffset);
                triangles.Add(geom.Indices[i + 1] + vertexOffset);
                triangles.Add(geom.Indices[i + 2] + vertexOffset);

                // Back face (reversed winding order)
                triangles.Add(geom.Indices[i + 2] + vertexOffset + geom.Vertices.Length);
                triangles.Add(geom.Indices[i + 1] + vertexOffset + geom.Vertices.Length);
                triangles.Add(geom.Indices[i] + vertexOffset + geom.Vertices.Length);
            }

            // Create side faces by connecting front and back vertices
            for (int i = 0; i < geom.Vertices.Length; i++)
            {
                int next = (i + 1) % geom.Vertices.Length;

                int front1 = i + vertexOffset;
                int front2 = next + vertexOffset;
                int back1 = i + vertexOffset + geom.Vertices.Length;
                int back2 = next + vertexOffset + geom.Vertices.Length;

                // Add triangles for sides
                triangles.Add(front1);
                triangles.Add(front2);
                triangles.Add(back1);

                triangles.Add(back1);
                triangles.Add(front2);
                triangles.Add(back2);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }
}