Shader "Unlit/ThresholdSmoke"
{
    Properties
    {
        _AccumTex ("Accumulation Texture", 2D) = "white" {}
        _Threshold ("Alpha Threshold", Range(0, 5)) = 1.0
        _SmokeColor ("Smoke Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD1;
            };

            sampler2D _AccumTex;
            float _Threshold;
            fixed4 _SmokeColor;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = v.uv;
                return o;
            }

			sampler2D_float _CameraDepthTexture;
            fixed4 frag (v2f i) : SV_Target {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

				// Sample our custom buffer
				float4 smokeData = tex2D(_AccumTex, screenUV);
				float accumulatedAlpha = smokeData.a;
				float smokeDepth = smokeData.r;

				// Sample the scene's depth buffer
				float sceneDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);

				// Discard smoke pixel if it's behind the scene geometry
				if (sceneDepth > smokeDepth) {
					discard;
				}
				// return fixed4(smokeDepth, smokeDepth, smokeDepth, 1);

				// Discard smoke pixel if it's below the alpha threshold
				if (accumulatedAlpha < _Threshold) {
					discard;
				}

				// approximate smoke density by fading it out
				// the closer it is to the camera
				_SmokeColor.a *= saturate(1-smokeDepth*5);
				return _SmokeColor;
            }
            ENDCG
        }
    }
}
