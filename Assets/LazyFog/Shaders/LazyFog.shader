// Конвертация LemonSpawn/LazyFog → URP (HLSL)
// Логика полностью сохранена, только API заменён на URP-совместимый

Shader "LemonSpawn/LazyFog"
{
    Properties
    {
        _Color      ("Color",     Color)        = (1,1,1,1)
        _MainTex    ("Albedo (RGB)", 2D)        = "white" {}
        _Scale      ("Scale",     Range(0,5))   = 1
        _Intensity  ("Intensity", Range(0,1))   = 0.5
        _Alpha      ("Alpha",     Range(0,2.5)) = 0.75
        _AlphaSub   ("AlphaSub",  Range(0,1))   = 0.0
        _Pow        ("Pow",       Range(0,4))   = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent+101"
            "IgnoreProjector"= "True"
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 400
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "LazyFog"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float  _Scale;
                float  _Intensity;
                float  _Alpha;
                float  _AlphaSub;
                float  _Pow;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;       // vertex color (как v.color в оригинале)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float2 uv            : TEXCOORD0;
                float3 normalOS      : TEXCOORD1;
                float4 vertexColor   : TEXCOORD2;
                float3 worldPosition : TEXCOORD3;
                float  fogCoord      : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionCS    = posInputs.positionCS;
                OUT.worldPosition = posInputs.positionWS;
                OUT.uv            = IN.uv;
                OUT.normalOS      = normalize(IN.normalOS);
                OUT.vertexColor   = IN.color;
                OUT.fogCoord      = ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // ── оригинальная логика фрагментного шейдера ──────────

                // Сэмплируем текстуру с масштабом (как c = tex2D(_MainTex, IN.uv*_Scale))
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv * _Scale);

                // xx = c.r * Intensity, потом степень Pow
                float xx = c.r * _Intensity;
                xx = pow(max(xx, 0.0), _Pow);

                // Цвет из яркости + Color (как в оригинале)
                c.rgb = float3(xx * _Color.r, xx * _Color.g, xx * _Color.b);

                // Alpha: c.r в основе
                c.a = c.r;

                // Затухание от краёв — длина от центра UV (0.5, 0.5)
                // c.a *= IN.color.a - 2.5 * length(IN.uv - float2(0.5, 0.5))
                float distFromCenter = length(IN.uv - float2(0.5, 0.5));
                c.a *= IN.vertexColor.a - 2.5 * distFromCenter;

                c.a *= _Alpha;
                c.a -= _AlphaSub;
                c.a  = saturate(c.a);   // зажимаем [0,1] — в оригинале не было, но нужно для URP

                // Туман сцены
                c.rgb = MixFog(c.rgb, IN.fogCoord);

                return c;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
