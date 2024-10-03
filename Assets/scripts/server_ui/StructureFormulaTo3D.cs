using UnityEngine;
using Unity.VectorGraphics;
using System.Collections.Generic;
using System;
using DataStructures.ViliWonka.KDTree;
using chARpackTypes;
using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEditor.AddressableAssets.Build;
using Microsoft.MixedReality.Toolkit.Extensions.Tracking;
using Unity.Entities.UniversalDelegates;
using UnityEngine.Analytics;

public class StructureFormulaTo3D : MonoBehaviour
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
        var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id);
        if (mol == null) return;

        Debug.Log("[StructureFormulaTo3D] Extruding SVG...");

        // Extrude the geometry into 3D
        var extrudedMesh = CreateFlatMeshFromGeometry(geometry);
        var total_bounds = new Bounds();
        foreach (var exmesh in extrudedMesh)
        {
            total_bounds.Encapsulate(exmesh.Item2);
        }



        // Create a GameObject to display the extruded mesh
        var mat = Resources.Load<Material>("materials/sfExtrude");
        var mol2D = new GameObject($"extrude_{mol.name}").AddComponent<Molecule2D>();
        mol2D.transform.Rotate(new Vector3(180f, 0f, 0f));
        mol2D.transform.position = total_bounds.center;
        mol2D.molReference = mol;

        var symbol_object_list = new List<GameObject>();
        var symbol_point_list = new List<Vector3>();

        var bond_object_list = new List<GameObject>();
        var bond_point_list = new List<Vector3>();

        foreach (var (mesh, i) in extrudedMesh.WithIndex())
        {
            if (i == 0) continue; // omit background

            var child = new GameObject($"Mesh_{i}");
            child.transform.position = mesh.Item3;


            var meshFilter = child.AddComponent<MeshFilter>();
            var meshRenderer = child.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh.Item1;

            // material
            var current_mat = Instantiate(mat) as Material;
            current_mat.color = geometry[i].Color;
            meshRenderer.material = current_mat;

            child.transform.parent = mol2D.transform;
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
                //var points = new List<Vector3>();
                //points.Add(startPoint);
                //points.Add(endPoint);
                //points.Add(centroid);
                //foreach (var point in points)
                //{
                //    var debug_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //    debug_sphere.name = child.name;
                //    debug_sphere.transform.parent = child.transform;
                //    debug_sphere.transform.localScale = 0.5f * Vector3.one;
                //    debug_sphere.transform.position = point;
                //}
                bond_object_list.Add(child);
                bond_object_list.Add(child);
                bond_point_list.Add(startPoint);
                bond_point_list.Add(endPoint);
            }
            else // atom symbol (e.g. H, O, .. )
            {
                //var debug_cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //debug_cube.name = child.name;
                //debug_cube.transform.parent = child.transform;
                //debug_cube.transform.localScale = 0.5f * Vector3.one;
                //debug_cube.transform.position = child.transform.position;

                symbol_object_list.Add(child);
                symbol_point_list.Add(child.transform.position);
            }

        }

        var atom2D_list = new List<Atom2D>();

        // Assign SYMBOLS
        var symbol_tree = new KDTree();
        symbol_tree.Build(symbol_point_list);
        var query = new KDQuery();

        // generate debug spheres for atom coordinates
        foreach (var atom in GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList)
        {

            //var debug_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //debug_sphere.name = atom.name;
            //debug_sphere.transform.parent = atom.transform;
            //debug_sphere.transform.localScale = 0.5f * Vector3.one;
            
            if (atom.m_data.m_abbre != "C")
            {
                var results = new List<int>();
                // check for nearest point 
                query.KNearest(symbol_tree, atom.structure_coords, 1, results);

                if (results.Count > 0)
                {
                    //Debug.Log($"[SVGto3D] Found neighbor {symbol_object_list[results[0]].name}");
                    var atom2D = symbol_object_list[results[0]].AddComponent<Atom2D>();
                    atom2D.atomReference = atom;
                    atom2D_list.Add(atom2D);
                    symbol_object_list[results[0]].name = atom.name;
                    //debug_sphere.transform.parent = symbol_object_list[results[0]].transform;
                    //debug_sphere.transform.position = atom.structure_coords;
                }
            }
        }
        // Assign BONDS and CARBON atoms
        var line_tree = new KDTree();
        line_tree.Build(bond_point_list);
        query = new KDQuery();

        var endpoints_ids = new HashSet<int>();
        var split_lines_ids = new HashSet<Tuple<int,int>>();
        var nodes_ids = new List<List<int>>();
        var nodes_objects = new List<List<GameObject>>();

        // check for nodes or split bonds
        foreach (var (endpoint,current_id) in bond_point_list.WithIndex())
        {
            bool skip = false;
            foreach (var s_entry in split_lines_ids)
            {
                if (s_entry.Item1 == current_id || s_entry.Item2 == current_id) skip = true;

            }
            foreach (var n_entry in nodes_ids)
            {
                if (n_entry.Contains(current_id)) skip = true;
            }
            if (!skip) // prevent duplicates
            {
                var results = new List<int>();
                query.Radius(line_tree, endpoint, 0.1f, results);
                if (results.Count == 1) // endpoint (probably at symbol)
                {
                    endpoints_ids.Add(results[0]);
                }
                else if (results.Count == 2) // split line/bond
                {
                    split_lines_ids.Add(new Tuple<int, int>(results[0], results[1]));
                }
                else if (results.Count > 2) // carbon atom
                {
                    var tmp_list = new List<int>();
                    var node_object_list = new List<GameObject>();
                    for (int i = 0; i < results.Count; i++)
                    {
                        tmp_list.Add(results[i]);
                        node_object_list.Add(bond_object_list[results[i]]);
                    }
                    nodes_ids.Add(tmp_list);
                    nodes_objects.Add(node_object_list);
                }
            }
        }

        //check if nodes are connected
        for (int i = 0; i < nodes_ids.Count; i++)
        {
            for (int j = 0; j < nodes_ids[i].Count; j++)
            {
                var obj = nodes_objects[i][j];
                for (int x = 0; x < nodes_ids.Count; x++)
                {
                    if (x != i)
                    {
                        if (nodes_objects[x].Contains(obj) && !endpoints_ids.Contains(nodes_ids[i][j]))
                        {
                            endpoints_ids.Add(nodes_ids[i][j]);
                        }
                    }
                }
            }
        }

        // sf coords tree
        var sf_coords_tree = new KDTree();
        var atom_sf_coords = new List<Vector3>();
        foreach (var atom in GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList)
        {
            atom_sf_coords.Add(atom.structure_coords);
        }
        sf_coords_tree.Build(atom_sf_coords);
        query = new KDQuery();

        // create atoms at nodes
        foreach (var nd_id in nodes_ids)
        {
            var node_atom = new GameObject().AddComponent<Atom2D>();
            node_atom.transform.parent = mol2D.transform;
            List<int> res = new List<int>();
            query.KNearest(sf_coords_tree, bond_point_list[nd_id[0]], 1, res);
            var ref_atom = mol.atomList[res[0]];
            node_atom.name = ref_atom.name;
            node_atom.atomReference = ref_atom;
            node_atom.transform.position = ref_atom.structure_coords;
            atom2D_list.Add(node_atom);
        }

        // sort atom list and add to mol2d
        var atom2D_list_sorted = new List<Atom2D>();
        foreach (var atom in mol.atomList)
        {
            var match = atom2D_list.Find(a => a.atomReference == atom);
            atom2D_list_sorted.Add(match);
        }
        mol2D.atoms = atom2D_list_sorted;


        var bond_list = new List<Bond2D>();
        List<Bond2D> merge_bond_list = new List<Bond2D>();
        foreach (var sl in split_lines_ids)
        {
            var merge_bond = new GameObject("Bond");
            merge_bond.transform.position = bond_point_list[sl.Item1];
            merge_bond.transform.parent = mol2D.transform;
            bond_object_list[sl.Item1].transform.parent = merge_bond.transform;
            bond_object_list[sl.Item2].transform.parent = merge_bond.transform;
            var b2d = merge_bond.AddComponent<Bond2D>();
            merge_bond_list.Add(b2d);

            var endpoint_ids_obj1 = bond_object_list.GetAllIndicesOf(bond_object_list[sl.Item1]);
            var endpoint_ids_obj2 = bond_object_list.GetAllIndicesOf(bond_object_list[sl.Item2]);

            if (endpoint_ids_obj1.Count != 2 && endpoint_ids_obj2.Count != 2)
            {
                Debug.LogError("[MergeHalfBonds] Could not retreive all end points.");
                return;
            }
            var finalEndPoint_ids = new List<int>();
            if (Vector3.Distance(bond_point_list[endpoint_ids_obj1[0]], bond_point_list[endpoint_ids_obj2[0]]) < 0.1f)
            {
                finalEndPoint_ids.Add(endpoint_ids_obj1[1]);
                finalEndPoint_ids.Add(endpoint_ids_obj2[1]);
            }
            else if (Vector3.Distance(bond_point_list[endpoint_ids_obj1[0]], bond_point_list[endpoint_ids_obj2[1]]) < 0.1f)
            {
                finalEndPoint_ids.Add(endpoint_ids_obj1[1]);
                finalEndPoint_ids.Add(endpoint_ids_obj2[0]);
            }
            else if (Vector3.Distance(bond_point_list[endpoint_ids_obj1[1]], bond_point_list[endpoint_ids_obj2[0]]) < 0.1f)
            {
                finalEndPoint_ids.Add(endpoint_ids_obj1[0]);
                finalEndPoint_ids.Add(endpoint_ids_obj2[1]);
            }
            else
            {
                finalEndPoint_ids.Add(endpoint_ids_obj1[0]);
                finalEndPoint_ids.Add(endpoint_ids_obj2[0]);
            }

            foreach (var (ep_id,i) in finalEndPoint_ids.WithIndex())
            {

                Debug.Log($"[Final endpoints] {ep_id} {i}");
                List<int> res = new List<int>();
                query.KNearest(sf_coords_tree, bond_point_list[ep_id], 1, res);

                if (i ==  0)
                {
                    b2d.atom1ref = mol.atomList[res[0]];
                    b2d.atom1 = mol2D.atoms[res[0]];
                    b2d.atom1ConnectionOffset = bond_point_list[ep_id] - atom_sf_coords[res[0]];
                    b2d.end1 = bond_point_list[ep_id];
                }
                if (i == 1)
                {
                    b2d.atom2ref = mol.atomList[res[0]];
                    b2d.atom2 = mol2D.atoms[res[0]];
                    b2d.atom2ConnectionOffset = bond_point_list[ep_id] - atom_sf_coords[res[0]];
                    b2d.end2 = bond_point_list[ep_id];
                }
            }
            b2d.bondReference = b2d.atom1ref.getBond(b2d.atom2ref);
            bond_list.Add(b2d);
        }

        // process simple bonds
        var processed_bonds = new List<GameObject>();
        foreach (var ep_id in endpoints_ids)
        {
            var obj = bond_object_list[ep_id];
            bool skip = false;
            if (processed_bonds.Contains(obj) || obj.GetComponentInParent<Bond2D>() != null) skip = true;

            if (!skip)
            {
                var final_ep_ids = bond_object_list.GetAllIndicesOf(obj);
                Debug.Log($"[SimpleBonds] final end point ids {final_ep_ids.ToArray().Print()}");
                var b2d = obj.AddComponent<Bond2D>();
                b2d.name = "Bond";

                foreach (var (fep_id, i) in final_ep_ids.WithIndex())
                {

                    List<int> res = new List<int>();
                    query.KNearest(sf_coords_tree, bond_point_list[fep_id], 1, res);

                    if (i == 0)
                    {
                        b2d.atom1ref = mol.atomList[res[0]];
                        b2d.atom1 = mol2D.atoms[res[0]];
                        b2d.atom1ConnectionOffset = bond_point_list[fep_id] - atom_sf_coords[res[0]];
                        b2d.end1 = bond_point_list[fep_id];
                    }
                    if (i == 1)
                    {
                        b2d.atom2ref = mol.atomList[res[0]];
                        b2d.atom2 = mol2D.atoms[res[0]];
                        b2d.atom2ConnectionOffset = bond_point_list[fep_id] - atom_sf_coords[res[0]];
                        b2d.end2 = bond_point_list[fep_id];
                    }
                }
                b2d.bondReference = b2d.atom1ref.getBond(b2d.atom2ref);
                bond_list.Add(b2d);
                processed_bonds.Add(obj);
            }
        }
        mol2D.bonds = bond_list;
        mol2D.transform.Rotate(new Vector3(180f, 0f, 0f));

        mol2D.transform.localScale = 0.002f * Vector3.one;
        mol2D.transform.position = GlobalCtrl.Singleton.getCurrentSpawnPos();

        mol2D.transform.parent = GlobalCtrl.Singleton.atomWorld.transform;
        //mol2D.initialized = true;
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


    static List<Triple<Mesh, Bounds, Vector3>> CreateFlatMeshFromGeometry(List<VectorUtils.Geometry> geometries)
    {
        var mesh_list = new List<Triple<Mesh, Bounds, Vector3>>();
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
            Bounds bounds = new Bounds();
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i] - origin;
                bounds.Encapsulate(vertices[i]);
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();



            mesh_list.Add(new Triple<Mesh, Bounds, Vector3>(mesh, bounds, origin));
        }

        return mesh_list;
    }
}