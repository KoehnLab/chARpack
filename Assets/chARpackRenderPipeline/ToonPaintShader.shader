Shader "Custom/ToonPaint"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    Varyings vert(Attributes IN)
    {
        Varyings OUT;
        OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
        OUT.uv = IN.uv;
        return OUT;
    }

    half4 frag(Varyings IN) : SV_Target
    {
        half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

        // Toon quantization
        float brightness = dot(color.rgb, float3(0.3, 0.59, 0.11));
        float toonShade = step(0.5, brightness) * 0.5 + step(0.75, brightness) * 0.5;
        return float4(1.0,1.0,1.0,1.0);
        //return float4(color.rgb * toonShade, color.a);
    }
    ENDHLSL
    SubShader
    {
        Pass
        {
            Name "PaintPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}

