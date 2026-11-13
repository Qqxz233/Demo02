Shader "Unlit/ToonWaterShader"
{
    Properties
    {
        [Header(General)]
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [Toggle(_USE_ALPHA)] _UseAlpha("Use Alpha", Float) = 0.0 // 默认关闭透明
        [Toggle(_USE_BLINN_PHONG_MODEL)] _UseBlinnPhongModel("Use Blinn Phong Model", Float) = 1.0
        [Toggle(_USE_BLINN_PHONG_SPECULAR)] _UseBlinnPhongSpecular("Use Blinn Phong Specular", Float) = 1.0
        _SpecularColor ("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SpecularPower ("Specular Power", Float) = 8.0

        [Header(Controllers)]
        _WaterDepthController ("Water Depth Controller", Float) = 1.0
        _DistanceController ("Far Water Color Distance Controller", Float) = 0.1
        _WaterFadeController ("Water Fade Controller", Float) = 0.7
        _WaterMixController ("Water Mix Controller", Range(0, 1)) = 0.3
        [Toggle(_USE_REFRACTED_DEPTH_CONTROLLER)] _UseRefractedDepthController("Use Refracted Depth Controller", Float) = 1.0
        _RefractedDepthController ("Refracted Depth Controller", Range(0.0001, 3)) = 0.2

        [Header(Water Color)]
        [Toggle(_USE_SHALLOW_COLOR)] _UseShallowColor ("Use Shallow Color", Float) = 1.0
        [HDR] _ShallowColor ("Shallow Color", Color) = (0.2, 0.8, 0.8, 1.0)
        [HDR] _DeepColor ("Deep Color", Color) = (0.02, 0.1, 0.3, 1.0)
        [Toggle(_USE_FAR_COLOR)] _UseFarColor ("Use Far Color", Float) = 0.0
        [HDR] _FarColor ("Far Color", Color) = (0.5, 0.7, 0.9, 1.0)
        [Toggle(_USE_DEEP_WATER_OPAQUE)] _UseDeepWaterOpaque ("Use Deep Water Opaque", Float) = 0.0
        _OpaqueDepthMinEdge ("Opaque Depth Min Edge", Float) = 3.5
        _OpaqueDepthRange ("Opaque Depth Range", Float) = 3.5

        [Header(Water Color Cosine Gradient)]
        [Toggle(_USE_COSINE_GRADIENT)] _UseCosineGradient("Use Cosine Gradient", Float) = 1.0
        _WaterColorController ("Water Color Controller", Float) = 1.5

        [Header(Normal Map)]
        [Toggle(_USE_NORMAL_MAP)] _UseNormalMap ("Use Normal Map", Float) = 1.0
        _FirstNormalMap("NormalMap 1", 2D) = "white" {}
        _FirstNormalSpeedInverse("NormalSpeedInverse 1", Float) = 20
        _SecondNormalMap("NormalMap 2", 2D) = "white" {}
        _SecondNormalSpeedInverse("NormalSpeedInverse 2", Float) = -15

        [Header(Sine Wave)]
        [Toggle(_USE_SINE_WAVE)] _UseSineWave ("Use Sine Wave", Float) = 1.0
        _SinePeriod ("Sine Period", Float) = 20
        _SineSpeed ("Sine Speed", Float) = 1
        _SineAmplitude ("Sine Amplitude", Float) = 0.7 
        _SineMaskThreshold ("Sine Mask Threshold", Range(0, 1)) = 0.65
        _SineStrength ("Sine Strength", Float) = 1.0

        [Header(Sine Wave Noise Controller)]
        _NoiseSpeed ("Noise Speed", Float) = 0.05
        _NoiseSize ("Noise Size", Float) = 50
        _NoiseMinEdge ("Noise Min Edge", Float) = 1.25
        _NoiseMaxEdge ("Noise Max Edge", Float) = 1.8

        [Header(Refraction)]
        [Toggle(_USE_REFRACTION)] _UseRefraction ("Use Refraction", Float) = 0.0 // 默认关闭防报错
        _RefractedScale ("Refracted Scale", Range(0.0, 0.1)) = 0.05
        _RefractedSpeed ("Refracted Speed", Range(0.0, 0.2)) = 0.1
        _RefractedStrength ("Refracted Strength", Range(0.0, 0.1)) = 0.03

        [Header(Planar Reflection)]
        [Toggle(_USE_PLANAR_REFLECTION)] _UsePlanarReflection ("Use Planar Reflection", Float) = 0.0 // 默认关闭防白屏
        _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        _FresnelPower ("Fresnel Power", Range(0.01, 64.0)) = 5.0
        _FresnelEdge ("Fresnel Edge", Range(0.0, 1.0)) = 0.7
    }   
    SubShader
    {
        Tags {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off // 强制双面渲染，防止背面剔除

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 移除外部依赖，只保留 URP 核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #pragma shader_feature_local_fragment _USE_ALPHA
            #pragma shader_feature_local_fragment _USE_SHALLOW_COLOR
            #pragma shader_feature_local_fragment _USE_DEEP_WATER_OPAQUE
            #pragma shader_feature_local_fragment _USE_FAR_COLOR
            #pragma shader_feature_local_fragment _USE_REFRACTED_DEPTH_CONTROLLER
            #pragma shader_feature_local_fragment _USE_BLINN_PHONG_MODEL
            #pragma shader_feature_local_fragment _USE_BLINN_PHONG_SPECULAR
            #pragma shader_feature_local_fragment _USE_COSINE_GRADIENT
            #pragma shader_feature_local_fragment _USE_NORMAL_MAP
            #pragma shader_feature_local_fragment _USE_SINE_WAVE
            #pragma shader_feature_local_fragment _USE_PLANAR_REFLECTION
            #pragma shader_feature_local_fragment _USE_REFRACTION

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _SpecularColor;
                float _SpecularPower;
                float _WaterDepthController;
                float _WaterColorController;
                float _DistanceController;
                float _WaterFadeController;
                float _WaterMixController;
                float _RefractedDepthController;
                float _OpaqueDepthMinEdge;
                float _OpaqueDepthRange;
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FarColor;
                float4 _FirstNormalMap_ST;
                float4 _SecondNormalMap_ST;
                float _FirstNormalSpeedInverse;
                float _SecondNormalSpeedInverse;
                float _SinePeriod;
                float _SineSpeed;
                float _SineAmplitude;
                float _SineMaskThreshold;
                float _SineStrength;
                float _NoiseSpeed;
                float _NoiseSize;
                float _NoiseMinEdge;
                float _NoiseMaxEdge;
                float _FresnelPower;
                float _FresnelEdge;
                float _RefractedScale;
                float _RefractedSpeed;
                float _RefractedStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap);                SAMPLER(sampler_BaseMap);
            TEXTURE2D(_FirstNormalMap);         SAMPLER(sampler_FirstNormalMap);
            TEXTURE2D(_SecondNormalMap);        SAMPLER(sampler_SecondNormalMap);
            TEXTURE2D(_ReflectionTex);          SAMPLER(sampler_ReflectionTex);
            TEXTURE2D(_CameraOpaqueTexture);    SAMPLER(sampler_CameraOpaqueTexture);

            // ============ 内联工具函数（替代 Utils.hlsl） ============
            float Remap(float val, float2 inMinMax, float2 outMinMax) {
                return outMinMax.x + (val - inMinMax.x) * (outMinMax.y - outMinMax.x) / (inMinMax.y - inMinMax.x);
            }
            float FresnelEffect(float3 normal, float3 viewDir, float power) {
                return pow(1.0 - saturate(dot(normal, viewDir)), power);
            }
            // 简易 2D 噪声（替代 UnityNoise.hlsl）
            float SimpleNoise(float2 uv, float scale) {
                float2 st = uv * scale;
                float2 i = floor(st);
                float2 f = frac(st);
                float a = frac(sin(dot(i, float2(12.9898, 78.233))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1.0, 0.0), float2(12.9898, 78.233))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0.0, 1.0), float2(12.9898, 78.233))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1.0, 1.0), float2(12.9898, 78.233))) * 43758.5453);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            // ======================================================

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                output.tangentWS = normalize(normalInputs.tangentWS);
                output.bitangentWS = normalize(normalInputs.bitangentWS);
                output.screenPos = ComputeScreenPos(positionInputs.positionCS);
                output.positionCS = positionInputs.positionCS;
                return output;
            }

            float3 GetWaterColorByCosineGradient(float depth)
            {
                float4 phase = float4(0.28, 0.44, 0.00, 0.);
                float4 amplitude = float4(3.27, 0.14, 0.39, 0.);
                float4 frequency = float4(0.00, 0.67, 0.28, 0.);
                float4 offset = float4(0.04, 0.14, 0.14, 0.);
                float TAU = 2 * 3.14159265;
                phase *= TAU;
                depth *= TAU;
                float4 waterColor = float4(
                    offset.r + amplitude.r * 0.5 * cos(depth * frequency.r + phase.r) + 0.5,
                    offset.g + amplitude.g * 0.5 * cos(depth * frequency.g + phase.g) + 0.5,
                    offset.b + amplitude.b * 0.5 * cos(depth * frequency.b + phase.b) + 0.5,
                    offset.a + amplitude.a * 0.5 * cos(depth * frequency.a + phase.a) + 0.5
                );
                return waterColor.rgb;
            }
            
            float3 ScenePosition(float3 positionWS, float4 screenPos, float2 uv)
            {
                float3 viewVector = positionWS - _WorldSpaceCameraPos.xyz;
                float screenPositionDepth = screenPos.w;
                float samplerDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                viewVector = viewVector / screenPositionDepth * samplerDepth;
                return viewVector + _WorldSpaceCameraPos.xyz;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // 获取水深
                float3 waterBedPosition = ScenePosition(input.positionWS, input.screenPos, screenUV);
                float waterDepth = (input.positionWS - waterBedPosition).y;
                
                // 防止没有“底”时深度异常
                waterDepth = max(waterDepth, 0.001); 
                
                float waterDepthExponential = exp((-waterDepth) / _WaterDepthController);
                float waterDepth01 = 1.0 - saturate(waterDepthExponential);
                
                // 颜色计算
                float3 waterColor = _DeepColor.rgb;
                #ifdef _USE_SHALLOW_COLOR
                    waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, waterDepth01);
                #endif
                #ifdef _USE_COSINE_GRADIENT
                    waterColor = GetWaterColorByCosineGradient(saturate(_WaterColorController - waterDepth01));
                #endif

                // 折射（默认关闭，防错）
                #ifdef _USE_REFRACTION
                    // 简化折射逻辑
                    float3 surfaceColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV).rgb;
                    waterColor *= surfaceColor;
                #endif

                // 法线
                float3 normalWS = input.normalWS;
                #ifdef _USE_NORMAL_MAP
                    float3x3 tbnMatrix = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                    float3 firstNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_FirstNormalMap, sampler_FirstNormalMap, input.uv * _FirstNormalMap_ST.xy + _Time.y / _FirstNormalSpeedInverse));
                    float3 secondNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_SecondNormalMap, sampler_SecondNormalMap, input.uv * _SecondNormalMap_ST.xy + _Time.y / _SecondNormalSpeedInverse));
                    float3 normalTS = normalize(firstNormalTS + secondNormalTS);
                    normalWS = mul(normalTS, tbnMatrix);
                #endif

                float3 finalColor = waterColor;
                float3 viewVector = input.positionWS - _WorldSpaceCameraPos.xyz;
                float3 viewDirWS = normalize(-viewVector);

                Light mainLight = GetMainLight();
                float3 lightDirWS = normalize(mainLight.direction);
                
                // 漫反射与高光
                #ifdef _USE_BLINN_PHONG_MODEL
                    float NdotL = dot(normalWS, lightDirWS);
                    float halfLambert = 0.5 * NdotL + 0.5;
                    finalColor = waterColor * mainLight.color * halfLambert;
                #endif
                #ifdef _USE_BLINN_PHONG_SPECULAR
                    float3 halfwayDirWS = normalize(lightDirWS + viewDirWS);
                    float NdotH = saturate(dot(normalWS, halfwayDirWS));
                    float3 specularColor = _SpecularColor.rgb * mainLight.color * pow(NdotH, _SpecularPower);
                    finalColor += specularColor;
                #endif

                // 岸边白沫 (Sine Wave)
                #ifdef _USE_SINE_WAVE
                    float sineWave = saturate(_SineAmplitude * sin(_SinePeriod * PI * waterDepth + _Time.y * _SineSpeed));
                    float waterDepthMask = 1.0 - step(1.0 - waterDepth01, _SineMaskThreshold);
                    
                    // 使用内联的 SimpleNoise
                    float noiseValue = SimpleNoise(float2((input.uv * 10.0).r, _Time.y * _NoiseSpeed), _NoiseSize);
                    sineWave += noiseValue;
                    sineWave = smoothstep(_NoiseMinEdge, _NoiseMaxEdge, sineWave);
                    sineWave *= waterDepthMask;
                    sineWave *= Remap(clamp(1.0 - waterDepth01, _SineMaskThreshold, 1.0), float2(_SineMaskThreshold, 1.0), float2(0.0, 1.0));
                    sineWave *= _SineStrength;
                    
                    finalColor += sineWave;
                #endif

                // 透明度
                float waterAlpha = 1.0;
                #ifdef _USE_ALPHA
                    waterAlpha = Remap(waterDepth01, float2(0, _WaterFadeController), float2(0, 1.0));
                #endif
                
                return float4(finalColor, waterAlpha);
            }
            ENDHLSL
        }
    }
}