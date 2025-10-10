Shader "UI/NoiseDistortion"
{
    Properties
    {
        _DistortTex ("Normal Texture", 2D) = "white" {}
        _MainTex ("Dummy Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
		_NoiseStr("Noise Strength", Float) = 1
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
                float4 color    : COLOR;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD1;
				fixed4 color    : COLOR;
            };

            
            fixed4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.color = v.color * _Color;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			sampler2D _DistortTex;
			float4 _DistortTex_ST;

			float _NoiseStr;

            fixed4 frag (v2f i) : SV_Target {
                // distort i.uv by the normal noise texture
				float2 uv = i.uv;

				float2 noiseUV = i.uv * _DistortTex_ST.xy + _DistortTex_ST.zw;
				float4 noise = tex2D(_DistortTex, noiseUV);

				noise = tex2D(_DistortTex, noise.xy);

				uv += noise.xy * _MainTex_TexelSize.xy * _NoiseStr;

				// then sample the texture at that uv
				return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
