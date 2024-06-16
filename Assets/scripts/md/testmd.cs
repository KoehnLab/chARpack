using System;
using System.IO;
using UnityEngine;
using Python.Runtime;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Google.Protobuf.WellKnownTypes;


public class testmd : MonoBehaviour
{
    private Thread thread;
    private Mutex mutex = new Mutex();
    private static testmd _singleton;

    public static testmd Singleton
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
                Debug.Log($"[{nameof(testmd)}] Instance already exists, destroying duplicate!");
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

        var path = Path.Combine(Application.streamingAssetsPath, "PythonEnv");
        string pythonHome = null;
        string pythonExe = null;
        foreach (var p in path.Split(';'))
        {
            var fullPath = Path.Combine(p, "python.exe");
            if (File.Exists(fullPath))
            {
                pythonHome = Path.GetDirectoryName(fullPath);
                pythonExe = fullPath;
                break;
            }
        }

        string pythonPath = null;
        string dll_path = null;
        string zip_path = null;
        if (pythonHome != null)
        {
            // check for the dll
            foreach (var dllName in possibleDllNames)
            {
                var fullPath = Path.Combine(pythonHome, dllName);
                if (File.Exists(fullPath))
                {
                    dll_path = fullPath;
                    zip_path = fullPath.Split('.')[0] + ".zip";
                    break;
                }
            }
            if (dll_path == null)
            {
                throw new Exception("Couldn't find python DLL");
            }
        }
        else
        {
            throw new Exception("Couldn't find python.exe");
        }

        //// Set the path to the embedded Python environment
        pythonPath = pythonHome + ";" + Path.Combine(pythonHome, "Lib\\site-packages") + ";" + zip_path + ";" + Path.Combine(pythonHome, "DLLs") + ";" + Path.Combine(Application.streamingAssetsPath, "md");
        Environment.SetEnvironmentVariable("PYTHONHOME", null);
        Environment.SetEnvironmentVariable("PYTHONPATH", null);

        // Display Python runtime details
        Debug.Log($"Python DLL: {dll_path}");
        Debug.Log($"Python executable: {pythonExe}");
        Debug.Log($"Python home: {pythonHome}");
        Debug.Log($"Python path: {pythonPath}");


        // Initialize the Python runtime
        Runtime.PythonDLL = dll_path;

        // Initialize the Python engine with the embedded Python environment
        PythonEngine.PythonHome = pythonHome;
        PythonEngine.PythonPath = pythonPath;
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();

        EventManager.Singleton.OnGrabAtom += applyConstraint;
    }

    bool running = false;
    dynamic apax;
    List<Vector3> posList;
    string[] symbolList;
    List<Vector3> sim_results;
    Molecule currentMol;
    List<Atom> grabbedAtoms = new List<Atom>();

    private void prepareSim()
    {
        currentMol = GlobalCtrl.Singleton.List_curMolecules.Values.First();
        sim_results = new List<Vector3>();

        // Prepare lists
        posList = new List<Vector3>();
        for (int i = 0; i < currentMol.atomList.Count; i++)
        {
            var atom = currentMol.atomList[i];
            var pos = atom.transform.localPosition * GlobalCtrl.u2aa / GlobalCtrl.scale;
            posList.Add(pos);
        }

        symbolList = new string[currentMol.atomList.Count];
        for (int i = 0; i < currentMol.atomList.Count; i++)
        {
            var atom = currentMol.atomList[i];
            if (atom.m_data.m_abbre.ToLower() == "dummy")
            {
                symbolList[i] = "H";
            }
            else
            {
                symbolList[i] = atom.m_data.m_abbre;
            }
        }

        var index_array = new int[currentMol.atomList.Count];
        for (int i = 0; i < currentMol.atomList.Count; i++)
        {
            index_array[i] = currentMol.atomList[i].m_id;
        }

        // Acquire the GIL before using any Python APIs
        //using (Py.GIL())
        //{
            Debug.Log("[testmd] started python environment");
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

            var pyIndexList = new PyList();
            foreach (var id in index_array)
            {
                pyIndexList.Append(new PyInt(id));
            }

            // Import your Python script
            dynamic script = Py.Import("test_md");

            apax = script.ApaxMD();
            apax.setData(pyPosList, pySymbolList, pyIndexList);
        // }
        Debug.Log("[testmd] Preparations done");
    }


    dynamic python_return;
    private IEnumerator spreadSimulation()
    {
        //yield return new WaitForSeconds(0.01f);
        var done = false;
        new Thread(() =>
        {
            Debug.Log("[testmd] started thread");
            apax.run(1);
            Debug.Log("[testmd] run done");
            python_return = apax.getPositions();
            Debug.Log("[testmd] got positions");
            done = true;
        }).Start();

        while (!done)
        {
            yield return null;
        }
        //apax.run();
        //python_return = apax.getPositions();

        Debug.Log("[testmd] got values from sim");
        sim_results = new List<Vector3>();
        foreach (var res in python_return)
        {
            sim_results.Add(GlobalCtrl.scale / GlobalCtrl.u2aa * new Vector3(res[0].As<float>(), res[1].As<float>(), res[2].As<float>()));
        }
        yield return null;
    }

    private void applyConstraint(Atom a, bool value)
    {
        if (apax == null) return;
        //using (Py.GIL())
        //{
            apax.fixAtom(a.m_id, value);
        //}
        if (value)
        {
            grabbedAtoms.Add(a);
        }
        else
        {
            grabbedAtoms.Remove(a);
        }
    }


    private void FixedUpdate()
    {
        if (!running) return;
        if (apax != null)
        {
            StartCoroutine(spreadSimulation());
        }
        if (sim_results?.Count > 0)
        {
            Debug.Log("[testmd] applying new positions");
            for (int i = 0; i < currentMol.atomList.Count; i++)
            {
                currentMol.atomList[i].transform.localPosition = sim_results[i];
            }
            sim_results = null;
        }
        //using (Py.GIL())
        //{
            foreach (var atom in grabbedAtoms)
            {
                var pos = atom.transform.localPosition * GlobalCtrl.u2aa / GlobalCtrl.scale;
                var pyPos = new PyList();
                pyPos.Append(new PyFloat(pos.x));
                pyPos.Append(new PyFloat(pos.y));
                pyPos.Append(new PyFloat(pos.z));
                apax.changeAtomPosition(atom.m_id, pyPos);

                dynamic symbols = apax.atoms.get_chemical_symbols();
                Debug.Log($"[testmd] {symbols}");
                Debug.Log($"[testmd] {symbols[atom.m_id]}");
                Debug.Log($"[testmd] {symbols[apax.constraint_atoms[0]]}");
            }
            //Debug.Log($"[testmd] Force: {apax.getPosZero()}");
        //}
    }

    public void stopSim()
    {
        running = false;
    }

    public void toggleSim()
    {
        running = !running;
        if (running)
        {
            prepareSim();
        }
    }

    private void OnDestroy()
    {
        // Shutdown the Python engine
        PythonEngine.Shutdown();
    }
#endif
}
