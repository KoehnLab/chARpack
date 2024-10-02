using UnityEngine;
using Unity.VectorGraphics;
using System.Collections.Generic;
using System;
using DataStructures.ViliWonka.KDTree;
using System.Drawing;

public class SVGTo3D : MonoBehaviour
{
    public Material meshMaterial; // Material for the 3D mesh

    public static void generate3DRepresentation(List<VectorUtils.Geometry> geometry, Guid mol_id)
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
        var extrudedMesh = CreateFlatMeshFromGeometry(geometry);

        // Create a GameObject to display the extruded mesh
        var mat = Resources.Load<Material>("materials/sfExtrude");
        var meshCollection = new GameObject("Extruded SVG Mesh");
        //meshCollection.transform.Rotate(new Vector3(180f, 0f, 0f));

        var object_list = new List<GameObject>();
        var point_list = new List<Vector3>();

        foreach (var (mesh, i) in extrudedMesh.WithIndex())
        {
            if (i == 0) continue; // omit background
            var child = new GameObject($"Mesh_{i}");
            child.transform.position = mesh.Item2;
            var meshFilter = child.AddComponent<MeshFilter>();
            var meshRenderer = child.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh.Item1;

            // material
            var current_mat = Instantiate(mat) as Material;
            current_mat.color = geometry[i].Color;
            meshRenderer.material = current_mat;

            child.transform.parent = meshCollection.transform;

            //if (MeshLine.IsMeshALine(meshFilter, 3f))
            //{
            //    var points = MeshLine.ExtractLineEndpointsWithDegeneracy(child.transform);

            //    Debug.Log($"[SVGTo3D] principal axis points {points.Print()}");

            //    foreach (var point in points)
            //    {
            //        var debug_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //        debug_sphere.transform.localScale = 0.5f * Vector3.one;
            //        debug_sphere.transform.localPosition = point;
            //    }
            //}

            Vector2 eigenvector1, eigenvector2, centroid;
            float eigenvalue1, eigenvalue2;
            Vector2 startPoint, endPoint;
            PrincipalAxis2D.GetEigenCenterEndpoints(child.transform, out eigenvalue1, out eigenvalue2, out eigenvector1, out eigenvector2, out centroid, out startPoint, out endPoint);
            if (PrincipalAxis2D.IsLine(eigenvalue1, eigenvalue2, 3f)) // line
            {
                var points = new List<Vector3>();
                points.Add(startPoint);
                points.Add(endPoint);
                //points.Add(centroid);
                foreach (var point in points)
                {
                    var debug_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    debug_sphere.name = child.name;
                    debug_sphere.transform.parent = child.transform;
                    debug_sphere.transform.localScale = 0.5f * Vector3.one;
                    debug_sphere.transform.position = point;
                }
                object_list.Add(child);
                object_list.Add(child);
                point_list.Add(startPoint);
                point_list.Add(endPoint);
            }
            else // atom symbol (e.g. H, O, .. )
            {
                var debug_cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debug_cube.name = child.name;
                debug_cube.transform.parent = child.transform;
                debug_cube.transform.localScale = 0.5f * Vector3.one;
                debug_cube.transform.position = child.transform.position;

                object_list.Add(child);
                point_list.Add(child.transform.position);
            }

        }

        var tree = new KDTree();
        tree.Build(point_list);

        var query = new KDQuery();

        // generate debug spheres for atom coordinates
        foreach (var atom in GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList)
        {

            var debug_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debug_sphere.name = atom.name;
            debug_sphere.transform.parent = atom.transform;
            //debug_sphere.transform.localScale = 0.5f * Vector3.one;
            

            var results = new List<int>();
            // check for nearest point 
            query.KNearest(tree, atom.structure_coords, 1, results);

            if (results.Count > 0)
            {
                Debug.Log($"[SVGto3D] Found neighbor {object_list[results[0]].name}");
                debug_sphere.transform.parent = object_list[results[0]].transform;
                debug_sphere.transform.position = atom.structure_coords;
            }
        }



        

        //meshCollection.transform.Rotate(new Vector3(180f, 0f, 0f));

        // // Assign material to the extruded mesh
    }

    static List<Mesh> Create3DMeshFromGeometry(List<VectorUtils.Geometry> geometries, float depth)
    {
        var mesh_list = new List<Mesh>();
        foreach (var geom in geometries)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

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
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            mesh_list.Add(mesh);
        }

        return mesh_list;
    }


    static List<Tuple<Mesh, Vector3>> CreateFlatMeshFromGeometry(List<VectorUtils.Geometry> geometries)
    {
        var mesh_list = new List<Tuple<Mesh, Vector3>>();
        foreach (var geom in geometries)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            int vertexOffset = vertices.Count;

            // Add front face vertices (Z = 0)
            foreach (var vertex in geom.Vertices)
            {
                vertices.Add(new Vector3(vertex.x, vertex.y, 0));
            }

            // Create front and back triangles
            for (int i = 0; i < geom.Indices.Length; i += 3)
            {
                // Front face
                triangles.Add(geom.Indices[i] + vertexOffset);
                triangles.Add(geom.Indices[i + 1] + vertexOffset);
                triangles.Add(geom.Indices[i + 2] + vertexOffset);

            }

            Vector3 origin = PrincipalAxis2D.ComputeAreaWeightedCentroid(vertices.ToArray(), triangles.ToArray());

            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i] - origin;
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            mesh_list.Add(new Tuple<Mesh, Vector3>(mesh, origin));
        }

        return mesh_list;
    }
}