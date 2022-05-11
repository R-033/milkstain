Shader "Milkdrop/BorderShader"
{
    Properties
    {
        borderColor ("borderColor", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Cull Off

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 borderColor;

            float4 frag (v2f i) : SV_Target
            {
                return borderColor;
            }
            ENDCG
        }
    }
}
