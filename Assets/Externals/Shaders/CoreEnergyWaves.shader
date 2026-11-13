Shader "4CORE/Core Energy Waves"
{
    Properties
    {
        // CoreLinkView feeds the PIXELFX "core-link" beam sheet and the current frame's rect (_MainTex_ST).
        [MainTexture] _MainTex ("Beam Sheet", 2D) = "white" {}
        _HotBoost ("Hot Core Boost", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "CoreLinkColor"
            // The project uses URP's 2D Renderer, which draws this unlit pass.
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_point_clamp);
            float4 _MainTex_ST;
            half _HotBoost;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // The beam sheet is greyscale pixel art: tint it with the LineRenderer's red-to-blue
                // vertex gradient and let the hottest pixels stay near white. Without a sheet this is
                // the plain vertex-color ribbon.
                half4 beam = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv);
                half hot = pow(beam.r, 6.0h) * _HotBoost;
                half3 rgb = beam.rgb * input.color.rgb + hot;
                return half4(saturate(rgb), beam.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
