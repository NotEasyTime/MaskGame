Shader "UI/MetallicShimmer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)

        _ShimmerColor ("Shimmer Color", Color) = (1,1,1,1)
        _ShimmerWidth ("Shimmer Width", Range(0.01, 0.5)) = 0.15
        _ShimmerSpeed ("Shimmer Speed", Range(0.1, 5)) = 1
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 2)) = 1

        _CurveStrength ("Curve Strength", Range(-2, 2)) = 0.5
        _MaxGap ("Max Gap", Range(0.2, 5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;
            float4 _ShimmerColor;
            float _ShimmerWidth;
            float _ShimmerSpeed;
            float _ShimmerIntensity;
            float _CurveStrength;
            float _MaxGap;

            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv) * _Color * i.color;

                // Sweep duration is always 1.0
                float sweepDuration = 1.0;
                float cycleDuration = sweepDuration + _MaxGap;

                float t = _Time.y * _ShimmerSpeed;

                float cycleIndex = floor(t / cycleDuration);
                float cycleTime = frac(t / cycleDuration) * cycleDuration;

                // Random start time inside the gap window
                float startTime = hash(cycleIndex) * _MaxGap;

                // Active only during sweep window
                float active = step(startTime, cycleTime) *
                               step(cycleTime, startTime + sweepDuration);

                // Normalized sweep progress
                float sweepT = saturate((cycleTime - startTime) / sweepDuration);

                // Full off-screen travel
                float shimmerPos = lerp(
                    -_ShimmerWidth,
                    1.0 + _ShimmerWidth,
                    sweepT
                );

                // Curved shimmer coordinate
                float curvedUV =
                    i.uv.x +
                    i.uv.y +
                    _CurveStrength * (i.uv.y - 0.5) * (i.uv.y - 0.5);

                float shimmerMask =
                    smoothstep(shimmerPos - _ShimmerWidth, shimmerPos, curvedUV) *
                    (1 - smoothstep(shimmerPos, shimmerPos + _ShimmerWidth, curvedUV));

                baseCol.rgb +=
                    _ShimmerColor.rgb *
                    shimmerMask *
                    _ShimmerIntensity *
                    active *
                    baseCol.a;

                return baseCol;
            }
            ENDCG
        }
    }
}
