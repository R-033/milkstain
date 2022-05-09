Shader "Milkdrop/DarkenCenter"
{
    Properties
    {
        _MainTex ("sampler_main", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

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
                float4 color : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.vertex.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                return float4(lerp(tex2D(_MainTex, uv).rgb, i.color.rgb, i.color.a), 1.0);
            }
            ENDCG
        }
    }
}
