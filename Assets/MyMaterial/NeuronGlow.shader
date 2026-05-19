// NeuronGlow.shader — шейдер для нейронов и синапсов
// Эффекты: Fresnel rim, пульсирующее свечение, subsurface glow
Shader "Custom/NeuronGlow"
{
    Properties
    {
        [HDR] _BaseColor      ("Base Color",         Color)        = (0.1, 0.2, 0.9, 1)
        [HDR] _RimColor       ("Rim / Glow Color",   Color)        = (0.3, 0.6, 1.0, 1)
        [HDR] _PulseColor     ("Pulse Color",        Color)        = (0.5, 0.8, 1.0, 1)
        _Smoothness           ("Smoothness",          Range(0,1))   = 0.6
        _Metallic             ("Metallic",            Range(0,1))   = 0.0
        _FresnelPower         ("Rim Width",           Range(0.5,8)) = 3.5
        _RimIntensity         ("Rim Intensity",       Range(0,5))   = 1.8
        _PulseSpeed           ("Pulse Speed",         Range(0,4))   = 1.2
        _PulseAmount          ("Pulse Amount",        Range(0,1))   = 0.35
        _EmissionBase         ("Base Emission",       Range(0,3))   = 0.4
        _MainTex              ("Albedo",              2D)           = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "NeuronGlow_Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float4 _PulseColor;
                float4 _MainTex_ST;
                float  _Smoothness;
                float  _Metallic;
                float  _FresnelPower;
                float  _RimIntensity;
                float  _PulseSpeed;
                float  _PulseAmount;
                float  _EmissionBase;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float  fogCoord   : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos  = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   norm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = norm.normalWS;
                OUT.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogCoord   = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                // ── Базовый цвет ─────────────────────────────────────────
                half4 tex    = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;

                // ── Fresnel (Rim) ─────────────────────────────────────────
                float NdotV  = saturate(dot(N, V));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // ── Пульс — плавный синус по времени ─────────────────────
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                pulse = lerp(1.0 - _PulseAmount, 1.0, pulse);

                // ── Освещение (URP Lighting) ──────────────────────────────
                InputData lightData  = (InputData)0;
                lightData.positionWS = IN.positionWS;
                lightData.normalWS   = N;
                lightData.viewDirectionWS = V;
                lightData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                lightData.fogCoord    = IN.fogCoord;
                lightData.vertexLighting = float3(0,0,0);
                lightData.bakedGI       = SampleSH(N);
                lightData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                lightData.shadowMask = unity_ProbesOcclusion;

                SurfaceData surf    = (SurfaceData)0;
                surf.albedo         = albedo;
                surf.metallic       = _Metallic;
                surf.smoothness     = _Smoothness;
                surf.normalTS       = float3(0,0,1);
                surf.occlusion      = 1;
                surf.alpha          = 1;

                // Emission = базовая + rim glow + пульс
                surf.emission  = albedo * _EmissionBase;
                surf.emission += _RimColor.rgb * fresnel * _RimIntensity * pulse;
                surf.emission += _PulseColor.rgb * (pulse - 0.65) * fresnel * 0.5;

                half4 color = UniversalFragmentPBR(lightData, surf);
                color.rgb = MixFog(color.rgb, IN.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
