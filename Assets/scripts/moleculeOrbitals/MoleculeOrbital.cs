using MarchingCubes;
using UnityEngine;


namespace chARpack
{

    public class MoleculeOrbital : MonoBehaviour
    {
        // required to be set by user
        [HideInInspector] public Molecule mol_reference;
        [HideInInspector] public ScalarVolume volume_data;

        // initialized later
        [HideInInspector] public ComputeBuffer compute_buffer;
        [HideInInspector] public MeshBuilder builder;

        // with default values
        public bool initialized = false;
        public float target_iso_value = 0.01f;
        [HideInInspector] public float current_iso_value = 0f;


        public void Initialize()
        {
            //Debug.Log($"Num values: {volume_data.values.Count}; VoxelCount: {volume_data.num_voxels}");
            compute_buffer = new ComputeBuffer(volume_data.num_voxels, sizeof(float));
            compute_buffer.SetData(volume_data.values.ToArray());
            builder = new MeshBuilder(volume_data.dim, MoleculeOrbitals.getTriangleBudget(), MoleculeOrbitals.getBuilderCompute());

            // Do a calculation
            builder.BuildIsosurface(compute_buffer, target_iso_value, volume_data.spacing);
            GetComponent<MeshFilter>().sharedMesh = builder.Mesh;

            current_iso_value = target_iso_value;

            initialized = true;

            // Voxel data conversion (ushort -> float)
            //using var readBuffer = new ComputeBuffer(VoxelCount / 2, sizeof(uint));
            //readBuffer.SetData(vol.values.ToArray());

            //_converterCompute.SetInts("Dims", _dimensions);
            //_converterCompute.SetBuffer(0, "Source", readBuffer);
            //_converterCompute.SetBuffer(0, "Voxels", _voxelBuffer);
            //_converterCompute.DispatchThreads(0, _dimensions);

        }

        public void DisposeBuffers()
        {
            if (compute_buffer != null)
            {
                compute_buffer.Dispose();
            }
            if (builder != null)
            {
                builder.Dispose();

            }
        }

        void OnDestroy()
        {
            DisposeBuffers();
            MoleculeOrbitals.mos.Remove(this);
        }

    }
}
