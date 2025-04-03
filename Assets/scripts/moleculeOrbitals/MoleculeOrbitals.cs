using IngameDebugConsole;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace chARpack
{

    class MoleculeOrbitals : MonoBehaviour
    {
        // [SerializeField] Vector3Int _dimensions = new Vector3Int(256, 256, 113);
        // int VoxelCount => _dimensions.x * _dimensions.y * _dimensions.z;
        // [SerializeField] Vector3 _spacing = Vector3.one;
        [SerializeField] int _triangleBudget = 65536 * 16;

        //[SerializeField] ComputeShader _converterCompute = null;
        [SerializeField] ComputeShader _builderCompute = null;
        public static List<MoleculeOrbital> mos;


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

        public static int getTriangleBudget()
        {
            return Singleton._triangleBudget;
        }

        public static ComputeShader getBuilderCompute()
        {
            return Singleton._builderCompute;
        }

        private void Awake()
        {
            Singleton = this;
        }


        private void Start()
        {
            DebugLogConsole.AddCommand("loadOrbital", "Opens a file load dialog for molecule orbital CSV files.", loadData);
            DebugLogConsole.AddCommand<int, float>("setIsoValue", "Set an iso value of mol", setIsoValue);
            mos = new List<MoleculeOrbital>();
        }

        private void setIsoValue(int mol_list_id, float iso_value)
        {
            var mo = mos[mol_list_id];
            mo.target_iso_value = iso_value;
            mos[mol_list_id] = mo;
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
                    var mol = GlobalCtrl.Singleton.List_curMolecules.First().Value;
                    var volume = CSVToVolume.LoadCSV(fi.FullName);

                    var go = new GameObject("orbital");
                    go.transform.position = mol.transform.position;
                    go.transform.parent = mol.transform;

                    var mo_comp = go.AddComponent<MoleculeOrbital>();
                    mo_comp.mol_reference = mol;
                    mo_comp.volume_data = volume;

                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.material = new Material(Shader.Find("Shader Graphs/chARpackTransparentMaterial"));
                    renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, 0.15f);
                    go.AddComponent<MeshFilter>();

                    mo_comp.Initialize();

                    mos.Add(mo_comp);
                    
                }
                else
                {
                    yield break;
                }
            }
        }

        public void addOrbital(ScalarVolume vol, Molecule mol)
        {
            int current_mos_id = mos.FindIndex(x => x.mol_reference == mol);
            if (current_mos_id != -1)
            {
                mos[current_mos_id].volume_data = vol;
                mos[current_mos_id].DisposeBuffers();
                mos[current_mos_id].Initialize();
            }
            else
            {
                var go = new GameObject($"orbital");
                //var orbital_whd = vol.spacing.multiply(new Vector3(vol.dim.x, vol.dim.y, vol.dim.z));
                //var orbital_pos = vol.origin + 0.5f * orbital_whd;
                go.transform.position = mol.getBounds().center;
                var mol_comp = go.AddComponent<MoleculeOrbital>();
                go.transform.parent = mol.transform;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.material = new Material(Shader.Find("Shader Graphs/chARpackTransparentMaterial"));
                renderer.material.color = new Color(renderer.material.color.r, renderer.material.color.g, renderer.material.color.b, 0.15f);
                go.AddComponent<MeshFilter>();
                mol_comp.mol_reference = mol;
                mol_comp.volume_data = vol;

                mol_comp.Initialize();
                mos.Add(mol_comp);
            }
        }

        public void Clear()
        {
            foreach (MoleculeOrbital m in mos)
            {
                Destroy(m.gameObject);
            }
        }

        void Update()
        {
            for (int i = 0; i < mos.Count(); i++)
            {
                if (!mos[i].initialized) continue;

                // Rebuild the isosurface only when the target value has been changed.
                if (mos[i].target_iso_value == mos[i].current_iso_value) continue;

                mos[i].builder.BuildIsosurface(mos[i].compute_buffer, mos[i].target_iso_value, mos[i].volume_data.spacing);
                mos[i].gameObject.GetComponent<MeshFilter>().sharedMesh = mos[i].builder.Mesh;

                mos[i].current_iso_value = mos[i].target_iso_value;
            }
        }
    }
}