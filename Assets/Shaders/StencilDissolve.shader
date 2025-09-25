// Unity ShaderLab code for an alpha dissolve particle effect
// that uses a stencil buffer to draw a solid color where
// particle density is high.
Shader "Particles/StencilDissolve"
{
    Properties
    {
        _SolidColor("Solid Color", Color) = (1, 0.5, 0, 1)
        _DissolveTex("Dissolve Texture (Grayscale)", 2D) = "white" {}
        _StencilThreshold("Stencil Threshold", Range(0, 255)) = 5
        _DissolveStrength("Dissolve Strength", Range(0, 1)) = 0.5
    }
    SubShader
    {
        // Use a transparent queue for proper sorting with other transparent objects.
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        // =======================================================================
        // ALPHA ACCUMULATION PASSES
        // We use multiple passes to make the contribution to the stencil buffer
        // proportional to the particle's alpha. A high-alpha fragment will pass
        // the clip test in more stages, incrementing the stencil buffer multiple times.
        // =======================================================================

        // Pass for alpha > 0.2
        Pass
        {
            ColorMask 0
            ZWrite Off
            Blend One One
            Stencil { Ref 1 Comp Always Pass IncrSat }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _DissolveTex;
            float _DissolveStrength;

            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float particleAlpha = i.color.a;
                clip(particleAlpha - 0.2); // Threshold for this pass

                float dissolveValue = tex2D(_DissolveTex, i.uv).a;
                float clipThreshold = particleAlpha * _DissolveStrength;
                clip(dissolveValue - clipThreshold);

                return fixed4(1,1,1,1);
            }
            ENDCG
        }

        // Pass for alpha > 0.4
        Pass
        {
            ColorMask 0
            ZWrite Off
            Blend One One
            Stencil { Ref 1 Comp Always Pass IncrSat }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _DissolveTex;
            float _DissolveStrength;

            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float particleAlpha = i.color.a;
                clip(particleAlpha - 0.4); // Threshold for this pass

                float dissolveValue = tex2D(_DissolveTex, i.uv).a;
                float clipThreshold = particleAlpha * _DissolveStrength;
                clip(dissolveValue - clipThreshold);

                return fixed4(1,1,1,1);
            }
            ENDCG
        }

        // Pass for alpha > 0.6
        Pass
        {
            ColorMask 0
            ZWrite Off
            Blend One One
            Stencil { Ref 1 Comp Always Pass IncrSat }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _DissolveTex;
            float _DissolveStrength;

            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float particleAlpha = i.color.a;
                clip(particleAlpha - 0.6); // Threshold for this pass

                float dissolveValue = tex2D(_DissolveTex, i.uv).a;
                float clipThreshold = particleAlpha * _DissolveStrength;
                clip(dissolveValue - clipThreshold);

                return fixed4(1,1,1,1);
            }
            ENDCG
        }

        // Pass for alpha > 0.8
        Pass
        {
            ColorMask 0
            ZWrite Off
            Blend One One
            Stencil { Ref 1 Comp Always Pass IncrSat }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _DissolveTex;
            float _DissolveStrength;

            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float particleAlpha = i.color.a;
                clip(particleAlpha - 0.8); // Threshold for this pass

                float dissolveValue = tex2D(_DissolveTex, i.uv).a;
                float clipThreshold = particleAlpha * _DissolveStrength;
                clip(dissolveValue - clipThreshold);

                return fixed4(1,1,1,1);
            }
            ENDCG
        }

        // =======================================================================
        // PASS 2: Draw the solid color based on the stencil buffer's contents
        // =======================================================================
        Pass
        {
            // We write color now, but still don't affect the depth buffer.
            ZWrite Off
            ColorMask RGBA

            // Use standard alpha blending for the final result.
            Blend SrcAlpha OneMinusSrcAlpha

            // Stencil Buffer Operations
            Stencil
            {
                Ref [_StencilThreshold] // Compare against the user-defined threshold.
                Comp Less // The test: only draw if the stencil buffer's value is > Ref.
                Pass Keep // Don't modify the stencil buffer on this pass.
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            
            fixed4 _SolidColor;

            v2f vert (appdata v)
            {
                v2f o;
                // In this pass, we just need to draw over the entire area
                // covered by the particles. Re-rendering the particles themselves
                // is an easy way to ensure this.
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // If the stencil test passed, we simply return the solid color.
                return _SolidColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}

