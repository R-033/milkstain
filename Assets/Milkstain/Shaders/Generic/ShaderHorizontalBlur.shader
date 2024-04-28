Shader "Milkdrop/HorBlur"
{
    Properties
    {
        _MainTex ("sampler_main", 2D) = "white" {}
        texsize ("texsize", Vector) = (1,1,1,1)
        scale ("scale", Float) = 0
        bias ("bias", Float) = 0
        ws ("ws", Vector) = (1,1,1,1)
        ds ("ds", Vector) = (1,1,1,1)
        wdiv ("wdiv", Float) = 0
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
            float scale;
            float bias;
            float4 ws;
            float4 ds;
            float wdiv;

            float4 frag (v2f i) : SV_Target
            {
                float w1 = ws[0];
                float w2 = ws[1];
                float w3 = ws[2];
                float w4 = ws[3];
                float d1 = ds[0];
                float d2 = ds[1];
                float d3 = ds[2];
                float d4 = ds[3];
                
                float2 uv = i.uv;
                float2 uv2 = i.uv;

                float4 blur =
                    ( tex2D(_MainTex, uv2 + float2( d1 * texsize.z,0.0) )
                    + tex2D(_MainTex, uv2 + float2(-d1 * texsize.z,0.0) )) * w1 +
                    ( tex2D(_MainTex, uv2 + float2( d2 * texsize.z,0.0) )
                    + tex2D(_MainTex, uv2 + float2(-d2 * texsize.z,0.0) )) * w2 +
                    ( tex2D(_MainTex, uv2 + float2( d3 * texsize.z,0.0) )
                    + tex2D(_MainTex, uv2 + float2(-d3 * texsize.z,0.0) )) * w3 +
                    ( tex2D(_MainTex, uv2 + float2( d4 * texsize.z,0.0) )
                    + tex2D(_MainTex, uv2 + float2(-d4 * texsize.z,0.0) )) * w4;

                blur.xyz *= wdiv;
                blur.xyz = blur.xyz * scale + bias;

                float3 texHDR = DecodeHDR(blur, _MainTex_HDR);

                return float4(texHDR.xyz, 1.0);
            }
            ENDCG
        }
    }
}
