Shader "Boxhead/Soul"
{
    Properties
    {
        _Color ("Color", Color) = (0.3,1,1,1)
        _Glow ("Glow", Range(0,10)) = 2
        _Alpha ("Alpha", Range(0,1)) = 0.5

        _Wobble ("Wobble", Range(0,0.2)) = 0.03
        _Speed ("Speed", Range(0,10)) = 3
        _Frequency ("Frequency", Range(0,20)) = 8

        _FresnelPower ("Fresnel Power", Range(0,8)) = 4
        _Phase ("Phase", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)

            float4 _Color;
            float _Glow;
            float _Alpha;

            float _Wobble;
            float _Speed;
            float _Frequency;

            float _FresnelPower;
            float _Phase;

            CBUFFER_END

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                Varyings o;

                float t = _Time.y * _Speed + _Phase;

                float wobble =
                    sin(v.positionOS.y * _Frequency + t) *
                    _Wobble;

                float3 posOS =
                    v.positionOS.xyz +
                    v.normalOS * wobble;

                VertexPositionInputs pos =
                    GetVertexPositionInputs(posOS);

                VertexNormalInputs norm =
                    GetVertexNormalInputs(v.normalOS);

                o.positionCS = pos.positionCS;
                o.normalWS = normalize(norm.normalWS);
                o.viewDirWS =
                    GetWorldSpaceViewDir(pos.positionWS);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDirWS);

                float fresnel =
                    pow(
                        1 - saturate(dot(viewDir, i.normalWS)),
                        _FresnelPower);

                float pulse =
                    0.8 + sin(_Time.y * 2 + _Phase) * 0.2;

                float alpha =
                    fresnel * _Alpha * pulse;

                float3 color =
                    _Color.rgb *
                    fresnel *
                    _Glow *
                    pulse;

                return float4(color, alpha);
            }

            ENDHLSL
        }
    }
}