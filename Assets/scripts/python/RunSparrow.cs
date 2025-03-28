using UnityEngine;
using Python.Runtime;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using IngameDebugConsole;
using System;


namespace chARpack
{
    public class RunSparrow : MonoBehaviour
    {
        private static RunSparrow _singleton;

        public static RunSparrow Singleton
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
                    Debug.Log($"[{nameof(RunSparrow)}] Instance already exists, destroying duplicate!");
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
            isInitialized = false;
            EventManager.Singleton.OnGrabAtom += applyConstraint;
            DebugLogConsole.AddCommand("startSparrow", "starts a simulation using sparrow", startSim);
            DebugLogConsole.AddCommand("stopSparrow", "stops the current simulation", stopSim);
        }

        public bool isInitialized { get; private set; }
        public bool isRunning { get; private set; }
        dynamic sparrow;
        dynamic builtins;
        List<Atom> id_convert;
        List<Vector3> sim_result;
        int num_atoms = 0;

        private void prepareSim()
        {
            if (!PythonEnvironmentManager.Singleton)
            {
                isRunning = false;
                isInitialized = false;
                Debug.LogWarning("[RunSparrow] No PythonEnvironmentManager found.");
                return;
            }
            if (!PythonEnvironmentManager.Singleton.isInitialized)
            {
                isRunning = false;
                isInitialized = false;
                Debug.LogWarning("[RunSparrow] PythonEnvironment not initialized yet.");
                return;
            }

            if (GlobalCtrl.Singleton.List_curMolecules.Count == 0)
            {
                Debug.LogWarning("[RunSparrow] Preparing system for empty scene. Abort.");
                return;
            }
            sim_result = new List<Vector3>();
            id_convert = new List<Atom>();

            // Prepare lists
            var posList = new List<Vector3>();
            num_atoms = 0;
            var symbolList = new List<string>();
            foreach (var mol in GlobalCtrl.Singleton.List_curMolecules.Values)
            {
                for (int i = 0; i < mol.atomList.Count; i++)
                {
                    var atom = mol.atomList[i];
                    id_convert.Add(atom);
                    //var pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(atom.transform.position) * GlobalCtrl.u2aa / GlobalCtrl.scale;
                    var pos = atom.transform.localPosition * GlobalCtrl.u2aa / GlobalCtrl.scale;
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
                builtins = Py.Import("builtins");
                dynamic script = Py.Import("test_sparrow");
                sparrow = script.chARpackSparrow();
                sparrow.setData(pyPosList, pySymbolList);
            }

            Debug.Log("[RunSparrow] Preparation complete.");
        }

        private IEnumerator spreadSimulation()
        {
            //yield return new WaitForSeconds(1f);
            using (Py.GIL())
            {
                //yield return sparrow.run();
                sparrow.setCalcMOs(true);
                dynamic python_pos_return = sparrow.getPositions();
                dynamic python_mo_return = sparrow.getMO();
                if (builtins.len(python_pos_return) > 0)
                {
                    sim_result = new List<Vector3>();
                    for (int i = 0; i < num_atoms; i++)
                    {
                        sim_result.Add(GlobalCtrl.scale / GlobalCtrl.u2aa * new Vector3(python_pos_return[i][0].As<float>(), python_pos_return[i][1].As<float>(), python_pos_return[i][2].As<float>()));
                        //Debug.Log($"[RunSparrow] Sparrow atom {i} with position {sim_result[i]}");
                    }
                }
                if (builtins.len(python_mo_return.keys()) > 0) {
                    Debug.Log($"[RunSparrow] Got MO {python_mo_return}");
                    Debug.Log($"[RunSparrow] Got Orbitals are not all zero {python_mo_return["data"].any()}");
                    Debug.Log($"[RunSparrow] Got Orbitals MAX: {python_mo_return["data"].max()} MIN: {python_mo_return["data"].min()}");
                    var volume = new ScalarVolume();
                    volume.dim = new Vector3Int(python_mo_return["dimensions"][0].As<int>(), python_mo_return["dimensions"][1].As<int>(), python_mo_return["dimensions"][2].As<int>());
                    volume.spacing = new Vector3(python_mo_return["spacing"][0].As<float>(), python_mo_return["spacing"][1].As<float>(), python_mo_return["spacing"][2].As<float>());
                    volume.origin = new Vector3(python_mo_return["origin"][0].As<float>(), python_mo_return["origin"][1].As<float>(), python_mo_return["origin"][2].As<float>());
                    var list = new float[volume.dim[0] * volume.dim[1] * volume.dim[2]];
                    var d_list = python_mo_return["data"].As<double[]>();
                    for (int i = 0; i < list.Count(); i++)
                    {
                        list[i] = (float)d_list[i];
                    }
                    volume.values = list.ToList();
                    MoleculeOrbitals.Singleton.addOrbital(volume, GlobalCtrl.Singleton.getFirstMarkedObject().GetComponent<Molecule>());
                }
                //Debug.Log($"[RunSparrow] {sim_result.ToArray().Print()}");
            }
            yield return null;
        }

        public void applyConstraint(Atom a, bool value)
        {
            //if (sparrow == null) return;
            //using (Py.GIL())
            //{
            //    var id = id_convert.IndexOf(a);
            //    sparrow.fixAtom(id, value);
            //}
        }


        private void FixedUpdate()
        {
            if (!isInitialized) return;
            StartCoroutine(spreadSimulation());
            if (sim_result?.Count == id_convert.Count)
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
                        //atom.transform.position = GlobalCtrl.Singleton.atomWorld.transform.TransformPoint(sim_result[i]);
                        atom.transform.localPosition = sim_result[i];
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
                        //var pos = GlobalCtrl.Singleton.atomWorld.transform.InverseTransformPoint(id_convert[i].transform.position) * GlobalCtrl.u2aa / GlobalCtrl.scale;
                        var pos = id_convert[i].transform.localPosition * GlobalCtrl.u2aa / GlobalCtrl.scale;
                        var pyPos = new PyList();
                        pyPos.Append(new PyFloat(pos.x));
                        pyPos.Append(new PyFloat(pos.y));
                        pyPos.Append(new PyFloat(pos.z));
                        sparrow.changeAtomPosition(i, pyPos);
                        //Debug.Log($"[RunSparrow] Pushing position change to sparrow. Atom {i} pos {pos}");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            stopSim();
        }

        public void stopSim()
        {
            if (sparrow != null)
            {
                using (Py.GIL())
                {
                    sparrow.stopContinuousRun();
                }
            }

            isInitialized = false;
            isRunning = false;
        }

        public void startSim()
        {
            if (!isInitialized && !isRunning)
            {
                ForceField.Singleton.enableForceFieldMethodUI(false);
                prepareSim();
                isInitialized = !isInitialized;
                using (Py.GIL())
                {
                    sparrow.startContinuousRun();
                }
                isRunning = !isRunning;
            }
        }
#endif
    }
}
