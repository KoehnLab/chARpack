using System;
using System.IO;
using UnityEngine;
using Python.Runtime;
using System.Collections.Generic;
using System.Collections;
using IngameDebugConsole;

namespace chARpack
{
    public class StructureFormulaGenerator : MonoBehaviour
    {

        private static StructureFormulaGenerator _singleton;

        public static StructureFormulaGenerator Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                {
                    _singleton = value;
                }
                else if (_singleton != value)
                {
                    Debug.Log($"[{nameof(StructureFormulaGenerator)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }

            }
        }

        private void Awake()
        {
            Singleton = this;
        }

#if UNITY_STANDALONE || UNITY_EDITOR
        void Start()
        {
            EventManager.Singleton.OnMolDataChanged += requestStructureFormula;
            if (SettingsData.autogenerateStructureFormulas)
            {
                EventManager.Singleton.OnMoleculeLoaded += immediateRequestStructureFormula;
            }
            DebugLogConsole.AddCommand("generate3Dformula", "Generate mesh from 2D representation", generate3DfromSelected);
        }

        public void requestStructureFormula(Molecule mol)
        {
            if (StructureFormulaManager.Singleton.svg_instances.ContainsKey(mol.m_id))
            {
                StartCoroutine(waitAndGenerate(mol));
            }
        }

        public void immediateRequestStructureFormula(Molecule mol)
        {
            generate(mol);
        }

        private IEnumerator waitAndGenerate(Molecule mol)
        {
            yield return new WaitForSeconds(1f); // wait for relaxation of the molecule
            generate(mol);
        }

        private void generate3DfromSelected()
        {
            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
            {
                if ( mol.isMarked ) 
                {
                    mol.markMolecule(false);
                    generate3D(mol);
                }
            }
        }

        private void generate3D(Molecule mol)
        {
            List<Vector2> coords;
            var svg_content = fetchSVGContent(mol, out coords);
            if (svg_content == "") return;

            // push 2D coords
            for (int i = 0; i < coords.Count; i++)
            {
                mol.atomList[i].structure_coords = coords[i];
            }

            // push content
            StructureFormulaTo3D.generateFromSVGContentUI(svg_content, mol.m_id, coords);
        }

        private void generate(Molecule mol)
        {
            List<Vector2> coords;
            var svg_content = fetchSVGContent(mol, out coords);
            if (svg_content == "") return;

            // push 2D coords
            for (int i = 0; i < coords.Count; i++)
            {
                mol.atomList[i].structure_coords = coords[i];
            }

            if (StructureFormulaManager.Singleton)
            {
                StructureFormulaManager.Singleton.pushContent(mol.m_id, svg_content);
            }
            else
            {
                Debug.LogError("[structureReceiveComplete] Could not find StructureFormulaManager");
                return;
            }

            //write svg to file
            var file_path = Path.Combine(Application.streamingAssetsPath, $"{svg_content.Length}.svg");
            if (File.Exists(file_path))
            {
                Debug.Log(file_path + " already exists.");
                return;
            }
            var sr = File.CreateText(file_path);
            sr.Write(svg_content);
            sr.Close();
        }


        private string fetchSVGContent(Molecule mol, out List<Vector2> coords)
        {
            coords = new List<Vector2>();
            if (!PythonEnvironmentManager.Singleton) return "";
            if (!PythonEnvironmentManager.Singleton.isInitialized) return "";
            // Prepare lists
            List<Vector3> posList = new List<Vector3>();
            for (int i = 0; i < mol.atomList.Count; i++)
            {
                var atom = mol.atomList[i];
                var pos = atom.transform.localPosition * GlobalCtrl.u2aa / GlobalCtrl.scale;
                posList.Add(pos);
            }

            string[] symbolList = new string[mol.atomList.Count];
            for (int i = 0; i < mol.atomList.Count; i++)
            {
                var atom = mol.atomList[i];
                if (atom.m_data.m_abbre.ToLower() == "dummy")
                {
                    symbolList[i] = "H";
                }
                else
                {
                    symbolList[i] = atom.m_data.m_abbre;
                }
            }

            // define outputs
            string svgContent = "";
            // Acquire the GIL before using any Python APIs
            try
            {
                using (Py.GIL())
                {
                    // Convert the C# float array to a Python list
                    var pyPosList = new PyList();
                    foreach (var p in posList)
                    {
                        var pos = new PyList();
                        pos.Append(new PyFloat(p.x));
                        pos.Append(new PyFloat(p.y));
                        pos.Append(new PyFloat(p.z));
                        pyPosList.Append(pos);
                    }

                    var pySymbolList = new PyList();
                    foreach (var s in symbolList)
                    {
                        pySymbolList.Append(new PyString(s));
                    }

                    //// Import and run the Python script
                    //dynamic sys = Py.Import("sys");
                    //sys.path.append(Path.Combine(Application.streamingAssetsPath + "PythonScripts"));

                    //// Import the built-in module
                    //dynamic builtins = Py.Import("builtins");

                    // Import your Python script
                    dynamic script = Py.Import("StructureFormulaPythonBackend");

                    //// Print the attributes of the imported module
                    //Debug.Log("Attributes of the imported module:");
                    //foreach (string key in builtins.dir(script))
                    //{
                    //    Debug.Log(key);
                    //}

                    // Call the function from the Python script
                    dynamic result = script.gen_structure_formula(pyPosList, pySymbolList);

                    // Extract values from the returned tuple
                    svgContent = result[0].ToString();
                    dynamic coordsList = result[1];

                    // Convert the Python list of coordinates to a C# array
                    for (int i = 0; i < coordsList.Length(); i++)
                    {
                        var coord = coordsList[i];
                        coords.Add(new Vector2(coord[0].As<float>(), coord[1].As<float>()));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[StructureFormulaGenerator] Could not generate a structure formula. This is expected for small Molecules.");
                return "";
            }

            return svgContent;
        }
        private void OnDestroy()
        {
            //EventManager.Singleton.OnMolDataChanged -= requestStructureFormula;
            //EventManager.Singleton.OnMoleculeLoaded -= immediateRequestStructureFormula;
        }
#endif
    }
}
