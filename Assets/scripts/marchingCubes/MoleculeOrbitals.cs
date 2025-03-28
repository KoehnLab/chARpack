using IngameDebugConsole;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System.Collections.Generic;
using MarchingCubes;
using System.Linq;
using UnityEngine;
using System;
using chARpack.Types;

namespace chARpack
{
    struct MoleculeOrbitalComputeShaderData
    {
        public Molecule mol;
        public ScalarVolume volume_data;
        public ComputeBuffer compute_buffer;
        public MeshBuilder builder;
        public GameObject game_object;
        public bool initialized;
        public float target_iso_value;
        public float current_iso_value;
    }


    class MoleculeOrbitals : MonoBehaviour
    {
        //[SerializeField] Vector3Int _dimensions = new Vector3Int(256, 256, 113);
        //int VoxelCount => _dimensions.x * _dimensions.y * _dimensions.z;
        [SerializeField] Vector3 _spacing = Vector3.one;
        [SerializeField] int _triangleBudget = 65536 * 16;

        //[SerializeField] ComputeShader _converterCompute = null;
        [SerializeField] ComputeShader _builderCompute = null;
        static List<MoleculeOrbitalComputeShaderData> mos;


        private static MoleculeOrbitals _singleton;

        public static MoleculeOrbitals Singleton
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
                    Debug.Log($"[{nameof(MoleculeOrbitals)}] Instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        private void Awake()
        {
            Singleton = this;
        }


        private void Start()
        {
            DebugLogConsole.AddCommand("loadOrbital", "Opens a file load dialog for molecule orbital CSV files.", loadData);
            // DebugLogConsole.AddCommand("setIsoValue", "Set an iso value of mol", loadData);
            mos = new List<MoleculeOrbitalComputeShaderData>();
        }

        void loadData()
        {
            StartCoroutine(ShowLoadDialogCoroutine());
        }

        IEnumerator ShowLoadDialogCoroutine()
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files);


            if (FileBrowser.Success)
            {
                if (FileBrowser.Result.Length != 1)
                {
                    UnityEngine.Debug.LogError("[SurfaceNets] Path from FileBrowser is empty. Abort.");
                    yield break;
                }
                FileInfo fi = new FileInfo(FileBrowser.Result[0]);
                UnityEngine.Debug.Log($"[SurfaceNets] Current extension: {fi.Extension}");
                if (!fi.Exists)
                {
                    UnityEngine.Debug.LogError("[SurfaceNets] Something went wrong during path conversion. Abort.");
                    yield break;
                }

                if (fi.Extension.Contains("csv"))
                {
                    var go = new GameObject();
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.material = new Material(Shader.Find("Shader Graphs/chARpackTransparentMaterial"));
                    renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, 0.15f);
                    go.AddComponent<MeshFilter>();
                    var volume = CSVToVolume.LoadCSV(fi.FullName);
                    var mo_data = new MoleculeOrbitalComputeShaderData();
                    mo_data.volume_data = volume;
                    mo_data.game_object = go;
                    mo_data.mol = GlobalCtrl.Singleton.getFirstMarkedObject().GetComponent<Molecule>();
                    mos.Add(mo_data);
                    Initialize(mo_data);
                }
                else
                {
                    yield break;
                }
            }
        }

        public void addOrbital(ScalarVolume vol, Molecule mol)
        {
            int current_mos_id = mos.FindIndex(x => x.mol == mol);
            if (current_mos_id == -1)
            {
                var mo_data = new MoleculeOrbitalComputeShaderData();
                var go = new GameObject();
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.material = new Material(Shader.Find("Shader Graphs/chARpackTransparentMaterial"));
                renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, 0.15f);
                go.AddComponent<MeshFilter>();
                mo_data.game_object = go;
                mo_data.mol = mol;
                mo_data.volume_data = vol;
                Initialize(mo_data);
            }
            else
            {
                var mo = mos[current_mos_id];
                mo.volume_data = vol;
                mo.compute_buffer.SetData(vol.values.ToArray());
                mos[current_mos_id] = mo;
            }

        }

        void Initialize(MoleculeOrbitalComputeShaderData mo_data)
        {
            var voxel_count = mo_data.volume_data.dim.x * mo_data.volume_data.dim.y * mo_data.volume_data.dim.z;
            Debug.Log($"Num values: {mo_data.volume_data.values.Count}; VoxelCount: {voxel_count}");
            mo_data.compute_buffer = new ComputeBuffer(voxel_count, sizeof(float));
            mo_data.compute_buffer.SetData(mo_data.volume_data.values.ToArray());
            mo_data.builder = new MeshBuilder(mo_data.volume_data.dim, _triangleBudget, _builderCompute);
            mo_data.initialized = true;
            // Voxel data conversion (ushort -> float)
            //using var readBuffer = new ComputeBuffer(VoxelCount / 2, sizeof(uint));
            //readBuffer.SetData(vol.values.ToArray());

            //_converterCompute.SetInts("Dims", _dimensions);
            //_converterCompute.SetBuffer(0, "Source", readBuffer);
            //_converterCompute.SetBuffer(0, "Voxels", _voxelBuffer);
            //_converterCompute.DispatchThreads(0, _dimensions);

        }

        void OnDestroy()
        {
            foreach (var mo in mos)
            {
                if (mo.compute_buffer != null)
                {
                    mo.compute_buffer.Dispose();
                }
                if (mo.builder != null)
                {
                    mo.builder.Dispose();
                }
            }
        }

        void Update()
        {
            for (int i = 0; i < mos.Count(); i++)
            {
                var mo = mos[i];
                if (!mo.initialized) continue;
                // Rebuild the isosurface only when the target value has been changed.
                if (mo.target_iso_value == mo.current_iso_value) continue;

                mo.builder.BuildIsosurface(mo.compute_buffer, TargetValue, _spacing);
                mo.game_object.GetComponent<MeshFilter>().sharedMesh = mo.builder.Mesh;

                mo.current_iso_value = mo.target_iso_value;
                mos[i] = mo;
            }
        }
    }

}