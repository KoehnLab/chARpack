using UnityEngine;
using Python.Runtime;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using IngameDebugConsole;
using System.Threading;
using System;
using System.ComponentModel.Design;


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
            isRunning = new(false);
            isInitialized = new(false);
            DebugLogConsole.AddCommand("startSparrow", "starts a simulation using sparrow", startSim);
            DebugLogConsole.AddCommand("stopSparrow", "stops the current simulation", stopSim);
            DebugLogConsole.AddCommand<bool>("sparrowGenerateOrbitals", "stops the current simulation", setGenOrbitals);
        }

        void setGenOrbitals(bool value)
        {
            generateOrbitals.Value = value;
            if (!value) MoleculeOrbitals.Singleton.Clear();
        }

        public ThreadSafe<bool> isInitialized { get; private set; }
        public ThreadSafe<bool> isRunning { get; private set; }
        private ThreadSafe<bool> generateOrbitals = new(true);
        volatile dynamic sparrow;
        volatile dynamic builtins;
        List<Atom> id_convert = new();
        Queue<List<Vector3>> sim_result = new();
        const int SIM_QUEUE_MAX_COUNT = 5;
        Queue<ScalarVolume> mo_result = new();
        int num_atoms = 0;
        int continousRunID;

        private void prepareSim()
        {
            if (!PythonEnvironmentManager.Singleton)
            {
                isRunning.Value = false;
                isInitialized.Value = false;
                Debug.LogWarning("[RunSparrow] No PythonEnvironmentManager found.");
                return;
            }
            if (!PythonEnvironmentManager.Singleton.isInitialized)
            {
                isRunning.Value = false;
                isInitialized.Value = false;
                Debug.LogWarning("[RunSparrow] PythonEnvironment not initialized yet.");
                return;
            }

            if (GlobalCtrl.Singleton.List_curMolecules.Count == 0)
            {
                Debug.LogWarning("[RunSparrow] Preparing system for empty scene. Abort.");
                return;
            }
            sim_result.Clear();
            mo_result.Clear();
            id_convert.Clear();

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

            continousRunID = PythonDispatcher.AddContinousAction(
                delegate
                {
                    sparrow.run();
                    dynamic python_pos_return = sparrow.getPositions();
                    if (builtins.len(python_pos_return) > 0)
                    {
                        var res = new List<Vector3>();
                        for (int i = 0; i < num_atoms; i++)
                        {
                            res.Add(GlobalCtrl.scale / GlobalCtrl.u2aa * new Vector3(python_pos_return[i][0].As<float>(), python_pos_return[i][1].As<float>(), python_pos_return[i][2].As<float>()));
                            //Debug.Log($"[RunSparrow] Sparrow atom {i} with position {sim_result[i]}");
                        }
                        if (sim_result.Count > SIM_QUEUE_MAX_COUNT)
                        {
                            sim_result.Dequeue();
                        }
                        sim_result.Enqueue(res);
                    }
                    if (generateOrbitals.Value)
                    {
                        sparrow.generateOrbitals();
                        dynamic python_mo_return = sparrow.getMO();
                        if (builtins.len(python_mo_return.keys()) > 0)
                        {
                            //Debug.Log($"[RunSparrow] Got MO {python_mo_return}");
                            //Debug.Log($"[RunSparrow] Got Orbitals are not all zero {python_mo_return["data"].any()}");
                            //Debug.Log($"[RunSparrow] Got Orbitals MAX: {python_mo_return["data"].max()} MIN: {python_mo_return["data"].min()}");
                            var volume = new ScalarVolume();
                            volume.dim = new Vector3Int(python_mo_return["dimensions"][0].As<int>(), python_mo_return["dimensions"][1].As<int>(), python_mo_return["dimensions"][2].As<int>());
                            volume.spacing = new Vector3(python_mo_return["spacing"][0].As<float>(), python_mo_return["spacing"][1].As<float>(), python_mo_return["spacing"][2].As<float>()) * GlobalCtrl.scale / GlobalCtrl.u2aa;
                            volume.origin = new Vector3(python_mo_return["origin"][0].As<float>(), python_mo_return["origin"][1].As<float>(), python_mo_return["origin"][2].As<float>()) * GlobalCtrl.scale / GlobalCtrl.u2aa;
                            var list = new float[volume.dim[0] * volume.dim[1] * volume.dim[2]];
                            var d_list = python_mo_return["data"].As<double[]>();
                            for (int i = 0; i < list.Count(); i++)
                            {
                                list[i] = (float)d_list[i];
                            }
                            volume.values = list.ToList();
                            mo_result.Enqueue(volume);
                        }
                    }
                    //Debug.Log($"[RunSparrow] {sim_result.ToArray().Print()}");
                });

            Debug.Log("[RunSparrow] Preparation complete.");
        }


        volatile List<Vector3> grabbed_positions = new();
        volatile List<int> grabbed_ids = new();
        private void FixedUpdate()
        {
            if (!isInitialized.Value) return;
            if (sim_result.Count > 1)
            {
                var res = sim_result.Dequeue();
                for (int i = 0; i < id_convert.Count; i++)
                {
                    var atom = id_convert[i];
                    if (!atom.isGrabbed)
                    {
                        atom.transform.localPosition = res[i];
                        EventManager.Singleton.MoveAtom(atom.m_molecule.m_id, atom.m_id, atom.transform.localPosition);
                    }
                }
            }
            if (generateOrbitals.Value && mo_result.Count > 0)
            {
                var volume = mo_result.Dequeue();
                MoleculeOrbitals.Singleton.addOrbital(volume, GlobalCtrl.Singleton.List_curMolecules.First().Value);

            }

            grabbed_positions.Clear();
            grabbed_ids.Clear();
            for (int i = 0; i < id_convert.Count; i++)
            {
                if (id_convert[i].isGrabbed)
                {
                    grabbed_positions.Add(id_convert[i].transform.localPosition * GlobalCtrl.u2aa / GlobalCtrl.scale);
                    grabbed_ids.Add(i);
                }
            }


            PythonDispatcher.RunInPythonThread(
                delegate
                {
                    for (int j = 0; j < grabbed_positions.Count; j++)
                    {
                        var pyPos = new PyList();
                        pyPos.Append(new PyFloat(grabbed_positions[j].x));
                        pyPos.Append(new PyFloat(grabbed_positions[j].y));
                        pyPos.Append(new PyFloat(grabbed_positions[j].z));
                        sparrow.changeAtomPosition(grabbed_ids[j], pyPos);
                        //Debug.Log($"[RunSparrow] Pushing position change to sparrow. Atom {i} pos {pos}");
                    }
                }
            );
        }

        private void OnDestroy()
        {
            stopSim();
        }

        public void stopSim()
        {
            if (sparrow != null)
            {
                //PythonDispatcher.RunInPythonThread(sparrow.stopContinuousRun());
            }

            isInitialized.Value = false;
            isRunning.Value = false;
            PythonDispatcher.RemoveContinousAction(continousRunID);
        }

        public void startSim()
        {
            if (!isInitialized.Value && !isRunning.Value)
            {
                ForceField.Singleton.enableForceFieldMethodUI(false);
                prepareSim();
                isInitialized.Value = !isInitialized.Value;
                //PythonDispatcher.RunInPythonThread(sparrow.startContinuousRun());
                isRunning.Value = !isRunning.Value;
            }
        }
#endif
    }
}
