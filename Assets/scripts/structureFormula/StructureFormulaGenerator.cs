using System;
using System.IO;
using UnityEngine;
using Python.Runtime;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Utilities.Editor;


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

    void Start()
    {
        string[] possibleDllNames = new string[]
        {
        "python313.dll",
        "python312.dll",
        "python311.dll",
        "python310.dll",
        "python39.dll",
        "python38.dll",
        "python37.dll",
        "python36.dll",
        "python35.dll",
        };

        //var path = Environment.GetEnvironmentVariable("PATH");
        var path = Environment.GetEnvironmentVariable("PATH");
        string pythonHome = null;
        string pythonExe = null;
        foreach (var p in path.Split(new char[] { ';' }))
        {
            var fullPath = Path.Combine(p, "python.exe");
            if (File.Exists(fullPath))
            {
                pythonHome = p;
                pythonExe = fullPath;
                break;
            }
        }

        string pythonPath = null;
        string dll_path = null;
        if (pythonHome != null)
        {
            // check for the dll
            foreach (var dllName in possibleDllNames)
            {
                var fullPath = Path.Combine(pythonHome, dllName);
                if (File.Exists(fullPath))
                {
                    dll_path = fullPath;
                    break;
                }
            }
            if (dll_path == null)
            {
                throw new Exception("Couldn't find python DLL");
            }

            Environment.SetEnvironmentVariable("PATH", pythonHome);
            Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
            pythonPath = pythonHome + ";" + Path.Combine(pythonHome, "Lib") + ";" + Path.Combine(pythonHome, "Lib/site-packages");
            Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath);
        }
        else
        {
            throw new Exception("Couldn't find python.exe");
        }

        //// Set the path to the embedded Python environment
        //pythonHome = "D:\\unity_projects\\python-3.10.9-embed-amd64";
        //pythonHome = Path.Combine(Application.dataPath, "../PythonEnv");
        //pythonPath = pythonHome + ";" + Path.Combine(pythonHome, "Lib/site-packages") + ";" + Path.Combine(pythonHome, "Lib");

        //Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
        //Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath + ";" + Path.Combine(pythonPath, "site-packages"));

        // Display Python runtime details
        Debug.Log($"Python DLL: {dll_path}");
        Debug.Log($"Python executable: {pythonExe}");
        Debug.Log($"PYTHONHOME: {Environment.GetEnvironmentVariable("PYTHONHOME")}");
        Debug.Log($"PYTHONPATH: {Environment.GetEnvironmentVariable("PYTHONPATH")}");



        // Initialize the Python runtime
        //Runtime.PythonDLL = "D:\\unity_projects\\python-3.10.9-embed-amd64\\python310.dll";
        //Runtime.PythonDLL = Path.Combine(pythonHome, "python310.dll");
        Runtime.PythonDLL = dll_path;

        // Initialize the Python engine with the embedded Python environment
        PythonEngine.PythonHome = pythonHome;
        PythonEngine.PythonPath = pythonPath;
        PythonEngine.Initialize();
    }

    public void requestStructureFormula(Molecule mol)
    {

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
        var coordsArray = new List<Vector2>();

        // Acquire the GIL before using any Python APIs
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

            // Import and run the Python script
            dynamic sys = Py.Import("sys");
            sys.path.append(Application.dataPath + "/scripts/structureFormula/");

            // Import the built-in module
            dynamic builtins = Py.Import("builtins");

            // Import your Python script
            dynamic script = Py.Import("StructureFormulaPythonBackend");

            // Print the attributes of the imported module
            Debug.Log("Attributes of the imported module:");
            foreach (string key in builtins.dir(script))
            {
                Debug.Log(key);
            }

            // Call the function from the Python script
            dynamic result = script.gen_structure_formula(pyPosList, pySymbolList);
            //dynamic myClass = script.MyClass("hallo");
            //string greeting = myClass.greet();
            //Debug.Log(greeting);

            // Extract values from the returned tuple
            svgContent = result[0].ToString();
            dynamic coordsList = result[1];

            // Convert the Python list of coordinates to a C# array
            for (int i = 0; i < coordsList.Length(); i++)
            {
                var coord = coordsList[i];
                coordsArray.Add(new Vector2(coord[0].As<float>(), coord[1].As<float>()));
            }
        }

        // push content
        for (int i = 0; i < coordsArray.Count; i++)
        {
            mol.atomList[i].structure_coords = coordsArray[i];
        }

        if (StructureFormulaManager.Singleton)
        {
            StructureFormulaManager.Singleton.pushContent(mol.m_id, svgContent);
        }
        else
        {
            Debug.LogError("[structureReceiveComplete] Could not find StructureFormulaManager");
            return;
        }

        //write svg to file
        var file_path = Path.Combine(Application.streamingAssetsPath, $"{svgContent.Length}.svg");
        if (File.Exists(file_path))
        {
            Debug.Log(file_path + " already exists.");
            return;
        }
        var sr = File.CreateText(file_path);
        sr.Write(svgContent);
        sr.Close();

    }

    private void OnDestroy()
    {
        // Shutdown the Python engine
        PythonEngine.Shutdown();
    }
}