Shader "Unlit/AccumulateAlpha"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        // Additive blending for the alpha channel
        // Blend One OneMinusSrcAlpha
        Blend One Zero, One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR; // Particle color
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
				float4 projPos : TEXCOORD1; // For depth calculation
            };

            sampler2D _MainTex;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
				o.projPos = ComputeScreenPos(o.vertex);
                return o;
            }

			sampler2D_float _CameraDepthTexture;
            fixed4 frag (v2f i) : SV_Target {
                // We use the particle's alpha (from texture and its color tint)
                // The Blend mode will add this to the render texture
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
				float depth = i.projPos.z / i.projPos.w;
				float2 screenUV = i.projPos.xy / i.projPos.w;
				float sceneDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
				if (col.a <= 0) {
					discard;
				}
                return fixed4(depth, 0, 0, col.a);
            }
            ENDCG
        }
    }
}
