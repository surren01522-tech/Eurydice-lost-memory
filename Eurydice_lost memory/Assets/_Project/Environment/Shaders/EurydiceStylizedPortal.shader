Shader "Eurydice/URP Stylized Portal"
{
    Properties
    {
        [MainTexture] _PortalTexture ("Portal Interior Texture", 2D) = "white" {}
        _TintColor ("Portal Tint", Color) = (0.45, 0.9, 0.5, 1)
        _EdgeColor ("Edge Glow", Color) = (0.75, 1, 0.6, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.78
        _Emission ("Emission", Range(0, 8)) = 2.2
        _Distortion ("Distortion", Range(0, 0.15)) = 0.035
        _FlowSpeed ("Flow Speed", Range(-5, 5)) = 0.45
        _EdgeWidth ("Edge Width", Range(0.01, 0.4)) = 0.13
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+20" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "PortalForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _PortalTexture_ST;
                half4 _TintColor;
                half4 _EdgeColor;
                float _Opacity;
                float _Emission;
                float _Distortion;
                float _FlowSpeed;
                float _EdgeWidth;
            CBUFFER_END
            TEXTURE2D(_PortalTexture);
            SAMPLER(sampler_PortalTexture);

            float PortalNoise(float2 p)
            {
                float first = sin(p.x * 8.7 + sin(p.y * 5.1));
                float second = sin(p.y * 13.3 - p.x * 3.7);
                return first * 0.62 + second * 0.38;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.worldPos = world;
                output.uv = TRANSFORM_TEX(input.uv, _PortalTexture);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _FlowSpeed;
                float2 centered = input.uv - 0.5;
                float noiseA = PortalNoise(input.uv * float2(3.2, 4.8) + float2(time * 0.27, -time));
                float noiseB = PortalNoise(input.uv.yx * float2(5.7, 2.3) + float2(-time * 0.41, time * 0.22));
                float2 distortion = float2(noiseA, noiseB) * _Distortion;
                float2 textureUV = input.uv + distortion;
                half4 interior = SAMPLE_TEXTURE2D(_PortalTexture, sampler_PortalTexture, textureUV);

                float edgeDistance = min(min(input.uv.x, 1.0 - input.uv.x), min(input.uv.y, 1.0 - input.uv.y));
                float innerMask = smoothstep(0.005, 0.035, edgeDistance);
                float edgeGlow = 1.0 - smoothstep(0.015, _EdgeWidth, edgeDistance);
                float verticalFlow = pow(saturate(0.5 + 0.5 * sin(input.uv.y * 17.0 - time * 3.2 + noiseA)), 5.0);
                float softPulse = 0.86 + sin(_Time.y * 1.7 + input.worldPos.y * 0.8) * 0.14;

                half3 color = interior.rgb * _TintColor.rgb;
                color += _EdgeColor.rgb * (edgeGlow * _Emission * softPulse);
                color += _EdgeColor.rgb * verticalFlow * 0.18 * _Emission;
                color = MixFog(color, input.fogFactor);
                half alpha = interior.a * _TintColor.a * _Opacity * innerMask;
                alpha = saturate(alpha + edgeGlow * 0.28 + verticalFlow * 0.05);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
