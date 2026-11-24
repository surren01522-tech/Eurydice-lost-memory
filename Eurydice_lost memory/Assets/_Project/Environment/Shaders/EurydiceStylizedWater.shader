Shader "Eurydice/URP Stylized Water"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.015, 0.10, 0.28, 0.88)
        _ShallowColor ("Shallow Color", Color) = (0.02, 0.58, 0.78, 0.72)
        _HorizonColor ("Horizon Color", Color) = (0.12, 0.38, 0.68, 1)
        _FoamColor ("Foam Color", Color) = (0.82, 0.96, 1, 1)
        _DepthFade ("Depth Fade", Range(0.1, 12)) = 3.2
        _FoamWidth ("Intersection Foam", Range(0.02, 2)) = 0.32
        _WaveHeight ("Vertex Wave Height", Range(0, 1)) = 0.16
        _WaveScale ("Wave Scale", Range(0.05, 3)) = 0.42
        _WaveSpeed ("Wave Speed", Range(0, 4)) = 0.72
        _NormalStrength ("Surface Detail", Range(0, 2)) = 0.82
        _SparkleStrength ("Sun Sparkles", Range(0, 4)) = 1.35
        _SparkleSize ("Sparkle Size", Range(8, 160)) = 72
        _CrestFoam ("Wave Crest Foam", Range(0, 1)) = 0.24
        _ReflectionStrength ("Sky Reflection", Range(0, 1)) = 0.38
        _RefractionStrength ("Refraction", Range(0, 0.08)) = 0.022
        _Transparency ("Transparency", Range(0.05, 1)) = 0.82
        _RippleRadius ("Ripple Maximum Radius", Range(0.5, 8)) = 2.8
        _RippleWidth ("Ripple Line Width", Range(0.02, 0.5)) = 0.14
        _RippleHeight ("Ripple Height", Range(0, 0.2)) = 0.045
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardWater"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                float waveHeight : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor, _ShallowColor, _HorizonColor, _FoamColor;
                float _DepthFade, _FoamWidth, _WaveHeight, _WaveScale, _WaveSpeed;
                float _NormalStrength, _SparkleStrength, _SparkleSize, _CrestFoam;
                float _ReflectionStrength, _RefractionStrength, _Transparency;
                float _RippleRadius, _RippleWidth, _RippleHeight;
                float4 _RippleData[4];
                float4 _RippleShape[4];
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float SmoothNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 fraction = frac(p);
                float2 blend = fraction * fraction * (3.0 - 2.0 * fraction);
                float bottom = lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), blend.x);
                float top = lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + 1.0), blend.x);
                return lerp(bottom, top, blend.y);
            }

            float WaveField(float2 p, float time)
            {
                float a = sin(dot(p, normalize(float2(1.0, 0.28))) * 1.42 + time * 0.72);
                float b = sin(dot(p, normalize(float2(-0.38, 1.0))) * 2.18 - time * 0.91);
                float c = sin(dot(p, normalize(float2(0.72, 1.0))) * 3.37 + time * 1.16);
                float d = sin(dot(p, normalize(float2(-1.0, 0.61))) * 4.83 - time * 1.43);
                return a * 0.38 + b * 0.28 + c * 0.20 + d * 0.14;
            }

            float DetailField(float2 p, float time)
            {
                float first = sin(p.x * 2.7 + p.y * 1.3 + time * 1.7);
                float second = sin(p.x * -1.4 + p.y * 3.1 - time * 1.31);
                return first * 0.55 + second * 0.45;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float time = _Time.y * _WaveSpeed;
                float wave = WaveField(world.xz * _WaveScale, time);
                world.y += wave * _WaveHeight;
                [unroll]
                for (int rippleIndex = 0; rippleIndex < 4; rippleIndex++)
                {
                    float4 rippleData = _RippleData[rippleIndex];
                    float4 rippleShape = _RippleShape[rippleIndex];
                    float radius = rippleData.z * _RippleRadius;
                    float2 delta = world.xz - rippleData.xy;
                    float2 axis = float2(cos(rippleShape.x), sin(rippleShape.x));
                    float2 perpendicular = float2(-axis.y, axis.x);
                    float2 ellipse = float2(dot(delta, axis) / max(rippleShape.y, 0.01), dot(delta, perpendicular));
                    float distanceToRipple = length(ellipse);
                    distanceToRipple += (SmoothNoise(delta * 1.7 + rippleShape.z) - 0.5) * 0.12;
                    float width = lerp(_RippleWidth, _RippleWidth * 0.35, rippleData.z);
                    float ringDistance = abs(distanceToRipple - radius);
                    float ringBand = saturate(1.0 - ringDistance / max(width * 1.8, 0.001));
                    float fade = pow(saturate(1.0 - rippleData.z), 2.0) * rippleData.w;
                    world.y += cos(ringDistance / max(width, 0.001) * 1.5708)
                        * ringBand * _RippleHeight * fade * 0.65;
                }
                output.positionCS = TransformWorldToHClip(world);
                output.worldPos = world;
                output.waveHeight = wave;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _WaveSpeed;
                float2 p = input.worldPos.xz * _WaveScale;
                float epsilon = 0.08;
                float heightX = WaveField(p + float2(epsilon, 0), time) - WaveField(p - float2(epsilon, 0), time);
                float heightZ = WaveField(p + float2(0, epsilon), time) - WaveField(p - float2(0, epsilon), time);
                float detailX = DetailField(p * 3.2 + float2(0.17, 0), time) - DetailField(p * 3.2 - float2(0.17, 0), time);
                float detailZ = DetailField(p * 3.2 + float2(0, 0.17), time) - DetailField(p * 3.2 - float2(0, 0.17), time);
                float3 normalWS = normalize(float3(-(heightX + detailX * 0.18) * _NormalStrength, 1.0,
                    -(heightZ + detailZ * 0.18) * _NormalStrength));

                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float2 refractedUV = saturate(screenUV + normalWS.xz * _RefractionStrength);
                float rawDepth = SampleSceneDepth(refractedUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float waterDepth = -TransformWorldToView(input.worldPos).z;
                float depthGap = max(0.0, sceneDepth - waterDepth);
                float shallow = 1.0 - saturate(depthGap / max(_DepthFade, 0.001));

                half3 sceneColor = SampleSceneColor(refractedUV);
                half3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, shallow);
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.worldPos);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), 4.0);
                waterColor = lerp(waterColor, _HorizonColor.rgb, fresnel * _ReflectionStrength);
                waterColor = lerp(sceneColor, waterColor, saturate(_Transparency + fresnel * 0.12));

                Light mainLight = GetMainLight();
                float3 halfVector = normalize(mainLight.direction + viewDir);
                float sunSpecular = pow(saturate(dot(normalWS, halfVector)), 128.0);
                float sparkleScale = _SparkleSize * 0.055;
                float2 sparkleUV1 = input.worldPos.xz * float2(sparkleScale * 1.8, sparkleScale * 0.42)
                    + float2(time * 0.31, -time * 0.18);
                float2 sparkleUV2 = input.worldPos.zx * float2(sparkleScale * 0.73, sparkleScale * 2.15)
                    + float2(-time * 0.22, time * 0.27);
                float sparkleNoise = SmoothNoise(sparkleUV1) * 0.58 + SmoothNoise(sparkleUV2) * 0.42;
                float sparkleSignal = sparkleNoise * 0.48 + sunSpecular * 0.82;
                float sparkleAA = max(fwidth(sparkleSignal) * 1.5, 0.015);
                float sparkleGate = smoothstep(0.82 - sparkleAA, 0.94 + sparkleAA, sparkleSignal);
                float sparkles = pow(sunSpecular, 1.7) * sparkleGate * _SparkleStrength;
                waterColor += mainLight.color * sparkles;

                float detail = DetailField(p * 2.1, time * 0.82);
                float brokenNoise = saturate(0.55 + detail * 0.45);
                float shoreFoam = (1.0 - smoothstep(0.015, _FoamWidth, depthGap)) * brokenNoise;
                float crest = smoothstep(0.84, 1.04, input.waveHeight + detail * 0.06) * _CrestFoam;

                float rippleFoam = 0.0;
                [unroll]
                for (int rippleIndex = 0; rippleIndex < 4; rippleIndex++)
                {
                    float4 rippleData = _RippleData[rippleIndex];
                    float4 rippleShape = _RippleShape[rippleIndex];
                    float radius = rippleData.z * _RippleRadius;
                    float2 delta = input.worldPos.xz - rippleData.xy;
                    float2 axis = float2(cos(rippleShape.x), sin(rippleShape.x));
                    float2 perpendicular = float2(-axis.y, axis.x);
                    float2 ellipse = float2(dot(delta, axis) / max(rippleShape.y, 0.01), dot(delta, perpendicular));
                    float warpedDistance = length(ellipse);
                    float breakupNoise = SmoothNoise(delta * 2.3 + rippleShape.z + time * 0.05);
                    warpedDistance += (breakupNoise - 0.5) * 0.13;
                    float ringDistance = abs(warpedDistance - radius);
                    float width = lerp(_RippleWidth, _RippleWidth * 0.35, rippleData.z);
                    float antiAlias = max(fwidth(ringDistance) * 1.4, 0.004);
                    float ring = 1.0 - smoothstep(width - antiAlias, width + antiAlias, ringDistance);
                    float secondDistance = abs(warpedDistance - max(0.0, radius - width * 2.4));
                    float secondRing = 1.0 - smoothstep(width * 0.55 - antiAlias, width * 0.55 + antiAlias, secondDistance);
                    float fade = pow(saturate(1.0 - rippleData.z), 2.0) * rippleData.w;
                    float brokenEdge = lerp(0.38, 1.0, smoothstep(0.28, 0.78, breakupNoise));
                    rippleFoam += (ring + secondRing * 0.32) * fade * brokenEdge * 0.72;
                }

                float foam = saturate(shoreFoam + crest + rippleFoam);
                waterColor = lerp(waterColor, _FoamColor.rgb, foam * _FoamColor.a);
                waterColor = MixFog(waterColor, input.fogFactor);
                float alpha = saturate(lerp(_DeepColor.a, _ShallowColor.a, shallow) * _Transparency + foam * 0.18 + fresnel * 0.08);
                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }
}
