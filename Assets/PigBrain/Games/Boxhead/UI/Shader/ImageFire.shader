Shader "Boxhead/ImageFire"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Amount ("Amount", Range(0, 1)) = 0.25
        _Intensity ("Intensity", Float) = 1
        _Speed ("Speed", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _Amount;
            float _Intensity;
            float _Speed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise2d(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));

                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;

                // base uv
                float2 uv = i.uv;

                float2 grid = float2(20.0, 28.0);
                float2 cell = floor(uv * grid);
                float2 uvBlock = (cell + 0.5) / grid;

                // per-particle randomness
                float seed = hash21(cell);

                // particle properties
                float speed = lerp(0.5, 2.5, seed);
                float life = lerp(0.3, 0.8, hash21(cell + 1.3));

                // deterministic per-cell particle (no flicker)
                float spawnSeed = hash21(cell + 2.17);
                float spawn = step(spawnSeed, _Amount);

                // each cell = one particle rising over time
                float yPos = frac(_Time.y * _Speed * speed + seed);

                // particle exists only when it passes this cell
                float inCell = step(abs(uvBlock.y - yPos), 0.04);

                // fade based on lifetime (different per particle)
                float fade = saturate(1.0 - (yPos / life));

                float particle = spawn * inCell * fade;

                if (particle <= 0.001) return 0;

                // sample color slightly below to anchor to icon
                float2 srcUV = uvBlock + float2(0, -0.1);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, srcUV);

                float3 col = tex.rgb * i.color.rgb;

                return float4(col, particle * i.color.a);
            }
            ENDHLSL
        }
    }
}