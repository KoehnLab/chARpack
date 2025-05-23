using IngameDebugConsole;
using SimpleFileBrowser;
using System.Collections;
using System.IO;
using UnityEngine;

public class SurfaceNets : MonoBehaviour
{
    public ComputeShader surfaceNetsShader;
    private int gridSizeX = 128; // X dimension of the grid
    private int gridSizeY = 128; // Y dimension of the grid
    private int gridSizeZ = 128; // Z dimension of the grid
    public float threshold = 0.5f; // Adjustable threshold for surface generation
    private Texture3D voxelFieldTexture;

    private ComputeBuffer verticesBuffer;
    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer verticesCountBuffer;

    private int kernelHandle;

    private bool isInitialized = false;


    private void Start()
    {
        DebugLogConsole.AddCommand("testSurfaceNets", "runs a test for the surface nets algorithm", loadData);
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

            if(fi.Extension.Contains("csv"))
            {
                var volume = CSVToVolume.LoadCSV(fi.FullName);
                yield return Initialize(volume.dim.x * volume.dim.y * volume.dim.z);
            }
            else
            {
                yield break;
            }
        }
    }


    IEnumerator Initialize(int num_voxels)
    {
        // Set up the compute shader
        kernelHandle = surfaceNetsShader.FindKernel("CSMain");

        // Create buffers to hold the results
        verticesBuffer = new ComputeBuffer(num_voxels, sizeof(float) * 3);
        trianglesBuffer = new ComputeBuffer(num_voxels, sizeof(int) * 3);
        verticesCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        verticesCountBuffer.SetData(new int[1] { 0 });


        // TODO generate texture
        // Set up the voxel field as a 3D texture
        surfaceNetsShader.SetTexture(kernelHandle, "voxelField", voxelFieldTexture);
        surfaceNetsShader.SetBuffer(kernelHandle, "vertices", verticesBuffer);
        surfaceNetsShader.SetBuffer(kernelHandle, "triangles", trianglesBuffer);
        surfaceNetsShader.SetBuffer(kernelHandle, "verticesCountBuffer", verticesCountBuffer);

        // Set dynamic grid size and threshold as constants in the shader
        surfaceNetsShader.SetInt("gridSizeX", gridSizeX);
        surfaceNetsShader.SetInt("gridSizeY", gridSizeY);
        surfaceNetsShader.SetInt("gridSizeZ", gridSizeZ);
        surfaceNetsShader.SetFloat("threshold", threshold);

        // Dispatch the compute shader
        // Ensure grid sizes are multiples of 8, adjust if necessary
        int dispatchX = Mathf.CeilToInt((float)gridSizeX / 8);
        int dispatchY = Mathf.CeilToInt((float)gridSizeY / 8);
        int dispatchZ = Mathf.CeilToInt((float)gridSizeZ / 8);
        surfaceNetsShader.Dispatch(kernelHandle, dispatchX, dispatchY, dispatchZ);

        isInitialized = true;

        yield return null;
    }

    void OnDestroy()
    {
        // Clean up buffers
        if (verticesBuffer != null)
            verticesBuffer.Release();
        if (trianglesBuffer != null)
            trianglesBuffer.Release();
        if (verticesCountBuffer != null)
            verticesCountBuffer.Release();
    }

    void Update()
    {
        if (!isInitialized) return;

        // Get the number of vertices generated by the compute shader
        int[] vertexCountArray = new int[1];
        verticesCountBuffer.GetData(vertexCountArray);
        int verticesCount = vertexCountArray[0];
        if (verticesCount == 0)
        {
            Debug.LogWarning("No vertices were generated!");
            return;
        }


        // Once the compute shader finishes, construct a mesh
        Vector3[] vertices = new Vector3[verticesCount];
        verticesBuffer.GetData(vertices);

        int[] triangles = new int[trianglesBuffer.count];
        trianglesBuffer.GetData(triangles);
        if (triangles.Length % 3 != 0)
        {
            Debug.LogError("Triangle count is not a multiple of 3!");
            return;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Optionally add normals, UVs, etc.
        mesh.RecalculateNormals();

        // Create a new GameObject and assign the mesh
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
}
