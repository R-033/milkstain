Shader "Milkdrop/WaveformShader"
{
    Properties
    {
        _MainTex ("sampler_main", 2D) = "white" {}
        waveColor ("waveColor", Vector) = (1,1,1,1)
        additivewave ("additivewave", Float) = 0
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD2;
            };

            float aspect_ratio;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = float2(v.vertex.x / aspect_ratio, v.vertex.y) * float2(0.5, 0.5) + float2(0.5, 0.5);
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex; // sampler_main
            float4 _MainTex_HDR;
            float additivewave;
            float4 waveColor;

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float4 color = i.color * waveColor;

                float4 tex = tex2D(_MainTex, uv);
                float3 texHDR = DecodeHDR(tex, _MainTex_HDR);

                if (additivewave != 0)
                {
                    return float4(texHDR.xyz + color.xyz * color.w, 1.0);
                }
                
                return float4(lerp(texHDR.xyz, color.xyz, color.w), 1.0);
            }
            ENDCG
        }
    }
}
