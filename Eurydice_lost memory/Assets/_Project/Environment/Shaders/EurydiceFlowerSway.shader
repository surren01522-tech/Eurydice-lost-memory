Shader "Eurydice/URP Flower Sway"
{
    Properties
    {
        _BaseColor ("Color Tint", Color) = (1,1,1,1)
        [MainTexture] _FlowerTexture ("Flower Texture", 2D) = "white" {}
        _WindStrength ("Wind Strength", Range(0, 0.5)) = 0.16
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.35
        _WindScale ("Wind World Scale", Range(0.01, 2)) = 0.32
        _GustStrength ("Gust Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 color : COLOR;
                half fogFactor : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half flowerMask : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _WindStrength;
                float _WindSpeed;
                float _WindScale;
                float _GustStrength;
            CBUFFER_END
            TEXTURE2D(_FlowerTexture);
            SAMPLER(sampler_FlowerTexture);
            float4 _GrassInteractorPosition;
            float4 _GrassInteractionParams;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float phase = dot(world.xz, float2(0.83, 1.17)) * _WindScale + _Time.y * _WindSpeed;
                float gust = sin(phase) + sin(phase * 0.47 + 1.9) * _GustStrength;
                float tipMask = saturate(input.color.a);
                world.xz += float2(gust, gust * 0.35) * _WindStrength * tipMask;

                float2 playerDelta = world.xz - _GrassInteractorPosition.xz;
                float playerDistance = length(playerDelta);
                float interactionRadius = max(_GrassInteractionParams.x, 0.001);
                float interaction = 1.0 - smoothstep(interactionRadius * 0.2, interactionRadius, playerDistance);
                float2 pushDirection = playerDelta / max(playerDistance, 0.001);
                float ripple = sin(playerDistance * 8.0 - _Time.y * _GrassInteractionParams.w);
                float pushAmount = (_GrassInteractionParams.y + ripple * _GrassInteractionParams.z) * interaction;
                world.xz += pushDirection * pushAmount * tipMask;

                output.positionCS = TransformWorldToHClip(world);
                half lightPulse = 0.78h + 0.22h * saturate(sin(phase * 0.25h) * 0.5h + 0.5h);
                output.color = input.color.rgb * _BaseColor.rgb * lightPulse;
                output.uv = input.uv;
                output.flowerMask = step(input.color.g, max(input.color.r, input.color.b));
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 flower = SAMPLE_TEXTURE2D(_FlowerTexture, sampler_FlowerTexture, input.uv);
                clip(lerp(1.0h, flower.a, input.flowerMask) - 0.35h);
                half3 textured = input.color * lerp(1.0h.xxx, flower.rgb, input.flowerMask);
                return half4(MixFog(textured, input.fogFactor), 1.0h);
            }
            ENDHLSL
        }
    }
}
