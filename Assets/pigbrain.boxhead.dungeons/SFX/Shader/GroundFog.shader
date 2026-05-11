Shader "Boxhead/GroundFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.8,0.85,0.9,1)
        _Opacity ("Opacity", Range(0,1)) = 0.25
        _NoiseScale ("Noise Scale", Float) = 0.2
        _NoiseSpeed ("Noise Speed (XY)", Vector) = (0.1, 0.0, 0, 0)
        _DepthFade ("Depth Fade Distance", Float) = 1.5
        _DepthBias ("Depth Bias", Float) = 0.02
        _EdgeFade ("Edge Fade", Float) = 5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _Opacity;
                float _NoiseScale;
                float4 _NoiseSpeed;
                float _DepthFade;
                float _DepthBias;
                float _EdgeFade;
            CBUFFER_END

            float hash(float2 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * (p.x + p.y));
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash(i);
                float b = hash(i + float2(1,0));
                float c = hash(i + float2(0,1));
                float d = hash(i + float2(1,1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) +
                       (c - a) * u.y * (1.0 - u.x) +
                       (d - b) * u.x * u.y;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.worldPos);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 worldUV = IN.worldPos.xz * _NoiseScale;
                worldUV += _Time.y * _NoiseSpeed.xy;

                float n1 = noise(worldUV);
                float n2 = noise(worldUV * 2.1 + 10.3);
                float fogNoise = (n1 * 0.6 + n2 * 0.4);

                // Soft intersection fade (device depth -> eye depth; works for ortho + perspective)
                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                float sceneDepth = SampleSceneDepth(uv);
                float fogDepth = IN.screenPos.z / IN.screenPos.w;

                float sceneEye = LinearEyeDepth(sceneDepth, _ZBufferParams);
                float fogEye = LinearEyeDepth(fogDepth, _ZBufferParams);

                float depthFade = saturate((sceneEye - fogEye + _DepthBias) / max(_DepthFade, 1e-4));

                float alpha = fogNoise * _Opacity * depthFade;

                return half4(_FogColor.rgb, alpha);
            }

            ENDHLSL
        }
    }
}