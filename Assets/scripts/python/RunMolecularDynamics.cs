using System;
using System.IO;
using UnityEngine;
using Python.Runtime;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.Rendering.Universal;



public class RunMolecularDynamics : MonoBehaviour
{
    private static RunMolecularDynamics _singleton;

    public static RunMolecularDynamics Singleton
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
                Debug.Log($"[{nameof(RunMolecularDynamics)}] Instance already exists, destroying duplicate!");
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
        EventManager.Singleton.OnGrabAtom += applyConstraint;
    }

    bool running = false;
    dynamic apax;
    List<Vector3> posList;
    string[] symbolList;
    List<Vector3> sim_results;
    Molecule currentMol;
    List<Atom> grabbedAtoms = new List<Atom>();
    string base_dir;

    private void prepareSim()
    {
        if (!PythonEnvironmentManager.Singleton) return;
        if (!PythonEnvironmentManager.Singleton.isInitialized) return;

        base_dir = Path.Combine(Application.streamingAssetsPath, "md");
        if (!Directory.Exists(base_dir))
        {
            Debug.LogError($"[RunMolecularDynamics] Base dir with scripts and models does not exist.\nGiven path: {base_dir}");
            return;
        }

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

            var pyIndexList = new PyList();
            foreach (var id in index_array)
            {
                pyIndexList.Append(new PyInt(id));
            }

            // Import your Python script
            dynamic script = Py.Import("test_md");
            apax = script.ApaxMD(base_dir: base_dir, mode: "thermostat");
            apax.setData(pyPosList, pySymbolList, pyIndexList);
        }
        Debug.Log("[RunMolecularDynamics] Preparation complete.");
    }


    dynamic python_return;
    private IEnumerator spreadSimulation()
    {
        //yield return new WaitForSeconds(0.1f);
        yield return apax.run();
        python_return = apax.getPositions();

        sim_results = new List<Vector3>();
        foreach (var res in python_return)
        {
            sim_results.Add(GlobalCtrl.scale / GlobalCtrl.u2aa * new Vector3(res[0].As<float>(), res[1].As<float>(), res[2].As<float>()));
        }
        yield return null;
    }

    public void applyConstraint(Atom a, bool value)
    {
        if (apax == null) return;
        using (Py.GIL())
        {
            apax.fixAtom(a.m_id, value);
        }
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
            for (int i = 0; i < currentMol.atomList.Count; i++)
            {
                if (grabbedAtoms.Find(at => at.m_id == i) != null)
                {
                    currentMol.atomList[i].transform.localPosition = sim_results[i];
                    EventManager.Singleton.MoveAtom(currentMol.m_id, currentMol.atomList[i].m_id, currentMol.atomList[i].transform.localPosition);
                }
            }
            sim_results = null;
        }
        using (Py.GIL())
        {
            foreach (var atom in grabbedAtoms)
            {
                var pos = atom.transform.localPosition * GlobalCtrl.u2aa / GlobalCtrl.scale;
                var pyPos = new PyList();
                pyPos.Append(new PyFloat(pos.x));
                pyPos.Append(new PyFloat(pos.y));
                pyPos.Append(new PyFloat(pos.z));
                apax.changeAtomPosition(atom.m_id, pyPos);
            }
        }
    }

    public void stopSim()
    {
        running = false;
    }

    public void toggleSim()
    {
        running = !running;
        ForceField.Singleton.toggleForceFieldUI();
        if (running)
        {
            prepareSim();
        }
    }
#endif
}
