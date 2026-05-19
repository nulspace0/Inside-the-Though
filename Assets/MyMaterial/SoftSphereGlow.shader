// SoftSphereGlow.shader v2 — мягкая светящаяся сфера (портал / мозг)
// Улучшения: FBM шум, пульс, HDR emission, мягкий Fresnel, хроматический сдвиг
Shader "Custom/SoftSphereGlow"
{
    Properties
    {
        [HDR] _ColorMain     ("Core Color",           Color)        = (0.1, 0.18, 0.98, 1)
        [HDR] _ColorRim      ("Rim Color",            Color)        = (0.3, 0.6,  1.0,  1)
        _ColorLighten        ("Brightness Multiplier", Float)        = 6.0
        _Alpha               ("Alpha",                Range(0,1))   = 0.7
        _NoiseTex            ("Noise Texture",         2D)          = "white" {}
        _PortalFade          ("Fade",                 Range(0,1))   = 0.5

        [Header(Fresnel Soft Edges)]
        _FresnelPower        ("Edge Softness",        Range(0.3,8)) = 2.5
        _FresnelStrength     ("Edge Falloff",         Range(0,1))   = 0.92

        [Header(Noise Animation)]
        _NoiseSpeedX         ("Speed X",              Float)        = 0.10
        _NoiseSpeedY         ("Speed Y",              Float)        = 0.07
        _NoiseTiling         ("Tiling",               Float)        = 2.0
        _NoiseContrast       ("Contrast",             Range(0.1,4)) = 1.4

        [Header(Pulse)]
        _PulseSpeed          ("Pulse Speed",          Range(0,4))   = 1.0
        _PulseAmount         ("Pulse Amount",         Range(0,0.5)) = 0.12

        [Header(Chromatic)]
        _ChromaShift         ("Chromatic Shift",      Range(0,0.02))= 0.004
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+10"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SoftSphereGlow"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Gradient Noise (Perlin-like) ──────────────────────────────
            float2 _GradHash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float GradNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float a = dot(_GradHash(i),               f);
                float b = dot(_GradHash(i + float2(1,0)), f - float2(1,0));
                float c = dot(_GradHash(i + float2(0,1)), f - float2(0,1));
                float d = dot(_GradHash(i + float2(1,1)), f - float2(1,1));
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            // ── FBM — 4 октавы ────────────────────────────────────────────
            float FBM(float2 uv, float time)
            {
                float val = 0, amp = 0.5, freq = 1.0;
                float2 d  = float2(time * 0.55, time * 0.38);
                val += GradNoise((uv + d * 1.00) * freq) * amp; amp *= 0.5; freq *= 2.1;
                val += GradNoise((uv + d * 0.65) * freq) * amp; amp *= 0.5; freq *= 2.1;
                val += GradNoise((uv - d * 0.45) * freq) * amp; amp *= 0.5; freq *= 2.1;
                val += GradNoise((uv + d * 0.25) * freq) * amp;
                return val * 0.5 + 0.5;
            }

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorMain;
                float4 _ColorRim;
                float  _ColorLighten;
                float  _Alpha;
                float4 _NoiseTex_ST;
                float  _PortalFade;
                float  _FresnelPower;
                float  _FresnelStrength;
                float  _NoiseSpeedX;
                float  _NoiseSpeedY;
                float  _NoiseTiling;
                float  _NoiseContrast;
                float  _PulseSpeed;
                float  _PulseAmount;
                float  _ChromaShift;
            CBUFFER_END

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

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
                float2 uv         : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
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
                OUT.normalWS   = norm.normalWS;
                OUT.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                OUT.uv         = IN.uv;
                OUT.fogCoord   = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 uv = IN.uv;
                float  t  = _Time.y;

                // ── 1. FBM шум ────────────────────────────────────────────
                float2 scaled = uv * _NoiseTiling;
                float n1 = FBM(scaled,          t * _NoiseSpeedX);
                float n2 = FBM(scaled * 1.4 + 0.6, -t * _NoiseSpeedY);
                float noiseBlend = lerp(n1, n2, 0.4);
                noiseBlend = saturate(pow(noiseBlend, _NoiseContrast));

                // Текстурный шум
                float2 scrollUV = uv + float2(_NoiseSpeedX, _NoiseSpeedY) * t;
                float texN = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, scrollUV).r;
                float noise = noiseBlend * 0.6 + texN * 0.4;

                // ── 2. Fresnel ────────────────────────────────────────────
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                float NdotV   = saturate(dot(N, V));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                float edgeFade = 1.0 - fresnel * _FresnelStrength;

                // ── 3. Пульс ──────────────────────────────────────────────
                float pulse = 0.5 + 0.5 * sin(t * _PulseSpeed);
                float pulseMod = 1.0 + _PulseAmount * (pulse - 0.5) * 2.0;

                // ── 4. Хроматический сдвиг (имитация) ────────────────────
                float2 uvR = uv + float2(_ChromaShift, 0) * (1.0 - NdotV);
                float2 uvB = uv - float2(_ChromaShift, 0) * (1.0 - NdotV);
                float nR = FBM(uvR * _NoiseTiling, t * _NoiseSpeedX);
                float nB = FBM(uvB * _NoiseTiling, t * _NoiseSpeedX);

                // ── 5. Цвет ───────────────────────────────────────────────
                float3 coreColor = _ColorMain.rgb * _ColorLighten;
                float3 rimColor  = _ColorRim.rgb  * _ColorLighten * 0.5;

                float3 color;
                color.r = (coreColor.r * nR + rimColor.r * fresnel) * pulseMod;
                color.g = (coreColor.g * noise + rimColor.g * fresnel) * pulseMod;
                color.b = (coreColor.b * nB + rimColor.b * fresnel) * pulseMod;

                // Шум добавляет яркость в светлые области
                color += coreColor * (noise - 0.4) * 0.5;
                // Rim ярче по краям
                color += rimColor * fresnel * pulseMod;

                // ── 6. Alpha ──────────────────────────────────────────────
                float alpha = _Alpha * edgeFade;
                alpha *= (_PortalFade * 0.5 + 0.5 + noise * 0.15);
                alpha = saturate(alpha);

                half4 result = half4(color, alpha);
                result.rgb   = MixFog(result.rgb, IN.fogCoord);
                return result;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
