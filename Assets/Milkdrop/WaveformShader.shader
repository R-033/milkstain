Shader "Milkdrop/WaveformShader"
{
    Properties
    {
        _MainTex ("sampler_main", 2D) = "white" {}
        waveColor ("waveColor", Vector) = (1,1,1,1)
        additivewave ("additivewave", Float) = 0
        wave_dots ("wave_dots", Float) = 0
        aspect_ratio ("aspect_ratio", Float) = 1
    }
    SubShader
    {
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv_orig : TEXCOORD1;
            };

            float aspect_ratio;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = float2(v.vertex.x / aspect_ratio, v.vertex.y) * float2(0.5, 0.5) + float2(0.5, 0.5);
                o.uv_orig = v.uv;
                return o;
            }

            sampler2D _MainTex; // sampler_main
            float additivewave;
            float4 waveColor;
            float wave_dots;

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 uv_orig = i.uv_orig;

                float4 colorW = waveColor.w;

                if (wave_dots != 0)
                {
                    colorW *= round((uv_orig.x * 100) % 1);
                }

                if (additivewave != 0)
                {
                    return float4(tex2D(_MainTex, uv).xyz + waveColor.xyz * colorW, 1.0);
                }
                
                return float4(lerp(tex2D(_MainTex, uv).xyz, waveColor.xyz, colorW), 1.0);
            }
            ENDCG
        }
    }
}
