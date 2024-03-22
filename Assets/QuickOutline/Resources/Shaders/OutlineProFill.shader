Shader "Custom/OutlinePro Fill" {
    Properties{
      [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0

      //_OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
      //_OutlineWidth("Outline Width", Range(0, 10)) = 2
      _NumOutlines("Num Outlines", Range(1,4)) = 1
      _MultiMode("Multi Outline Mode", Range(0,1)) = 0
    }

        SubShader{
          Tags {
            "Queue" = "Transparent+110"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
          }

          Pass {
            Name "Fill"
            Cull Off
            ZTest[_ZTest]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            Stencil {
              Ref 1
              Comp NotEqual
            }

            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
              float4 vertex : POSITION;
              float3 normal : NORMAL;
              float3 smoothNormal : TEXCOORD3;
              UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
              float4 position : SV_POSITION;
              fixed4 color : COLOR;
              UNITY_VERTEX_OUTPUT_STEREO
            };

            uniform int _MultiMode;
            uniform int _NumOutlines;
            uniform fixed4 _OutlineColor[4];
            uniform float _OutlineWidth[4];
            static const float pi = 3.141592653589793238462f;

            v2f vert(appdata input) {
              v2f output;

              UNITY_SETUP_INSTANCE_ID(input);
              UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

              float3 normal = any(input.smoothNormal) ? input.smoothNormal : input.normal;
              float3 viewPosition = UnityObjectToViewPos(input.vertex);
              float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));

              switch (_MultiMode) {
              case 0:
                  if (_NumOutlines == 2) {
                      if (viewNormal.x > 0.f) {
                          output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[0] / 1000.0);
                          output.color = _OutlineColor[0];
                      }
                      else {
                          output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[1] / 1000.0);
                          output.color = _OutlineColor[1];
                      }
                  }
                  else if (_NumOutlines == 3) {
                      float3 up = { 0.f, -1.f, 0.f };
                      float angle = atan2(viewNormal.y, viewNormal.x) + pi;
                      if (angle < (2.f * pi / 3.f)) {
                          output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[1] / 1000.0);
                          output.color = _OutlineColor[1];
                      }
                      else if (angle > (2.f * pi / 3.f) && angle < 2.f* (2.f * pi / 3.f)) {
                          output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[0] / 1000.0);
                          output.color = _OutlineColor[0];
                      }
                      else {
                          output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[2] / 1000.0);
                          output.color = _OutlineColor[2];
                      }
                  }
                  else if (_NumOutlines == 4) {
                      if (viewNormal.x >= 0.f && viewNormal.y >= 0.f) {
                          output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[0] / 1000.0);
                          output.color = _OutlineColor[0];
                      }
                      else if (viewNormal.x >= 0.f && viewNormal.y < 0.f) {
                          output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[1] / 1000.0);
                          output.color = _OutlineColor[1];
                      }
                      else if (viewNormal.x < 0.f && viewNormal.y < 0.f) {
                          output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[2] / 1000.0);
                          output.color = _OutlineColor[2];
                      }
                      else if (viewNormal.x < 0.f && viewNormal.y >= 0.f) {
                          output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[3] / 1000.0);
                          output.color = _OutlineColor[3];
                      }
                  }
                  else {
                      output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[0] / 1000.0);
                      output.color = _OutlineColor[0];
                  }
                  break;
              case 1:
                  output.position = UnityViewToClipPos(viewPosition + viewNormal * -viewPosition.z * _OutlineWidth[0] / 1000.0);
                  output.color = _OutlineColor[0];
                  break;
              }

              return output;
            }

            fixed4 frag(v2f input) : SV_Target {
              return input.color;
            }
            ENDCG
          }
    }
}
