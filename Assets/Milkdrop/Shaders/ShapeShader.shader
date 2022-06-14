Shader "Milkdrop/ShapeShader"
{
    Properties
    {
        _MainTex ("sampler_main", 2D) = "white" {}
        _MainTexPrev ("prev sampler", 2D) = "white" {}
        uTextured ("uTextured", Float) = 0
        additive ("additive", Float) = 0
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
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv_orig : TEXCOORD1;
                float4 color : TEXCOORD2;
            };

            float aspect_ratio;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.vertex.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                o.uv_orig = v.uv;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex; // sampler_main
            sampler2D _MainTexPrev;
            float uTextured;
            float additive;

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 uv_orig = i.uv_orig;

                float4 waveColor = i.color;

                if (uTextured != 0)
                {
                    waveColor.xyz *= tex2D(_MainTexPrev, uv_orig).xyz;
                }

                if (additive != 0)
                {
                    return float4(tex2D(_MainTex, uv).xyz + waveColor.xyz * waveColor.w, 1.0);
                }
                
                return float4(lerp(tex2D(_MainTex, uv).xyz, waveColor.xyz, waveColor.w), 1.0);
            }
            ENDCG
        }
    }
}
