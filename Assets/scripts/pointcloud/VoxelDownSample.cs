using IngameDebugConsole;

namespace chARpack
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class VoxelDownSample : MonoBehaviour
    {
        public ComputeShader voxelDownSampleShader;
        public ComputeBuffer inputPosBuffer, outputPosBuffer, inputColBuffer, outputColBuffer, voxelGridBuffer, outputCountBuffer;


        public float voxelSize = 5.0f;

        void Start()
        {
            DebugLogConsole.AddCommand("voxeldownsample", "Performs a voxel downsample", PerformDownsampling);
            DebugLogConsole.AddCommand<float>("setVoxelSize", "Sets the voxel size for downsampling", setVoxelSize);
        }

        void setVoxelSize(float vSize)
        {
            voxelSize = vSize;
        }

        void PerformDownsampling()
        {
            var cloud_go = GameObject.Find("calibration_cloud_0");
            var cloud_mesh = cloud_go.GetComponent<MeshFilter>().sharedMesh;


            int numPoints = cloud_mesh.vertices.Length;
            UnityEngine.Debug.Log($"NumPoints before Downsampling: {numPoints}");

            var sw = Stopwatch.StartNew();

            inputPosBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
            inputColBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
            outputPosBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
            outputColBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
            //int estimatedVoxels = 10 * numPoints; // / ((int)Mathf.Pow(voxelSize,3)); //2 * (numPoints / 10);
            int estimatedVoxels = numPoints / 10;
            voxelGridBuffer = new ComputeBuffer(estimatedVoxels, sizeof(int));
            outputCountBuffer = new ComputeBuffer(1, sizeof(int));

            inputPosBuffer.SetData(cloud_mesh.vertices);
            inputColBuffer.SetData(cloud_mesh.colors);
            outputCountBuffer.SetData(new int[] { 0 });
            voxelGridBuffer.SetData(Enumerable.Repeat(-1, estimatedVoxels).ToArray());

            voxelDownSampleShader.SetFloat("voxelSize", voxelSize);
            voxelDownSampleShader.SetInt("numPoints", numPoints);
            voxelDownSampleShader.SetInt("voxelGridSize", estimatedVoxels);
            voxelDownSampleShader.SetBuffer(0, "InputPositions", inputPosBuffer);
            voxelDownSampleShader.SetBuffer(0, "InputColors", inputColBuffer);
            voxelDownSampleShader.SetBuffer(0, "OutputPositions", outputPosBuffer);
            voxelDownSampleShader.SetBuffer(0, "OutputColors", outputColBuffer);
            voxelDownSampleShader.SetBuffer(0, "VoxelGrid", voxelGridBuffer);
            voxelDownSampleShader.SetBuffer(0, "OutputCount", outputCountBuffer);

            

            int threadGroupCount = Mathf.CeilToInt((float)numPoints / 64);
            voxelDownSampleShader.Dispatch(0, threadGroupCount, 1, 1);

            // Retrieve number of valid output points
            int[] newPointCount = new int[1];
            outputCountBuffer.GetData(newPointCount);
            int validPoints = newPointCount[0];

            // Retrieve only the valid points
            Vector3[] outputPositions = new Vector3[validPoints];
            Color[] outputColors = new Color[validPoints];

            outputPosBuffer.GetData(outputPositions);
            outputColBuffer.GetData(outputColors);

            //Vector3[] allOutputPositions = new Vector3[numPoints];
            //Color[] allOutputColors = new Color[numPoints];

            //outputPosBuffer.GetData(allOutputPositions);
            //outputColBuffer.GetData(allOutputColors);

            //// Then slice out only the valid part
            //var outputPositions = allOutputPositions.Take(validPoints).ToArray();
            //var outputColors = allOutputColors.Take(validPoints).ToArray();

            sw.Stop();
            UnityEngine.Debug.Log($"Downsampling Performance: {sw.ElapsedMilliseconds} ms");

            //for (int i = 0; i < validPoints/100; i++)
            //{
            //    UnityEngine.Debug.Log($"{outputPositions}");
            //}

            UnityEngine.Debug.Log("Downsampling complete. Output Points: " + validPoints);

            var go = new GameObject();
            var meshr = go.AddComponent<MeshRenderer>();
            meshr.sharedMaterial = Instantiate(Resources.Load<Material>("materials/myPointCloudMaterial"));
            meshr.sharedMaterial.SetFloat("_PointSize", 1.0f);
            var meshf = go.AddComponent<MeshFilter>();

            var new_mesh = new Mesh();
            new_mesh.vertices = outputPositions;
            new_mesh.colors = outputColors;
            new_mesh.indexFormat = validPoints > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            new_mesh.SetIndices(Enumerable.Range(0, validPoints).ToArray(), MeshTopology.Points, 0);

            meshf.sharedMesh = new_mesh;

            //cloud_mesh.vertices = outputPositions;
            //cloud_mesh.colors = outputColors;

            inputPosBuffer.Dispose();
            inputColBuffer.Dispose();
            outputPosBuffer.Dispose();
            outputColBuffer.Dispose();
            voxelGridBuffer.Dispose();
            outputCountBuffer.Dispose();
        }

        void OnDestroy()
        {
            inputPosBuffer.Release();
            inputColBuffer.Release();
            outputPosBuffer.Release();
            outputColBuffer.Release();
            voxelGridBuffer.Release();
            outputCountBuffer.Release();
        }
    }
}
