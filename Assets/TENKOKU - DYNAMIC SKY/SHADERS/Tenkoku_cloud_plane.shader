Shader "TENKOKU/cloud_plane"
{
    Properties
    {
        _MainTex ("Clouds A", 2D) = "white" {}
        _CloudTexB ("Clouds B", 2D) = "white" {}
        _BlendTex ("Blend", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TintColor ("Tint", Color) = (1,1,1,1)
        _brightMult ("Brightness", Range(0.1, 4.0)) = 1.0

        _amtCloud ("Cloud Master", Range(0.0, 1.0)) = 0.5
        _amtCloudC ("Cloud Cirrus", Range(0.0, 1.0)) = 0.5
        _amtCloudM ("Cloud Cumulus", Range(0.0, 1.0)) = 0.5
        _amtCloudO ("Cloud Overcast", Range(0.0, 1.0)) = 0.5
        _amtCloudS ("Cloud Stratus", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CloudTexB);
            SAMPLER(sampler_CloudTexB);
            TEXTURE2D(_BlendTex);
            SAMPLER(sampler_BlendTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _CloudTexB_ST;
                float4 _BlendTex_ST;
                float4 _Color;
                float4 _TintColor;
                float _brightMult;
                float _amtCloud;
                float _amtCloudC;
                float _amtCloudM;
                float _amtCloudO;
                float _amtCloudS;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uvA = TRANSFORM_TEX(input.uv, _MainTex);
                float2 uvB = TRANSFORM_TEX(input.uv, _CloudTexB);
                float2 uvBlend = TRANSFORM_TEX(input.uv, _BlendTex);

                half4 cloudA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvA);
                half4 cloudB = SAMPLE_TEXTURE2D(_CloudTexB, sampler_CloudTexB, uvB);
                half blend = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, uvBlend).r;

                // In original Tenkoku workflow, these are animated by runtime scripts and can be 0 in assets.
                // Keep clouds visible in standalone usage with a safe minimum density.
                half density = max(max(max(_amtCloud, _amtCloudM), max(_amtCloudS, _amtCloudC)), _amtCloudO);
                density = max(density, 0.35h);

                half3 rgb = lerp(cloudA.rgb, cloudB.rgb, 0.5h);
                half alpha = saturate((cloudA.a * 0.65h + cloudB.a * 0.35h) * blend * density);

                half4 col;
                col.rgb = rgb * _Color.rgb * _TintColor.rgb * max(_brightMult, 0.1h);
                // Tenkoku cloud particles can use startColor alpha at 0, so avoid vertex alpha here.
                col.a = alpha * _Color.a * _TintColor.a;
                return col;
            }
            ENDHLSL
        }
    }
}
