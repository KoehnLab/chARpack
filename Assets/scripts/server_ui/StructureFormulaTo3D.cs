using UnityEngine;
using Unity.VectorGraphics;
using System.Collections.Generic;
using System;
using DataStructures.ViliWonka.KDTree;
using chARpack.Types;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Text.RegularExpressions;



namespace chARpack
{
    public class StructureFormulaTo3D : MonoBehaviour
    {
        public Material meshMaterial; // Material for the 3D mesh

        private static List<XElement> GetSVGPathElements(string svgContent)
        {

            //// Load the SVG content into an XElement for parsing
            //XElement svgRoot = XElement.Parse(svgContent);

            //// Find all <path> elements
            //return svgRoot.Descendants("path").ToList();
            try
            {
                // Load the SVG content into an XElement for parsing
                XElement svgRoot = XElement.Parse(svgContent);

                // Debugging: Log the root element
                Debug.Log("SVG Root: " + svgRoot);

                // Get the namespace of the SVG
                XNamespace svgNamespace = svgRoot.GetDefaultNamespace();

                // Debugging: Log the namespace
                Debug.Log("SVG Namespace: " + svgNamespace);

                // Find all <path> elements within the namespace
                List<XElement> paths = svgRoot.Descendants(svgNamespace + "path").ToList();

                // Debugging: Log the number of paths found
                Debug.Log("Found Path Elements: " + paths.Count);

                return paths;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error parsing SVG: " + ex.Message);
                return new List<XElement>();
            }
        }

        static List<object> ParseSVGPathElements(List<XElement> pathElements)
        {
            List<object> parsedData = new List<object>();
            Regex bondRegex = new Regex(@"bond-(\d+).*atom-(\d+).*atom-(\d+)");
            Regex atomRegex = new Regex(@"atom-(\d+)");
            Regex bondCoordinatesRegex = new Regex(@"([\d.]+),([\d.]+)");
            Regex atomCoordinatesRegex = new Regex(@"([\d.]+) ([\d.]+)");

            foreach (XElement path in pathElements)
            {
                //Debug.Log($"Element: {path}");
                string classAttr = path.Attribute("class")?.Value;
                string dAttr = path.Attribute("d")?.Value;

                if (string.IsNullOrEmpty(classAttr) || string.IsNullOrEmpty(dAttr))
                    continue;

                Match bondMatch = bondRegex.Match(classAttr);
                if (bondMatch.Success)
                {
                    int bondId = int.Parse(bondMatch.Groups[1].Value);
                    int atomId1 = int.Parse(bondMatch.Groups[2].Value);
                    int atomId2 = int.Parse(bondMatch.Groups[3].Value);

                    var matches = bondCoordinatesRegex.Matches(dAttr);
                    Vector2[] endpoints = null;
                    Vector2 center = Vector2.zero;

                    float x1 = float.Parse(matches[0].Groups[1].Value);
                    float y1 = float.Parse(matches[0].Groups[2].Value);
                    float x2 = float.Parse(matches[1].Groups[1].Value);
                    float y2 = float.Parse(matches[1].Groups[2].Value);

                    if (matches.Count > 2)
                    {
                        float x3 = float.Parse(matches[2].Groups[1].Value);
                        float y3 = float.Parse(matches[2].Groups[2].Value);

                        // Calculate the midpoint of the last two coordinates
                        Vector2 midpoint = new Vector2((x2 + x3) / 2, (y2 + y3) / 2);

                        endpoints = new[] { new Vector2(x1, y1), midpoint };
                    }
                    else
                    {
                        endpoints = new[] { new Vector2(x1, y1), new Vector2(x2, y2) };
                    }
                    center = 0.5f * (endpoints[0] + endpoints[1]);
                    parsedData.Add(new BondDataSVG
                    {
                        BondId = bondId,
                        AtomIds = new List<int> { atomId1, atomId2 },
                        Endpoints = endpoints,
                        Center = center
                    });
                    continue;
                }

                Match atomMatch = atomRegex.Match(classAttr);
                if (atomMatch.Success)
                {
                    int atomId = int.Parse(atomMatch.Groups[1].Value);
                    var matches = atomCoordinatesRegex.Matches(dAttr);

                    if (matches.Count < 1)
                    {
                        matches = bondCoordinatesRegex.Matches(dAttr);
                    }

                    Vector2 center = Vector2.zero;
                    //foreach (var match in matches)
                    for (int i = 0; i < matches.Count; i++)
                    {
                        float x = float.Parse(matches[i].Groups[1].Value);
                        float y = float.Parse(matches[i].Groups[2].Value);
                        center += new Vector2(x, y);
                    }
                    center /= matches.Count;

                    parsedData.Add(new AtomDataSVG
                    {
                        AtomId = atomId,
                        Center = center
                    });
                }
            }

            return parsedData;
        }

        public class BondDataSVG
        {
            public int BondId { get; set; }
            public List<int> AtomIds { get; set; }
            public Vector2[] Endpoints { get; set; }
            public Vector2 Center;
        }

        public class AtomDataSVG
        {
            public int AtomId { get; set; }
            public Vector2 Center;
        }

        public enum BondType
        {
            SINGLE,
            DOUBLE,
            SPLITDOUBLE,
            RINGDOUBLE,
            SPLITSINGLE,
            DASHEDWEDGE
        }

        public class BondDataCollection
        {
            public int BondId;
            public int[] AtomIds;
            public float[] Offsets;
            public List<Vector2[]> EndPoints;
            public Vector2[] FinalEndPoints;
            public Vector2 FinalCenter;
            public List<Triple<Mesh, Bounds, Vector3>> MeshData = new List<Triple<Mesh, Bounds, Vector3>>();
            public BondType BondType;
            public List<GameObject> elements;
        }

        public class AtomDataCollection
        {
            public int AtomId;
            public List<Triple<Mesh, Bounds, Vector3>> MeshData = new List<Triple<Mesh, Bounds, Vector3>>();
            public List<GameObject> elements;
        }

        public static void generateFromSVGContent(Guid mol_id, string svg_content, List<Vector2> coords)
        {
            var mol = GlobalCtrl.Singleton.List_curMolecules.ElementAtOrNull(mol_id);
            if (mol == null)
            {
                Debug.LogError($"[StructureFormulaTo3D] Molecule with id {mol_id} does not exist. Abort.");
                return;
            }

            // push 2D coords
            for (int i = 0; i < coords.Count; i++)
            {
                mol.atomList[i].structure_coords = coords[i];
            }
            mol.svgFormula = svg_content;

            // Tessellate
            var sceneInfo = SVGParser.ImportSVG(new StringReader(svg_content));
            var tessellationOptions = new VectorUtils.TessellationOptions
            {
                StepDistance = 0.1f,
                SamplingStepSize = 50,
                MaxCordDeviation = 0.5f,
                MaxTanAngleDeviation = 0.1f
            };

            var geometries = VectorUtils.TessellateScene(sceneInfo.Scene, tessellationOptions);
            generate3DRepresentation(geometries, svg_content, mol_id);
        }

        static public Vector2 CalculateGeometryCenter(VectorUtils.Geometry geometry)
        {
            if (geometry.Vertices == null || geometry.Vertices.Length == 0) return Vector2.zero;

            var center = Vector2.zero;
            foreach (var vertex in geometry.Vertices)
            {
                center += vertex;
            }
            center /= geometry.Vertices.Length;
            return center;
        }

        public static bool ApproximatelyAtTheSamePlace(Vector2 in1, Vector2 in2, float tolerance = 0.5f)
        {
            return Vector2.Distance(in1, in2) < tolerance;
        }

        static public Rect CalculateShapeBounds(Shape shape)
        {
            Debug.Log($"[CalculateShapeBounds] num shape contours {shape.Contours.Length}");
            if (shape == null || shape.Contours == null || shape.Contours.Length == 0)
                return new Rect();

            Debug.Log($"[CalculateShapeBounds] num shape contours {shape.Contours.Length}");

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            // Loop through each contour in the shape
            foreach (var contour in shape.Contours)
            {
                foreach (var segment in contour.Segments)
                {
                    // Check all control points for the segment (Bezier curves have 4 points)
                    min = Vector2.Min(min, segment.P0);
                    min = Vector2.Min(min, segment.P1);
                    min = Vector2.Min(min, segment.P2);

                    max = Vector2.Max(max, segment.P0);
                    max = Vector2.Max(max, segment.P1);
                    max = Vector2.Max(max, segment.P2);
                }
            }

            Debug.Log($"[CalculateGeometryBounds] shape rect calculated {min.x} {min.y} {max.x} {max.y}");

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static bool BoundsAreApproximatelyEqual(Rect boundsA, Rect boundsB, float tolerance = 0.01f)
        {
            return Mathf.Abs(boundsA.x - boundsB.x) < tolerance &&
                   Mathf.Abs(boundsA.y - boundsB.y) < tolerance &&
                   Mathf.Abs(boundsA.width - boundsB.width) < tolerance &&
                   Mathf.Abs(boundsA.height - boundsB.height) < tolerance;
        }

        static Rect CalculateGeometryBounds(VectorUtils.Geometry geometry)
        {
            if (geometry.Vertices == null || geometry.Vertices.Length == 0)
                return new Rect();

            Debug.Log($"[CalculateGeometryBounds] num geo vertices {geometry.Vertices.Length}");
            Vector2 min = geometry.Vertices[0];
            Vector2 max = geometry.Vertices[0];

            foreach (var vertex in geometry.Vertices)
            {
                min = Vector2.Min(min, vertex);
                max = Vector2.Max(max, vertex);
            }
            Debug.Log($"[CalculateGeometryBounds] geo rect calculated {min.x} {min.y} {max.x} {max.y}");

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static void generateFromSVGContentUI(string svg_content, Guid mol_id, List<Vector2> coords)
        {
            generateFromSVGContent(mol_id, svg_content, coords);
            EventManager.Singleton.Generate3DFormula(mol_id, svg_content, coords);
        }


        public static void generate3DRepresentation(List<VectorUtils.Geometry> geometry, string svg_content, Guid mol_id)
        {
            //Debug.Log($"[StructureFormulaTo3D] SVG content:\n {svg_content}");
            //Debug.Log($"[StructureFormulaTo3D] Found {path_elements.Count} path elements in SVG.");
            //Assert.AreNotEqual(geometry.Count, path_elements.Count);
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

            geometry.RemoveAt(0);
            // Extrude the geometry into 3D
            var extrudedMesh = CreateFlatMeshFromGeometry(geometry);

            var em_center_list = new List<Vector3>();
            foreach (var em in extrudedMesh)
            {
                em_center_list.Add(em.Item3);
            }
            var em_center_tree = new KDTree();
            em_center_tree.Build(em_center_list);

            var path_elements = GetSVGPathElements(svg_content);
            var parsed_data = ParseSVGPathElements(path_elements);
            var sorted_mesh = new List<Triple<Mesh, Bounds, Vector3>>();
            var sorted_color = new List<Color>();
            Debug.Log("Matching geometries");
            try
            {

                foreach (var element in parsed_data)
                {
                    List<int> res = new List<int>();
                    var q = new KDQuery();

                    if (element is BondDataSVG bond)
                    {
                        q.KNearest(em_center_tree, bond.Center, 1, res);
                        sorted_mesh.Add(extrudedMesh[res[0]]);
                        sorted_color.Add(geometry[res[0]].Color);
                    }
                    else if (element is AtomDataSVG atom)
                    {
                        Debug.Log($"ATOM CENTER: {atom.Center}");
                        q.KNearest(em_center_tree, atom.Center, 1, res);
                        Debug.Log($"ATOM RES: {res[0]}");
                        sorted_mesh.Add(extrudedMesh[res[0]]);
                        sorted_color.Add(geometry[res[0]].Color);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message} {e.StackTrace}");
            }


            Debug.Log($"parsed {parsed_data.Count} matched {sorted_mesh.Count}");
            //var total_bounds = new Bounds();
            //foreach (var exmesh in extrudedMesh)
            //{
            //    total_bounds.Encapsulate(exmesh.Item2);
            //}

            // Create a GameObject to display the extruded mesh
            var mat = Resources.Load<Material>("materials/sfExtrude");
            var mol2D = new GameObject($"extrude_{mol.name}").AddComponent<Molecule2D>();
            //mol2D.transform.Rotate(new Vector3(180f, 0f, 0f));
            //mol2D.transform.position = total_bounds.center;
            mol2D.molReference = mol;
            var atom2D_list = new List<Atom2D>();
            var bond_list = new List<Bond2D>();

            var symbol_object_list = new List<GameObject>();
            var symbol_point_list = new List<Vector3>();

            var bond_object_list = new List<GameObject>();
            var bond_point_list = new List<Vector3>();
            var bond_point_perp_list = new List<Vector3>();

            var bond_eigenvector_list = new List<Vector3>();
            var bond_eigenvalue_list = new List<float>();

            // sf coords tree
            Debug.Log("[StructureFormulaTo3D] Generating kd Tree ...");
            var sf_coords_tree = new KDTree();
            var atom_sf_coords = new List<Vector3>();
            var atom_sf_coords_2d = new List<Vector2>();
            var sf_coords_center = Vector3.zero;
            foreach (var atom in GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList)
            {
                atom_sf_coords.Add(atom.structure_coords);
                atom_sf_coords_2d.Add(atom.structure_coords);
                sf_coords_center += new Vector3(atom.structure_coords.x, atom.structure_coords.y, 0f);
            }
            sf_coords_tree.Build(atom_sf_coords);
            sf_coords_center /= GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList.Count;
            mol2D.transform.position = sf_coords_center;

            Debug.Log("[StructureFormulaTo3D] Parsing path elements...");
            var bond_data_collection = new List<BondDataCollection>();
            var atom_data_collection = new List<AtomDataCollection>();
            Debug.Log($"[StructureFormulaTo3D] Parsing complete. Found {parsed_data.Count} elements.");
            Debug.Log("[StructureFormulaTo3D] Filling Bond and Atom collections...");
            foreach (var (entry, i) in parsed_data.WithIndex())
            {
                var current_mesh = sorted_mesh[i];

                var child = new GameObject($"Mesh_{i}");
                child.transform.position = current_mesh.Item3;

                var meshFilter = child.AddComponent<MeshFilter>();
                var meshRenderer = child.AddComponent<MeshRenderer>();
                meshFilter.mesh = current_mesh.Item1;

                // material
                var current_mat = Instantiate(mat) as Material;
                current_mat.color = sorted_color[i];
                meshRenderer.material = current_mat;

                child.transform.parent = mol2D.transform;

                if (entry is BondDataSVG bond)
                {
                    var find = bond_data_collection.Find(x => x.BondId == bond.BondId);
                    if (find == null)
                    {
                        bond_data_collection.Add(new BondDataCollection());
                        find = bond_data_collection.Last();
                        find.BondId = bond.BondId;
                        find.AtomIds = bond.AtomIds.ToArray();
                        find.EndPoints = new List<Vector2[]> { bond.Endpoints };
                        find.elements = new List<GameObject> { child };
                    }
                    else
                    {
                        find.EndPoints.Add(bond.Endpoints);
                        find.elements.Add(child);
                    }
                    find.MeshData.Add(current_mesh);



                    //Debug.Log($"Bond ID: {bond.BondId}, Atom IDs: {string.Join(", ", bond.AtomIds)}");
                    //Debug.Log($"current Bond dict length: {bond_data_collection.Count}; current id {i}");
                }
                else if (entry is AtomDataSVG atom)
                {
                    var find = atom_data_collection.Find(x => x.AtomId == atom.AtomId);
                    if (find == null)
                    {
                        atom_data_collection.Add(new AtomDataCollection());
                        find = atom_data_collection.Last();
                        find.AtomId = atom.AtomId;
                        find.elements = new List<GameObject> { child };
                    }
                    else
                    {
                        find.elements.Add(child);
                    }
                    find.MeshData.Add(current_mesh);


                    //Debug.Log($"Atom ID: {atom.AtomId}");
                    //Debug.Log($"current aton dict length: {atom_data_collection.Count}; current id {i}");
                }
            }

            Debug.Log($"[StructureFormulaTo3D] Found {atom_data_collection.Count} Atom objects, and {bond_data_collection.Count} Bond objects in SVG.");

            Debug.Log("[StructureFormulaTo3D] Classifiying Bonds...");
            // bond classification
            foreach (var bond in bond_data_collection)
            {
                if (bond.MeshData.Count == 1)
                {
                    bond.BondType = BondType.SINGLE;
                    var offset1 = Vector2.Distance(atom_sf_coords[bond.AtomIds[0]], bond.EndPoints[0][0]);
                    var offset2 = Vector2.Distance(atom_sf_coords[bond.AtomIds[1]], bond.EndPoints[0][1]);
                    bond.Offsets = new float[] { offset1, offset2 };
                    bond.FinalEndPoints = bond.EndPoints[0];
                    bond.FinalCenter = bond.FinalEndPoints[0] + 0.5f * (bond.FinalEndPoints[1] - bond.FinalEndPoints[0]);
                }
                else if (bond.MeshData.Count == 2)
                {
                    //Debug.Log($"[StructureFormulaTo3D] Bond0 endpoints {bond.EndPoints[0][0]} {bond.EndPoints[0][1]}");
                    //Debug.Log($"[StructureFormulaTo3D] Bond1 endpoints {bond.EndPoints[1][0]} {bond.EndPoints[1][1]}");
                    //Debug.Log($"[StructureFormulaTo3D] Bond[0][0] endpoints {bond.EndPoints[0][0]} Atom[0] {atom_sf_coords_2d[bond.AtomIds[0]]}");
                    //Debug.Log($"[StructureFormulaTo3D] Bond[0][1] endpoints {bond.EndPoints[0][1]} Atom[1] {atom_sf_coords_2d[bond.AtomIds[1]]}");
                    if (bond.EndPoints[0][1] == bond.EndPoints[1][0]) // split bond
                    {
                        bond.BondType = BondType.SPLITSINGLE;
                        var offset1 = Vector2.Distance(atom_sf_coords[bond.AtomIds[0]], bond.EndPoints[0][0]);
                        var offset2 = Vector2.Distance(atom_sf_coords[bond.AtomIds[1]], bond.EndPoints[1][1]);
                        bond.Offsets = new float[] { offset1, offset2 };
                        bond.FinalEndPoints = new Vector2[] { bond.EndPoints[0][0], bond.EndPoints[1][1] };
                        bond.FinalCenter = bond.FinalEndPoints[0] + 0.5f * (bond.FinalEndPoints[1] - bond.FinalEndPoints[0]);
                    }
                    else if (Vector2.Distance(bond.EndPoints[0][0], atom_sf_coords_2d[bond.AtomIds[0]]) < 0.5f && Vector2.Distance(bond.EndPoints[0][1], atom_sf_coords_2d[bond.AtomIds[1]]) < 0.5f)
                    {
                        bond.BondType = BondType.RINGDOUBLE;
                        bond.Offsets = new float[] { 0f, 0f };
                        bond.FinalEndPoints = new Vector2[] { bond.EndPoints[0][0], bond.EndPoints[0][1] };
                        bond.FinalCenter = bond.FinalEndPoints[0] + 0.5f * (bond.FinalEndPoints[1] - bond.FinalEndPoints[0]);
                    }
                    else
                    {
                        bond.BondType = BondType.DOUBLE;
                        var ep1 = 0.5f * (bond.EndPoints[0][0] + bond.EndPoints[1][0]);
                        var ep2 = 0.5f * (bond.EndPoints[0][1] + bond.EndPoints[1][1]);
                        bond.FinalEndPoints = new Vector2[] { ep1, ep2 };
                        var offset1 = Vector2.Distance(atom_sf_coords[bond.AtomIds[0]], bond.FinalEndPoints[0]);
                        var offset2 = Vector2.Distance(atom_sf_coords[bond.AtomIds[1]], bond.FinalEndPoints[1]);
                        bond.Offsets = new float[] { offset1, offset2 };
                        bond.FinalCenter = bond.FinalEndPoints[0] + 0.5f * (bond.FinalEndPoints[1] - bond.FinalEndPoints[0]);
                    }
                }
                else if (bond.MeshData.Count == 4)
                {
                    if (bond.EndPoints[0][1] == bond.EndPoints[1][0] && bond.EndPoints[2][1] == bond.EndPoints[3][0])
                    {
                        bond.BondType = BondType.SPLITDOUBLE;
                        var ep1 = 0.5f * (bond.EndPoints[0][0] + bond.EndPoints[2][0]);
                        var ep2 = 0.5f * (bond.EndPoints[1][1] + bond.EndPoints[3][1]);
                        bond.FinalEndPoints = new Vector2[] { ep1, ep2 };
                        var offset1 = Vector2.Distance(atom_sf_coords[bond.AtomIds[0]], bond.FinalEndPoints[0]);
                        var offset2 = Vector2.Distance(atom_sf_coords[bond.AtomIds[1]], bond.FinalEndPoints[1]);
                        bond.Offsets = new float[] { offset1, offset2 };
                        bond.FinalCenter = bond.FinalEndPoints[0] + 0.5f * (bond.FinalEndPoints[1] - bond.FinalEndPoints[0]);
                    }
                    else
                    {
                        bond.BondType = BondType.DASHEDWEDGE;
                        var ep1 = atom_sf_coords_2d[bond.AtomIds[0]];
                        var ep2 = 0.5f * (bond.EndPoints.Last()[0] + bond.EndPoints.Last()[1]);
                        bond.FinalEndPoints = new Vector2[] { ep1, ep2 };
                        var offset2 = Vector2.Distance(atom_sf_coords[bond.AtomIds[1]], bond.FinalEndPoints[1]);
                        bond.Offsets = new float[] { 0f, offset2 };
                        bond.FinalCenter = bond.FinalEndPoints[0] + 0.5f * (bond.FinalEndPoints[1] - bond.FinalEndPoints[0]);
                    }
                }
                else
                {
                    bond.BondType = BondType.DASHEDWEDGE;
                    var ep1 = atom_sf_coords_2d[bond.AtomIds[0]];
                    var ep2 = 0.5f * (bond.EndPoints.Last()[0] + bond.EndPoints.Last()[1]);
                    bond.FinalEndPoints = new Vector2[] { ep1, ep2 };
                    var offset2 = Vector2.Distance(atom_sf_coords[bond.AtomIds[1]], bond.FinalEndPoints[1]);
                    bond.Offsets = new float[] { 0f, offset2 };
                    bond.FinalCenter = bond.FinalEndPoints[0] + 0.5f * (bond.FinalEndPoints[1] - bond.FinalEndPoints[0]);
                }
                Debug.Log($"[StructureFormulaTo3D] Detected {bond.BondType}");
            }

            Debug.Log("[StructureFormulaTo3D] Processing Atoms...");
            // process atoms
            for (int i = 0; i < mol.atomList.Count; i++)
            {
                var atom = mol.atomList[i];

                var find = atom_data_collection.Find(x => x.AtomId == i);
                if (find != null)
                {
                    var atom2D = new GameObject().AddComponent<Atom2D>();
                    atom2D.transform.parent = mol2D.transform;
                    atom2D.transform.position = atom.structure_coords;
                    foreach (var element in find.elements)
                    {
                        element.transform.parent = atom2D.transform;
                    }
                    //atom2D.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                    var ref_atom = mol.atomList[i];
                    atom2D.name = ref_atom.name;
                    atom2D.atomReference = ref_atom;
                    atom2D_list.Add(atom2D);
                }
                else
                {
                    var node_atom = new GameObject().AddComponent<Atom2D>();
                    node_atom.transform.parent = mol2D.transform;

                    var ref_atom = mol.atomList[i];
                    node_atom.name = ref_atom.name;
                    node_atom.atomReference = ref_atom;
                    node_atom.transform.position = ref_atom.structure_coords;
                    atom2D_list.Add(node_atom);
                }
            }

            // sort atom list and add to mol2d
            var atom2D_list_sorted = new List<Atom2D>();
            foreach (var atom in mol.atomList)
            {
                var match = atom2D_list.Find(a => a.atomReference == atom);
                atom2D_list_sorted.Add(match);
            }
            mol2D.Atoms = atom2D_list_sorted;

            Debug.Log("[StructureFormulaTo3D] Processing Bonds...");
            foreach (var bond in bond_data_collection)
            {
                var bond_obj = new GameObject("Bond");
                bond_obj.transform.position = bond.FinalCenter;

                var look_at_endpoint = bond.FinalEndPoints[0].y > bond.FinalEndPoints[1].y ? bond.FinalEndPoints[0] : bond.FinalEndPoints[1];
                var lower_at_endpoint = bond.FinalEndPoints[0].y > bond.FinalEndPoints[1].y ? bond.FinalEndPoints[1] : bond.FinalEndPoints[0];
                var target_direction = look_at_endpoint - lower_at_endpoint;
                // Normalize the target direction to avoid scaling issues
                target_direction.Normalize();

                // Check if the forward vector is almost aligned with the up vector
                if (Mathf.Abs(Vector3.Dot(target_direction, Vector3.up)) > 0.99f)
                {
                    // If almost aligned with up, use a different up vector to avoid gimbal lock
                    Vector3 alternativeUp = Vector3.Cross(target_direction, Vector3.right);

                    // If the cross product gives a zero vector (targetDirection is parallel to right), use world forward instead
                    if (alternativeUp.sqrMagnitude < 0.001f)
                    {
                        alternativeUp = Vector3.Cross(target_direction, Vector3.forward);
                    }

                    // Set the rotation using the alternative up vector
                    bond_obj.transform.rotation = Quaternion.LookRotation(target_direction, alternativeUp);
                    bond_obj.transform.Rotate(Vector3.forward, 90f);
                }
                else
                {
                    // Align normally with the world up vector
                    bond_obj.transform.rotation = Quaternion.LookRotation(target_direction, Vector3.up);
                }
                bond_obj.transform.Rotate(Vector3.forward, 180f);
                // orient the empty object along length of bond
                //b2d_obj.transform.LookAt(look_at_endpoint);


                // make sure the rotation along the z-axis is well defined
                //if (b2d_obj.transform.eulerAngles.x.approx(0f, 1f))
                //{
                //    Vector3 perp_vector = bond_point_perp_list[final_ep_ids[0]] - bond_point_perp_list[final_ep_ids[1]];
                //    b2d_obj.transform.Rotate(Vector3.forward, -Vector3.Angle(b2d_obj.transform.up, perp_vector));
                //}
                //if (Mathf.Abs(b2d_obj.transform.forward.y).approx(1f))
                //{
                //    b2d_obj.transform.Rotate(Vector3.forward, 90f);
                //}

                // put bond into empty object for correct pivot and orientation
                bond_obj.transform.parent = mol2D.transform;
                foreach (var obj in bond.elements)
                {
                    obj.transform.parent = bond_obj.transform;
                }

                var b2d = bond_obj.AddComponent<Bond2D>();

                b2d.atom1ref = mol.atomList[bond.AtomIds[0]];
                b2d.atom1 = mol2D.Atoms[bond.AtomIds[0]];
                b2d.atom1ConnectionOffset = bond.Offsets[0];
                b2d.end1 = bond.FinalEndPoints[0];

                b2d.atom2ref = mol.atomList[bond.AtomIds[1]];
                b2d.atom2 = mol2D.Atoms[bond.AtomIds[1]];
                b2d.atom2ConnectionOffset = bond.Offsets[1];
                b2d.end2 = bond.FinalEndPoints[1];

                b2d.bondReference = b2d.atom1ref.getBond(b2d.atom2ref);
                var length = Vector3.Distance(bond.FinalEndPoints[0], bond.FinalEndPoints[1]);
                b2d.initialLength = length;
                b2d.initialLookAt = b2d.atom1.transform.position.y > b2d.atom2.transform.position.y ? b2d.atom1 : b2d.atom2;

                bond_list.Add(b2d);
            }

            mol2D.bonds = bond_list;
            mol2D.transform.Rotate(new Vector3(180f, 0f, 0f));

            mol2D.transform.localScale = 0.002f * Vector3.one;
            mol2D.transform.position = mol.transform.position;//GlobalCtrl.Singleton.getCurrentSpawnPos();

            mol2D.transform.parent = GlobalCtrl.Singleton.atomWorld.transform;
            mol2D.transform.rotation = GlobalCtrl.Singleton.currentCamera.transform.rotation;
            mol2D.transform.Rotate(Vector3.right, 180f);
            mol2D.align3D();

            Molecule2D.molecules.Add(mol2D);

        }

        public static void generate3DRepresentationGeometryBased(List<VectorUtils.Geometry> geometry, Guid mol_id)
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
            //var total_bounds = new Bounds();
            //foreach (var exmesh in extrudedMesh)
            //{
            //    total_bounds.Encapsulate(exmesh.Item2);
            //}

            // Create a GameObject to display the extruded mesh
            var mat = Resources.Load<Material>("materials/sfExtrude");
            var mol2D = new GameObject($"extrude_{mol.name}").AddComponent<Molecule2D>();
            //mol2D.transform.Rotate(new Vector3(180f, 0f, 0f));
            //mol2D.transform.position = total_bounds.center;
            mol2D.molReference = mol;

            var symbol_object_list = new List<GameObject>();
            var symbol_point_list = new List<Vector3>();

            var bond_object_list = new List<GameObject>();
            var bond_point_list = new List<Vector3>();
            var bond_point_perp_list = new List<Vector3>();

            var bond_eigenvector_list = new List<Vector3>();
            var bond_eigenvalue_list = new List<float>();

            // sf coords tree
            var sf_coords_tree = new KDTree();
            var atom_sf_coords = new List<Vector3>();
            var sf_coords_center = Vector3.zero;
            foreach (var atom in GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList)
            {
                atom_sf_coords.Add(atom.structure_coords);
                sf_coords_center += new Vector3(atom.structure_coords.x, atom.structure_coords.y, 0f);
            }
            sf_coords_tree.Build(atom_sf_coords);
            sf_coords_center /= GlobalCtrl.Singleton.List_curMolecules[mol_id].atomList.Count;
            mol2D.transform.position = sf_coords_center;

            var list_uncategorized_small = new List<Transform>();
            var list_uncategorized_large = new List<Transform>();
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
                Vector2 startPoint, endPoint, startPointPerp, endPointPerp;
                PrincipalAxis2D.GetEigenCenterEndpoints(child.transform, out eigenvalue1, out eigenvalue2, out eigenvector1, out eigenvector2, out centroid, out startPoint, out endPoint, out startPointPerp, out endPointPerp);


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

                    var q = new KDQuery();
                    var res_start = new List<int>();
                    var dist_start = new List<float>();
                    q.KNearest(sf_coords_tree, startPoint, 1, res_start, dist_start);
                    var res_end = new List<int>();
                    var dist_end = new List<float>();
                    q.KNearest(sf_coords_tree, endPoint, 1, res_end, dist_end);

                    bool add = true;
                    if (Vector3.Distance(startPoint, endPoint) < 5f)
                    {
                        list_uncategorized_small.Add(child.transform);
                        add = false;
                    }
                    if (dist_start[0] > 1f && dist_end[0] > 1f &&
                        res_start[0] != res_end[0] &&
                        Vector3.Distance(startPoint, endPoint) > 20f) // checking for lines inside ring
                    {
                        list_uncategorized_large.Add(child.transform);
                        add = false;
                    }
                    if (add)
                    {
                        bond_object_list.Add(child);
                        bond_object_list.Add(child);
                        bond_point_list.Add(startPoint);
                        bond_point_list.Add(endPoint);
                        bond_point_perp_list.Add(startPointPerp);
                        bond_point_perp_list.Add(endPointPerp);
                        bond_eigenvalue_list.Add(eigenvalue1);
                        bond_eigenvalue_list.Add(eigenvalue2);
                        bond_eigenvector_list.Add(eigenvector1);
                        bond_eigenvector_list.Add(eigenvector2);
                    }
                    else
                    {
                        Debug.Log($"[Filter] Filtered {child.name}");
                    }

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
            var split_lines_ids = new HashSet<Tuple<int, int>>();
            var nodes_ids = new List<List<int>>();
            var nodes_objects = new List<List<GameObject>>();

            // check for nodes or split bonds
            foreach (var (endpoint, current_id) in bond_point_list.WithIndex())
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
                        var sf_query = new KDQuery();
                        var res = new List<int>();
                        sf_query.Radius(sf_coords_tree, endpoint, 8f, res); // check for connected double bond
                        Debug.Log($"[splitLineOrNode] res count {res.Count}");
                        if (res.Count == 0)
                        {
                            split_lines_ids.Add(new Tuple<int, int>(results[0], results[1]));
                        }
                        else
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

            // create atoms at nodes
            foreach (var nd_id in nodes_ids)
            {
                var node_atom = new GameObject().AddComponent<Atom2D>();
                node_atom.transform.parent = mol2D.transform;
                List<int> res = new List<int>();
                var q = new KDQuery();
                q.KNearest(sf_coords_tree, bond_point_list[nd_id[0]], 1, res);
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
            mol2D.Atoms = atom2D_list_sorted;


            // process split bonds
            var bond_list = new List<Bond2D>();
            List<GameObject> merge_bond_list = new List<GameObject>();
            var merge_bond_end_points = new List<Tuple<Vector3, Vector3>>();
            foreach (var sl in split_lines_ids)
            {
                var endpoint_ids_obj1 = bond_object_list.GetAllIndicesOf(bond_object_list[sl.Item1]);
                var endpoint_ids_obj2 = bond_object_list.GetAllIndicesOf(bond_object_list[sl.Item2]);
                if (endpoint_ids_obj1.Count != 2 && endpoint_ids_obj2.Count != 2)
                {
                    Debug.LogError("[MergeHalfBonds] Could not retreive all end points.");
                    return;
                }

                Tuple<Vector3, Vector3> feps;
                Tuple<int, int> fep_ids;
                if (Vector3.Distance(bond_point_list[endpoint_ids_obj1[0]], bond_point_list[endpoint_ids_obj2[0]]) < 0.1f)
                {
                    feps = new Tuple<Vector3, Vector3>(bond_point_list[endpoint_ids_obj1[1]], bond_point_list[endpoint_ids_obj2[1]]);
                    fep_ids = new Tuple<int, int>(endpoint_ids_obj1[1], endpoint_ids_obj2[1]);
                    //finalEndPoint_ids.Add(endpoint_ids_obj1[1]);
                    //finalEndPoint_ids.Add(endpoint_ids_obj2[1]);
                }
                else if (Vector3.Distance(bond_point_list[endpoint_ids_obj1[0]], bond_point_list[endpoint_ids_obj2[1]]) < 0.1f)
                {
                    feps = new Tuple<Vector3, Vector3>(bond_point_list[endpoint_ids_obj1[1]], bond_point_list[endpoint_ids_obj2[0]]);
                    fep_ids = new Tuple<int, int>(endpoint_ids_obj1[1], endpoint_ids_obj2[0]);
                    //finalEndPoint_ids.Add(endpoint_ids_obj1[1]);
                    //finalEndPoint_ids.Add(endpoint_ids_obj2[0]);
                }
                else if (Vector3.Distance(bond_point_list[endpoint_ids_obj1[1]], bond_point_list[endpoint_ids_obj2[0]]) < 0.1f)
                {
                    feps = new Tuple<Vector3, Vector3>(bond_point_list[endpoint_ids_obj1[0]], bond_point_list[endpoint_ids_obj2[1]]);
                    fep_ids = new Tuple<int, int>(endpoint_ids_obj1[0], endpoint_ids_obj2[1]);
                    //finalEndPoint_ids.Add(endpoint_ids_obj1[0]);
                    //finalEndPoint_ids.Add(endpoint_ids_obj2[1]);
                }
                else
                {
                    feps = new Tuple<Vector3, Vector3>(bond_point_list[endpoint_ids_obj1[0]], bond_point_list[endpoint_ids_obj2[0]]);
                    fep_ids = new Tuple<int, int>(endpoint_ids_obj1[0], endpoint_ids_obj2[0]);
                    //finalEndPoint_ids.Add(endpoint_ids_obj1[0]);
                    //finalEndPoint_ids.Add(endpoint_ids_obj2[0]);
                }
                merge_bond_end_points.Add(feps);

                var merge_bond = new GameObject("Bond");
                //merge_bond.transform.position = bond_point_list[sl.Item1];
                merge_bond.transform.position = (feps.Item1 + feps.Item2) / 2f;
                var look_at_endpoint = feps.Item1.y > feps.Item2.y ? feps.Item1 : feps.Item2;
                var lower_at_endpoint = feps.Item1.y > feps.Item2.y ? feps.Item2 : feps.Item1;
                var target_direction = look_at_endpoint - lower_at_endpoint;
                // Normalize the target direction to avoid scaling issues
                target_direction.Normalize();

                // Check if the forward vector is almost aligned with the up vector
                if (Mathf.Abs(Vector3.Dot(target_direction, Vector3.up)) > 0.99f)
                {
                    // If almost aligned with up, use a different up vector to avoid gimbal lock
                    Vector3 alternativeUp = Vector3.Cross(target_direction, Vector3.right);

                    // If the cross product gives a zero vector (targetDirection is parallel to right), use world forward instead
                    if (alternativeUp.sqrMagnitude < 0.001f)
                    {
                        alternativeUp = Vector3.Cross(target_direction, Vector3.forward);
                    }

                    // Set the rotation using the alternative up vector
                    merge_bond.transform.rotation = Quaternion.LookRotation(target_direction, alternativeUp);
                    merge_bond.transform.Rotate(Vector3.forward, 90f);
                }
                else
                {
                    // Align normally with the world up vector
                    merge_bond.transform.rotation = Quaternion.LookRotation(target_direction, Vector3.up);
                }
                merge_bond.transform.Rotate(Vector3.forward, 180f);

                // orient the empty object along length of bond
                //merge_bond.transform.LookAt(look_at_endpoint);

                // make sure the rotation along the z-axis is well defined
                //if (merge_bond.transform.eulerAngles.x.approx(0f, 1f))
                //{
                //    Vector3 perp_vector = bond_point_perp_list[fep_ids.Item1] - bond_point_perp_list[fep_ids.Item2];
                //    merge_bond.transform.Rotate(Vector3.forward, -Vector3.Angle(merge_bond.transform.up, perp_vector));
                //}


                //if (Mathf.Abs(merge_bond.transform.forward.y).approx(1f, 0.1f))
                //{
                //    merge_bond.transform.Rotate(Vector3.forward, 90f);
                //}

                // put bond into empty object for correct pivot and orientation
                merge_bond.transform.parent = mol2D.transform;
                bond_object_list[sl.Item1].transform.parent = merge_bond.transform;
                bond_object_list[sl.Item2].transform.parent = merge_bond.transform;

                merge_bond_list.Add(merge_bond);


                

            }


            // process simple bonds
            var processed_objects = new List<GameObject>();
            var simple_bond_list = new List<GameObject>();
            var simple_bond_end_points = new List<Tuple<Vector3, Vector3>>();
            foreach (var ep_id in endpoints_ids)
            {
                var obj = bond_object_list[ep_id];
                bool skip = false;

                if (processed_objects.Contains(obj) || merge_bond_list.ContainedInChildren(obj)) skip = true;

                if (!skip)
                {
                    Debug.Log($"[SimpleBond] processing {ep_id}");
                    var final_ep_ids = bond_object_list.GetAllIndicesOf(obj);
                    var current_eps = new Tuple<Vector3, Vector3>(bond_point_list[final_ep_ids[0]], bond_point_list[final_ep_ids[1]]);
                    simple_bond_end_points.Add(current_eps);
                    //Debug.Log($"[SimpleBonds] final end point ids {final_ep_ids.ToArray().Print()}");
                    //var b2d = obj.AddComponent<Bond2D>();
                    var b2d_obj = new GameObject("Bond");
                    b2d_obj.transform.position = obj.transform.position;

                    var look_at_endpoint = current_eps.Item1.y > current_eps.Item2.y ? current_eps.Item1 : current_eps.Item2;
                    var lower_at_endpoint = current_eps.Item1.y > current_eps.Item2.y ? current_eps.Item2 : current_eps.Item1;
                    var target_direction = look_at_endpoint - lower_at_endpoint;
                    // Normalize the target direction to avoid scaling issues
                    target_direction.Normalize();

                    // Check if the forward vector is almost aligned with the up vector
                    if (Mathf.Abs(Vector3.Dot(target_direction, Vector3.up)) > 0.99f)
                    {
                        // If almost aligned with up, use a different up vector to avoid gimbal lock
                        Vector3 alternativeUp = Vector3.Cross(target_direction, Vector3.right);

                        // If the cross product gives a zero vector (targetDirection is parallel to right), use world forward instead
                        if (alternativeUp.sqrMagnitude < 0.001f)
                        {
                            alternativeUp = Vector3.Cross(target_direction, Vector3.forward);
                        }

                        // Set the rotation using the alternative up vector
                        b2d_obj.transform.rotation = Quaternion.LookRotation(target_direction, alternativeUp);
                        b2d_obj.transform.Rotate(Vector3.forward, 90f);
                    }
                    else
                    {
                        // Align normally with the world up vector
                        b2d_obj.transform.rotation = Quaternion.LookRotation(target_direction, Vector3.up);
                    }
                    b2d_obj.transform.Rotate(Vector3.forward, 180f);
                    // orient the empty object along length of bond
                    //b2d_obj.transform.LookAt(look_at_endpoint);


                    // make sure the rotation along the z-axis is well defined
                    //if (b2d_obj.transform.eulerAngles.x.approx(0f, 1f))
                    //{
                    //    Vector3 perp_vector = bond_point_perp_list[final_ep_ids[0]] - bond_point_perp_list[final_ep_ids[1]];
                    //    b2d_obj.transform.Rotate(Vector3.forward, -Vector3.Angle(b2d_obj.transform.up, perp_vector));
                    //}
                    //if (Mathf.Abs(b2d_obj.transform.forward.y).approx(1f))
                    //{
                    //    b2d_obj.transform.Rotate(Vector3.forward, 90f);
                    //}

                    // put bond into empty object for correct pivot and orientation
                    b2d_obj.transform.parent = mol2D.transform;
                    obj.transform.parent = b2d_obj.transform;

                    processed_objects.Add(obj);
                    simple_bond_list.Add(b2d_obj);
                }
            }


            // check for double bonds
            var to_remove_mb = new List<GameObject>();
            var to_remove_fep = new List<Tuple<Vector3, Vector3>>();
            var double_bond_list = new List<GameObject>();
            var double_bond_end_points = new List<Tuple<Vector3, Vector3>>();
            foreach (var (mb, mb_id) in merge_bond_list.WithIndex())
            {
                var direction = merge_bond_end_points[mb_id].Item1 - merge_bond_end_points[mb_id].Item2;

                foreach (var (other_mb, other_mb_id) in merge_bond_list.WithIndex())
                {
                    if (other_mb != mb && !to_remove_mb.Contains(other_mb) && !to_remove_mb.Contains(mb))
                    {
                        var bond_direction = merge_bond_end_points[other_mb_id].Item1 - merge_bond_end_points[other_mb_id].Item2;
                        if (Mathf.Abs(Vector3.Dot(bond_direction.normalized, direction.normalized)).approx(1f, 0.001f) &&
                            Vector3.Distance(mb.transform.position, other_mb.transform.position) < 10f)
                        {
                            to_remove_mb.Add(mb);
                            to_remove_mb.Add(other_mb);
                            to_remove_fep.Add(merge_bond_end_points[mb_id]);
                            to_remove_fep.Add(merge_bond_end_points[other_mb_id]);
                            var double_bond = new GameObject("DoubleBond");
                            double_bond.transform.position = 0.5f * (mb.transform.position + other_mb.transform.position);
                            double_bond.transform.rotation = mb.transform.rotation;

                            double_bond.transform.parent = mol2D.transform;
                            mb.transform.parent = double_bond.transform;
                            other_mb.transform.parent = double_bond.transform;
                            double_bond.transform.Rotate(Vector3.forward, 180f);

                            Debug.Log($"[DoubleBondEndPointAssignment] mb ({mb_id}) {merge_bond_end_points[mb_id].ToString()}  other mb ({other_mb_id}) {merge_bond_end_points[other_mb_id].ToString()}");

                            // determine which end points belong together
                            Vector3 start_point;
                            Vector3 end_point;
                            if (Vector3.Distance(merge_bond_end_points[mb_id].Item1, merge_bond_end_points[other_mb_id].Item1) < Vector3.Distance(merge_bond_end_points[mb_id].Item1, merge_bond_end_points[other_mb_id].Item2))
                            {
                                start_point = 0.5f * (merge_bond_end_points[mb_id].Item1 + merge_bond_end_points[other_mb_id].Item1);
                                end_point = 0.5f * (merge_bond_end_points[mb_id].Item2 + merge_bond_end_points[other_mb_id].Item2);
                            }
                            else
                            {
                                start_point = 0.5f * (merge_bond_end_points[mb_id].Item1 + merge_bond_end_points[other_mb_id].Item2);
                                end_point = 0.5f * (merge_bond_end_points[mb_id].Item2 + merge_bond_end_points[other_mb_id].Item1);
                            }

                            double_bond_end_points.Add(new Tuple<Vector3, Vector3>(start_point, end_point));
                            double_bond_list.Add(double_bond);
                        }
                    }
                }
            }
            // remove double bonds from merge_bond list
            for (int i = 0; i < to_remove_mb.Count; i++)
            {
                merge_bond_list.Remove(to_remove_mb[i]);
                merge_bond_end_points.Remove(to_remove_fep[i]);
            }


            // cereate complete list of end points from processed bonds
            var new_endpoints = new List<Vector3>();
            foreach (var db_ep in double_bond_end_points)
            {
                new_endpoints.Add(db_ep.Item1);
                new_endpoints.Add(db_ep.Item2);
            }
            foreach (var mb_ep in merge_bond_end_points)
            {
                new_endpoints.Add(mb_ep.Item1);
                new_endpoints.Add(mb_ep.Item2);
            }
            foreach (var sb_ep in simple_bond_end_points)
            {
                new_endpoints.Add(sb_ep.Item1);
                new_endpoints.Add(sb_ep.Item2);
            }

            //var ep_tree = new KDTree();
            //ep_tree.Build(new_endpoints);
            //nodes_ids.Clear();
            //foreach (var (ep, ep_id) in new_endpoints.WithIndex())
            //{
            //    var skip = false;
            //    foreach (var n_entry in nodes_ids)
            //    {
            //        if (n_entry.Contains(ep_id)) skip = true;
            //    }
            //    if (!skip) // prevent duplicates
            //    {
            //        var results = new List<int>();
            //        query.Radius(ep_tree, ep, 2f, results);
            //        nodes_ids.Add(results);
            //        if (results.Count > 2) // carbon atom
            //        {
            //            var node_atom = new GameObject().AddComponent<Atom2D>();
            //            node_atom.transform.parent = mol2D.transform;
            //            List<int> res = new List<int>();
            //            query.KNearest(sf_coords_tree, ep, 1, res);
            //            var ref_atom = mol.atomList[res[0]];
            //            node_atom.name = ref_atom.name;
            //            node_atom.atomReference = ref_atom;
            //            node_atom.transform.position = ref_atom.structure_coords;
            //            atom2D_list.Add(node_atom);
            //        }
            //    }
            //}

            //// sort atom list and add to mol2d
            //var atom2D_list_sorted = new List<Atom2D>();
            //foreach (var atom in mol.atomList)
            //{
            //    var match = atom2D_list.Find(a => a.atomReference == atom);
            //    atom2D_list_sorted.Add(match);
            //}
            //mol2D.atoms = atom2D_list_sorted;


            // Find connected atoms for double bonds
            foreach (var (db, db_id) in double_bond_list.WithIndex())
            {
                var b2d = db.AddComponent<Bond2D>();
                var length = Vector3.Distance(double_bond_end_points[db_id].Item1, double_bond_end_points[db_id].Item2);

                Debug.Log($"[DoubleBondAtomAssignment] end points {double_bond_end_points[db_id].ToString()}");

                List<int> res = new List<int>();
                var q = new KDQuery();
                q.KNearest(sf_coords_tree, double_bond_end_points[db_id].Item1, 1, res);
                b2d.atom1ref = mol.atomList[res[0]];
                b2d.atom1 = mol2D.Atoms[res[0]];
                b2d.atom1ConnectionOffset = Vector3.Distance(double_bond_end_points[db_id].Item1, atom_sf_coords[res[0]]);
                b2d.end1 = double_bond_end_points[db_id].Item1;

                res.Clear();
                q.KNearest(sf_coords_tree, double_bond_end_points[db_id].Item2, 1, res);
                b2d.atom2ref = mol.atomList[res[0]];
                b2d.atom2 = mol2D.Atoms[res[0]];
                b2d.atom2ConnectionOffset = Vector3.Distance(double_bond_end_points[db_id].Item2, atom_sf_coords[res[0]]);
                b2d.end2 = double_bond_end_points[db_id].Item2;

                b2d.bondReference = b2d.atom1ref.getBond(b2d.atom2ref);
                b2d.initialLength = length;
                b2d.initialLookAt = b2d.atom1.transform.position.y > b2d.atom2.transform.position.y ? b2d.atom1 : b2d.atom2;

                bond_list.Add(b2d);
            }

            // Find connected atoms
            foreach (var (mb, mb_id) in merge_bond_list.WithIndex())
            {
                var b2d = mb.AddComponent<Bond2D>();
                var length = Vector3.Distance(merge_bond_end_points[mb_id].Item1, merge_bond_end_points[mb_id].Item2);

                Debug.Log($"[MergeBond] length: {length}");

                List<int> res = new List<int>();
                query.KNearest(sf_coords_tree, merge_bond_end_points[mb_id].Item1, 1, res);
                b2d.atom1ref = mol.atomList[res[0]];
                b2d.atom1 = mol2D.Atoms[res[0]];
                b2d.atom1ConnectionOffset = Vector3.Distance(merge_bond_end_points[mb_id].Item1, atom_sf_coords[res[0]]);
                b2d.end1 = merge_bond_end_points[mb_id].Item1;

                res.Clear();
                query.KNearest(sf_coords_tree, merge_bond_end_points[mb_id].Item2, 1, res);
                b2d.atom2ref = mol.atomList[res[0]];
                b2d.atom2 = mol2D.Atoms[res[0]];
                b2d.atom2ConnectionOffset = Vector3.Distance(merge_bond_end_points[mb_id].Item2, atom_sf_coords[res[0]]);
                b2d.end2 = merge_bond_end_points[mb_id].Item2;

                b2d.bondReference = b2d.atom1ref.getBond(b2d.atom2ref);
                b2d.initialLength = length;
                b2d.initialLookAt = b2d.atom1.transform.position.y > b2d.atom2.transform.position.y ? b2d.atom1 : b2d.atom2;

                bond_list.Add(b2d);
            }

            foreach (var (sb, sb_id) in simple_bond_list.WithIndex())
            {
                var b2d = sb.AddComponent<Bond2D>();
                // find connected atoms

                List<int> res = new List<int>();
                query.KNearest(sf_coords_tree, simple_bond_end_points[sb_id].Item1, 1, res);


                b2d.atom1ref = mol.atomList[res[0]];
                b2d.atom1 = mol2D.Atoms[res[0]];
                b2d.atom1ConnectionOffset = Vector3.Distance(simple_bond_end_points[sb_id].Item1, atom_sf_coords[res[0]]);
                b2d.end1 = simple_bond_end_points[sb_id].Item1;

                res.Clear();
                query.KNearest(sf_coords_tree, simple_bond_end_points[sb_id].Item2, 1, res);
                b2d.atom2ref = mol.atomList[res[0]];
                b2d.atom2 = mol2D.Atoms[res[0]];
                b2d.atom2ConnectionOffset = Vector3.Distance(simple_bond_end_points[sb_id].Item2, atom_sf_coords[res[0]]);
                b2d.end2 = simple_bond_end_points[sb_id].Item2;

                b2d.bondReference = b2d.atom1ref.getBond(b2d.atom2ref);
                var length = Vector3.Distance(simple_bond_end_points[sb_id].Item1, simple_bond_end_points[sb_id].Item2);
                b2d.initialLength = length;
                b2d.initialLookAt = b2d.atom1.transform.position.y > b2d.atom2.transform.position.y ? b2d.atom1 : b2d.atom2;

                bond_list.Add(b2d);
            }

            // build tree for categorized items
            var pos_list = new List<Vector3>();
            var trans_list = new List<Transform>();
            foreach (Transform t in mol2D.transform)
            {
                if (!t.name.Contains("Mesh_"))
                {
                    trans_list.Add(t);
                    pos_list.Add(t.position);
                }
            }

            var categorized_objects_tree = new KDTree();
            categorized_objects_tree.Build(pos_list);

            foreach (var t in list_uncategorized_small)
            {
                var q = new KDQuery();
                var res = new List<int>();
                q.KNearest(categorized_objects_tree, t.position, 2, res);
                Transform new_parent;
                if (trans_list[res[0]].GetComponent<Atom2D>() == null)
                {
                    if (trans_list[res[1]].GetComponent<Atom2D>() == null)
                    {
                        new_parent = trans_list[res[0]];
                    }
                    else
                    {
                        new_parent = trans_list[res[1]];
                    }
                }
                else
                {
                    new_parent = trans_list[res[0]];
                }
                t.parent = new_parent;
            }
            foreach (var t in list_uncategorized_large)
            {
                var q = new KDQuery();
                var res = new List<int>();
                q.KNearest(categorized_objects_tree, t.position, 1, res);
                t.parent = trans_list[res[0]];
            }

            // second pass: find still uncategorized items and assign them
            // find unprocessed items and attach them to the closest obejct
            var left_overs = new List<Transform>();
            foreach (Transform t in mol2D.transform)
            {
                if (t.name.Contains("Mesh_"))
                {
                    left_overs.Add(t);
                }
            }
            foreach (var t in left_overs)
            {
                var q = new KDQuery();
                var res = new List<int>();
                q.KNearest(categorized_objects_tree, t.position, 2, res);
                Transform new_parent;
                if (trans_list[res[0]].GetComponent<Atom2D>() == null)
                {
                    if (trans_list[res[1]].GetComponent<Atom2D>() == null)
                    {
                        new_parent = trans_list[res[0]];
                    }
                    else
                    {
                        new_parent = trans_list[res[1]];
                    }
                }
                else
                {
                    new_parent = trans_list[res[0]];
                }
                t.parent = new_parent;
            }


            mol2D.bonds = bond_list;
            mol2D.transform.Rotate(new Vector3(180f, 0f, 0f));

            mol2D.transform.localScale = 0.002f * Vector3.one;
            mol2D.transform.position = mol.transform.position;//GlobalCtrl.Singleton.getCurrentSpawnPos();

            mol2D.transform.parent = GlobalCtrl.Singleton.atomWorld.transform;
            mol2D.align3D();

            Molecule2D.molecules.Add(mol2D);
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
}
