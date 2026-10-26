Shader "4CORE/Freeze Ripple"
{
    Properties
    {
        _MainTex("Texture",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _Center("Ripple center",Vector)=(0.5,0.5,0,0)
        _Radius("Ripple radius",Float)=2.5
        _Aspect("Screen aspect",Float)=1.7778
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;float4 _Center;float _Radius;float _Aspect;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv;output.color=input.color*_Color;return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 delta=(input.uv-_Center.xy)*float2(_Aspect,1);
                float distance=length(delta);
                float inside=1-smoothstep(_Radius-.02,_Radius+.02,distance);
                float edge=1-smoothstep(.002,.018,abs(distance-_Radius));
                half4 result=input.color;result.rgb=lerp(result.rgb,half3(.72,.55,1),edge*.7);
                result.a*=inside;result.a+=edge*input.color.a*.9;return result;
            }
            ENDHLSL
        }
    }
}
