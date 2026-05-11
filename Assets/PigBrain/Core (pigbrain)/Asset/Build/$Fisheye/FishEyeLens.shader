Shader "Pigbrain/FishEyeLens"
{
    Properties
    {
        _Strength ("Strength", Range(0,2)) = 0.3
        _Zoom ("Zoom", Range(0.8,2)) = 1.1
        _OutColor ("Out Of Bounds Color", Color) = (0,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            Name "FishEyeLens"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Strength;
            float _Zoom;
            float4 _OutColor;

half4 Frag(Varyings input) : SV_Target
{
    float2 uv = input.texcoord;

    // apply zoom toward the center
    uv = (uv - 0.5) / _Zoom + 0.5;

    float2 p = uv - 0.5;
    float r2 = dot(p, p);

    float k = _Strength * 1.5;

    // basic barrel distortion
    float2 uvDistorted = 0.5 + p * (1.0 + k * r2);

    // show configurable color if final UV leaves the render texture
    if (uvDistorted.x < 0 || uvDistorted.x > 1 || uvDistorted.y < 0 || uvDistorted.y > 1)
        return _OutColor;

    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvDistorted);
}
            ENDHLSL
        }
    }
}