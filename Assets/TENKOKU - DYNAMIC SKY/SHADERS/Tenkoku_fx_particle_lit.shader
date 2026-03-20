Shader "TENKOKU/fx_Particle_Lit"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (1,1,1,0.5)
        _MainTex ("Particle Texture", 2D) = "white" {}
        _NightFac ("Night Factor", Range(0.0,1.0)) = 0.1
        _LightFac ("Light Factor", Range(0.0,1.0)) = 1.0
        _LightningFac ("Lightning Factor", Range(0.0,1.0)) = 1.0
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
        _OverBright ("Overbright", Range(0.0,5.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float _OverBright;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 col = texColor * input.color * _TintColor;
                col.rgb *= max(_OverBright, 0.0);
                return col;
            }
            ENDHLSL
        }
    }
}
