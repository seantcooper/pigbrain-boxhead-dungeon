Shader "Boxhead/SquareBarUI"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _Fill ("Fill", Range(0,1)) = 0
        _Color ("Fill Color", Color) = (1,0.8,0.2,1)
        _BackgroundColor ("Background Color", Color) = (0.15,0.15,0.15,1)

        _Radius ("Corner Radius", Range(0,0.5)) = 0.15
        _Soft ("Edge Softness", Range(0,0.05)) = 0.01

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _Outline ("Outline Width", Range(0,0.2)) = 0.02

        //_Cutoff ("Alpha Cutoff", Range(0,1)) = 0.001

        _RectSize ("Rect Size", Vector) = (1,1,0,0)
        // [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float _Fill, _Radius, _Soft, _Outline, _Cutoff;
            float4 _Color, _OutlineColor, _BackgroundColor;
            float4 _RectSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float sdRoundRect(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q,0)) + min(max(q.x,q.y),0) - r;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * 2 - 1;

                // Object scale (for world objects)
                float3 col0 = float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20);
                float3 col1 = float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21);

                float scaleX = length(col0);
                float scaleY = length(col1);

                // RectTransform size (for UI usage)
                float rectAspect = _RectSize.x / max(_RectSize.y, 0.0001);

                // Combined aspect: world scale * UI aspect
                float aspect = (scaleX / max(scaleY, 0.0001)) * rectAspect;

                // Square SDF space
                float2 sdfUV = float2(uv.x * aspect, uv.y);
                float2 bounds = float2(aspect, 1.0);

                float dOuter = sdRoundRect(sdfUV, bounds, _Radius);
                float dInner = sdRoundRect(sdfUV, bounds - float2(_Outline, _Outline), max(0, _Radius - _Outline));

                float aOuter = smoothstep(_Soft, -_Soft, dOuter);
                float aInner = smoothstep(_Soft, -_Soft, dInner);

                float outlineAlpha = saturate(aOuter - aInner);

                float fillMask = step(uv.x, lerp(-1, 1, _Fill));
                float fillAlpha = aInner * fillMask;

                float backgroundAlpha = aInner * (1 - fillMask);

                float fillA = fillAlpha * _Color.a;
                float bgA   = backgroundAlpha * _BackgroundColor.a;
                float outA  = outlineAlpha * _OutlineColor.a;

                float alpha = saturate(fillA + bgA + outA);

                float3 col =
                    _Color.rgb * fillA +
                    _BackgroundColor.rgb * bgA +
                    _OutlineColor.rgb * outA;

                if (alpha > 0) col /= alpha;

                // no cutoff - preserve full alpha

                col *= i.color.rgb;
                alpha *= i.color.a;
                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}