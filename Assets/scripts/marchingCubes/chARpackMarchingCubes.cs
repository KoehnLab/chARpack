using IngameDebugConsole;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System.Collections.Generic;
using MarchingCubes;
using System.Linq;
using UnityEngine;

class chARpackMarchingCubes : MonoBehaviour
{
    [SerializeField] Vector3Int _dimensions = new Vector3Int(256, 256, 113);
    [SerializeField] Vector3 _spacing = Vector3.one;
    [SerializeField] int _triangleBudget = 65536 * 16;


    [SerializeField] ComputeShader _converterCompute = null;
    [SerializeField] ComputeShader _builderCompute = null;

    public float TargetValue = 0.01f;
    float _builtTargetValue;

    static Dictionary<ScalarVolume, GameObject> volumes;

    int VoxelCount => _dimensions.x * _dimensions.y * _dimensions.z;

    ComputeBuffer _voxelBuffer;
    MeshBuilder _builder;
    bool initialized = false;

    private void Start()
    {
        DebugLogConsole.AddCommand("testMarchingCubes", "runs a test for the marching cubes algorithm", loadData);
        volumes = new Dictionary<ScalarVolume, GameObject>();
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
                _dimensions =volume.dim;
                _spacing = volume.spacing;
                volumes.Add(volume, go);
                Initialize(volume);
            }
            else
            {
                yield break;
            }
        }
    }


    void Initialize(ScalarVolume vol)
    {
        Debug.Log($"Num values: {vol.values.Count}; VoxelCount: {VoxelCount}");
        _voxelBuffer = new ComputeBuffer(VoxelCount, sizeof(float));
        _voxelBuffer.SetData(vol.values.ToArray());
        _builder = new MeshBuilder(_dimensions, _triangleBudget, _builderCompute);

        // Voxel data conversion (ushort -> float)
        //using var readBuffer = new ComputeBuffer(VoxelCount / 2, sizeof(uint));
        //readBuffer.SetData(vol.values.ToArray());

        //_converterCompute.SetInts("Dims", _dimensions);
        //_converterCompute.SetBuffer(0, "Source", readBuffer);
        //_converterCompute.SetBuffer(0, "Voxels", _voxelBuffer);
        //_converterCompute.DispatchThreads(0, _dimensions);

        initialized = true;

    }

    void OnDestroy()
    {
        _voxelBuffer.Dispose();
        _builder.Dispose();
    }

    void Update()
    {
        if (!initialized) return;
        // Rebuild the isosurface only when the target value has been changed.
        if (TargetValue == _builtTargetValue) return;

        _builder.BuildIsosurface(_voxelBuffer, TargetValue, _spacing);
        volumes.First().Value.GetComponent<MeshFilter>().sharedMesh = _builder.Mesh;

        _builtTargetValue = TargetValue;
    }

}

