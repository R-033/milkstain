Shader "Milkdrop/DefaultWarpShader"
{
    Properties
    {
        _MainTex ("sampler_main", 2D) = "white" {}
        _MainTex2 ("sampler_fw_main", 2D) = "white" {}
        _MainTex3 ("sampler_fc_main", 2D) = "white" {}
        _MainTex4 ("sampler_pw_main", 2D) = "white" {}
        _MainTex5 ("sampler_pc_main", 2D) = "white" {}
        _MainTex6 ("sampler_blur1", 2D) = "white" {}
        _MainTex7 ("sampler_blur2", 2D) = "white" {}
        _MainTex8 ("sampler_blur3", 2D) = "white" {}
        _MainTex9 ("sampler_noise_lq", 2D) = "white" {}
        _MainTex10 ("sampler_noise_lq_lite", 2D) = "white" {}
        _MainTex11 ("sampler_noise_mq", 2D) = "white" {}
        _MainTex12 ("sampler_noise_hq", 2D) = "white" {}
        _MainTex13 ("sampler_pw_noise_lq", 2D) = "white" {}
        _MainTex14 ("sampler_noisevol_lq", 3D) = "white" {}
        _MainTex15 ("sampler_noisevol_hq", 3D) = "white" {}
        time ("time", Float) = 1
        decay ("decay", Float) = 1
        resolution ("resolution", Vector) = (1,1,1,1)
        aspect ("aspect", Vector) = (1,1,1,1)
        texsize ("texsize", Vector) = (1,1,1,1)
        texsize_noise_lq ("texsize_noise_lq", Vector) = (1,1,1,1)
        texsize_noise_mq ("texsize_noise_mq", Vector) = (1,1,1,1)
        texsize_noise_hq ("texsize_noise_hq", Vector) = (1,1,1,1)
        texsize_noise_lq_lite ("texsize_noise_lq_lite", Vector) = (1,1,1,1)
        texsize_noisevol_lq ("texsize_noisevol_lq", Vector) = (1,1,1,1)
        texsize_noisevol_hq ("texsize_noisevol_hq", Vector) = (1,1,1,1)
        bass ("bass", Float) = 1
        mid ("mid", Float) = 1
        treb ("treb", Float) = 1
        vol ("vol", Float) = 1
        bass_att ("bass_att", Float) = 1
        mid_att ("mid_att", Float) = 1
        treb_att ("treb_att", Float) = 1
        vol_att ("vol_att", Float) = 1
        frame ("frame", Float) = 1
        fps ("fps", Float) = 1
        _qa ("_qa", Vector) = (1,1,1,1)
        _qb ("_qb", Vector) = (1,1,1,1)
        _qc ("_qc", Vector) = (1,1,1,1)
        _qd ("_qd", Vector) = (1,1,1,1)
        _qe ("_qe", Vector) = (1,1,1,1)
        _qf ("_qf", Vector) = (1,1,1,1)
        _qg ("_qg", Vector) = (1,1,1,1)
        _qh ("_qh", Vector) = (1,1,1,1)
        slow_roam_cos ("slow_roam_cos", Vector) = (1,1,1,1)
        roam_cos ("roam_cos", Vector) = (1,1,1,1)
        slow_roam_sin ("slow_roam_sin", Vector) = (1,1,1,1)
        roam_sin ("roam_sin", Vector) = (1,1,1,1)
        blur1_min ("blur1_min", Float) = 1
        blur1_max ("blur1_max", Float) = 1
        blur2_min ("blur2_min", Float) = 1
        blur2_max ("blur2_max", Float) = 1
        blur3_min ("blur3_min", Float) = 1
        blur3_max ("blur3_max", Float) = 1
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv_orig = v.vertex.xy * float2(0.5, 0.5) + float2(0.5, 0.5);
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex; // sampler_main
            sampler2D _MainTex2; // sampler_fw_main
            sampler2D _MainTex3; // sampler_fc_main
            sampler2D _MainTex4; // sampler_pw_main
            sampler2D _MainTex5; // sampler_pc_main
            sampler2D _MainTex6; // sampler_blur1
            sampler2D _MainTex7; // sampler_blur2
            sampler2D _MainTex8; // sampler_blur3
            sampler2D _MainTex9; // sampler_noise_lq
            sampler2D _MainTex10; // sampler_noise_lq_lite
            sampler2D _MainTex11; // sampler_noise_mq
            sampler2D _MainTex12; // sampler_noise_hq
            sampler2D _MainTex13; // sampler_pw_noise_lq
            sampler3D _MainTex14; // sampler_noisevol_lq
            sampler3D _MainTex15; // sampler_noisevol_hq
            float time;
            float decay;
            float2 resolution;
            float4 aspect;
            float4 texsize;
            float4 texsize_noise_lq;
            float4 texsize_noise_mq;
            float4 texsize_noise_hq;
            float4 texsize_noise_lq_lite;
            float4 texsize_noisevol_lq;
            float4 texsize_noisevol_hq;
            float bass;
            float mid;
            float treb;
            float vol;
            float bass_att;
            float mid_att;
            float treb_att;
            float vol_att;
            float frame;
            float fps;
            float4 _qa;
            float4 _qb;
            float4 _qc;
            float4 _qd;
            float4 _qe;
            float4 _qf;
            float4 _qg;
            float4 _qh;

            #define q1 _qa.x
            #define q2 _qa.y
            #define q3 _qa.z
            #define q4 _qa.w
            #define q5 _qb.x
            #define q6 _qb.y
            #define q7 _qb.z
            #define q8 _qb.w
            #define q9 _qc.x
            #define q10 _qc.y
            #define q11 _qc.z
            #define q12 _qc.w
            #define q13 _qd.x
            #define q14 _qd.y
            #define q15 _qd.z
            #define q16 _qd.w
            #define q17 _qe.x
            #define q18 _qe.y
            #define q19 _qe.z
            #define q20 _qe.w
            #define q21 _qf.x
            #define q22 _qf.y
            #define q23 _qf.z
            #define q24 _qf.w
            #define q25 _qg.x
            #define q26 _qg.y
            #define q27 _qg.z
            #define q28 _qg.w
            #define q29 _qh.x
            #define q30 _qh.y
            #define q31 _qh.z
            #define q32 _qh.w

            float4 slow_roam_cos;
            float4 roam_cos;
            float4 slow_roam_sin;
            float4 roam_sin;
            float blur1_min;
            float blur1_max;
            float blur2_min;
            float blur2_max;
            float blur3_min;
            float blur3_max;
            float scale1;
            float scale2;
            float scale3;
            float bias1;
            float bias2;
            float bias3;
            float4 rand_frame;
            float4 rand_preset;

            float PI = 3.14159265359;

            // header text

            float4 frag (v2f i) : SV_Target
            {
                float3 ret;

                float2 uv = i.uv;
                float2 uv_orig = i.uv_orig;

                float rad = length(uv_orig - 0.5);
                float ang = atan2(uv_orig.x - 0.5, uv_orig.y - 0.5);

                // part that changes
                ret = tex2D(_MainTex, uv).rgb * decay;

                return float4(lerp(tex2D(_MainTex, uv_orig).rgb, ret * i.color.rgb, i.color.a), 1.0);
            }
            ENDCG
        }
    }
}
