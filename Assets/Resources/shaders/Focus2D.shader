Shader "Focus2D"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0

        // Custom
        _NumFoci("Number of Foci", Range(1,4)) = 1
    }

        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
                "PreviewType" = "Plane"
                "CanUseSpriteAtlas" = "True"
            }

            Stencil
            {
                Ref[_Stencil]
                Comp[_StencilComp]
                Pass[_StencilOp]
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
            }

            Cull Off
            Lighting Off
            ZWrite Off
            ZTest[unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask[_ColorMask]

            Pass
            {
                Name "Default"
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #include "UnityCG.cginc"
                #include "UnityUI.cginc"

                #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
                #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

                struct appdata_t
                {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord  : TEXCOORD0;
                    float4 worldPosition : TEXCOORD1;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                fixed4 _Color;
                fixed4 _TextureSampleAdd;
                float4 _ClipRect;
                float4 _MainTex_ST;
                //
                uniform float _NumFoci;
                uniform fixed4 _FociColors[4];
                static const float pi = 3.141592653589793238462f;


                v2f vert(appdata_t v)
                {
                    v2f OUT;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                    OUT.worldPosition = v.vertex;
                    OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                    OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);


                    if (_NumFoci == 2) {
                        if (OUT.texcoord.x > 0.5f) {
                            OUT.color = _FociColors[0];
                        }
                        else {
                            OUT.color = _FociColors[1];
                        }
                    }
                    else if (_NumFoci == 3) {
                        float angle = atan2(OUT.texcoord.y, OUT.texcoord.x) + pi;
                        if (angle < (2.f * pi / 3.f)) {
                            OUT.color = _FociColors[1];
                        }
                        else if (angle > (2.f * pi / 3.f) && angle < 2.f * (2.f * pi / 3.f)) {
                            OUT.color = _FociColors[0];
                        }
                        else {
                            OUT.color = _FociColors[2];
                        }
                    }
                    else if (_NumFoci == 4) {
                        if (OUT.texcoord.x >= 0.5f && OUT.texcoord.y >= 0.5f) {
                            OUT.color = _FociColors[0];
                        }
                        else if (OUT.texcoord.x >= 0.5f && OUT.texcoord.y < 0.5f) {
                            OUT.color = _FociColors[1];
                        }
                        else if (OUT.texcoord.x < 0.5f && OUT.texcoord.y < 0.5f) {
                            OUT.color = _FociColors[2];
                        }
                        else if (OUT.texcoord.x < 0.5f && OUT.texcoord.y >= 0.5f) {
                            OUT.color = _FociColors[3];
                        }
                    }
                    else {
                        OUT.color = _FociColors[0];
                    }


                    //OUT.color = v.color * _Color;
                    return OUT;
                }

                fixed4 frag(v2f IN) : SV_Target
                {
                    //half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                    //#ifdef UNITY_UI_CLIP_RECT
                    //color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                    //#endif

                    //#ifdef UNITY_UI_ALPHACLIP
                    //clip(color.a - 0.001);
                    //#endif

                    float alpha = clamp(0.0f, 0.8f, IN.color.a);
                    return float4(IN.color.rgb, alpha);
                }
            ENDCG
            }
        }
}