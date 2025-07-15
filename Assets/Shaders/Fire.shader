// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom2D/Fire"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_CoreColor ("Core Color", Color) = (1, 1, 1, 1)
		_Color ("Base Color", Color) = (1,1,1,1)
		_RimColor ("Rim Color", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		_Noise("Noise Texture", 2D) = "white" {}
		_Voronoise("Voronoi Texture", 2D) = "white" {}
		_Gradient("Gradient Texture", 2D) = "white" {}
		_ScrollSpeed("Scroll Speed", Vector) = (1, 1, 1, 1)
		_NoiseScale("Noise Scale", Vector) = (1, 1, 1, 1)
		_CoreSize("Core Size", Float) = 0.1
		_MidSize("Mid Size", Float) = 0.30
		_RimSize("Rim Size", Float) = 0.42
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend OneMinusDstColor One

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
			};
			
			fixed4 _Color, _RimColor, _CoreColor;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif

				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float _AlphaSplitEnabled;
			float4 _ScrollSpeed;
			float4 _NoiseScale;
			sampler2D _Voronoise, _Noise, _Gradient;
			float _CoreSize, _MidSize, _RimSize;

			fixed4 frag(v2f IN) : SV_Target {
				float2 uv = IN.texcoord;

				uv *= _NoiseScale;
				uv.y -= _ScrollSpeed.y * _Time.x;
				uv.x += _ScrollSpeed.x * _Time.x;
				fixed noiseScroll = tex2D(_Noise, uv).r + tex2D(_Voronoise, uv).r;
				noiseScroll = clamp(noiseScroll, 0.0, 1.0);

				noiseScroll *= tex2D(_Gradient, IN.texcoord).r;

				// need separate params for outerfire and innerfire
				float x = round(noiseScroll+_CoreSize);
				fixed4 core = fixed4(x, x, x, x) * _CoreColor;

				float a = round(noiseScroll+_MidSize);
				fixed4 base = fixed4(a, a, a, a) * _Color;

				float b = round(noiseScroll+_RimSize);
				fixed4 rim = fixed4(b, b, b, b) * _RimColor;

				fixed4 c = lerp(rim, base, a);
				c = lerp(c, core, core.a);
				return c;
			}
		ENDCG
		}
	}
}
