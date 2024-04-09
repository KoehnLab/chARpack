Shader "Point Cloud/Disk"
{
    Properties
    {
        _Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _PointSize("Point Size", Float) = 0.05
    }
        SubShader
        {
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "Queue" = "Transparent"
            }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                Name "FORWARD"
                Tags { "LightMode" = "UniversalForward" }

                CGPROGRAM
                #pragma vertex Vertex
                #pragma geometry Geometry
                #pragma fragment Fragment
                #include "UnityCG.cginc"

                #pragma multi_compile_instancing

                #define _UNITY_COLORSPACE_GAMMA
                #include "Disk.cginc"

                ENDCG
            }

            Pass
            {
                Name "SHADOWCASTER"
                Tags { "LightMode" = "ShadowCaster" }

                CGPROGRAM
                #pragma vertex Vertex
                #pragma geometry Geometry
                #pragma fragment Fragment

                #define _COMPUTE_BUFFER
                #define PCX_SHADOW_CASTER 0
                #include "Disk.cginc"

                ENDCG
            }
        }
}
