Shader "Milkdrop/VertBlur"
{
    Properties
    {
        _MainTex ("sampler_main", 2D) = "white" {}
        texsize ("texsize", Vector) = (1,1,1,1)
        ed1 ("ed1", Float) = 0
        ed2 ("ed2", Float) = 0
        ed3 ("ed3", Float) = 0
        wds ("wds", Vector) = (1,1,1,1)
        wdiv ("wdiv", Vector) = (1,1,1,1)
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
            float4 _MainTex_HDR;

            float4 texsize;
            float ed1;
            float ed2;
            float ed3;
            float4 wds;
            float wdiv;

            float4 frag (v2f i) : SV_Target
            {
                float w1 = wds[0];
                float w2 = wds[1];
                float d1 = wds[2];
                float d2 = wds[3];
                
                float2 uv = i.uv;
                float2 uv2 = i.uv;

                float4 blur =
                    ( tex2D(_MainTex, uv2 + float2(0.0, d1 * texsize.w) )
                    + tex2D(_MainTex, uv2 + float2(0.0,-d1 * texsize.w) )) * w1 +
                    ( tex2D(_MainTex, uv2 + float2(0.0, d2 * texsize.w) )
                    + tex2D(_MainTex, uv2 + float2(0.0,-d2 * texsize.w) )) * w2;

                blur.xyz *= wdiv;

                float t = min(min(uv.x, uv.y), 1.0 - max(uv.x, uv.y));
                t = sqrt(t);
                t = ed1 + ed2 * clamp(t * ed3, 0.0, 1.0);
                blur.xyz *= t;

                float3 texHDR = DecodeHDR(blur, _MainTex_HDR);

                return float4(texHDR.xyz, 1.0);
            }
            ENDCG
        }
    }
}
