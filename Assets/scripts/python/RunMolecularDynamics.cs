using System.IO;
using UnityEngine;
using Python.Runtime;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using chARpackTypes;


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
        isRunning = false;
        EventManager.Singleton.OnGrabAtom += applyConstraint;
    }

    public bool isRunning { get; private set; }
    dynamic apax;
    List<Atom> id_convert;
    List<Vector3> sim_result;
    string base_dir;

    private void prepareSim()
    {
        if (!PythonEnvironmentManager.Singleton)
        {
            isRunning = false;
            Debug.LogWarning("[RunMolecularDynamics] No PythonEnvironmentManager found.");
            return;
        }
        if (!PythonEnvironmentManager.Singleton.isInitialized)
        {
            isRunning = false;
            Debug.LogWarning("[RunMolecularDynamics] PythonEnvironment not initialized yet.");
            return;
        }


        base_dir = Path.Combine(Application.streamingAssetsPath, "md");
        if (!Directory.Exists(base_dir))
        {
            Debug.LogError($"[RunMolecularDynamics] Base dir with scripts and models does not exist.\nGiven path: {base_dir}");
            return;
        }

        if (GlobalCtrl.Singleton.List_curMolecules.Count == 0)
        {
            Debug.LogWarning("[RunMolecularDynamics] Preparing system for empty scene. Abort.");
            return;
        }
        sim_result = new List<Vector3>();
        id_convert = new List<Atom>();

        // Prepare lists
        var posList = new List<Vector3>();
        int num_atoms = 0;
        var symbolList = new List<string>();
        foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
        {
            for (int i = 0; i < mol.atomList.Count; i++)
            {
                var atom = mol.atomList[i];
                id_convert.Add(atom);
                var pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(atom.transform.position) * GlobalCtrl.u2aa / GlobalCtrl.scale;
                posList.Add(pos);
                num_atoms++;

                if (atom.m_data.m_abbre.ToLower() == "dummy")
                {
                    symbolList.Add("H");
                }
                else
                {
                    symbolList.Add(atom.m_data.m_abbre);
                }
            }
        }

        var posAndSymbols = posList.Zip(symbolList, (p, s) => new { Pos = p, Symbol = s });
        // Acquire the GIL before using any Python APIs
        using (Py.GIL())
        {
            // Convert the C# float array to a Python list
            var pyPosList = new PyList();
            var pySymbolList = new PyList();
            foreach (var possym in posAndSymbols)
            {
                var pos = new PyList();
                pos.Append(new PyFloat(possym.Pos.x));
                pos.Append(new PyFloat(possym.Pos.y));
                pos.Append(new PyFloat(possym.Pos.z));
                pyPosList.Append(pos);

                pySymbolList.Append(new PyString(possym.Symbol));
            }

            // Import your Python script
            dynamic script = Py.Import("test_md");
            apax = script.ApaxMD(base_dir: base_dir, mode: "thermostat");
            apax.setData(pyPosList, pySymbolList);
        }

        Debug.Log("[RunMolecularDynamics] Preparation complete.");
    }

    private IEnumerator spreadSimulation()
    {
        //yield return new WaitForSeconds(0.1f);
        yield return apax.run();
        dynamic python_pos_return = apax.getPositions();
        int num_atoms = apax.getNumAtoms().As<int>();

        sim_result = new List<Vector3>();
        for (int i = 0; i < num_atoms; i++)
        {
            sim_result.Add(GlobalCtrl.scale / GlobalCtrl.u2aa * new Vector3(python_pos_return[i][0].As<float>(), python_pos_return[i][1].As<float>(), python_pos_return[i][2].As<float>()));
        }
        yield return null;
    }

    public void applyConstraint(Atom a, bool value)
    {
        if (apax == null) return;
        using (Py.GIL())
        {
            var id = id_convert.IndexOf(a);
            apax.fixAtom(id, value);
        }
    }


    private void FixedUpdate()
    {
        if (!isRunning) return;
        if (apax != null)
        {
            StartCoroutine(spreadSimulation());
        }
        if (sim_result?.Count > 0)
        {
            //int offset = 0;
            //foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
            //{
            //    for (int i = 0; i < mol.atomList.Count; i++)
            //    {
            //        var atom = mol.atomList[i];
            //        if (!atom.isGrabbed)
            //        {
            //            atom.transform.position = GlobalCtrl.Singleton.atomWorld.transform.TransformPoint(sim_results[offset + i]);
            //            EventManager.Singleton.MoveAtom(mol.m_id, mol.atomList[i].m_id, mol.atomList[i].transform.localPosition);
            //        }
            //    }
            //    offset += mol.atomList.Count;
            // }
            for (int i = 0; i < id_convert.Count; i++)
            {
                var atom = id_convert[i];
                if (!atom.isGrabbed)
                {
                    atom.transform.position = GlobalCtrl.Singleton.atomWorld.transform.TransformPoint(sim_result[i]);
                    EventManager.Singleton.MoveAtom(atom.m_molecule.m_id, atom.m_id, atom.transform.localPosition);
                }
            }
            sim_result = null;
        }
        using (Py.GIL())
        {
            for (int i = 0; i < id_convert.Count; i++)
            {
                if (id_convert[i].isGrabbed)
                {
                    var pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(id_convert[i].transform.position) * GlobalCtrl.u2aa / GlobalCtrl.scale;
                    var pyPos = new PyList();
                    pyPos.Append(new PyFloat(pos.x));
                    pyPos.Append(new PyFloat(pos.y));
                    pyPos.Append(new PyFloat(pos.z));
                    apax.changeAtomPosition(i, pyPos);
                }
            }
        }
    }

    public void stopSim()
    {
        isRunning = false;
    }

    public void toggleSim()
    {
        ForceField.Singleton.toggleForceFieldUI();
        if (!isRunning)
        {
            prepareSim();
        }
        isRunning = !isRunning;
    }
#endif
}
