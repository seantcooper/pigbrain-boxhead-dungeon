Shader "Pigbrain/FloorWarp"
{
    Properties
    {
        _Strength ("Strength", Range(0,0.1)) = 0.02
        _Freq ("Frequency", Range(0,2)) = 0.3
        _Light ("Light", Range(0,1)) = 0.2
        _Dark ("Dark", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _Strength = 0.02;
            float _Freq = 0.3;
            float _Light = 0.2;
            float _Dark = 0.2;

            float3 ReconstructWorld(float2 uv)
            {
                float depth = SampleSceneDepth(uv);
                return ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;

                float3 wp = ReconstructWorld(uv);

                float wave =
                    sin(wp.x * _Freq) +
                    sin(wp.z * (_Freq * 0.8));

                uv.y += wave * _Strength;

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float shade = (wave > 0) ? wave * _Light : wave * _Dark;
                col.rgb *= 1 + shade;
                return col;
            }

            ENDHLSL
        }
    }
}