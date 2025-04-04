using System.Diagnostics;
using Draco;
using Draco.Encode;
using UnityEngine;


public class Decoder : MonoBehaviour
{
    // Start is called before the first frame update
    async void Start()
    {
        var cloud_go = GameObject.Find("calibration_cloud_0");

        var cloud_mesh = cloud_go.GetComponent<MeshFilter>().mesh;


        UnityEngine.Debug.Log($"Has Color {cloud_mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)}");

        //UnityEngine.Debug.Log($"Not encoded Length {cloud_mesh.}");

        var speed_settings = new SpeedSettings(0, 0);



        EncodeResult[] res_array = new EncodeResult[10];
        for (var i =0; i < 10; i++)
        {
            Stopwatch swe = Stopwatch.StartNew();
            var encode_res = await DracoEncoder.EncodeMesh(cloud_mesh, QuantizationSettings.Default, speed_settings);
            res_array[i] = encode_res[0];
            swe.Stop();

            UnityEngine.Debug.Log($"ENCODE TIME {swe.ElapsedMilliseconds}");
            UnityEngine.Debug.Log($"ENCODE Length {encode_res[0].data.Length}");
        }

        for (var i = 0; i < 10; i++)
        {
            Stopwatch swd = Stopwatch.StartNew();
            var d_mesh = await DracoDecoder.DecodeMesh(res_array[i].data);
            swd.Stop();
            UnityEngine.Debug.Log($"DECODE TIME {swd.ElapsedMilliseconds}");
            UnityEngine.Debug.Log($"DECODE Has Color {d_mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)}");
        }



    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
