Shader "Custom3D/CustomSwimEffect"
{
    Properties
    {
        [NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
        [NoScaleOffset]_Disolve("Disolve", 2D) = "white" {}
        // These are not needed for a standard shader
        // [HideInInspector] _texcoord( "", 2D ) = "white" {}
        // [HideInInspector] _texcoord2( "", 2D ) = "white" {}
        [HideInInspector] __dirty( "", Int ) = 1
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+1" "IgnoreProjector" = "True" }
        Cull Off

        Pass
        {
            Tags { "LightMode" = "ForwardBase" } // or just leave it empty for an unlit effect

            // Blending for transparency
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 vertexColor : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            uniform sampler2D _MainTex;
            uniform sampler2D _Disolve;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.vertexColor = v.color;
                o.uv = v.uv;
                o.uv2 = v.uv2;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate color from vertex color
                fixed3 albedo = i.vertexColor.rgb;

                // Calculate dissolve and alpha
                float dissolveValue = tex2D(_Disolve, i.uv2).r;
                float alphaValue = tex2D(_MainTex, i.uv).a;
                float dissolveEffect = step(1.0 - i.vertexColor.a, (dissolveValue * 0.5) + 0.5);
                fixed alpha = dissolveEffect * alphaValue;

                // Final color is just the albedo with the calculated alpha
                return fixed4(albedo, alpha);
            }

            ENDCG
        }
    }
    Fallback "Legacy Shaders/Transparent/Diffuse"
}
