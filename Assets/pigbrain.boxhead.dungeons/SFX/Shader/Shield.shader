Shader "Boxhead/Shield"
{
    Properties
    {
        _Color ("Color", Color) = (0,0.6,1,1)
        _Emission ("Emission", Float) = 2
        _FresnelPower ("Fresnel", Float) = 3
        _RippleWidth ("Ripple Width", Float) = 0.5
        _Speed ("Ripple Speed", Float) = 5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define MAX_HITS 8

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            float4 _Color;
            float _Emission;
            float _FresnelPower;
            float _RippleWidth;
            float _Speed;

            float4 _HitPos[MAX_HITS];
            float _HitTime[MAX_HITS];
            float _TimeNow;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionOS = v.positionOS.xyz;
                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            float Ripple(float dist, float radius)
            {
                return smoothstep(radius, radius - _RippleWidth, dist) *
                       smoothstep(radius, radius + _RippleWidth, dist);
            }

half4 frag (Varyings i) : SV_Target
{
    float3 p = normalize(i.positionOS);

    float rippleSum = 0;

    [unroll]
    for (int j = 0; j < MAX_HITS; j++)
    {
        float t = _TimeNow - _HitTime[j];
        if (t < 0) continue;

        float3 h = normalize(_HitPos[j].xyz);
        float d = dot(p, h);
        float dist = acos(clamp(d, -1.0, 1.0));

        float radius = t * _Speed * 0.5;

        float ring =
            smoothstep(radius + _RippleWidth, radius, dist) *
            smoothstep(radius - _RippleWidth, radius, dist);

        float core = smoothstep(0.3, 0.0, dist);

        float decay = exp(-t * 3);

        float r = max(core * 0.6, ring);

        rippleSum = max(rippleSum, r * decay);
    }

    float3 viewDir = normalize(GetWorldSpaceViewDir(i.worldPos));
    float fresnel = pow(1 - saturate(dot(i.normalWS, viewDir)), _FresnelPower);

    float intensity = (rippleSum + fresnel) * _Emission;

    return float4(_Color.rgb * intensity, intensity);
}
            ENDHLSL
        }
    }
}