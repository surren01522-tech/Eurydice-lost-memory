Shader "Eurydice/URP Moss Ground Sway"
{
    Properties
    {
        _MossDark ("Moss Dark", Color) = (0.10, 0.30, 0.055, 1)
        _MossLight ("Moss Light", Color) = (0.38, 0.63, 0.16, 1)
        _NoiseScale ("Moss Noise Scale", Range(0.05, 4)) = 0.65
        _WindStrength ("Short Grass Wind", Range(0, 0.2)) = 0.035
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.1
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                half3 vertexColor : COLOR;
                half grassMask : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _MossDark;
                half4 _MossLight;
                float _NoiseScale;
                float _WindStrength;
                float _WindSpeed;
            CBUFFER_END

            float4 _GrassInteractorPosition;
            float4 _GrassInteractionParams;

            float MossNoise(float2 p)
            {
                float a = sin(p.x * 1.37 + sin(p.y * 0.91));
                float b = sin(p.y * 2.11 + p.x * 0.63);
                float c = sin((p.x + p.y) * 3.17) * 0.35;
                return saturate((a + b + c) * 0.22 + 0.5);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float tip = saturate(input.color.a);
                float phase = dot(world.xz, float2(0.72, 1.19)) * 0.8 + _Time.y * _WindSpeed;
                world.xz += float2(sin(phase), cos(phase * 0.71)) * _WindStrength * tip;

                float2 playerDelta = world.xz - _GrassInteractorPosition.xz;
                float playerDistance = length(playerDelta);
                float interactionRadius = max(_GrassInteractionParams.x, 0.001);
                float interaction = 1.0 - smoothstep(interactionRadius * 0.2, interactionRadius, playerDistance);
                float2 pushDirection = playerDelta / max(playerDistance, 0.001);
                float ripple = sin(playerDistance * 8.0 - _Time.y * _GrassInteractionParams.w);
                float pushAmount = (_GrassInteractionParams.y + ripple * _GrassInteractionParams.z) * interaction;
                world.xz += pushDirection * pushAmount * tip;

                output.positionCS = TransformWorldToHClip(world);
                output.worldPos = world;
                output.vertexColor = input.color.rgb;
                output.grassMask = tip;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float broad = MossNoise(input.worldPos.xz * _NoiseScale);
                float fine = MossNoise(input.worldPos.xz * (_NoiseScale * 5.3));
                half3 groundColor = lerp(_MossDark.rgb, _MossLight.rgb, broad * 0.72 + fine * 0.28);
                half3 color = lerp(groundColor, input.vertexColor, input.grassMask);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
