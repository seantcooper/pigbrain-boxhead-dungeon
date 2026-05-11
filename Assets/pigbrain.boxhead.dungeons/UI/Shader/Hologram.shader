Shader "Boxhead/Hologram"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (0,1,1,1)
        _FresnelPower ("Fresnel Power", Range(0.5,8)) = 3
        _FresnelIntensity ("Fresnel Intensity", Range(0,5)) = 1.5
        _ScanSpeed ("Scan Speed", Float) = 2
        _ScanScale ("Scan Scale", Float) = 50
        _ScanStrength ("Scan Strength", Range(0,1)) = 0.5
        _Flicker ("Flicker Strength", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float _FresnelPower, _FresnelIntensity;
            float _ScanSpeed, _ScanScale, _ScanStrength, _Flicker;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewDirWS = GetWorldSpaceViewDir(posWS);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewDirWS);

                // Fresnel
                float fresnel = pow(1 - saturate(dot(N, V)), _FresnelPower);
                fresnel *= _FresnelIntensity * 0.5;

                // Texture
                float2 uv = i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);

                // Scanlines
                float scan = sin(uv.y * _ScanScale + _Time.y * _ScanSpeed) * 0.5 + 0.5;

                // Flicker
                float flicker = 1 + sin(_Time.y * 20) * _Flicker;

                float scanBlend = lerp(1.0, scan, _ScanStrength);
                float alpha = tex.a * scanBlend * flicker;

                float3 baseCol = tex.rgb * _BaseColor.rgb;
                float3 holoCol = baseCol + fresnel * _BaseColor.rgb * (1 - tex.a);
                float3 col = lerp(baseCol, holoCol, scanBlend);
                col = saturate(col);

                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}