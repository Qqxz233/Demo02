Shader "Unlit/ToonWater_BuiltIn"
{
    Properties
    {
        [Header(General)]
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [Toggle(_USE_ALPHA)] _UseAlpha("Use Alpha", Float) = 0.0 
        [Toggle(_USE_BLINN_PHONG_MODEL)] _UseBlinnPhongModel("Use Blinn Phong Model", Float) = 1.0
        [Toggle(_USE_BLINN_PHONG_SPECULAR)] _UseBlinnPhongSpecular("Use Blinn Phong Specular", Float) = 1.0
        _SpecularColor ("Specular Color", Color) = (0.8, 0.9, 1.0, 1.0)
        _SpecularPower ("Specular Power", Float) = 32.0

        [Header(Controllers)]
        _WaterDepthController ("Water Depth Controller", Float) = 1.0
        _DistanceController ("Far Water Color Distance Controller", Float) = 0.1
        _WaterFadeController ("Water Fade Controller", Float) = 0.7
        _WaterMixController ("Water Mix Controller", Range(0, 1)) = 0.3

        [Header(Water Color)]
        [Toggle(_USE_SHALLOW_COLOR)] _UseShallowColor ("Use Shallow Color", Float) = 1.0
        [HDR] _ShallowColor ("Shallow Color", Color) = (0.2, 0.8, 0.8, 1.0)
        [HDR] _DeepColor ("Deep Color", Color) = (0.02, 0.1, 0.3, 1.0)
        [Toggle(_USE_FAR_COLOR)] _UseFarColor ("Use Far Color", Float) = 0.0
        [HDR] _FarColor ("Far Color", Color) = (0.5, 0.7, 0.9, 1.0)

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
    }   
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            #pragma shader_feature_local _USE_ALPHA
            #pragma shader_feature_local _USE_SHALLOW_COLOR
            #pragma shader_feature_local _USE_FAR_COLOR
            #pragma shader_feature_local _USE_BLINN_PHONG_MODEL
            #pragma shader_feature_local _USE_BLINN_PHONG_SPECULAR
            #pragma shader_feature_local _USE_COSINE_GRADIENT
            #pragma shader_feature_local _USE_NORMAL_MAP
            #pragma shader_feature_local _USE_SINE_WAVE

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(6)
            };

            float4 _BaseMap_ST;
            float4 _SpecularColor;
            float _SpecularPower;
            float _WaterDepthController;
            float _WaterColorController;
            float _DistanceController;
            float _WaterFadeController;
            float _WaterMixController;
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

            sampler2D _BaseMap;
            sampler2D _FirstNormalMap;
            sampler2D _SecondNormalMap;
            sampler2D _CameraDepthTexture;
            sampler2D _CameraOpaqueTexture;

            float Remap(float val, float2 inMinMax, float2 outMinMax) {
                return outMinMax.x + (val - inMinMax.x) * (outMinMax.y - outMinMax.x) / (inMinMax.y - inMinMax.x);
            }
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

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.tangentWS = UnityObjectToWorldDir(v.tangent.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangent.w;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.screenPos = ComputeScreenPos(o.pos);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
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

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth);
                float waterDepth = sceneDepth - i.screenPos.w; 

                waterDepth = max(waterDepth, 0.001); 
                float waterDepth01 = 1.0 - saturate(exp((-waterDepth) / _WaterDepthController));
                
                float3 waterColor = _DeepColor.rgb;
                #ifdef _USE_SHALLOW_COLOR
                    waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, waterDepth01);
                #endif
                #ifdef _USE_COSINE_GRADIENT
                    waterColor = GetWaterColorByCosineGradient(saturate(_WaterColorController - waterDepth01));
                #endif

                float3 normalWS = i.normalWS;
                #ifdef _USE_NORMAL_MAP
                    float3x3 tbnMatrix = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                    float3 firstNormalTS = UnpackNormal(tex2D(_FirstNormalMap, i.uv * _FirstNormalMap_ST.xy + _Time.y / _FirstNormalSpeedInverse));
                    float3 secondNormalTS = UnpackNormal(tex2D(_SecondNormalMap, i.uv * _SecondNormalMap_ST.xy + _Time.y / _SecondNormalSpeedInverse));
                    float3 normalTS = normalize(firstNormalTS + secondNormalTS);
                    normalWS = mul(normalTS, tbnMatrix);
                #endif

                float3 finalColor = waterColor;
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - i.positionWS);

                float3 lightDirWS = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                
                #ifdef _USE_BLINN_PHONG_MODEL
                    float NdotL = dot(normalWS, lightDirWS);
                    float halfLambert = 0.5 * NdotL + 0.5;
                    finalColor = waterColor * lightColor * halfLambert;
                #endif
                
                #ifdef _USE_BLINN_PHONG_SPECULAR
                    float3 halfwayDirWS = normalize(lightDirWS + viewDirWS);
                    float NdotH = saturate(dot(normalWS, halfwayDirWS));
                    // 【卡通高光核心】使用 step 阶跃函数生成硬边色块，并根据阈值剔除
                    float specMask = step(0.95, pow(NdotH, _SpecularPower));
                    float3 specularColor = _SpecularColor.rgb * lightColor * specMask;
                    finalColor += specularColor;
                #endif

                #ifdef _USE_SINE_WAVE
                    float sineWave = saturate(_SineAmplitude * sin(_SinePeriod * 3.14159 * waterDepth + _Time.y * _SineSpeed));
                    float waterDepthMask = 1.0 - step(1.0 - waterDepth01, _SineMaskThreshold);
                    
                    float noiseValue = SimpleNoise(float2((i.uv * 10.0).r, _Time.y * _NoiseSpeed), _NoiseSize);
                    sineWave += noiseValue;
                    sineWave = smoothstep(_NoiseMinEdge, _NoiseMaxEdge, sineWave);
                    sineWave *= waterDepthMask;
                    sineWave *= Remap(clamp(1.0 - waterDepth01, _SineMaskThreshold, 1.0), float2(_SineMaskThreshold, 1.0), float2(0.0, 1.0));
                    sineWave *= _SineStrength;
                    finalColor += sineWave;
                #endif

                float waterAlpha = 1.0;
                #ifdef _USE_ALPHA
                    waterAlpha = Remap(waterDepth01, float2(0, _WaterFadeController), float2(0, 1.0));
                #endif
                
                // 【防爆白核心】强行钳制最大值，防止超过 1.0 变成纯白
                finalColor = saturate(finalColor);

                fixed4 col = fixed4(finalColor, waterAlpha);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}